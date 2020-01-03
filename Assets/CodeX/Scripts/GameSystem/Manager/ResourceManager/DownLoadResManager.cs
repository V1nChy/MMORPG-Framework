using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using GFW;
using GFW.ManagerSystem;
using LuaInterface;
using UnityEngine;

namespace CodeX
{
    public class DownloadInfo
    {
        public uint type;

        public int request_times = 0;

        public string server_asset_path = "";

        public string local_asset_path = "";

        public Action<DownLoadData> sharpCallback = null;

        public int outtime = 18000;
    }

    public class DownLoadData
    {
        public DownLoadData(DownloadInfo param)
        {
            this.evParam = param;
        }
        public DownloadInfo evParam;
    }

    public class DownLoadResManager : Manager
    {
        private WebClient m_client = null;
        private Thread m_normal_thread;
        private static Queue<DownloadInfo> m_normal_list = new Queue<DownloadInfo>();
        private List<string> m_version_file_list = new List<string>();
        private ProgressEventProxy m_pro_event_proxy = null;
        private LuaFunction m_lua_func = null;

        public bool m_normal_download_state = false;
        public string m_current_res = null;
        public int m_cur_download_size = 0;
        public bool m_wait_callback = false;
        public bool m_cur_download_state = false;
        public float m_start_download_time = 0f;
        public float m_dowmload_timeout = 20f;
        public bool m_start_res_download_outtime_check = true;
        public bool m_open_delay_write_version_mode = true;
        public bool m_init_thread = false;

        public void AddEvent(DownloadInfo ev)
        {
            Queue<DownloadInfo> normal_list = DownLoadResManager.m_normal_list;
            lock (normal_list)
            {
                bool flag2 = DownLoadResManager.m_normal_list.Count == 0;
                if (flag2)
                {
                    this.m_start_download_time = Time.time;
                }
                DownLoadResManager.m_normal_list.Enqueue(ev);
            }
            bool flag3 = !this.m_init_thread;
            if (flag3)
            {
                this.m_normal_thread = new Thread(new ThreadStart(this.OnUpdateNormal));
                this.m_normal_thread.Priority = System.Threading.ThreadPriority.BelowNormal;
                this.m_pro_event_proxy = new ProgressEventProxy();
                this.m_pro_event_proxy.progressMethod = new Action<DownloadInfo, DownloadProgressChangedEventArgs>(this.ProgressChanged);
                this.m_pro_event_proxy.completeMethod = new Action<DownloadInfo, AsyncCompletedEventArgs>(this.DownloadFileCompleted);
                this.m_normal_thread.Start();
                this.m_init_thread = true;
            }
        }

        public void SetDownloadFileFunc(LuaFunction func)
        {
            this.m_lua_func = func;
        }

        private void OnSyncEvent(DownLoadData data, bool state = true)
        {
            bool flag = data != null && data.evParam != null && data.evParam.sharpCallback != null;
            if (flag)
            {
                data.evParam.sharpCallback(data);
            }
            this.m_cur_download_state = state;
            this.m_wait_callback = true;
        }

        public void Update()
        {
            bool start_res_download_outtime_check = this.m_start_res_download_outtime_check;
            if (start_res_download_outtime_check)
            {
                bool flag = this.m_normal_download_state && this.m_start_download_time == 0f;
                if (flag)
                {
                    this.m_start_download_time = Time.time;
                }
                bool flag2 = this.m_start_download_time > 0f && Time.time - this.m_start_download_time > this.m_dowmload_timeout && (DownLoadResManager.m_normal_list.Count > 0 || this.m_current_res != null);
                if (flag2)
                {
                    string file_name = Util.GetAssetsBundlePathFromBase64(this.m_current_res);
                    string error_msg = "auto download res timeout:" + this.m_current_res + " raw filw:" + file_name;
                    Util.ThrowLuaException(error_msg, null, 1);
                    this.CancelDownload();
                }
            }
            bool wait_callback = this.m_wait_callback;
            if (wait_callback)
            {
                bool flag3 = this.m_lua_func != null;
                if (flag3)
                {
                    this.m_lua_func.Call(new object[]
                    {
                        this.m_cur_download_state,
                        this.m_cur_download_size
                    });
                }
                bool flag4 = this.m_cur_download_state && this.m_current_res != null;
                if (flag4)
                {
                    bool open_delay_write_version_mode = this.m_open_delay_write_version_mode;
                    if (open_delay_write_version_mode)
                    {
                        this.m_version_file_list.Add(this.m_current_res);
                    }
                    else
                    {
                        base.StartCoroutine(this.WriteVersionFile(this.m_current_res));
                    }
                }
                this.m_wait_callback = false;
                this.m_cur_download_state = false;
                this.m_cur_download_size = 0;
                this.m_current_res = null;
                this.m_normal_download_state = false;
            }
        }

