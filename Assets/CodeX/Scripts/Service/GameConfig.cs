using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using GFW;

namespace CodeX
{
    public class GameConfig : Singleton<GameConfig>
    {
        private Dictionary<string, string> m_ConfigMap = new Dictionary<string, string>();
        private string m_local_version_file = "ver.flag";
        private string m_update_flag_file = "rdata/update.flag";
        private string m_config_path = "phone/config.cfg";
        private bool m_load_succeed = false;

        private int m_local_app_version = 1;//本地apk版本
        private int m_pack_data_version = 1;//本地资源包版本

        public string this[string key]
        {
            get { return m_ConfigMap[key]; }
            set { m_ConfigMap[key] = value; }
        }

        public void LoadConfig()
        {
            MonoHelper.StartCoroutine(LoadLocalConfig());
        }

        //加载本地的config.cfg
        private IEnumerator LoadLocalConfig()
        {
            string root = AppUtil.AppContentPath();
            string src_path = root + this.m_config_path;
            string config_path = AppUtil.DataPath + this.m_config_path;
            string config_dir = Path.GetDirectoryName(config_path);
            if (!Directory.Exists(config_dir))
            {
                Directory.CreateDirectory(config_dir);
            }

            Debug.Log("GameConfig@LoadLocalConfig(): try load game config");
            if (Application.platform == RuntimePlatform.Android)
            {
                WWW www = new WWW(src_path);
                yield return www;
                bool isDone = www.isDone;
                if (!isDone)
                {
                    www.Dispose();
                    Debug.Log("GameConfig@LoadLocalConfig(): game config file no exist");
                    yield break;
                }
                File.WriteAllBytes(config_path, www.bytes);
                www.Dispose();
                www = null;
            }
            else
            {
                if (!File.Exists(src_path))
                {
                    Debug.Log("GameConfig@LoadLocalConfig(): game config file no exist");
                    yield break;
                }
                File.Copy(src_path, config_path, true);
            }

            bool rt = LoadLocalXMLConfig(config_path);
            if (rt)
            {
                this.ReadLocalConfig();
            }
            yield break;
        }
        private bool LoadLocalXMLConfig(string config_path)
        {
            XmlDocument xml_doc = new XmlDocument();
            xml_doc.Load(config_path);
            bool rt = ParseConfig(xml_doc);
            return rt;
        }
        private void ReadLocalConfig()
        {
            string local_ver = GetValue("ApkVersion");
            if (!local_ver.Equals(""))
            {
                m_local_app_version = (int)short.Parse(local_ver);
            }
            string local_data = this.GetValue("DataVersion");
            if (!local_data.Equals(""))
            {
                m_pack_data_version = (int)short.Parse(local_data);
            }
            string cur_url = this.GetValue("ResUrl");
            this.SetValue("AppResUrl", cur_url);
        }
        private bool ParseConfig(XmlDocument xml_doc)
        {
            XmlNode root = xml_doc.FirstChild;
            XmlNodeList node_list = root.ChildNodes;
            foreach (object obj in node_list)
            {
                XmlElement node = (XmlElement)obj;
                XmlNodeList chlids = node.ChildNodes;
                if (chlids.Count > 1)
                {
                    foreach (object obj2 in chlids)
                    {
                        XmlElement child = (XmlElement)obj2;
                        this.m_ConfigMap[child.Name] = child.InnerText;
                    }
                }
                else
                {
                    this.m_ConfigMap[node.Name] = node.InnerText;
                }
            }
            this.m_load_succeed = true;
            return true;
        }
        //加载后台的config.cfg
        public bool LoadRemoteConfig(byte[] data)
        {
            Debug.Log("GameConfig@LoadRemoteConfig() 加载远程配置");
            XmlDocument xml_doc = new XmlDocument();
            try
            {
                xml_doc.Load(new MemoryStream(data));
            }
            catch (Exception e)
            {
                Debug.Log("GameConfig@LoadRemoteConfig: load remote config error :" + e.Message);
                return false;
            }
            return ParseConfig(xml_doc);
        }

        public bool Finish()
        {
            return this.m_load_succeed;
        }

        public void SetValue(string key, string value)
        {
            this.m_ConfigMap[key] = value;
        }

        public string GetValue(string name)
        {
            string out_str;
            if (this.m_ConfigMap.TryGetValue(name, out out_str))
            {
                return out_str;
            }
            else
            {
                return "";
            }
        }

        public bool ReadUpdateFinishState()
        {
            string local_file = AppUtil.DataPath + this.m_update_flag_file;
            if (File.Exists(local_file))
            {
                string flag = File.ReadAllText(local_file);
                Debug.Log("update flag: " + flag);
                return flag.Equals("finish");
            }
            return false;
        }

        public void WriteUpdateState(string flag)
        {
            string root = AppUtil.DataPath + "rdata";
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
            string local_file = AppUtil.DataPath + this.m_update_flag_file;
            StreamWriter file = new StreamWriter(local_file, false);
            file.Write(flag);
            file.Close();
            Debug.Log("WriteUpdateState:" + flag);
        }

        public void WriteVersion()
        {
            string local_file = AppUtil.DataPath + this.m_local_version_file;
            StreamWriter file = new StreamWriter(local_file, false);
            file.Write(m_pack_data_version);
            file.Close();
            Debug.Log("WriteVersion succeed :" + this.m_pack_data_version);
        }

        public bool IsAppChange()
        {
            string remote_version = GetValue("ApkVersion");
            if (!remote_version.Equals(""))
            {
                int cur_version = (int)short.Parse(remote_version);
                if (cur_version > m_local_app_version)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsPackChange()
        {
            int local_ver = ReadLocalVersion();
            Debug.Log(string.Concat(new object[]{"curver:",this.m_pack_data_version," localver:",local_ver}));
            if (local_ver != m_pack_data_version)
            {
                Debug.Log("GameConfig@IsPackChange: true");
                return true;
            }
            else
            {
                Debug.Log("GameConfig@IsPackChange: false");
                return false;
            }
        }

        /// <summary>
        /// 读取ver.flag文件
        /// </summary>
        private int ReadLocalVersion()
        {
            int ver = 0;
            string local_file = AppUtil.DataPath + this.m_local_version_file;
            if (File.Exists(local_file))
            {
                string cur_ver = File.ReadAllText(local_file);
                int.TryParse(cur_ver, out ver);
                Debug.Log("GameConfig@ReadLocalVersion: local version: " + cur_ver);
            }
            return ver;
        }

        public string GetAppUrl()
        {
            return this.GetValue("AppUrl");
        }

        public string FindCustomString(string path)
        {
            foreach (KeyValuePair<string, string> item in this.m_ConfigMap)
            {
                if (path.Contains(item.Key))
                {
                    path = path.Replace(item.Key, item.Value);
                }
            }
            return path;
        }
    }
}
