using UnityEngine;
using GFW;
using CodeX;

public class AppMain : MonoBehaviour {

    public bool DebugMode = true;

    // Use this for initialization
    void Start () {
        AppConst.DebugMode = DebugMode;
        if (AppConst.DebugMode)
        {
            AppConst.LuaBundleMode = false;
            AppConst.UpdateMode = false;
        }
        else
        {
            AppConst.LuaBundleMode = true;
            AppConst.UpdateMode = true;
        }

        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            LogMgr.EnableLog = true;
            gameObject.AddComponent<LogViewer>();
        }

        ModuleStarter.Instance.StartUp();
    }

    private void Update()
    {
        
    }

    private void OnDestroy()
    {
        ModuleStarter.Instance.ReleaseAll();
    }

    private void OnApplicationQuit()
    {
        ModuleStarter.Instance.Dispose();
    }
}
