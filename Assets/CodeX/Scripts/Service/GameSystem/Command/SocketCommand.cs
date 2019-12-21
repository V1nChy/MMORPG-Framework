using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LuaFramework;
using GFW.ManagerSystem;

namespace CodeX
{
    public class SocketCommand : ControllerCommand
    {

        public override void Execute(IMessage message)
        {
            object data = message.Body;
            if (data == null) return;
            KeyValuePair<int, ByteBuffer> buffer = (KeyValuePair<int, ByteBuffer>)data;
            switch (buffer.Key)
            {
                default: AppUtil.CallMethod("Network", "OnSocket", buffer.Key, buffer.Value); break;
            }
        }
    }
}
