using System;

namespace GFW.ManagerSystem
{
    public interface IController
    {
        void RegisterCommand(Type commandType);

        void ExecuteCommand(IMessage message);

        void RemoveCommand(string commandName);

        bool HasCommand(string commandName);
    }
}
