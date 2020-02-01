using System;
using UnityEngine;
using GFW;

namespace CodeX
{
    public class UpdateConfigStateListner : IGameStateListner
    {
        private bool m_finish_state = false;

        public override void OnStateEnter(GameState pCurState)
        {
            string backup_url = GameConfig.Instance["ResUrl"];
            if (CheckUpdateService.Instance.backupUrlState == BackUpUrlState.RUN)
            {
                backup_url = GameConfig.Instance["BackUpUrl"];
            }
            string remote_cfg_url = backup_url + GameConfig.Instance["ConfigUrl"] + "?v=" + Util.GetTimeStamp();
            MemoryQuest memory_quest = new MemoryQuest();
            memory_quest.RelativePath = remote_cfg_url;
            string server_config = GameConfig.Instance["GetServerConfig"];
            BusinessManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "UpdateMessage");
            MemoryLoadCallbackFunc delay_func = delegate (bool is_suc, byte[] buffer)
            {
                bool load_state = false;
                if (is_suc)
                {
                    load_state = true;
                    if (!GameConfig.Instance.LoadRemoteConfig(buffer))
                    {
                        load_state = false;
                    }
                    else
                    {
                        //使用新的Cnd地址
                        string new_cdn_url = GameConfig.Instance.GetValue("NewCdnUrl");
                        if (new_cdn_url != null && new_cdn_url != "")
                        {
                            CheckUpdateService.Instance.backupUrlState = BackUpUrlState.NEW;
                            CheckUpdateService.Instance.ToUpdateConfigState();
                            return;
                        }
                        if (CheckUpdateService.Instance.backupUrlState == BackUpUrlState.RUN)
                        {
                            CheckUpdateService.Instance.backupUrlState = BackUpUrlState.None;
                            string url = GameConfig.Instance.GetValue("BackUpUrl");
                            GameConfig.Instance.SetValue("ResUrl", url);
                        }
                        this.m_finish_state = true;
                    }
                }
                if (!load_state)
                {
                    this.RetryLoadConfig();
                }
            };
            ResRequest.Instance.RequestMemoryAsync(memory_quest, delay_func);
        }

