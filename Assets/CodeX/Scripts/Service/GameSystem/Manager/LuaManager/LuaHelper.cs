using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using LuaInterface;
using System;
using GFW;

namespace CodeX {
    public static class LuaHelper {

        /// <summary>
        /// getType
        /// </summary>
        /// <param name="classname"></param>
        /// <returns></returns>
        public static System.Type GetType(string classname) {
            Assembly assb = Assembly.GetExecutingAssembly();  //.GetExecutingAssembly();
            System.Type t = null;
            t = assb.GetType(classname); ;
            if (t == null) {
                t = assb.GetType(classname);
            }
            return t;
        }

        /// <summary>
        /// 面板管理器
        /// </summary>
        public static UIAgentManager GetUIManager() {
            return GameSystem.Instance.GetManager<UIAgentManager>();
        }

        /// <summary>
        /// 资源管理器
        /// </summary>
        public static ResourceManager GetResourceManager() {
            return GameSystem.Instance.GetManager<ResourceManager>();
        }

        /// <summary>
        /// 网络管理器
        /// </summary>
        public static NetworkManager GetNetManager() {
            return GameSystem.Instance.GetManager<NetworkManager>();
        }

        /// <summary>
        /// 音乐管理器
        /// </summary>
        public static SoundManager GetSoundManager() {
            return GameSystem.Instance.GetManager<SoundManager>();
        }


        /// <summary>
        /// pbc/pblua函数回调
        /// </summary>
        /// <param name="func"></param>
        public static void OnCallLuaFunc(LuaByteBuffer data, LuaFunction func) {
            if (func != null) func.Call(data);
            Debug.LogWarning("OnCallLuaFunc length:>>" + data.buffer.Length);
        }

        /// <summary>
        /// cjson函数回调
        /// </summary>
        /// <param name="data"></param>
        /// <param name="func"></param>
        public static void OnJsonCallFunc(string data, LuaFunction func) {
            Debug.LogWarning("OnJsonCallback data:>>" + data + " lenght:>>" + data.Length);
            if (func != null) func.Call(data);
        }

        public static int GetSystemMemorySize()
        {
            return SystemInfo.systemMemorySize;
        }
    }
}