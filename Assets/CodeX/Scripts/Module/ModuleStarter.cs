﻿using CodeX;
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

        ModuleManager.Instance.StartModule(ModuleDef.LaunchModule);
    }

    private void InitServices()
    {
        ModuleManager.Instance.Init("CodeX");
        UIManager.Instance.Init("UI/Prefab/");
        GameSystem.Instance.Init("CodeX");
    }
    private void InitBusiness()
    {
        ModuleManager.Instance.CreateModule(ModuleDef.LaunchModule);
        ModuleManager.Instance.CreateModule(ModuleDef.LuaGameModule);
    }

    public void Dispose()
    {
        ModuleManager.Instance.ReleaseAll();
    }
}