        public void AutoWriteAllVersionFile()
        {
            bool flag = !this.m_open_delay_write_version_mode;
            if (!flag)
            {
                try
                {
                    this.WriteVersionFileList(this.m_version_file_list.Count);
                }
                catch (Exception e)
                {
                    string msg = "no error";
                    bool flag2 = e != null && e.Message != null;
                    if (flag2)
                    {
                        msg = e.Message;
                    }
                    Debug.LogWarning("AutoWriteAllVersionFile:" + msg);
                }
            }
        }

        public void AutoWriteVersionFile(int count)
        {
            bool flag = !this.m_open_delay_write_version_mode;
            if (!flag)
            {
                int res_count = this.m_version_file_list.Count;
                bool flag2 = res_count > count;
                if (flag2)
                {
                    res_count = count;
                }
                bool flag3 = res_count > 0;
                if (flag3)
                {
                    base.StartCoroutine(this.AutoWriteAsyncVersionFile(res_count));
                }
                bool flag4 = count == 9999;
                if (flag4)
                {
                    this.OnDestroy();
                }
            }
        }

        public IEnumerator AutoWriteAsyncVersionFile(int count)
        {
            this.WriteVersionFileList(count);
            yield break;
        }

        private void WriteVersionFileList(int res_count)
        {
            bool flag = res_count <= 0;
            if (!flag)
            {
                List<string> write_list = new List<string>();
                for (int i = 0; i < res_count; i++)
                {
                    string current_res = this.m_version_file_list[0];
                    this.m_version_file_list.RemoveAt(0);
                    string file_name = current_res.Replace(AppConst.CdnUrl, "/");
                    string[] array = file_name.Split(new char[]
                    {
                        '?'
                    });
                    bool flag2 = array.Length != 0;
                    if (flag2)
                    {
                        string real_name = array[0];
                        bool flag3 = !GameSystem.Instance.GetManager<ResourceManager>().ExistDownloadHistory(real_name);
                        if (flag3)
                        {
                            GameSystem.Instance.GetManager<ResourceManager>().AddDownloadHistory(file_name);
                            write_list.Add(real_name);
                        }
                    }
                }
                bool flag4 = write_list.Count > 0 && AppConst.OpenNoneImportResVersionSave;
                if (flag4)
                {
                    bool sqlite_succeed = ResUpdateManager.Instance.RepalceInfoListFromRemote("unitybundle", write_list);
                    bool flag5 = !sqlite_succeed;
                    if (flag5)
                    {
                        Util.ThrowLuaException("DownloadFileCompleted sqlite save error", null, 1);
                    }
                }
            }
        }

        private IEnumerator WriteVersionFile(string current_res)
        {
            string file_name = current_res.Replace(AppConst.CdnUrl, "/");
            string[] array = file_name.Split(new char[]
            {
                '?'
            });
            bool flag = array.Length != 0;
            if (flag)
            {
                string real_name = array[0];
                bool flag2 = !GameSystem.Instance.GetManager<ResourceManager>().ExistDownloadHistory(real_name);
                if (flag2)
                {
                    GameSystem.Instance.GetManager<ResourceManager>().AddDownloadHistory(file_name);
                    bool openNoneImportResVersionSave = AppConst.OpenNoneImportResVersionSave;
                    if (openNoneImportResVersionSave)
                    {
                        bool sqlite_succeed = ResUpdateManager.Instance.RepalceInfoFromRemote("unitybundle", real_name, null);
                        bool flag3 = !sqlite_succeed;
                        if (flag3)
                        {
                            Util.ThrowLuaException("DownloadFileCompleted sqlite save error:" + real_name, null, 1);
                        }
                    }
                }
                real_name = null;
            }
            yield break;
        }

        public int GetDownloadListCount()
        {
            return DownLoadResManager.m_normal_list.Count;
        }

        public int GetVersionFileListCount()
        {
            return this.m_version_file_list.Count;
        }

