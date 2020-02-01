using GFW;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CodeX
{
    public class Util
    {
        private static List<string> luaPaths = new List<string>();

        public static int Int(object o)
        {
            return Convert.ToInt32(o);
        }

        public static float Float(object o)
        {
            return (float)Math.Round(Convert.ToSingle(o), 2);
        }

        public static long Long(object o)
        {
            return Convert.ToInt64(o);
        }

        public static int Random(int min, int max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        public static float Random(float min, float max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        public static string Uid(string uid)
        {
            int position = uid.LastIndexOf('_');
            return uid.Remove(0, position + 1);
        }

        public static long GetTime()
        {
            TimeSpan ts = new TimeSpan(DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1, 0, 0, 0).Ticks);
            return (long)ts.TotalMilliseconds;
        }

        /// <summary>
        /// 搜索子物体组件-GameObject版
        /// </summary>
        public static T Get<T>(GameObject go, string subnode) where T : Component
        {
            if (go != null)
            {
                Transform sub = go.transform.Find(subnode);
                if (sub != null) return sub.GetComponent<T>();
            }
            return null;
        }

        /// <summary>
        /// 搜索子物体组件-Transform版
        /// </summary>
        public static T Get<T>(Transform go, string subnode) where T : Component
        {
            if (go != null)
            {
                Transform sub = go.Find(subnode);
                if (sub != null) return sub.GetComponent<T>();
            }
            return null;
        }

        /// <summary>
        /// 搜索子物体组件-Component版
        /// </summary>
        public static T Get<T>(Component go, string subnode) where T : Component
        {
            return go.transform.Find(subnode).GetComponent<T>();
        }

        /// <summary>
        /// 添加组件
        /// </summary>
        public static T Add<T>(GameObject go) where T : Component
        {
            if (go != null)
            {
                T[] ts = go.GetComponents<T>();
                for (int i = 0; i < ts.Length; i++)
                {
                    if (ts[i] != null) GameObject.Destroy(ts[i]);
                }
                return go.gameObject.AddComponent<T>();
            }
            return null;
        }

        /// <summary>
        /// 添加组件
        /// </summary>
        public static T Add<T>(Transform go) where T : Component
        {
            return Add<T>(go.gameObject);
        }

        /// <summary>
        /// 查找子对象
        /// </summary>
        public static GameObject Child(GameObject go, string subnode)
        {
            return Child(go.transform, subnode);
        }

        /// <summary>
        /// 查找子对象
        /// </summary>
        public static GameObject Child(Transform go, string subnode)
        {
            Transform tran = go.Find(subnode);
            if (tran == null) return null;
            return tran.gameObject;
        }

        /// <summary>
        /// 取平级对象
        /// </summary>
        public static GameObject Peer(GameObject go, string subnode)
        {
            return Peer(go.transform, subnode);
        }

        /// <summary>
        /// 取平级对象
        /// </summary>
        public static GameObject Peer(Transform go, string subnode)
        {
            Transform tran = go.parent.Find(subnode);
            if (tran == null) return null;
            return tran.gameObject;
        }

        /// <summary>
        /// 计算字符串的MD5值
        /// </summary>
        public static string md5(string source)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(source);
            byte[] md5Data = md5.ComputeHash(data, 0, data.Length);
            md5.Clear();

            string destString = "";
            for (int i = 0; i < md5Data.Length; i++)
            {
                destString += System.Convert.ToString(md5Data[i], 16).PadLeft(2, '0');
            }
            destString = destString.PadLeft(32, '0');
            return destString;
        }

        /// <summary>
        /// 计算文件的MD5值
        /// </summary>
        public static string md5file(string file)
        {
            try
            {
                FileStream fs = new FileStream(file, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);
                fs.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("md5file() fail, error:" + ex.Message);
            }
        }

        /// <summary>
        /// 清除所有子节点
        /// </summary>
        public static void ClearChild(Transform go)
        {
            if (go == null) return;
            for (int i = go.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(go.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 清理内存
        /// </summary>
        public static void ClearMemory()
        {
            GC.Collect(); Resources.UnloadUnusedAssets();
            LuaManager mgr = GameSystem.Instance.GetManager<LuaManager>();
            if (mgr != null) mgr.LuaGC();
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

        public static void Log(string str)
        {
            Debug.Log(str);
        }

        public static void LogWarning(string str)
        {
            Debug.LogWarning(str);
        }

        public static void LogError(string str)
        {
            Debug.LogError(str);
        }

        /// <summary>
        /// 执行Lua方法
        /// </summary>
        public static object[] CallMethod(string module, string func, params object[] args)
        {
            LuaManager luaMgr = GameSystem.Instance.GetManager<LuaManager>();
            if (luaMgr == null) return null;
            return luaMgr.CallFunction(module + "." + func, args);
        }

        public static int RandomNum(int min, int max)
        {
            System.Random ra = new System.Random((int)DateTime.Now.Ticks);
            return ra.Next(min, max);
        }

        public static void ThrowLuaException(string error, Exception exception = null, int skip = 1)
        {
            bool flag = Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android;
            if (flag)
            {
                Debug.LogWarning(error);
            }
            else
            {
                Debug.LogError(error);
            }
        }

        public static string GetTimeStamp()
        {
            return Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds).ToString();
        }

        public static string GetAssetsBundlePathFromBase64(string res_path)
        {
            string file = Path.GetFileNameWithoutExtension(res_path);
            byte[] byteArray = Convert.FromBase64String(file);
            return Encoding.UTF8.GetString(byteArray).ToLower();
        }

        /// <summary>
        /// 获得Base64加密过的文件名
        /// </summary>
		public static string GetAssetsBundlePath(string res_path)
        {
            string file = Path.GetFileNameWithoutExtension(res_path);
            byte[] byte_temp = Encoding.Default.GetBytes(file);
            string bast64_file_name = Convert.ToBase64String(byte_temp);
            string result;
            if (res_path.Contains("mdata/"))
            {
                result = string.Format("{0}{1}{2}", "mdata/", bast64_file_name, ".syrd");
            }
            else if (res_path.Contains("rdata/"))
            {
                result = string.Format("{0}{1}{2}", "rdata/", bast64_file_name, ".syrd");
            }
            else
            {
                if (res_path.Contains("ldata/"))
                {
                    result = string.Format("{0}{1}{2}", "ldata/", bast64_file_name, ".syld");
                }
                else
                {
                    result = res_path;
                }
            }
            return result;
        }

        public static bool AssetBundleExists(string file_path)
        {
            return File.Exists(file_path);
        }

        public static bool CheckMd5(string path, string file_md5, bool full = false)
        {
            byte[] buffs;
            bool dataToFile = Pathtool.GetDataToFile(path, out buffs);
            if (dataToFile)
            {
                string md5temp = Md5Helper.Md5Buffer(buffs);
                string md5 = md5temp;
                if (!full)
                {
                    md5 = md5temp.Remove(8);
                }
                if (md5.Equals(file_md5))
                {
                    return true;
                }
                bool openDownloadLog = AppConst.OpenDownloadLog;
                if (openDownloadLog)
                {
                    Debug.Log(string.Format("res md5 not same, res path:{0},self md5:{1},check md5:{2}", path, md5, file_md5));
                }
            }
            else
            {
                bool openDownloadLog2 = AppConst.OpenDownloadLog;
                if (openDownloadLog2)
                {
                    Debug.Log(string.Format("res md5 not same, res path not exist:{0}", path));
                }
            }
            return false;
        }
    }
}
