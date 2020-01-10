using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using LuaInterface;
using UnityEngine;
using UnityEngine.Networking;
using GFW;

namespace CodeX
{
    public enum UpdateState
    {
        None,
        Init_Sqlite,
        Check_List,
        Update_List,
        Show_Update,
        Complate,
        Errer
    }

    public delegate bool ResourceLoadCallbackFunc(byte[] buffer);

    public class UpdateInfo
    {
        public string http_str = "";
        public string file_path = "";
        public WWW www = null;
        public UnityWebRequest request = null;
        public int count = 0;
        public ResourceLoadCallbackFunc func;
        public int file_size = 0;
        public bool random_state = false;
        public bool download_again = false;
    }

    public class ResUpdateManager : Singleton<ResUpdateManager>
	{
        private UpdateState m_cur_state = UpdateState.None;
        private string m_http_path;
        private int m_loaded_count = 0;
        private int m_max_count = 8;
        private int m_res_count = 0;
        private string m_unity_assets_path;// /client/xxxres/
        private string m_unity_bundle_path;// /client/xxxbundle/
        private bool m_one_finish = true;
        private bool m_version_file_check_state = false;

        private float m_all_update_size = 0f;

        private float m_cur_update_size = 0f;

        private float m_last_download_time = 0f;

        private float m_lase_update_size = 0f;

        private float m_download_speed = 0f;

        private float m_download_process = 0f;

        private bool m_stop_update_state = false;

        private string m_scripts_version = "";

        private List<UpdateInfo> m_update_infos = new List<UpdateInfo>();

        private Dictionary<string, UpdateInfo> m_loadind_map = new Dictionary<string, UpdateInfo>();

        private string[] pack_names = null;//assets,luaconfig,unityres,unitybundle
        private List<string> m_pack_name = new List<string>();
        private List<SqliteFilePack> m_pack_list = new List<SqliteFilePack>();
        private Dictionary<string, bool> m_version_file_update = new Dictionary<string, bool>();//需要更新的版本文件

        public bool IsFinish
        {
            get
            {
                return this.m_cur_state == UpdateState.Complate;
            }
        }

        #region -开启关闭
        public void Init()
        {
            this.m_http_path = AppConst.CdnUrl;
            this.InitPackName();
        }
        private void InitPackName()
        {
            pack_names = GameConfig.Instance["PackName"].Split(new char[]
			{
				','
			});
            foreach (string item in pack_names)
            {
                m_pack_name.Add(item);
            }
        }
        public void Close()
        {
            this.CloseGameDataBase(false);
        }
        public void CloseGameDataBase(bool ignore_bundle)
        {
            for (int index = 0; index < this.m_pack_list.Count; index++)
            {
                SqliteFilePack pack = this.m_pack_list[index];
                if (!pack.m_pack_name.Equals("unitybundle") || !ignore_bundle)
                {
                    pack.CloseGameDataBase();
                }
            }
            Debug.Log("CloseGameDataBase Succeed");
        }
        public void OpenGameVersion()
        {
            for (int index = 0; index < m_pack_list.Count; index++)
            {
                SqliteFilePack pack = m_pack_list[index];
                pack.OpenGameVersion();
            }
            Debug.Log("OpenGameVersion Succeed");
        }
        #endregion

