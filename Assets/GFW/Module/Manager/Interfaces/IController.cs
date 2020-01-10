using System;

namespace GFW
{
    public interface IController
    {
        void RegisterCommand(Type commandType);

        void ExecuteCommand(IMessage message);

        void RemoveCommand(string commandName);

        bool HasCommand(string commandName);
    }
}
