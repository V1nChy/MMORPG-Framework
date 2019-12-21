using System;

namespace GFW.ManagerSystem
{
    public interface ICommand
    {
        void Execute(IMessage message);
    }
}