        #region -从后台下载pack_version.xml版本记录文件,与本地xxx_head.cymzq的MD5值对比,确定需要更新的pack,初始化m_version_file_update字典
        public void DownLoadVersionFileAndCheck()
        {
            bool ignoreUpdateState = AppConst.IgnoreUpdateState;
            if (ignoreUpdateState)
            {
                this.m_version_file_check_state = true;
            }
            else
            {
                string version_file = GameConfig.Instance["Md5FileName"];
                string version_file_url = string.Format("{0}{1}", this.m_http_path, version_file);
                string local_path = string.Format("{0}{1}", AppUtil.DataPath, version_file);
                MemoryLoadCallbackFunc delay_func = delegate (bool is_suc, byte[] buffer)
                {
                    if (is_suc)
                    {
                        LogMgr.Log("download version file Success");
                        this.ParseVersionFile(buffer);
                    }
                    else
                    {
                        LogMgr.Log("request version file error {0}", version_file_url);
                    }
                };
                MemoryQuest memory_quest = new MemoryQuest();
                string patchs = GameConfig.Instance.GetValue("PatchFileName");
                if (Application.platform == RuntimePlatform.Android)
                {
                    string androidpatchs = GameConfig.Instance.GetValue("AndroidPatchFileName");
                    version_file_url = version_file_url.Replace(patchs, androidpatchs);
                }
                else
                {
                    if (Application.platform == RuntimePlatform.IPhonePlayer)
                    {
                        string iospatchs = GameConfig.Instance.GetValue("IosPatchFileName");
                        version_file_url = version_file_url.Replace(patchs, iospatchs);
                    }
                }
                memory_quest.RelativePath = string.Format("{0}?v={1}", version_file_url, Util.GetTimeStamp());
                LogMgr.Log("download file:{0}",memory_quest.RelativePath);
                ResRequest.Instance.RequestMemoryAsync(memory_quest, delay_func);
            }
        }
        private void ParseVersionFile(byte[] data)
        {
            XmlDocument xml_doc = new XmlDocument();
            xml_doc.Load(new MemoryStream(data));
            XmlNode root = xml_doc.FirstChild;
            XmlNode version_node = root.NextSibling.FirstChild;
            XmlNodeList node_list = version_node.ChildNodes;
            foreach (object obj in node_list)
            {
                XmlElement node = (XmlElement)obj;
                string pack = node.GetAttribute("pack_name");
                string md5 = node.GetAttribute("md5");
                string local_path = string.Format("{0}{1}", AppUtil.DataPath, GetVersionFile(pack, false));//c:/luaframework/patchs/xxx_head.cymzq
                bool need_update = false;
                if (!Util.CheckMd5(local_path, md5, true))
                {
                    need_update = true;
                    if (pack.Equals("unitybundle"))
                    {
                        AppConst.SilentAssetsUpdate = true;
                    }
                }
                m_version_file_update.Add(pack, need_update);
            }
            version_node = version_node.NextSibling;
            if (version_node != null)
            {
                version_node = version_node.NextSibling;
                if (version_node != null && version_node.ChildNodes != null)
                {
                    XmlNode version_text_node = version_node.ChildNodes[0];
                    if (version_text_node != null)
                    {
                        this.m_scripts_version = version_text_node.Value;
                        GameConfig.Instance["scriptsver"] = this.m_scripts_version;
                    }
                }
            }
            Debug.Log("parse version file Success");
            this.m_version_file_check_state = true;
        }
        public bool VersionFileLoad()
        {
            return this.m_version_file_check_state;
        }
        #endregion

