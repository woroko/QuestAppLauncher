using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootRunner : MonoBehaviour
{

    public static RootRunner instance;
    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
            instance = this;
    }

    public void RunRootCommand(string command)
    {
        using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            currentActivity.Call("executeRootCommand", command);
        }
    }

    void OnApplicationPause(bool pause)
    {
        // disable vrshell when resuming
        if (pause == false)
        {
            RootRunner.instance.RunRootCommand("pm disable com.oculus.vrshell");
        }
    }

    public void PairControllers()
    {
        RunRootCommand("am startservice -a companion.CONTROLLER_SCAN_AND_PAIR -n com.oculus.companion.server/.CompanionService");
    }

    public void StartSystemSettings()
    {
        RunRootCommand("am start -n \"com.oculus.systemactivities/com.oculus.systemactivities.PlatformActivity\"");
    }

    public void StartBrowser()
    {
        RunRootCommand("am start -n \"com.oculus.vrshell/.MainActivity\" -d apk://com.oculus.browser");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
