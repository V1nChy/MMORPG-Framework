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
            if (AppConst.UpdateMode)
			{
                GameConfig.Instance.LoadConfig();
            }
			else
			{
                BusinessManager.Instance.SendMessage(ModuleDef.LaunchModule, "Command", "UPDATE_PROGRESS", 100);
                CheckUpdateService.Instance.ToLoadBgState();
			}
            BusinessManager.Instance.SendMessage(ModuleDef.LaunchModule, "Command", "UPDATE_PROGRESS", 100);
        }

		public override void OnStateUpdate(GameState pCurState, float elapseTime)
		{
            if (GameConfig.Instance.Finish())
			{
                //ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "SHOW_WINDOW_TEXT");
                //CheckUpdateService.Instance.ToUpdateConfigState();
			}
		}
	}
}