        #region -遍历pack列表，解压版本db文件到持久化路径下;初始化4个pack db库;更新luaconfig,unityres
        public IEnumerator CheckVersionFile()
        {
            string dataPath = AppUtil.DataPath;
            string resPath = AppUtil.AppContentPath();
            bool hight_res_update = false;
            int num;
            WWW www = null;
            for (int index = 0; index < m_pack_name.Count; index = num)
            {
                #region -解压版本文件到持久化路径下
                string pack_name = m_pack_name[index];
                string db_file = GetVersionFile(pack_name, true);//patchs/assets_main.sydb
                string version_file = GetVersionFile(pack_name, false);//patchs/assets_head.cymzq
                string patch_file_name = GameConfig.Instance["PatchFileName"];//patchs
                string patchs_path = string.Format("{0}{1}", dataPath, patch_file_name);//c:/luaframework/patchs
                if (!Directory.Exists(patchs_path))
                {
                    Pathtool.CreatePath(patchs_path);
                }
                string db_path = string.Format("{0}{1}", dataPath, db_file);//c:/luaframework/patchs/assets_main.sydb
                string version_path = string.Format("{0}{1}", dataPath, version_file);//c:/luaframework/patchs/assets_head.cymzq
                string src_db_path = string.Format("{0}{1}", resPath, db_file);//G:/Workspace/github/SyEngine/code/u3d/Assets/StreamingAssets/patchs/assets_main.sydb
                string src_version_path = string.Format("{0}{1}", resPath, version_file);//G:/Workspace/github/SyEngine/code/u3d/Assets/StreamingAssets/patchs/assets_head.cymzq
                bool pack_change = GameConfig.Instance.IsPackChange();
                if (pack_change || !File.Exists(db_path) || !File.Exists(version_path))
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        m_version_file_update[pack_name] = true;
                        if (File.Exists(db_path))
                        {
                            File.Delete(db_path);
                        }
                        www = new WWW(src_db_path);
                        yield return www;
                        if (www.isDone)
                        {
                            File.WriteAllBytes(db_path, www.bytes);
                        }
                        www.Dispose();
                        www = null;
                        yield return null;
                        if (File.Exists(version_path))
                        {
                            File.Delete(version_path);
                        }
                        www = new WWW(src_version_path);
                        yield return www;
                        if (www.isDone)
                        {
                            File.WriteAllBytes(version_path, www.bytes);
                        }
                        www.Dispose();
                        www = null;
                        yield return null;
                    }
                    else
                    {
                        m_version_file_update[pack_name] = true;
                        File.Copy(src_db_path, db_path, true);
                        File.Copy(src_version_path, version_path, true);
                    }
                }
                if (AppConst.IgnoreUpdateState)
                {
                    m_version_file_update[pack_name] = false;
                }
                if (this.m_version_file_update[pack_name])
                {
                    hight_res_update = true;
                }
                #endregion
                yield return new WaitForEndOfFrame();
                ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "UpdateProgress", (double)(index + 1) * 100.0 / (double)this.m_pack_name.Count);
                num = index + 1;
            }
            www = null;
            Debug.Log("ResUpdateManager@CheckVersionFile: init patchs succeed");
            InitSqlite();
            Debug.Log("init sqlite succeed");
            GameConfig.Instance.WriteVersion();
            yield break;
        }
        private void InitSqlite()
        {
            #region -初始化pack数据库
            for (int index = 0; index < m_pack_name.Count; index++)
            {
                string pack_name = m_pack_name[index];
                string db_file = GetVersionFile(pack_name, true);//patchs/xxx_main.sydb
                string remote_version_file = GetVersionFile(pack_name, false);//patchs/xxx_head.cymzq
                SqliteFilePack pack = new SqliteFilePack(m_http_path, db_file, remote_version_file, pack_name);
                if (pack_name == pack_names[1])//luaconfig
                {
                    pack.SetExtInfo(true, GameConfig.Instance["pack"], GameConfig.Instance["jyconfig"]);
                }
                else if (pack_name == pack_names[2])//unityres
                {
                    switch (Application.platform)
                    {
                        case RuntimePlatform.OSXPlayer:
                        case RuntimePlatform.IPhonePlayer:
                            m_unity_assets_path = GameConfig.Instance["IosRespath"];// /client/iosres/
                            break;
                        case RuntimePlatform.WindowsPlayer:
                        case RuntimePlatform.WindowsEditor:
                            m_unity_assets_path = GameConfig.Instance["UnityResPath"];// /client/unityres/
                            break;
                        case RuntimePlatform.Android:
                            m_unity_assets_path = GameConfig.Instance["AndroidResPath"];// /client/androidres/
                            break;
                    }
                    pack.AddIgnoreStr(m_unity_assets_path);//只是为了去掉这个路径
                    pack.SetGameNoUpdateState(true);
                }
                else if (pack_name == pack_names[3])//unitybundle
                {
                    switch (Application.platform)
                    {
                        case RuntimePlatform.IPhonePlayer:
                            m_unity_bundle_path = GameConfig.Instance["IosBundle"];
                            break;
                        case RuntimePlatform.WindowsEditor:
                            m_unity_bundle_path = GameConfig.Instance["UnityBundle"];
                            break;
                        case RuntimePlatform.Android:
                            m_unity_bundle_path = GameConfig.Instance["AndroidBundle"];
                            break;
    
                    }
                    pack.AddIgnoreStr(this.m_unity_bundle_path);
                }
                m_pack_list.Add(pack);
            }
            #endregion

            m_cur_state = UpdateState.Init_Sqlite;
            m_loaded_count = 0;
            for (int i = 0; i < m_pack_list.Count; i++)
            {
                string pack_name = m_pack_list[i].GetPackName();
                bool need_update = m_version_file_update[pack_name];
                m_pack_list[i].InitSqlite(need_update);
            }
        }
        public string GetVersionFile(string pack_name, bool main)
        {
            string patchs = GameConfig.Instance["PatchFileName"];
            string path = string.Format("{0}{1}", patchs, pack_name);
            if (main)
            {
                path = string.Format("{0}{1}", path, GameConfig.Instance["MainSydb"]);
            }
            else
            {
                path = string.Format("{0}{1}", path, GameConfig.Instance["HeadCymzq"]);
            }
            return path;
        }
        public float Update()
        {
            float result = 0;
            if (m_cur_state == UpdateState.Complate)
            {
                result = 100f;
            }
            else
            {
                switch (m_cur_state)
                {
                    case UpdateState.Init_Sqlite:
                        for (int i = 0; i < m_pack_list.Count; i++)
                        {
                            if (m_pack_list[i].Result == PackResult.Faild)
                            {
                                m_cur_state = UpdateState.Errer;
                                return 0f;
                            }
                            if (m_pack_list[i].Result != PackResult.Success)
                            {
                                return 0f;
                            }
                        }
                        m_cur_state = UpdateState.Check_List;
                        Debug.Log("ResUpdateManager@UpdateState: Init_Sqlite finish");
                        break;
                    case UpdateState.Check_List:
                        Debug.Log("UpdateState.Check_List");
                        this.m_update_infos.Clear();
                        this.m_res_count = 0;
                        for (int i = 0; i < m_pack_list.Count; i++)
                        {
                            //排除assets,unitybundle
                            if (!GameConfig.Instance["CheckListUpdate"].Contains(m_pack_list[i].m_pack_name))
                            {
                                Debug.Log("start check " + m_pack_list[i].m_pack_name);
                                float update_size = m_pack_list[i].CheckUpdateList(m_update_infos);
                                m_all_update_size += update_size;
                            }
                        }
                        m_res_count = m_update_infos.Count;
                        m_cur_state = UpdateState.Show_Update;
                        Debug.Log(string.Concat(new object[]{"NeedUpdate FileCount:",this.m_res_count," NeedUpdate FileSize:",this.m_all_update_size," kb"}));
                        break;
                    case UpdateState.Update_List:
                        {
                            UpdateInfosLoading();
                            if (Application.internetReachability == NetworkReachability.NotReachable)
                            {
                                return m_download_process;
                            }
                            return UpdateProcess();
                        }
                    case UpdateState.Show_Update:
                        {
                            if (m_all_update_size > 0f)
                            {
                                string update_tips = GameConfig.Instance["UpdateTips"];
                                string confirm_wifi = GameConfig.Instance["ConfirmWifi"];
                                string show_text = string.Format("{0}({1}M){2}", update_tips, (this.m_all_update_size / 1024f).ToString("f2"), confirm_wifi);
                                SetUpdateState(UpdateState.Update_List);
                            }
                            else
                            {
                                this.m_cur_state = UpdateState.Update_List;
                            }
                            break;
                        }
                }
                result = 0f;
            }
            return result;
        }
        private void UpdateInfosLoading()
        {
            int count = this.m_update_infos.Count;
            if (this.m_loadind_map.Count < this.m_max_count && count > 0 && !this.m_stop_update_state)
            {
                UpdateInfo info = this.m_update_infos[count - 1];
                this.m_update_infos.RemoveAt(count - 1);
                MemoryLoadCallbackFunc delay_func = delegate (bool is_suc, byte[] buffer)
                {
                    bool flag3 = this.m_loadind_map.ContainsKey(info.file_path);
                    if (flag3)
                    {
                        this.m_loadind_map.Remove(info.file_path);
                    }
                    if (is_suc)
                    {
                        bool state = true;
                        bool flag4 = info.func != null;
                        if (flag4)
                        {
                            bool flag5 = !AppConst.UseUpdatOriModeReal;
                            if (flag5)
                            {
                                state = info.func(buffer);
                            }
                            else
                            {
                                state = false;
                                string file_path = info.file_path;
                                byte[] buffs;
                                bool dataToFile = Pathtool.GetDataToFile(file_path, out buffs);
                                if (dataToFile)
                                {
                                    state = info.func(buffs);
                                }
                                buffs = null;
                            }
                        }
                        string md5_exit = GameConfig.Instance.GetValue("OpenMd5ErrorExitState");
                        bool flag7 = state || md5_exit.Equals("0");
                        if (flag7)
                        {
                            this.m_loaded_count++;
                            this.m_lase_update_size += (float)info.file_size;
                            this.m_cur_update_size += (float)info.file_size;
                            bool flag8 = this.m_last_download_time == 0f;
                            if (flag8)
                            {
                                this.m_download_speed = 100f;
                                this.m_last_download_time = Time.time;
                            }
                            else
                            {
                                float pass_time = Time.time - this.m_last_download_time;
                                bool flag9 = pass_time > 1f;
                                if (flag9)
                                {
                                    this.m_last_download_time = Time.time;
                                    this.m_download_speed = this.m_lase_update_size / pass_time;
                                    this.m_lase_update_size = 0f;
                                }
                            }
                        }
                        else
                        {
                            LogMgr.Log("download md5 error {0}", info.file_path);
                            bool random_state = info.random_state;
                            if (random_state)
                            {
                                this.m_stop_update_state = true;
                                string md5_error = GameConfig.Instance.GetValue("UpdateFileMd5ErrorMsg");
                                ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "UPDATE_RES_MD5_ERROR", md5_error);
                            }
                            else
                            {
                                LogMgr.Log("md5 check error {0}", info.file_path);
                                info.count = 0;
                                string[] array = info.http_str.Split(new char[]
                                {
                                    '?'
                                });
                                bool flag10 = array.Length != 0;
                                if (flag10)
                                {
                                    string new_httpstr = array[0] + "?v=" + Util.GetTimeStamp();
                                    info.http_str = new_httpstr;
                                    info.random_state = true;
                                    info.www = null;
                                    info.request = null;
                                    info.download_again = true;
                                    this.m_update_infos.Add(info);
                                }
                            }
                        }
                    }
                    else
                    {
                        info.www = null;
                        info.request = null;
                        info.download_again = true;
                        bool flag11 = info.count < 3;
                        if (flag11)
                        {
                            info.count++;
                            this.m_update_infos.Add(info);
                        }
                        else
                        {
                            info.count = 0;
                            this.m_update_infos.Add(info);
                        }
                        LogMgr.Log("download again {0}", info.file_path);
                    }
                };
                MemoryQuest memory_quest = new MemoryQuest();
                memory_quest.RelativePath = info.http_str;
                memory_quest.timeout = AppConst.UpdateFileTimeout;
                if (AppConst.UseUpdatOriModeReal)
                {
                    memory_quest.save_path = info.file_path;
                }
                if (info.download_again)
                {
                    memory_quest.timeout = AppConst.UpdateFileTimeout * 4;
                }
                if (AppConst.OpenDownloadLog)
                {
                    LogMgr.Log("res start update, res path:{0}", info.http_str);
                }
                ResRequest.Instance.RequestMemoryAsync(memory_quest, delay_func);
                if (memory_quest.Www != null)
                {
                    info.www = (memory_quest.Www as WWW);
                }
                else
                {
                    info.request = memory_quest.request;
                }
                this.m_loadind_map.Add(info.file_path, info);
            }
        }
        private float UpdateProcess()
        {
            float result;
            if (m_cur_state != UpdateState.Update_List)
            {
                result = 0f;
            }
            else
            {
                if (this.m_res_count == this.m_loaded_count || this.m_res_count == 0)
                {
                    this.m_cur_state = UpdateState.Complate;
                    result = 100f;
                }
                else
                {
                    float progress_current = (float)this.m_loaded_count;
                    foreach (UpdateInfo update_Info in this.m_loadind_map.Values)
                    {
                        if (update_Info.www == null)
                        {
                            progress_current += 0f;
                        }
                        else
                        {
                            if (update_Info.www.isDone && string.IsNullOrEmpty(update_Info.www.error))
                            {
                                progress_current += 1f;
                                this.m_lase_update_size += (float)update_Info.file_size;
                                this.m_cur_update_size += (float)update_Info.file_size;
                                if (this.m_last_download_time == 0f)
                                {
                                    this.m_download_speed = 100f;
                                    this.m_last_download_time = Time.time;
                                }
                                else
                                {
                                    float pass_time = Time.time - this.m_last_download_time;
                                    if (pass_time > 1f)
                                    {
                                        this.m_last_download_time = Time.time;
                                        this.m_download_speed = this.m_lase_update_size / pass_time;
                                        this.m_lase_update_size = 0f;
                                    }
                                }
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(update_Info.www.error))
                                {
                                    if (update_Info.www.progress >= 1f)
                                    {
                                        progress_current += 0.9f;
                                    }
                                    else
                                    {
                                        progress_current += update_Info.www.progress;
                                    }
                                }
                            }
                        }
                    }
                    this.m_download_process = progress_current / (float)this.m_res_count;
                    result = this.m_download_process;
                }
            }
            return result;
        }
        public void SetUpdateState(UpdateState state)
        {
            m_cur_state = state;
        }
        #endregion

        public bool IsRemoteFile(string file_name)
        {
            string pack_name = this.FindPackNameByFile(file_name);
            for (int index = 0; index < this.m_pack_list.Count; index++)
            {
                SqliteFilePack pack = this.m_pack_list[index];
                if (pack.m_pack_name == pack_name)
                {
                    return pack.ExistFile(file_name);
                }
            }
            return false;
        }
		public bool IsVersionFile(string abName)
		{
			string real_abname = Util.GetAssetsBundlePath(abName);
			string remote_ab = GetUnityBundlePath(real_abname);
			string remote_res = GetUnityAssetsPath(real_abname);
			return IsRemoteFile(remote_ab) || IsRemoteFile(remote_res);
		}
		public string FindPackNameByFile(string file_name)
		{
            for (int i = 0; i < this.m_pack_name.Count; i++)
            {
                string pack_name = this.m_pack_name[i];
                if(file_name.IndexOf(pack_name) != -1)
                {
                    return pack_name;
                }
                else if(file_name.IndexOf(this.m_unity_assets_path) != -1 && pack_name.IndexOf("res") != -1)
                {
                    return pack_name;
                }
                else if (file_name.IndexOf(this.m_unity_bundle_path) != -1 && pack_name.IndexOf("bundle") != -1)
                {
                    return pack_name;
                }
            }
            return "";
		}
		public bool FileNeedUpdate(string file_name, bool need_md5_check)
		{
            string pack_name = this.FindPackNameByFile(file_name);
			for (int index = 0; index < this.m_pack_list.Count; index++)
			{
				SqliteFilePack pack = this.m_pack_list[index];
				bool flag = pack.m_pack_name == pack_name;
				if (flag)
				{
					return pack.FileNeedUpdate(file_name, need_md5_check);
				}
			}
			return false;
		}
        private string GetUnityAssetsPath(string res_name)
        {
            return string.Format("{0}{1}", this.m_unity_assets_path, res_name);
        }
        public string GetUnityBundlePath(string res_name)
        {
            return string.Format("{0}{1}", this.m_unity_bundle_path, res_name);
        }
		public int GetFileVersion(string file_name)
		{
			string pack_name = this.FindPackNameByFile(file_name);
			for (int index = 0; index < this.m_pack_list.Count; index++)
			{
				SqliteFilePack pack = this.m_pack_list[index];
                if (pack.m_pack_name == pack_name)
				{
					return pack.GetFileVersion(file_name);
				}
			}
			return 0;
		}
		public string GetCurDownloadFileInfo()
		{
			return string.Format("{0} K/{1} K", this.m_cur_update_size, this.m_all_update_size);
		}
		public string GetCurDownloadSpeedInfo()
		{
			float show_value = this.m_download_speed;
			bool flag = this.m_download_speed > 1024f;
			string result;
			if (flag)
			{
				result = string.Format("{0} M/S", (this.m_download_speed / 1024f).ToString("f2"));
			}
			else
			{
				result = string.Format("{0} K/S", (int)show_value);
			}
			return result;
		}
		public bool RepalceInfoFromRemote(string pack_name, string file_name, string file_md5)
		{
			for (int index = 0; index < this.m_pack_list.Count; index++)
			{
				SqliteFilePack pack = this.m_pack_list[index];
				if (pack != null && pack.m_pack_name == pack_name)
				{
					return pack.RepalceInfoFromRemote(file_name, file_md5);
				}
			}
			return false;
		}

        #region -边玩边下
        public void StartCheckUpdatePack(string pack_name, LuaFunction func, int frame_check_count)
        {
            MonoHelper.StartCoroutine(CheckUpdateFileList(pack_name, func, frame_check_count));
        }
        private IEnumerator CheckUpdateFileList(string pack_name, LuaFunction func, int frame_check_count)
        {
            int num;
            for (int index = 0; index < m_pack_list.Count; index = num)
            {
                SqliteFilePack pack = m_pack_list[index];
                if (pack.m_pack_name == pack_name)
                {
                    yield return MonoHelper.StartCoroutine(pack.GetUpdateFileList(func, frame_check_count));
                }
                pack = null;
                num = index + 1;
            }
            yield break;
        }

        public bool RepalceInfoListFromRemote(string pack_name, List<string> file_list)
        {
            for (int index = 0; index < this.m_pack_list.Count; index++)
            {
                SqliteFilePack pack = this.m_pack_list[index];
                bool flag = pack != null && pack.m_pack_name == pack_name;
                if (flag)
                {
                    return pack.RepalceInfoListFromRemote(file_list);
                }
            }
            return false;
        }
        #endregion
    }
}
