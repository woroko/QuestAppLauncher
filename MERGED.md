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

Built successfully with **Unity 2019.4.40f1** (revision `ffc62b691db5`) targeting Android.

| Check | Result |
|---|---|
| Project import | clean — `Refresh completed`, **0 `error CS`**, no exceptions |
| Gradle | passed `compileReleaseJavaWithJavac`, dex builder and dex mergers |
| Output APK | 40.4 MB, valid, `armeabi-v7a` only (correct for the 32-bit Go) |
| Package id | `aaa.QuestAppLauncher.App` (unchanged) |
| Manifest | `HOME` / `DEFAULT` / `MONKEY` categories and `largeHeap` preserved |
| Back-ports in binary | `Assembly-CSharp.dll` contains `fileName`, `IsNullOrWhiteSpace`, `OnDownloadStart`, `ScrollRectOverride` |

`IsNullOrWhiteSpace` appears only because of the `17501f5` back-port, so its presence in the
shipped assembly confirms the change survived compilation.

Player settings were **not** modified — the project was already correct for the Go
(`AndroidTargetArchitectures: 1` = ARMv7 only, Mono scripting backend, `AndroidMinSdkVersion: 21`).

Two environment notes for anyone reproducing this on a modern Linux distro (neither is a project
issue): Unity 2019.4 needs `libgconf-2.so.4` and OpenSSL 1.1, and its bundled Java 8 cannot read a
PKCS12 `debug.keystore` produced by current Android tooling — regenerating that keystore as JKS
resolves the Gradle `packageRelease` signing failure.

## Suggested testing

1. **Grid scrolling** — highest-risk change here. `ScrollRect.OnScroll` applies its own scroll
   delta, so the added `base.OnScroll(eventData)` could double-apply against the fork's custom
   `OnMouseDrag` / `IMoveHandler` scrolling, or invert direction. Trivial to revert (one line).
2. **Download status text** — should read `Downloading <file> [42%]` with a real filename, and the
   indicator GameObject should no longer be silently renamed at runtime.
3. **appnames.json category-only override** — an entry setting only a category must leave the app
   name intact rather than blanking it.
4. **Go remote select/click** — regression check only; `OVRRawRaycaster.cs` was deliberately not
   touched (see below).
5. **Go-specific paths** (all untouched, worth a regression sweep): launching as HOME, 2D apps via
   Oculus TV, vrshell enable/disable, the volume-down listener, custom backgrounds.

## Open question for review

Back-port #1 (`2b8154d`) is deliberately unapplied — see the section above for why it does not
apply to this fork's `OVRRawRaycaster.cs`. If Go remote clicks do misbehave in practice, the place
to look is `OVRInputHelpers.GetControllerForButton()`, not upstream's hunk, which targets a method
this fork does not contain.