        private void OnUpdateNormal()
        {
            for (; ; )
            {
                bool normal_download_state = this.m_normal_download_state;
                if (normal_download_state)
                {
                    bool flag = Application.platform == RuntimePlatform.IPhonePlayer;
                    if (flag)
                    {
                        Thread.Sleep(1);
                    }
                }
                else
                {
                    try
                    {
                        bool flag2 = DownLoadResManager.m_normal_list.Count > 0;
                        if (flag2)
                        {
                            Queue<DownloadInfo> normal_list = DownLoadResManager.m_normal_list;
                            lock (normal_list)
                            {
                                DownloadInfo e = DownLoadResManager.m_normal_list.Dequeue();
                                this.OnDownloadFile(ref e);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError(ex.Message);
                    }
                    Thread.Sleep(1);
                }
            }
        }

        public void OnDownloadFile(ref DownloadInfo downloadInfo)
        {
            bool flag = downloadInfo == null;
            if (!flag)
            {
                try
                {
                    bool flag2 = GameSystem.Instance.GetManager<ResourceManager>().ExistDownloadHistory(downloadInfo.local_asset_path);
                    if (flag2)
                    {
                        this.CancelDownload();
                    }
                    else
                    {
                        this.m_current_res = downloadInfo.server_asset_path;
                        this.m_normal_download_state = true;
                        this.m_pro_event_proxy.downloadInfo = downloadInfo;
                        bool flag3 = this.m_client == null || this.m_client.IsBusy;
                        if (flag3)
                        {
                            bool flag4 = this.m_client != null;
                            if (flag4)
                            {
                                this.m_client.Dispose();
                            }
                            this.m_client = new WebClient();
                            this.m_client.DownloadFileCompleted += this.m_pro_event_proxy.DownloadFileCompleted;
                            this.m_client.DownloadProgressChanged += this.m_pro_event_proxy.OnProgrssChanged;
                        }
                        string save_file = downloadInfo.local_asset_path;
                        this.m_client.DownloadFileAsync(new Uri(this.m_current_res), save_file);
                    }
                }
                catch (Exception e)
                {
                    this.CancelDownload();
                    string file_name = Util.GetAssetsBundlePathFromBase64(downloadInfo.server_asset_path);
                    Util.ThrowLuaException(string.Concat(new string[]
                    {
                        "auto download res error, file name:",
                        downloadInfo.server_asset_path,
                        " error:",
                        e.Message,
                        " raw file:",
                        file_name
                    }), null, 1);
                }
            }
        }

        public void CheckDownloadFile(string res_name)
        {
            if (this.m_current_res != null && this.m_current_res.Contains(res_name))
            {
                this.CancelDownload();
            }
        }

        public void CancelDownload()
        {
            if (this.m_client != null)
            {
                this.m_client.Dispose();
                this.m_client = null;
            }
            this.m_normal_download_state = false;
            this.m_current_res = null;
            this.m_start_download_time = 0f;
            this.m_cur_download_state = true;
            this.m_wait_callback = true;
        }

        private void DownloadFileCompleted(DownloadInfo downloadInfo, AsyncCompletedEventArgs e)
        {
            this.m_start_download_time = 0f;
            bool download_succeed = true;
            bool flag = e != null && e.Error != null;
            if (flag)
            {
                download_succeed = false;
                bool flag2 = this.m_client != null;
                if (flag2)
                {
                    this.m_client.Dispose();
                    this.m_client = null;
                }
                string file_name = Util.GetAssetsBundlePathFromBase64(downloadInfo.server_asset_path);
                Util.ThrowLuaException(string.Concat(new object[]
                {
                    "DownloadFileCompleted download error:",
                    e.Error,
                    " file name:",
                    file_name,
                    " url:",
                    downloadInfo.server_asset_path
                }), null, 1);
            }
            DownLoadData data = new DownLoadData(downloadInfo);
            this.OnSyncEvent(data, download_succeed);
        }

        private void ProgressChanged(DownloadInfo downloadInfo, DownloadProgressChangedEventArgs e)
        {
            bool flag = this.m_lua_func != null;
            if (flag)
            {
                this.m_cur_download_size = (int)(e.BytesReceived / 1024L);
            }
        }

        private void OnDestroy()
        {
            bool flag = this.m_client != null;
            if (flag)
            {
                this.m_client.CancelAsync();
                this.m_client.Dispose();
                this.m_client = null;
            }
            bool flag2 = this.m_normal_thread != null;
            if (flag2)
            {
                this.m_normal_thread.Abort();
                this.m_normal_thread = null;
            }
        }
    }
}
