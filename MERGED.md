# MERGED.md — upstream back-ports into woroko's Go fork

Branch `go-merged`, branched from `origin/go` (woroko/QuestAppLauncher).
Upstream is `tverona1/QuestAppLauncher`.

## Baseline (verified, not assumed)

| Fact | Verified value |
|---|---|
| Fork point of `go` from upstream | `142fc16` = tag `v0.10.3` (Apr 2020) |
| Upstream commits missing from `go` | 21 (`git rev-list --count origin/go..upstream/master`) |
| woroko `master` vs upstream `master` | identical SHA `7a577a8` — ignored entirely |
| Fork Unity version | **2019.4.40f1** (upstream is 2019.3.12f1 — *not* downgraded) |

---

## Ported

### 1. `17501f5` — "Semi-transparent floor, fix appname override"
**Commit:** `Backport 17501f5: fix appname override blanking app names`

- **Taken:** `Assets/Scripts/AppProcessor.cs` (one hunk, line 283) — byte-identical to upstream.
  An `appnames.json` entry that overrides *only* a category left the name field empty, which
  blanked the app's display name. Now falls back to the existing `AppName`.
- **Skipped:** `Assets/Materials/GroundMat.mat` — woroko already modified this material for the
  Go build; upstream's semi-transparent-floor tweak would clobber it.

### 2. `ddea5d2` — "Support for hand tracking" (partial, hand-independent parts only)
**Commit:** `Backport ddea5d2 (partial): non-hand-tracking fixes only`

Taken — none of these are gated on `OVRPlugin.GetHandTrackingEnabled()` or `HandsManager`:

- `Assets/Scripts/DownloadStatusIndicator.cs` — renames the field and parameter `name` →
  `fileName`. This is a **real latent bug**, not cosmetics: the class derives from
  `MonoBehaviour`, so `this.name = name` was assigning to `MonoBehaviour.name` and **renaming
  the GameObject** instead of storing the download label. The status text then read
  "Downloading " with an empty name. File now matches upstream byte-for-byte.
- `Assets/Scripts/DownloadHandlerFileWithProgress.cs` — adds `[Obsolete]` to
  `ReceiveContentLength(int)` to suppress Unity's deprecation warning for the `int` overload
  (superseded by `ReceiveContentLengthHeader(ulong)`). File now matches upstream byte-for-byte.
- `Assets/Scripts/ScrollRectOverride.cs` — forwards `IScrollHandler.OnScroll` to
  `base.OnScroll(eventData)`; the empty override was swallowing scroll events. One line.

---

## Skipped, with reasons

### From `ddea5d2`

| Item | Why skipped |
|---|---|
| `HandsManager.cs` + `.meta`, hand materials/shader, `OVRPlatformToolSettings.asset` | Quest-only hand tracking; explicitly out of scope. |
| `ScrollRectOverride.Update()` dual-controller rework | Calls `OVRInputHelpers.GetConnectedControllers(HandFilter)` and the `GetSelectionRay(..., out Ray)` **bool** overload. **Neither exists in this fork** — its `OVRInputHelpers.cs` has *zero* `HandFilter` references and `GetSelectionRay` returns `Ray` directly. Would not compile without importing the hand-tracking helper rework. |
| `ScrollRectColliderMask.cs` (entire diff) | Same reason — the whole diff *is* that dual-controller rework. Nothing hand-independent to salvage. |
| `ScrollRectOverride` `OnBeginDrag` / `OnEndDrag` / `IDragHandler` / `parentRawRaycaster` | Explicitly gated on `OVRPlugin.GetHandTrackingEnabled()`, and calls `OVRRawRaycaster.OnBeginDrag/OnEndDrag` — members that only exist on upstream's hand-tracking raycaster (`IBeginDragHandler, IEndDragHandler`), which this fork does not implement. |
| `Assets/Scenes/QuestAppLauncher.unity`, `ProjectSettings/*`, `QualitySettings`, `XRSettings` | Out of scope; scene is rebuilt in the fork. |

### Excluded by instruction (not attempted)

`e13af7d` (Oculus Integration v16), `4272495` (XR Plugin Management + hand materials),
`d84c0cb` (Unity 2019.3.12f1 — would **downgrade** the fork), and the
`030eaf0` / `d8ce11e` / `841c893` / `d4e970a` / `fe735f3` skeleton-renderer churn.
All are Quest-oriented, touch the vendored Oculus SDK, and risk the working 2019.4.40f1 baseline.
Nothing in the ported set turned out to need any of them.

---

## ⚠️ NOT ported: `2b8154d` "Update app name, fix go controller" — premise does not hold

This was listed as REQUIRED, but **it is not applicable to this fork** and was deliberately left
out pending a decision. Details:

- The upstream fix widens a gate inside `OVRRawRaycaster.ProcessButtonPresses()`.
  **That method does not exist in this fork.** woroko replaced `OVRRawRaycaster.cs` wholesale
  with a much older single-controller design (fork file differs from upstream v0.10.3 by ~340
  lines: no dual-hand hit processing, no drag handling, no hand pinch).
- Upstream's gate is Touch-only and therefore *excludes* the Go remote:
  `if (isLeft && (activeController & LTouch) != LTouch || ...) return;`
  The fork's equivalent gate is simply **`if (activeController != OVRInput.Controller.None)`**,
  which already admits `LTrackedRemote` / `RTrackedRemote`. The fork never had this bug.
- The reason `grep -c TrackedRemote OVRRawRaycaster.cs` returns `0` is **not** missing Go
  support — it is that the fork's Go support lives one layer down in
  `OVRInputHelpers.GetControllerForButton()`, which has **6** `TrackedRemote` references and
  explicitly selects `RTrackedRemote` / `LTrackedRemote`. `GetSelectionRay()` then builds the
  pointer ray from that remote's pose, and there is a gaze-pointer fallback when no controller
  is connected.

**Consequence:** the requested check `grep -c TrackedRemote ... == 2` can only be satisfied by
first *introducing* a Touch-only restriction that this fork does not have, then relaxing it —
i.e. adding the bug in order to fix it. Taking upstream's file wholesale is also not an option:
it references `OVRHand`, `HandsManager`, `OVRInputHelpers.HandFilter` and the `out Ray` overload,
none of which exist here, so it would not compile.

No change was made. See "Open question" at the bottom.

---

## Build verification

**No Unity install, and no C# compiler of any kind (`dotnet`/`mono`/`mcs`/`csc`), is available in
this environment** — so the "compiles under 2019.4.40f1 targeting Android" step could not be
executed. Per instruction, a static review of the touched files was done instead:

| Check | Result |
|---|---|
| `String.IsNullOrWhiteSpace` resolvable | ✅ `using System;` at `AppProcessor.cs:1`; `apiCompatibilityLevel: 6` (.NET Standard 2.0) provides it |
| `appName` / `ProcessedApp.AppName` types | ✅ both `string` (`JsonAppNamesEntry.Name`, `ProcessedApp.AppName`) |
| `[Obsolete]` resolvable | ✅ `using System;` at `DownloadHandlerFileWithProgress.cs:1` |
| `base.OnScroll(PointerEventData)` exists | ✅ `ScrollRectOverride : ScrollRect`; `ScrollRect.OnScroll` is `public virtual` |
| `name`→`fileName` rename vs interface | ✅ `IDownloaderProgress.OnDownloadStart(string name)` — C# does not require matching parameter names; the sole caller (`AssetsDownloader.cs:393`) passes **positionally**, so no named-argument break |
| Stale `this.name` references | ✅ none remain |
| No new symbols introduced | ✅ every ported change uses only pre-existing APIs |

Two of the three files in back-port #2 are now **byte-identical to upstream's post-commit
version**, which is the strongest available evidence short of a compile: that exact text
compiled upstream.

---

## Please test on-device

1. **Pointer click / select with the 3DoF remote** — regression check only; `OVRRawRaycaster.cs`
   was deliberately **not** touched. Confirm trigger and touchpad select still work. If clicking
   is in fact broken on your device, that is a *different* bug from `2b8154d` and I should look
   at `OVRInputHelpers.GetControllerForButton()` rather than the raycaster.
2. **Scrolling the app grid** (`base.OnScroll` passthrough) — the highest-risk change here.
   `ScrollRect.OnScroll` applies its own scroll delta, so this could now **double-apply** with
   the fork's custom `OnMouseDrag`/`IMoveHandler` scrolling, or invert direction. Watch for
   over-fast, doubled, or reversed scrolling. Easy to revert (one line) if it feels wrong.
3. **Download status text** — trigger an app-list/banner download. Text should read
   "Downloading &lt;file&gt; [42%]" with a real filename. Also confirm the indicator GameObject
   is no longer being silently renamed at runtime.
4. **appnames.json category-only override** — add an entry that sets *only* a category and
   leaves the name empty; the app must keep its real name instead of going blank.
5. **Go-specific paths (regression sweep — all untouched, verify nothing shifted):**
   launching as HOME, 2D apps via Oculus TV (`AppProcessor.LaunchApp(packageId, is2DApp)`),
   vrshell enable/disable, the volume-down listener, and custom backgrounds.

## Open question

Back-port #1 (`2b8154d`) is unapplied. Options: **(a)** leave as-is — the fork's architecture
already covers the Go remote; **(b)** if remote clicks genuinely misbehave on-device, diagnose
against the fork's actual input path instead of force-fitting upstream's hunk.
