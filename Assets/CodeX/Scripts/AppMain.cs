using UnityEngine;
using GFW;
using CodeX;

public class AppMain : MonoBehaviour {

    public bool DebugMode = true;

    // Use this for initialization
    void Start () {
        AppConst.DebugMode = DebugMode;
        if (AppConst.DebugMode)
            AppConst.LuaBundleMode = false;

        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            LogManager.EnableLog = true;
            gameObject.AddComponent<LogViewer>();
        }

        ModuleStarter.Instance.StartUp();
    }

    void OnApplicationQuit()
    {
        ModuleStarter.Instance.Dispose();
    }
}
