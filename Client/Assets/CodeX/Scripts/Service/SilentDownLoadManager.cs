using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using GFW;
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

        public Action<NotiData> sharpCallback = null;

        public int outtime = 18000;
    }

    public class NotiData
    {
        public DownloadInfo evParam;
        public NotiData(DownloadInfo param)
        {
            this.evParam = param;
        }
    }

    public class ProgressEventProxy
    {
        public DownloadInfo downloadInfo;
        public Action<DownloadInfo, DownloadProgressChangedEventArgs> progressMethod;
        public Action<DownloadInfo, AsyncCompletedEventArgs> completeMethod;

        public void OnProgrssChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.progressMethod(this.downloadInfo, e);
        }

        public void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.completeMethod(this.downloadInfo, e);
        }
    }

    /// <summary>
    /// 资源静默下载器
    /// </summary>
    public class SilentDownLoadManager : ServiceModule<SilentDownLoadManager>
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
            Queue<DownloadInfo> normal_list = m_normal_list;
            lock (normal_list)
            {
                if (m_normal_list.Count == 0)
                {
                    this.m_start_download_time = Time.time;
                }
                m_normal_list.Enqueue(ev);
            }
            if (!this.m_init_thread)
            {
                this.m_pro_event_proxy = new ProgressEventProxy();
                this.m_pro_event_proxy.progressMethod = new Action<DownloadInfo, DownloadProgressChangedEventArgs>(this.ProgressChanged);
                this.m_pro_event_proxy.completeMethod = new Action<DownloadInfo, AsyncCompletedEventArgs>(this.DownloadFileCompleted);

                this.m_normal_thread = new Thread(new ThreadStart(this.OnUpdateNormal));
                this.m_normal_thread.Priority = System.Threading.ThreadPriority.BelowNormal;
                this.m_normal_thread.Start();
                this.m_init_thread = true;
            }
        }

        private void OnUpdateNormal()
        {
            while (true)
            {
                bool normal_download_state = this.m_normal_download_state;
                if (normal_download_state)
                {
                    if (Application.platform == RuntimePlatform.IPhonePlayer)
                    {
                        Thread.Sleep(1);
                    }
                }
                else
                {
                    try
                    {
                        if (m_normal_list.Count > 0)
                        {
                            Queue<DownloadInfo> normal_list = m_normal_list;
                            lock (normal_list)
                            {
                                DownloadInfo e = m_normal_list.Dequeue();
                                this.OnDownloadFile(ref e);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMgr.LogError(ex.Message);
                    }
                    Thread.Sleep(1);
                }
            }
        }

        public void OnDownloadFile(ref DownloadInfo downloadInfo)
        {
            if (downloadInfo != null)
            {
                try
                {
                    if (DownloadUpdateHistory.Instance.ExistDownloadHistory(downloadInfo.local_asset_path))
                    {
                        this.CancelDownload();
                    }
                    else
                    {
                        this.m_normal_download_state = true;
                        this.m_current_res = downloadInfo.server_asset_path;
                        this.m_pro_event_proxy.downloadInfo = downloadInfo;
                        if (this.m_client == null || this.m_client.IsBusy)
                        {
                            if (this.m_client != null)
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

        private void ProgressChanged(DownloadInfo downloadInfo, DownloadProgressChangedEventArgs e)
        {
            if (this.m_lua_func != null)
            {
                this.m_cur_download_size = (int)(e.BytesReceived / 1024L);
            }
        }

        private void DownloadFileCompleted(DownloadInfo downloadInfo, AsyncCompletedEventArgs e)
        {
            this.m_start_download_time = 0f;
            bool download_succeed = true;
            if (e != null && e.Error != null)
            {
                download_succeed = false;
                if (this.m_client != null)
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
            NotiData data = new NotiData(downloadInfo);
            this.OnSyncEvent(data, download_succeed);
        }

        private void OnSyncEvent(NotiData data, bool state = true)
        {
            if (data != null && data.evParam != null && data.evParam.sharpCallback != null)
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
                bool flag2 = this.m_start_download_time > 0f && Time.time - this.m_start_download_time > this.m_dowmload_timeout && (m_normal_list.Count > 0 || this.m_current_res != null);
                if (flag2)
                {
                    string file_name = Util.GetAssetsBundlePathFromBase64(this.m_current_res);
                    string error_msg = "auto download res timeout:" + this.m_current_res + " raw filw:" + file_name;
                    Util.ThrowLuaException(error_msg, null, 1);
                    this.CancelDownload();
                }
            }

            if (this.m_wait_callback)
            {
                if (this.m_lua_func != null)
                {
                    this.m_lua_func.Call(new object[]
                    {
                        this.m_cur_download_state,
                        this.m_cur_download_size
                    });
                }

                if (this.m_cur_download_state && this.m_current_res != null)
                {
                    if (this.m_open_delay_write_version_mode)
                    {
                        this.m_version_file_list.Add(this.m_current_res);
                    }
                    else
                    {
                        MonoHelper.StartCoroutine(this.WriteVersionFile(this.m_current_res));
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
            if (this.m_open_delay_write_version_mode)
            {
                try
                {
                    this.WriteVersionFileList(this.m_version_file_list.Count);
                }
                catch (Exception e)
                {
                    string msg = "no error";
                    if (e != null && e.Message != null)
                    {
                        msg = e.Message;
                    }
                    Debug.LogWarning("AutoWriteAllVersionFile:" + msg);
                }
            }
        }

        public void AutoWriteVersionFile(int count)
        {
            if (this.m_open_delay_write_version_mode)
            {
                int res_count = this.m_version_file_list.Count;
                if (res_count > count)
                {
                    res_count = count;
                }

                if (res_count > 0)
                {
                    MonoHelper.StartCoroutine(this.AutoWriteAsyncVersionFile(res_count));
                }

                if (count == 9999)
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
            if (res_count > 0)
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

                    if (array.Length != 0)
                    {
                        string real_name = array[0];
                        if (!DownloadUpdateHistory.Instance.ExistDownloadHistory(real_name))
                        {
                            DownloadUpdateHistory.Instance.AddDownloadHistory(file_name);
                            write_list.Add(real_name);
                        }
                    }
                }

                if (write_list.Count > 0 && AppConst.OpenNoneImportResVersionSave)
                {
                    bool sqlite_succeed = ResUpdateManager.Instance.RepalceInfoListFromRemote("unitybundle", write_list);
                    if (!sqlite_succeed)
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
            if (array.Length != 0)
            {
                string real_name = array[0];
                if (!DownloadUpdateHistory.Instance.ExistDownloadHistory(real_name))
                {
                    DownloadUpdateHistory.Instance.AddDownloadHistory(file_name);
                    if (AppConst.OpenNoneImportResVersionSave)
                    {
                        bool sqlite_succeed = ResUpdateManager.Instance.RepalceInfoFromRemote("unitybundle", real_name, null);
                        if (!sqlite_succeed)
                        {
                            Util.ThrowLuaException("DownloadFileCompleted sqlite save error:" + real_name, null, 1);
                        }
                    }
                }
                real_name = null;
            }
            yield break;
        }

        public void SetDownloadFileFunc(LuaFunction func)
        {
            this.m_lua_func = func;
        }

        public int GetDownloadListCount()
        {
            return m_normal_list.Count;
        }

        public int GetVersionFileListCount()
        {
            return this.m_version_file_list.Count;
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

        /// <summary>
        /// 检查是否正在下载某资源，取消当前下载
        /// </summary>
        public void CheckDownloadingAndCancel(string res_name)
        {
            if (this.m_current_res != null && this.m_current_res.Contains(res_name))
            {
                this.CancelDownload();
            }
        }

        private void OnDestroy()
        {
            if (this.m_client != null)
            {
                this.m_client.CancelAsync();
                this.m_client.Dispose();
                this.m_client = null;
            }
            if (this.m_normal_thread != null)
            {
                this.m_normal_thread.Abort();
                this.m_normal_thread = null;
            }
        }
    }
}
