using System;
using System.Collections;
using UnityEngine;
using GFW;

namespace CodeX
{
    public enum MainLoopStateType
    {
        Init,
        UpdateConfig,
        CheckAppUpdate,
        LoadBg,
        ExtractRes,
        VersionFile,
        UpdateRes,
        PreRes,
        IntoGame
    }

    public enum BackUpUrlState
    {
        None,
        RUN,
        END,
        NEW
    }

    public class CheckUpdateManager : ServiceModule<CheckUpdateManager>
    {
        protected GameStateMachine m_state_machine = new GameStateMachine();
        protected GameState m_init_state;               //1.加载本地的config.cfg配置，获得后台地址
        protected GameState m_update_config_state;      //2.向后台下载最新的config.cfg配置
        protected GameState m_check_app_update_state;   //3.检查apk版本
        protected GameState m_check_package_state;      //4.检查package是否改变
        protected GameState m_extract_res_state;        //  1).解压资源
        protected GameState m_update_version_file_state;//5.对比后台md5值
        protected GameState m_update_state;             //6.更新文件
        protected GameState m_pre_resource_state;       //7.退出资源检查，启动lua逻辑

        private GameObject m_loop_view;
        private bool m_finish_state = false;
        private uint m_config_try_load_count = 0u;

        public BackUpUrlState backupUrlState { get; set; }

        public void Start()
        {
            this.backupUrlState = BackUpUrlState.None;
            StartCheckUpdate();
        }

        private void StartCheckUpdate()
        {
            //this.m_loop_view = new GameObject("LoopView");
            //this.m_loop_view.AddComponent<LoopView>();

            this.m_state_machine.CreateSink("main");
            this.m_init_state = this.m_state_machine.CreateNormalState(MainLoopStateType.Init.ToString(), "main");
            this.m_update_config_state = this.m_state_machine.CreateNormalState(MainLoopStateType.UpdateConfig.ToString(), "main");
            this.m_check_app_update_state = this.m_state_machine.CreateNormalState(MainLoopStateType.CheckAppUpdate.ToString(), "main");
            this.m_check_package_state = this.m_state_machine.CreateNormalState(MainLoopStateType.LoadBg.ToString(), "main");
            this.m_update_version_file_state = this.m_state_machine.CreateNormalState(MainLoopStateType.VersionFile.ToString(), "main");
            this.m_update_state = this.m_state_machine.CreateNormalState(MainLoopStateType.UpdateRes.ToString(), "main");
            this.m_pre_resource_state = this.m_state_machine.CreateNormalState(MainLoopStateType.PreRes.ToString(), "main");
            this.m_extract_res_state = this.m_state_machine.CreateNormalState(MainLoopStateType.ExtractRes.ToString(), "main");

            this.m_init_state.SetCallbackAsCListner(new InitStateListner());
            this.m_init_state.AddOutStateName(this.m_check_package_state.GetName());
            this.m_init_state.AddOutStateName(this.m_update_config_state.GetName());

            this.m_update_config_state.SetCallbackAsCListner(new UpdateConfigStateListner());
            this.m_update_config_state.SetStateCanReEnter(true);
            this.m_update_config_state.AddOutStateName(this.m_check_package_state.GetName());
            this.m_update_config_state.AddOutStateName(this.m_update_config_state.GetName());
            this.m_update_config_state.AddOutStateName(this.m_check_app_update_state.GetName());

            this.m_check_app_update_state.SetCallbackAsCListner(new CheckAppUpdateStateListner());
            this.m_check_app_update_state.AddOutStateName(this.m_check_package_state.GetName());

            this.m_check_package_state.SetCallbackAsCListner(new CheckPackageStateListner());
            this.m_check_package_state.AddOutStateName(this.m_extract_res_state.GetName());
            this.m_check_package_state.AddOutStateName(this.m_update_version_file_state.GetName());
            this.m_check_package_state.AddOutStateName(this.m_pre_resource_state.GetName());

            this.m_extract_res_state.SetCallbackAsCListner(new ExtractResStateListner());
            this.m_extract_res_state.AddOutStateName(this.m_update_version_file_state.GetName());
            this.m_extract_res_state.AddOutStateName(this.m_pre_resource_state.GetName());

            this.m_update_version_file_state.SetCallbackAsCListner(new UpdateVersionFileStateListner());
            this.m_update_version_file_state.AddOutStateName(this.m_update_state.GetName());

            this.m_update_state.SetCallbackAsCListner(new UpdateResStateListner());
            this.m_update_state.AddOutStateName(this.m_pre_resource_state.GetName());
            
            this.m_pre_resource_state.SetCallbackAsCListner(new PreResStateListner());

            this.m_state_machine.ChangeState(this.m_init_state.GetName());
        }

        private void Update()
        {
            if (!this.m_finish_state)
            {
                if (this.m_state_machine != null)
                {
                    this.m_state_machine.UpdateNow(Time.deltaTime);
                }

                if (Application.platform == RuntimePlatform.Android && Input.GetKeyDown(KeyCode.Escape))
                {
                    ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "SHOW_MESSAGE", GameConfig.Instance["Exitgame"]);
                }
            }
        }

        public void Close()
        {
            if (AppConst.UpdateMode && ResUpdateManager.Instance != null)
            {
                ResUpdateManager.Instance.Close();
            }
        }

        public void DestoryStateMachine()
        {
            if (this.m_state_machine != null)
            {
                this.m_state_machine.Dispose();
                this.m_state_machine = null;
            }
            this.m_init_state = null;
            this.m_update_config_state = null;
            this.m_check_app_update_state = null;
            this.m_check_package_state = null;
            this.m_extract_res_state = null;
            this.m_update_version_file_state = null;
            this.m_update_state = null;
            this.m_pre_resource_state = null;
            this.m_finish_state = true;
            if (this.m_loop_view != null)
            {
                this.m_loop_view.SetActive(false);
                GameObject.Destroy(this.m_loop_view);
                this.m_loop_view = null;
            }
        }

        public bool IsInState(string state_name)
        {
            return this.m_state_machine.IsInState(state_name);
        }
        public void TryLoadConfig()
        {
            this.m_config_try_load_count += 1u;
        }
        public void CleanTryLoadConfig()
        {
            this.m_config_try_load_count = 0u;
        }
        public uint ConfigTryLoadCount()
        {
            return this.m_config_try_load_count;
        }

        #region -状态切换
        public void ToUpdateConfigState()
        {
            this.m_state_machine.ChangeState(this.m_update_config_state.GetName());
        }
        public void ToCheckUpdateAppState()
        {
            this.m_state_machine.ChangeState(this.m_check_app_update_state.GetName());
        }
        public void ToLoadBgState()
        {
            this.m_state_machine.ChangeState(this.m_check_package_state.GetName());
        }
        public void ToExtractResState()
        {
            this.m_state_machine.ChangeState(this.m_extract_res_state.GetName());
        }
        public void ToUpdateVersionFileState()
        {
            this.m_state_machine.ChangeState(this.m_update_version_file_state.GetName());
        }
        public void ToUpdateResState()
        {
            this.m_state_machine.ChangeState(this.m_update_state.GetName());
        }
        public void ToPreResState()
        {
            this.m_state_machine.ChangeState(this.m_pre_resource_state.GetName());
        }
        #endregion
    }
}
