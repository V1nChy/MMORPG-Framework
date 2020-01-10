using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GFW;
using UIManager = GFW.UIManager;

namespace CodeX
{
    public class LaunchModule : BusinessModule
    {
        public const string EVENT_CHANGE_VIEW = "EVENT_CHANGE_VIEW";

        AsyncOperation m_Async;

        protected override void Start(object arg)
        {
            UIViewHelper.ShowProgressView(null, OnFinish);
            UIManager.Instance.Open<UILaunchView>(this);

            CheckUpdateService.Instance.StartUp();
        }

        public void OnFinish()
        {
            UIManager.Instance.CloseAll();

            //GameSystem.Instance.ExecuteCommandOnce(GameCommandDef.GameStartUpCommand);
            //GameSystem.Instance.GetManager<LuaManager>().InitStart(); 
        }

        protected override void OnMessage(IMessage msg)
        {
            Event(EVENT_CHANGE_VIEW).Invoke(msg);
        }
    }
}
