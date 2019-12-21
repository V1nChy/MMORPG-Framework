using GFW;
using GFW.ManagerSystem;

namespace CodeX
{
    public class GameStartUpCommand : ControllerCommand
    {
        public override void Execute(IMessage message)
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

            GameSystem.Instance.StartUp();
        }
    }
}
