using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GFW;
using UIManager = GFW.UIManager;

namespace CodeX
{
    public class LaunchModule : BusinessModule
    {
        protected override void Start(object arg)
        {
            UIViewHelper.ShowProgressView(null, OnFinish);
            UIManager.Instance.Open<UILaunchView>();

            CheckUpdateManager.Instance.Start();
        }

        public void OnFinish()
        {
            UIManager.Instance.CloseAll();

            GameSystem.Instance.ExecuteCommandOnce(GameCommandDef.GameStartUpCommand);

            GameSystem.Instance.GetManager<LuaManager>().InitStart(); 
        }
    }
}
