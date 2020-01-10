using System;

namespace GFW
{
    public interface ICommand
    {
        void Execute(IMessage message);
    }
}