        private void RetryLoadConfig()
        {
            LogMgr.Log("UpdateConfigStateListner@RetryLoadConfig() 重新请求远程配置");
            if (CheckUpdateService.Instance.ConfigTryLoadCount() < 2u)
            {
                CheckUpdateService.Instance.TryLoadConfig();
                CheckUpdateService.Instance.ToUpdateConfigState();
            }
            else
            {
                if (GameConfig.Instance.GetValue("BackUpUrl") != "" && CheckUpdateService.Instance.backupUrlState == BackUpUrlState.None)
                {
                    CheckUpdateService.Instance.backupUrlState = BackUpUrlState.RUN;
                    CheckUpdateService.Instance.CleanTryLoadConfig();
                    CheckUpdateService.Instance.ToUpdateConfigState();
                }
                else
                {
                    CheckUpdateService.Instance.CleanTryLoadConfig();
                    CheckUpdateService.Instance.backupUrlState = BackUpUrlState.None;
                    BusinessManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "TRY_LOAD_CONFIG", GameConfig.Instance["NetWorkError"]);
                }
            }
        }

        public override void OnStateQuit(GameState pCurState)
        {
        }

        public override void OnStateUpdate(GameState pCurState, float elapseTime)
        {
            if (this.m_finish_state)
            {
                this.ReadRemoteConfig();
                CheckUpdateService.Instance.ToCheckUpdateAppState();
            }
        }

        private void CheckAlphaPlat()
        {
            string alpha_cdn_url = GameConfig.Instance["AlphaCdnUrl"];
            string alpha_plat_version = GameConfig.Instance["AppAlphaPlatVersion"];
            if (alpha_cdn_url != "" && alpha_plat_version != "")
            {
                string new_version = GameConfig.Instance["GameConfigKey"];
                if (!new_version.Equals("") && int.Parse(new_version) < int.Parse(alpha_plat_version))
                {
                    //AppConst.IOSAlphaState = true;
                    GameConfig.Instance.SetValue("ResUrl", alpha_cdn_url);
                }
            }
        }

        private void ReadRemoteConfig()
        {
            //if (Application.platform == RuntimePlatform.IPhonePlayer)
            //{
            //    this.CheckAlphaPlat();
            //}

            //string show_loading_bar = GameConfig.Instance.GetValue(GameConfigKey.ShowLoadingBar);
            //AppConst.IsIOSShowFromal = (!AppConst.IOSAlphaState || show_loading_bar.Equals("1"));

            //string updateState = GameConfig.Instance.GetValue("IgnoreUpdateState");
            //if (AppConst.IOSAlphaState && updateState.Equals("1"))
            //{
            //    AppConst.IgnoreUpdateState = true;
            //    Util.Log("IgnoreUpdateState Start");
            //}

            //string ignoreGameUpdateState = GameConfig.Instance.GetValue("IgnoreGameUpdateState");
            //if (AppConst.IOSAlphaState && ignoreGameUpdateState.Equals("1"))
            //{
            //    AppConst.IgnoreGameUpdateState = true;
            //}

            //string customAppResDir = GameConfig.Instance.GetValue("CustomAppResDir");
            //if (AppConst.IOSAlphaState && !customAppResDir.Equals(""))
            //{
            //    AppConst.CustomAppResDir = customAppResDir;
            //}

            //AppFacade.Instance.SendMessageCommand("CONFIRM_IOS_ALPHA", AppConst.IsIOSShowFromal ? "0" : "1");

            //AppConst.CdnUrl = GameConfig.Instance.GetValue("ResUrl");
            //Util.Log("AppConst.CdnUrl:" + AppConst.CdnUrl);

            //string show_version = GameConfig.Instance.GetValue("IgnoreShowVersion");
            //if (!show_version.Equals("1"))
            //{
            //    AppConst.IgnoreShowVersion = false;
            //    Util.Log("IgnoreShowVersion false");
            //}
            //string engine_version = GameConfig.Instance.GetValue("ShowEngineVersion");
            //if (!engine_version.Equals(""))
            //{
            //    AppConst.ShowEngineVersion = int.Parse(engine_version);
            //}
            //string openDownloadLog = GameConfig.Instance.GetValue("OpenDownloadLog");
            //if (openDownloadLog.Equals("1"))
            //{
            //    AppConst.OpenDownloadLog = true;
            //}

            //string openUpdateFinishNoCheckMode = GameConfig.Instance.GetValue("OpenUpdateFinishNoCheckMode");
            //if (openUpdateFinishNoCheckMode.Equals("0"))
            //{
            //    AppConst.OpenUpdateFinishNoCheckMode = false;
            //}

            //string updateFileTimeout = GameConfig.Instance.GetValue("UpdateFileTimeout");
            //if (updateFileTimeout != null && !updateFileTimeout.Equals(""))
            //{
            //    AppConst.UpdateFileTimeout = int.Parse(updateFileTimeout);
            //}

            //string useUpdateNewMode = GameConfig.Instance.GetValue("UseUpdateNewMode");
            //if (useUpdateNewMode != null && useUpdateNewMode.Equals("0"))
            //{
            //    AppConst.UseUpdateNewMode = false;
            //}
            //if (Application.platform == RuntimePlatform.IPhonePlayer)
            //{
            //    AppConst.UseUpdateNewMode = false;
            //    if (useUpdateNewMode != null && useUpdateNewMode.Equals("1"))
            //    {
            //        AppConst.UseUpdateNewMode = true;
            //    }
            //}
            //string useUpdateOriMode = GameConfig.Instance.GetValue("UseUpdatOriMode");
            //if (useUpdateOriMode != null && useUpdateOriMode.Equals("1"))
            //{
            //    AppConst.UseUpdatOriMode = true;
            //}
            //string useUpdateOriThreadMode = GameConfig.Instance.GetValue("UseUpdatOriThreadMode");
            //if (useUpdateOriThreadMode != null && useUpdateOriThreadMode.Equals("1"))
            //{
            //    AppConst.UseUpdatOriThreadMode = true;
            //}

            //string useDeleteRequestdMode = GameConfig.Instance.GetValue("UseDeleteRequestMode");
            //if (useDeleteRequestdMode != null && useDeleteRequestdMode.Equals("0"))
            //{
            //    AppConst.UseDeleteRequestMode = false;
            //}

            //string useAssetBundlePre = GameConfig.Instance.GetValue("UseAssetBundlePre");
            //if (useAssetBundlePre != null && !useAssetBundlePre.Equals(""))
            //{
            //    AppConst.UseAssetBundlePre = useAssetBundlePre;
            //}
        }

        public override void Free()
        {
        }
    }
}
