using GFW;

namespace CodeX
{
    public class GameStartUpCommand : ICommand
    {
        public void Execute(IMessage message)
        {
            if (!AppUtil.CheckEnvironment()) return;

            //-----------------关联命令-----------------------
            GameSystem.Instance.RegisterCommand(GameCommandDef.SocketCommand);

            //-----------------初始化管理器-----------------------
            GameSystem.Instance.CreateManager(ManagerName.LuaManager);
            GameSystem.Instance.CreateManager(ManagerName.GameManager);
            //AppFacade.Instance.AddManager<SoundManager>(ManagerName.Sound);
            //AppFacade.Instance.AddManager<TimerManager>(ManagerName.Timer);
            //AppFacade.Instance.AddManager<NetworkManager>(ManagerName.Network);
            GameSystem.Instance.CreateManager(ManagerName.ResourceManager);
            //AppFacade.Instance.AddManager<ThreadManager>(ManagerName.Thread);
            //AppFacade.Instance.AddManager<ObjectPoolManager>(ManagerName.ObjectPool);
            GameSystem.Instance.CreateManager(ManagerName.UIAgentManager);

            GameSystemMono.StartUp();//开始生命周期
        }
    }
}
