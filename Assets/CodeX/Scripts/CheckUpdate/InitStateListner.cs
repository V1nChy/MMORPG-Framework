using System;
using UnityEngine;
using GFW;

namespace CodeX
{
    /// <summary>
    /// 加载本地配置
    /// </summary>
	public class InitStateListner : IGameStateListner
	{
		public override void OnStateEnter(GameState pCurState)
		{
            Debug.Log("InitStateListner@OnStateEnter() 加载本地配置");
            if (AppConst.UpdateMode)
			{
                GameConfig.Instance.LoadConfig();
            }
			else
			{
                ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "UPDATE_PROGRESS", 100);
                CheckUpdateManager.Instance.ToLoadBgState();
			}
		}

		public override void OnStateUpdate(GameState pCurState, float elapseTime)
		{
            if (GameConfig.Instance.Finish())
			{
                ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "SHOW_WINDOW_TEXT");
                CheckUpdateManager.Instance.ToUpdateConfigState();
			}
		}

        public override void OnStateQuit(GameState pCurState)
        {

        }

		public override void Free()
		{

		}
	}
}
