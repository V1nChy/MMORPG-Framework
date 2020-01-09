using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CodeX
{
    public class AppConst
    {
        public static bool DebugMode = false;                       //调试模式-用于内部测试
        /// <summary>
        /// 如果想删掉框架自带的例子，那这个例子模式必须要
        /// 关闭，否则会出现一些错误。
        /// </summary>
        public const bool ExampleMode = true;                       //例子模式 

        public const int TimerInterval = 1;
        public const int GameFrameRate = 30;                        //游戏帧频

        /// <summary>
        /// 如果开启更新模式，前提必须启动框架自带服务器端。
        /// 否则就需要自己将StreamingAssets里面的所有内容
        /// 复制到自己的Webserver上面，并修改下面的WebUrl。
        /// </summary>
        public static bool UpdateMode = false;                       //更新模式-默认关闭 
        public static bool LuaByteMode = false;                       //Lua字节码模式-默认关闭 
        public static bool LuaBundleMode = true;                    //Lua代码AssetBundle模式

        public const string LuaTempDir = "Lua/";                    //临时目录
        public const string AppPrefix = AppName + "_";              //应用程序前缀
        public const string ExtName = ".unity3d";                   //素材扩展名
        public const string AssetDir = "StreamingAssets";           //素材目录
        public const string FrameworkDir = "LuaFramework";           //素材目录 
        public const string WebUrl = "http://localhost:6688/";      //测试更新地址

        public static string UserId = string.Empty;                 //用户ID
        public static int SocketPort = 0;                           //Socket服务器端口
        public static string SocketAddress = string.Empty;          //Socket服务器地址

        public const string AppName = "CodeX";               //应用程序名称
        public const string LuaAssetsDir = "ldata/";
        public const string MapAssetsDir = "mdata/";
        public const string NormalAssetsDir = "rdata/";
        public const string StreamingAssets = "StreamingAssets";    //打包出来的资源目录

        public static int UpdateFileTimeout = 5;
        public static string CdnUrl = "";
        public static bool IgnoreGameUpdateState = false;
        public static bool OpenNoneImportResVersionSave = false;
        public static bool LoadResWaitNextFrame = false;
        public static bool SilentAssetsUpdate = false;
        public static bool IgnoreUpdateState = false;
        public static bool LowSystemMode = false;
        public static bool OpenDownloadLog = false;
        public static bool UseUpdatOriModeReal = false;
        public static bool UseUpdatOriThreadMode = false;
        public static bool UseUpdateNewMode = false;
        public static bool UseDeleteRequestMode = false;

        public static string FrameworkRoot {
            get {
                return Application.dataPath + "/" + FrameworkDir;
            }
        }

        public static string AppDataPath
        {
            get
            {
                string dataPath = Application.dataPath;
                dataPath = dataPath.Replace("/Assets", "");
                return dataPath;
            }
        }
    }
}