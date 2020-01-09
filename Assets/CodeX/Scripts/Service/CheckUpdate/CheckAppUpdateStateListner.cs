using System;
using UnityEngine;
using GFW;

namespace CodeX
{
	public class CheckAppUpdateStateListner : IGameStateListner
	{
        private bool m_finish_state = false;

		public override void OnStateEnter(GameState pCurState)
		{
            Debug.Log("CheckAppUpdateStateListner@OnStateEnter()");
            if (Application.platform == RuntimePlatform.Android && GameConfig.Instance.IsAppChange())
			{
                ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand","UPDATE_APP_MESSAGE", GameConfig.Instance["UpdateApp"]);
			}
			else
			{
				this.m_finish_state = true;
			}
		}

		public override void OnStateQuit(GameState pCurState)
		{
		}

		public override void OnStateUpdate(GameState pCurState, float elapseTime)
		{
            if (this.m_finish_state)
			{
				this.m_finish_state = false;
				CheckUpdateService.Instance.ToLoadBgState();
			}
		}

		public override void Free()
		{
		}
	}
}
