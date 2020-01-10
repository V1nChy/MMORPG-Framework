using System;
using System.IO;
using UnityEngine;
using GFW;

namespace CodeX
{
	public class UpdateVersionFileStateListner : IGameStateListner
	{
		public override void OnStateEnter(GameState pCurState)
		{
			string res_update_cdn = GameConfig.Instance["ResUpdateCDN"];
			string ingore_cdn_flag = GameConfig.Instance["IngoreCDNFlag"];
            if (res_update_cdn.Equals("1") && !ingore_cdn_flag.Equals("1"))
			{
                BusinessManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand","UPDATE_CDN_MSG", GameConfig.Instance["UpdateCDNText"]);
			}
			else
			{
				Debug.Log("UpdateVersionFileStateListner@OnStateEnter()");
                BusinessManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "UpdateMessage", GameConfig.Instance["DetectionVersionFile"]);
				string mdata_path = AppUtil.DataPath + "mdata/";
                if (!Directory.Exists(mdata_path))
				{
					Directory.CreateDirectory(mdata_path);
				}
				string rdata_path = AppUtil.DataPath + "rdata/";
                if (!Directory.Exists(rdata_path))
				{
					Directory.CreateDirectory(rdata_path);
				}
				string ldata_path = AppUtil.DataPath + "ldata/";
                if (!Directory.Exists(ldata_path))
				{
					Directory.CreateDirectory(ldata_path);
				}
                ResUpdateManager.Instance.Init();
				ResUpdateManager.Instance.DownLoadVersionFileAndCheck();
			}
		}

        public override void OnStateUpdate(GameState pCurState, float elapseTime)
        {
            if (ResUpdateManager.Instance.VersionFileLoad())
            {
                CheckUpdateService.Instance.ToUpdateResState();
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
