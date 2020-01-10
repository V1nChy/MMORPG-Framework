using CodeX;
using GFW;

public class ModuleStarter : Singleton<ModuleStarter>
{
    /// <summary>
    /// 启动框架
    /// </summary>
    public void StartUp()
    {
        InitServices();
        InitBusiness();

        BusinessManager.Instance.StartModule(ModuleDef.LaunchModule);
    }

    private void InitServices()
    {
        BusinessManager.Instance.Init("CodeX");
        GameSystem.Instance.Init("CodeX");
        UIManager.Instance.Init("UI/Prefab/");
    }
    private void InitBusiness()
    {
        BusinessManager.Instance.CreateModule(ModuleDef.LaunchModule);
        BusinessManager.Instance.CreateModule(ModuleDef.LuaGameModule);
    }

    public void ReleaseAll()
    {
        BusinessManager.Instance.ReleaseAll();
        GameSystem.Instance.ReleaseAll();
    }

    public void Dispose()
    {
        
    }
}

