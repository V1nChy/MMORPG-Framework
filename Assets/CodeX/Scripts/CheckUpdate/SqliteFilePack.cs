using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zlib;
using LuaInterface;
using UnityEngine;
using GFW;

namespace CodeX
{
    public enum PackResult
    {
        None,
        Success,
        Faild
    }

    public class DbFileInfo
    {
        public PathHashInfo hash_info;

        public int version;

        public string file_name;

        public string file_md5;

        public int data_type;

        public int data_len;

        public int zip_flag;

        public int unzip_len;

        public int crypto_flag;

        public int ctl_flag;
    }
	public class SqliteFilePack
	{
        private string m_real_db_file;
        private string m_remote_version_file;
        private string m_http_path;
        public string m_pack_name;
        private List<string> m_ignore_str = new List<string>();
        private bool m_zip_state = false;
        private string m_zip_dir = "";
        private string m_zip_ext = "";
        private bool m_game_no_update = false;
        private Dictionary<string, DbFileInfo> m_remote_version = new Dictionary<string, DbFileInfo>();
        private Sqlite3tool m_main_sqlite = null;//xxx_main.sydb
        private PackResult m_result = PackResult.None;

		public PackResult Result
		{
			get
			{
				return this.m_result;
			}
		}

        public SqliteFilePack(string http_path, string real_db_file, string remote_version_file, string pack_name)
        {
            m_http_path = http_path;//http://192.168.7.223/
            m_real_db_file = real_db_file;//patchs/xxx_main.sydb
            m_remote_version_file = remote_version_file;// patchs/xxx_head.cymzq
            m_pack_name = pack_name;
        }

        public void InitSqlite(bool update_version)
        {
            string res_work_path = AppUtil.DataPath;// c:/luaframework/
            string http_str = m_http_path + m_remote_version_file;//http://192.168.7.223/patchs/xxx_head.cymzq
            RuntimePlatform platform = Application.platform;
            if (platform == RuntimePlatform.IPhonePlayer)
            {
                http_str = http_str.Replace("patchs", "patchsIos");
            }
            else if (platform == RuntimePlatform.Android)
            {
                http_str = http_str.Replace("patchs", "patchsAndroid");
            }

            string path = res_work_path + m_remote_version_file;//c:/luaframework/patchs/xxx_head.cymzq
            m_main_sqlite = new Sqlite3tool();
            string real_db_path = res_work_path + m_real_db_file;//c:/luaframework/patchs/xxx_main.sydb
            if (m_main_sqlite.OpenOrCreateDb(real_db_path))
            {
                Debug.Log("SqliteFilePack@InitSqlite: OpenOrCreateDb succeed");
                if (update_version)
                {
                    MemoryLoadCallbackFunc delay_func = delegate (bool is_suc, byte[] buffer)
                    {
                        if (is_suc)
                        {
                            this.Log("PackResult.Success");
                            Pathtool.DeleteToFile(path);
                            bool flag2 = Pathtool.SaveDataToFile(path, buffer);
                            if (flag2)
                            {
                                Sqlite3tool.OpenCyMzq(path, ref this.m_remote_version);
                                this.m_result = PackResult.Success;
                                this.Log("OpenCyMzq " + path);
                            }
                        }
                        else
                        {
                            this.Log("PackResult.Faild");
                            this.m_result = PackResult.Faild;
                            LogManager.LogError(string.Format("init sqlite error {0}", path));
                        }
                    };
                    MemoryQuest memory_quest = new MemoryQuest();
                    memory_quest.RelativePath = http_str + "?v=" + Util.GetTimeStamp();
                    this.Log("download file:" + memory_quest.RelativePath);
                    ResRequest.Instance.RequestMemoryAsync(memory_quest, delay_func);
                }
                else
                {
                    Sqlite3tool.OpenCyMzq(path, ref this.m_remote_version);
                    m_result = PackResult.Success;
                    Debug.Log("OpenCyMzq " + path);
                }
            }
            else
            {
                m_main_sqlite.CloseDb();
                Debug.Log("OpenOrCreateDb error");
            }
        }

