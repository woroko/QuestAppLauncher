using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootRunner : MonoBehaviour
{

    public static RootRunner instance;

    private bool disableVrShellOnNextResume = true;

    public const string shellRestartPrefix = "pm enable com.oculus.vrshell; sleep 1; am start -n \"com.oculus.vrshell.home/.PanelActivity\"; sleep 1; am start -n \"com.oculus.vrshell.home/.PanelActivity\"; sleep 1; ";

    public const string startAppLauncherPostfix = "; sleep 1; am start -n aaa.QuestAppLauncher.App/.AppInfo;";

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
            instance = this;
    }

    public void RunRootCommand(string command, bool runInBackground = false)
    {
        using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            if (runInBackground)
            {
                command = "(" + command + ") &";
            }
            currentActivity.Call("executeRootCommand", command);
        }
    }

    public void RunVolumeDownListener()
    {
        RunRootCommand("while true; do sleep 0.1; line=$(getevent -c 1); if echo \"$line\" | grep -q \"/dev/input/event0: 0001 0072 00000001\"; then am start -n aaa.QuestAppLauncher.App/.AppInfo; break; fi; done");
    }

    public void RunRootCommandAfterWaiting(string command, float waitForSeconds)
    {
        StartCoroutine(RunRootCommandAfterWaitingCoroutine(command, waitForSeconds));
    }

    IEnumerator RunRootCommandAfterWaitingCoroutine(string command, float waitForSeconds)
    {
        yield return new WaitForSeconds(waitForSeconds);
        RunRootCommand(command);
    }

    void OnApplicationPause(bool pause)
    {
        // disable vrshell when resuming
        if (pause == false)
        {
            if (disableVrShellOnNextResume)
                RootRunner.instance.RunRootCommand("pm disable com.oculus.vrshell");
            else
                disableVrShellOnNextResume = true;
        }
    }

    public void PairControllers()
    {
        RunRootCommand("am startservice -a companion.CONTROLLER_SCAN_AND_PAIR -n com.oculus.companion.server/.CompanionService");
    }

    public void StartSystemSettings()
    {
        //disableVrShellOnNextResume = false;
        RunRootCommand(shellRestartPrefix + "am start -n \"com.oculus.systemactivities/com.oculus.systemactivities.PlatformActivity\"", true);
        RunVolumeDownListener();
    }

    public void StartBrowser()
    {
        //disableVrShellOnNextResume = false;
        RunRootCommand(shellRestartPrefix + "am start -n \"com.oculus.vrshell/.MainActivity\" -d apk://com.oculus.browser", true);
        RunVolumeDownListener();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
