using System;
using UnityEngine;
using UnityEngine.UI;
using GFW;

namespace CodeX
{
    public class UILaunchView : UIWidget
    {
        LaunchModule mod;
        private void Awake()
        {
            
        }

        protected override void OnOpen(object arg = null)
        {
            mod = arg as LaunchModule;
            mod.Event(LaunchModule.EVENT_CHANGE_VIEW).AddListener(ChangeView);
        }

        private void ChangeView(object a)
        {
            IMessage msg = a as IMessage;
            LogMgr.Log("msg:{0},{1}", msg.Name, msg.Body.ToString());
        }
    }
}
