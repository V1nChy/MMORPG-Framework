using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LuaFramework;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using UnityEditor;
using GFW;

namespace CodeX
{
    public class AppUtil : MonoBehaviour
    {
        /// <summary>
        /// 取得数据存放目录
        /// </summary>
        public static string DataPath
        {
            get
            {
                string game = AppConst.FrameworkDir.ToLower();
                if (Application.isMobilePlatform)
                {
                    return Application.persistentDataPath + "/" + game + "/";
                }
                if (AppConst.DebugMode)
                {
                    return AppConst.AppDataPath + "/" + AppConst.StreamingAssets + "/";
                }
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    int i = Application.dataPath.LastIndexOf('/');
                    return Application.dataPath.Substring(0, i + 1) + game + "/";
                }
                return "c:/" + game + "/";
            }
        }

        /// <summary>
        /// 应用程序内容路径
        /// </summary>
        public static string AppContentPath()
        {
            string path = string.Empty;
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    path = "jar:file://" + Application.dataPath + "!/assets/";
                    break;
                case RuntimePlatform.IPhonePlayer:
                    path = Application.dataPath + "/Raw/";
                    break;
                default:
                    path = Application.dataPath + "/" + AppConst.AssetDir + "/";
                    break;
            }
            return path;
        }

        public static string LuaPath
        {
            get
            {
                return LuaConst.luaDir + "/";
            }
        }

        public static string GetRelativePath()
        {
            if (Application.isEditor)
                return "file://" + System.Environment.CurrentDirectory.Replace("\\", "/") + "/" + AppConst.StreamingAssets + "/";
            else if (Application.isMobilePlatform || Application.isConsolePlatform)
                return "file:///" + DataPath;
            else // For standalone player.
                return "file://" + Application.streamingAssetsPath + "/";
        }

        /// <summary>
        /// 取得行文本
        /// </summary>
        public static string GetFileText(string path)
        {
            return File.ReadAllText(path);
        }

        /// <summary>
        /// 网络可用
        /// </summary>
        public static bool NetAvailable
        {
            get
            {
                return Application.internetReachability != NetworkReachability.NotReachable;
            }
        }

        /// <summary>
        /// 是否是无线
        /// </summary>
        public static bool IsWifi
        {
            get
            {
                return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
            }
        }

        /// <summary>
        /// 防止初学者不按步骤来操作
        /// </summary>
        /// <returns></returns>
        public static int CheckRuntimeFile()
        {
            if (!Application.isEditor) return 0;
            string streamDir = AppConst.AppDataPath + "/StreamingAssets/";
            if (!Directory.Exists(streamDir))
            {
                return -1;
            }
            else
            {
                string[] files = Directory.GetFiles(streamDir);
                if (files.Length == 0) return -1;

                //if (!File.Exists(streamDir + "files.txt")) {
                //    return -1;
                //}
            }
            string sourceDir = AppConst.FrameworkRoot + "/ToLua/Source/Generate/";
            if (!Directory.Exists(sourceDir))
            {
                return -2;
            }
            else
            {
                string[] files = Directory.GetFiles(sourceDir);
                if (files.Length == 0) return -2;
            }
            return 0;
        }

        /// <summary>
        /// 检查运行环境
        /// </summary>
        public static bool CheckEnvironment()
        {
#if UNITY_EDITOR
            int resultId = AppUtil.CheckRuntimeFile();
            if (resultId == -1)
            {
                Debug.LogError("没有找到框架所需要的资源，单击Game菜单下Build xxx Resource生成！！");
                EditorApplication.isPlaying = false;
                return false;
            }
            else if (resultId == -2)
            {
                Debug.LogError("没有找到Wrap脚本缓存，单击Lua菜单下Gen Lua Wrap Files生成脚本！！");
                EditorApplication.isPlaying = false;
                return false;
            }
            if (Application.loadedLevelName == "Test" && !AppConst.DebugMode)
            {
                Debug.LogError("测试场景，必须打开调试模式，AppConst.DebugMode = true！！");
                EditorApplication.isPlaying = false;
                return false;
            }
#endif
            return true;
        }
    }
}