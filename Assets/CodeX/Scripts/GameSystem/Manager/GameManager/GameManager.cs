using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GFW.ManagerSystem;
using GFW;

namespace CodeX
{
    public class GameManager : Manager
    {
        protected static bool initialize = false;
        private List<string> downloadFiles = new List<string>();
        public bool ResInitOk = false;
        /// <summary>
        /// 初始化游戏管理器
        /// </summary>
        private void Awake()
        {
            Init();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        void Init() {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = AppConst.GameFrameRate;
        }

        private void Start()
        {
            ModuleManager.Instance.StartModule(ModuleDef.LuaGameModule);
        }
    }
}