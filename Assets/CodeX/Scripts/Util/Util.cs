using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CodeX
{
    public class Util
    {
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

        public static int RandomNum(int min, int max)
        {
            System.Random ra = new System.Random((int)DateTime.Now.Ticks);
            return ra.Next(min, max);
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
