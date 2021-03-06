﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GFW;

namespace CodeX
{
    public class SocketCommand : ICommand
    {
        public void Execute(IMessage message)
        {
            object data = message.Body;
            if (data == null) return;
            KeyValuePair<int, ByteBuffer> buffer = (KeyValuePair<int, ByteBuffer>)data;
            switch (buffer.Key)
            {
                default: Util.CallMethod("Network", "OnSocket", buffer.Key, buffer.Value); break;
            }
        }
    }
}
