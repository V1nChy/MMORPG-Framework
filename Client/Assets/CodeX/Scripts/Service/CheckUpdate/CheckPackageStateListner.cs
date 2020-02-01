using System;
using System.IO;
using UnityEngine;
using GFW;

namespace CodeX
{
	public class CheckPackageStateListner : IGameStateListner
	{
        private bool m_finish_state = false;
		public override void OnStateEnter(GameState pCurState)
		{
            Debug.Log("CheckPackageStateListner@OnStateEnter()");
            if (AppConst.UpdateMode)
            {
                //AppFacade.Instance.GetManager<ResourceManager>(ManagerName.Resource).ReadDownloadHistory();
            }
            m_finish_state = true;
		}

		public override void OnStateQuit(GameState pCurState)
		{
		}

        public override void OnStateUpdate(GameState pCurState, float elapseTime)
        {
            if (this.m_finish_state)
            {
                bool need_extract = GameConfig.Instance.IsPackChange();
                if (need_extract)
                {
                    CheckUpdateService.Instance.ToExtractResState();
                }
                else
                {
                    if (AppConst.UpdateMode)
                    {
                        CheckUpdateService.Instance.ToUpdateVersionFileState();
                    }
                    else
                    {
                        CheckUpdateService.Instance.ToPreResState();
                    }
                }
            }
        }

        public override void Free()
		{
		}
	}
}
