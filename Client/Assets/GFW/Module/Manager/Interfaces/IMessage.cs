using System;

namespace GFW
{
    public interface IMessage
    {
        string Name { get; }

        object Body { get; set; }

        string Type { get; set; }

        string ToString();
    }
}