        public float CheckUpdateList(List<UpdateInfo> update_infos)
        {
            if (m_main_sqlite != null && !AppConst.IgnoreUpdateState)
            {
                #region -检查所有文件的version，md5，移除本地db中远程版本没有的文件
                string res_work_path = AppUtil.DataPath;//c:/luaframework/
                List<DbFileInfo> delete_infolist = m_main_sqlite.CacheFileList();
                int index = 0;
                while (index < delete_infolist.Count)
                {
                    DbFileInfo old_dbinfo = delete_infolist[index];
                    DbFileInfo new_dbInfo;
                    if (this.m_remote_version.TryGetValue(old_dbinfo.file_name, out new_dbInfo))
                    {
                        bool is_need_new = false;
                        string file_path = new_dbInfo.file_name;
                        if (m_ignore_str.Count != 0)
                        {
                            foreach (string item in m_ignore_str)
                            {
                                file_path = file_path.Replace(item, "");
                            }
                        }
                        string full_path = res_work_path + file_path;
                        if (new_dbInfo.version != old_dbinfo.version)
                        {
                            is_need_new = true;
                            if (AppConst.OpenDownloadLog)
                            {
                                Debug.Log(string.Format("res version is not new , need updata::{0},{1},{2},", full_path, new_dbInfo.version, old_dbinfo.version));
                            }
                        }
                        else
                        {
                            if (!Util.CheckMd5(full_path, new_dbInfo.file_md5, false))
                            {
                                if (this.m_zip_state && Util.AssetBundleExists(full_path))
                                {
                                    is_need_new = false;
                                }
                                else
                                {
                                    is_need_new = true;
                                    if (AppConst.OpenDownloadLog)
                                    {
                                        Debug.Log(string.Format("res md5 is not new , need updata::{0},{1},", full_path, new_dbInfo.file_md5));
                                    }
                                }
                            }
                        }
                        if (!is_need_new)
                        {
                            this.m_remote_version.Remove(old_dbinfo.file_name);
                        }
                        delete_infolist.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
                DeleteFileInfoList(delete_infolist);//移除本地db中远程版本没有的文件
                #endregion

                #region -处理剩下需要更新的必要资源信息
                float update_size = 0f;
                foreach (KeyValuePair<string, DbFileInfo> item in this.m_remote_version)
                {
                    DbFileInfo info = item.Value;
                    string local_file_name = info.file_name;
                    if (this.m_ignore_str.Count != 0)
                    {
                        foreach (string str in this.m_ignore_str)
                        {
                            local_file_name = local_file_name.Replace(str, "");
                        }
                    }
                    string path = res_work_path + local_file_name;
                    UpdateInfo update_info = new UpdateInfo();
                    int file_version = ResUpdateManager.Instance.GetFileVersion(info.file_name);
                    string file_name = info.file_name;
                    if (this.m_zip_dir.Equals(""))
                    {
                        file_name = file_name.Remove(0, 1);
                    }
                    update_info.http_str = string.Concat(new object[]
					{
						this.m_http_path,
						this.m_zip_dir,
						file_name,
						this.m_zip_ext,
						"?v=",
						file_version
					});
                    update_info.file_path = path;
                    update_info.count = 0;
                    update_info.func = delegate(byte[] buffer)
                    {
                        Pathtool.DeleteToFile(path);
                        byte[] new_data = buffer;
                        if (this.m_zip_state)
                        {
                            new_data = ZlibStream.UncompressBuffer(buffer);
                        }
                        bool result2;
                        if (Pathtool.SaveDataToFile(path, new_data))
                        {
                            string md5;
                            if (this.m_zip_state)
                            {
                                md5 = Md5Helper.Md5Buffer(buffer);
                            }
                            else
                            {
                                md5 = Md5Helper.Md5Buffer(new_data);
                            }
                            if (!info.file_md5.Equals(md5.Remove(8)))
                            {
                                result2 = false;
                            }
                            else
                            {
                                info.file_md5 = md5;
                                this.m_main_sqlite.ReplaceFileInfoToDb(info);
                                result2 = true;
                            }
                        }
                        else
                        {
                            result2 = false;
                        }
                        return result2;
                    };

                    int file_size;
                    if (this.m_zip_state)
                    {
                        file_size = info.data_len / 10 / 1024;
                    }
                    else
                    {
                        file_size = info.data_len / 1024;
                    }
                    update_info.file_size = file_size;
                    update_infos.Add(update_info);
                    update_size += (float)file_size;
                }
                #endregion
                return update_size;
            }
            return 0;
        }

        public void SetExtInfo(bool zip_state, string zip_dir, string zip_ext)
        {
            this.m_zip_state = zip_state;
            this.m_zip_dir = zip_dir;//pack
            this.m_zip_ext = zip_ext;//.jyconfig
        }
		public void SetGameNoUpdateState(bool state)
		{
			this.m_game_no_update = state;
		}
		public void AddIgnoreStr(string value)
		{
			this.m_ignore_str.Add(value);
		}

		public string GetPackName()
		{
			return this.m_pack_name;
		}

		public void OpenGameVersion()
		{
			string path = AppUtil.DataPath + this.m_remote_version_file;
			this.m_remote_version.Clear();
			Sqlite3tool.OpenCyMzq(path, ref this.m_remote_version);
		}

		public void CloseGameDataBase()
		{
			bool flag = this.m_main_sqlite != null;
			if (flag)
			{
				this.m_main_sqlite.CloseDb();
				this.m_main_sqlite = null;
			}
		}

		public bool RepalceInfoFromRemote(string file_info, string file_md5)
		{
			DbFileInfo remote_dbinfo = new DbFileInfo();
			if (this.m_remote_version.TryGetValue(file_info, out remote_dbinfo))
			{
				if (file_md5 == null || remote_dbinfo.file_md5.Equals(file_md5.Remove(8)))
				{
					this.m_main_sqlite.ReplaceFileInfoToDb(remote_dbinfo);
					return true;
				}
			}
			return false;
		}

		public IEnumerator GetUpdateFileList(LuaFunction func, int frame_check_count)
		{
			uint update_size = 0u;
			string res_work_path = AppUtil.DataPath;
			int index = 0;
			List<DbFileInfo> local_infolist = this.m_main_sqlite.CacheFileList();
			List<string> res_list = new List<string>();
			foreach (KeyValuePair<string, DbFileInfo> item in this.m_remote_version)
			{
				res_list.Add(item.Key);
				update_size = (uint)((ulong)update_size + (ulong)((long)(item.Value.data_len / 1024)));
			}
			Dictionary<string, DbFileInfo>.Enumerator enumerator = default(Dictionary<string, DbFileInfo>.Enumerator);
			string ingore_str = GameConfig.Instance.GetValue("UnityBundle");
            if (Application.platform == RuntimePlatform.Android)
			{
				ingore_str = GameConfig.Instance.GetValue("AndroidBundle");
			}
			else
			{
                if (Application.platform == RuntimePlatform.IPhonePlayer)
				{
					ingore_str = GameConfig.Instance.GetValue("IosBundle");
				}
			}
			StringBuilder temp = new StringBuilder();
			ResourceManager res_mgr = GameSystem.Instance.GetManager<ResourceManager>();
			int num;
			for (int ix = 0; ix < local_infolist.Count; ix = num + 1)
			{
				DbFileInfo local_dbinfo = local_infolist[ix];
				DbFileInfo remote_dbInfo;
				bool flag3 = this.m_remote_version.TryGetValue(local_dbinfo.file_name, out remote_dbInfo);
				if (flag3)
				{
					bool flag4 = remote_dbInfo.version == local_dbinfo.version;
					if (flag4)
					{
						bool flag5 = res_mgr.ExistDownloadHistory(local_dbinfo.file_name);
						if (flag5)
						{
							res_list.Remove(local_dbinfo.file_name);
							update_size = (uint)((ulong)update_size - (ulong)((long)(remote_dbInfo.data_len / 1024)));
						}
					}
				}
				num = index;
				index = num + 1;
				bool flag6 = index > frame_check_count;
				if (flag6)
				{
					index = 0;
					yield return new WaitForEndOfFrame();
				}
				num = ix;
			}
			bool flag7 = func != null;
			if (flag7)
			{
				func.Call(new object[]
				{
					update_size,
					res_list.Count,
					res_list.ToArray()
				});
				func.Dispose();
				func = null;
			}
			res_list = null;
			yield break;
		}

		private void DeleteFileInfoList(List<DbFileInfo> delete_infolist)
		{
			for (int index = 0; index < delete_infolist.Count; index++)
			{
				this.m_main_sqlite.DeleteFileInfo(delete_infolist[index].hash_info);
				string file_local_path = AppUtil.DataPath + delete_infolist[index].file_name;
                if (Util.AssetBundleExists(file_local_path))
				{
				}
			}
		}

		public bool ExistFile(string file_name)
		{
			DbFileInfo new_dbInfo;
			return this.m_remote_version.TryGetValue(file_name, out new_dbInfo);
		}

		public int GetFileVersion(string file_name)
		{
			DbFileInfo dbInfo;
			bool flag = this.m_remote_version.TryGetValue(file_name, out dbInfo);
			int result;
			if (flag)
			{
				result = dbInfo.version;
			}
			else
			{
				result = 0;
			}
			return result;
		}

		public bool FileNeedUpdate(string file_name, bool need_md5_check)
		{
			bool is_need_new = false;
            if (!this.m_game_no_update)
            {
				DbFileInfo new_dbInfo;
				if (this.m_remote_version.TryGetValue(file_name, out new_dbInfo))
				{
					string file_path = new_dbInfo.file_name;
					if (this.m_ignore_str.Count != 0)
					{
						foreach (string item in this.m_ignore_str)
						{
							file_path = file_path.Replace(item, "");
						}
					}
					string res_path = AppUtil.DataPath + file_path;
					if (need_md5_check)
					{
						if (!Util.CheckMd5(res_path, new_dbInfo.file_md5, false))
						{
							is_need_new = true;
						}
					}
					else
					{
						if (!Util.AssetBundleExists(res_path))
						{
							is_need_new = true;
						}
						else
						{
							int file_version = 0;
							bool ret = this.m_main_sqlite.QueryFileVersionByHashInfo(file_name, out file_version);
							if (ret && file_version != new_dbInfo.version)
							{
								is_need_new = true;
							}
						}
					}
				}
			}
			return is_need_new;
		}

        public bool RepalceInfoListFromRemote(List<string> file_list)
        {
            List<DbFileInfo> info_list = new List<DbFileInfo>();
            for (int i = 0; i < file_list.Count; i++)
            {
                DbFileInfo remote_dbinfo = new DbFileInfo();
                bool flag = this.m_remote_version.TryGetValue(file_list[i], out remote_dbinfo);
                if (flag)
                {
                    info_list.Add(remote_dbinfo);
                }
            }
            bool flag2 = info_list.Count > 0;
            return flag2 && this.m_main_sqlite.ReplaceFileInfoListToDb(info_list);
        }
    }
}
