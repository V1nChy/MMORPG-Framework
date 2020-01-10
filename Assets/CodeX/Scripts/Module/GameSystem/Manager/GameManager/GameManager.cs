using UnityEngine;
using System.Collections.Generic;
using GFW;

namespace CodeX
{
    public class GameManager : ManagerModule
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
            BusinessManager.Instance.StartModule(ModuleDef.LuaGameModule);
        }
    }
}