using GFW;
using GFW.ManagerSystem;

namespace CodeX
{
    public class LuaCommand : ControllerCommand
    {

        public override void Execute(IMessage message)
        {
            Message msg = message as Message;
            switch (msg.Type)
            {
                case "StartModule":
                    string modName = message.Body as string;
                    GameSystem.Instance.GetManager<LuaManager>().StartModule(modName);
                    break;
                default:
                    break;
            }
        }
    }
}
