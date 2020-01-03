using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using GFW;

namespace CodeX
{
    public delegate void MemoryLoadCallbackFunc(bool is_suc, byte[] buffer);

    public class MemoryQuest
    {
        public string RelativePath;
        public object Www;
        public UnityWebRequest request;
        public int timeout;
        public string save_path;
    }

    internal class DownloadData
    {
        public MemoryQuest memory_quest;
        public MemoryLoadCallbackFunc callback;
        public bool IsFile;
    }

    internal class ProgressEventProxyUpdate
    {
        public MemoryLoadCallbackFunc CallBack;
        public Action<MemoryLoadCallbackFunc, AsyncCompletedEventArgs> FileCompleteMethod;
        public Action<MemoryLoadCallbackFunc, DownloadDataCompletedEventArgs> DataCompleteMethod;

        public void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.FileCompleteMethod(this.CallBack, e);
        }
        public void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            this.DataCompleteMethod(this.CallBack, e);
        }
    }

    internal class ResRequest : Singleton<ResRequest>
	{
        public delegate void ResLoadCallbackFunc(bool result, byte[] buffer);

        private Thread m_thread = null;
        private bool m_download_state = false;
        private static Queue<DownloadData> m_request_list = new Queue<DownloadData>();

        public void RequestMemoryAsync(MemoryQuest memory_quest, MemoryLoadCallbackFunc callback)
        {
            if (AppConst.UseUpdatOriModeReal && AppConst.UseUpdatOriThreadMode)
            {
                if (this.m_thread == null)
                {
                    this.m_thread = new Thread(new ThreadStart(this.OnThreadUpdate));
                    this.m_thread.Start();
                }
                DownloadData data = new DownloadData();
                data.callback = callback;
                data.memory_quest = memory_quest;
                //如果没有传save_path，则默认是下载byte[]数据
                if (string.IsNullOrEmpty(memory_quest.save_path))
                    data.IsFile = false;
                else
                    data.IsFile = true;
                Queue<DownloadData> request_list = ResRequest.m_request_list;
                lock (request_list)
                {
                    ResRequest.m_request_list.Enqueue(data);
                }
            }
            else
            {
                MonoHelper.StartCoroutine(this.LoadToMemoryAsyncImpl(memory_quest, callback));
            }
        }

        public void OnThreadUpdate()
        {
            for (; ; )
            {
                if (this.m_download_state)
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
                        if (m_request_list.Count > 0)
                        {
                            Queue<DownloadData> request_list = m_request_list;
                            lock (request_list)
                            {
                                DownloadData e = ResRequest.m_request_list.Dequeue();
                                if(e.IsFile)
                                {
                                    this.OnTreadDownloadFile(ref e);
                                }
                                else
                                {
                                    this.OnTreadDownloadData(ref e);
                                }
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

        private void OnTreadDownloadFile(ref DownloadData downloadInfo)
        {
            this.m_download_state = true;
            ProgressEventProxyUpdate pro_event_proxy = new ProgressEventProxyUpdate();
            pro_event_proxy.FileCompleteMethod = new Action<MemoryLoadCallbackFunc, AsyncCompletedEventArgs>(this.DownloadFileCompleted);
            pro_event_proxy.CallBack = downloadInfo.callback;
            using (WebClient client = new WebClient())
            {
                client.DownloadFileCompleted += pro_event_proxy.DownloadFileCompleted;
                string save_path = downloadInfo.memory_quest.save_path;
                try
                {
                    client.DownloadFileAsync(new Uri(downloadInfo.memory_quest.RelativePath), save_path);
                }
                catch(Exception ex)
                {
                    Debug.LogError(ex.Message);
                }
                this.Log("Thread Download:" + downloadInfo.memory_quest.RelativePath);
            }
        }

        private void DownloadFileCompleted(MemoryLoadCallbackFunc callback, AsyncCompletedEventArgs e)
        {
            bool flag = e != null && e.Error != null;
            if (flag)
            {
                this.Log(e.Error.ToString());
                callback(false, null);
            }
            else
            {
                callback(true, null);
            }
            this.m_download_state = false;
        }

        private void OnTreadDownloadData(ref DownloadData downloadInfo)
        {
            this.m_download_state = true;
            ProgressEventProxyUpdate pro_event_proxy = new ProgressEventProxyUpdate();
            pro_event_proxy.DataCompleteMethod = new Action<MemoryLoadCallbackFunc, DownloadDataCompletedEventArgs>(this.DownloadDataCompleted);
            pro_event_proxy.CallBack = downloadInfo.callback;
            using (WebClient client = new WebClient())
            {
                client.DownloadDataCompleted += pro_event_proxy.DownloadDataCompleted;
                try
                {
                    client.DownloadDataAsync(new Uri(downloadInfo.memory_quest.RelativePath));
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                }
                this.Log("Thread Download:" + downloadInfo.memory_quest.RelativePath);
            }
        }

        private void DownloadDataCompleted(MemoryLoadCallbackFunc callback, DownloadDataCompletedEventArgs e)
        {
            if (e != null && e.Error != null)
            {
                this.Log(e.Error.ToString());
                callback(false, null);
            }
            else
            {
                callback(true, e.Result);
            }
            this.m_download_state = false;
        }

        private IEnumerator LoadToMemoryAsyncImpl(MemoryQuest memory_quest, MemoryLoadCallbackFunc callback)
        {
            bool openDownloadLog = AppConst.OpenDownloadLog;
            if (openDownloadLog)
            {
                this.Log(string.Format("update file, url:{0},", memory_quest.RelativePath));
            }
            bool useUpdateNewMode = AppConst.UseUpdateNewMode;
            if (useUpdateNewMode)
            {
                UnityWebRequest webRequest = UnityWebRequest.Get(memory_quest.RelativePath);
                memory_quest.request = webRequest;
                webRequest.timeout = memory_quest.timeout;
                yield return webRequest.SendWebRequest();
                bool flag = webRequest.isNetworkError || webRequest.isHttpError || webRequest.downloadHandler == null || webRequest.downloadHandler.data == null;
                if (flag)
                {
                    this.Log(string.Format("res request error occurr, res url:{0}, error:{1}, responseCode:{2}, downloadProgress:{3},isNetworkError:{4}, isHttpError:{5}", new object[]
                    {
                        memory_quest.RelativePath,
                        webRequest.error,
                        webRequest.responseCode,
                        webRequest.downloadProgress,
                        webRequest.isNetworkError,
                        webRequest.isHttpError
                    }));
                    callback(false, null);
                }
                else
                {
                    callback(true, webRequest.downloadHandler.data);
                }
                bool useDeleteRequestMode = AppConst.UseDeleteRequestMode;
                if (useDeleteRequestMode)
                {
                    UnityWebRequest.Delete(memory_quest.RelativePath);
                }
                webRequest.Dispose();
                webRequest = null;
                webRequest = null;
            }
            else
            {
                bool useUpdatOriModeReal = AppConst.UseUpdatOriModeReal;
                if (useUpdatOriModeReal)
                {
                    ProgressEventProxyUpdate pro_event_proxy = new ProgressEventProxyUpdate();
                    pro_event_proxy.FileCompleteMethod = new Action<MemoryLoadCallbackFunc, AsyncCompletedEventArgs>(this.DownloadFileCompleted);
                    pro_event_proxy.CallBack = callback;
                    WebClient client = new WebClient();
                    client.DownloadFileCompleted += pro_event_proxy.DownloadFileCompleted;
                    string save_path = memory_quest.save_path;
                    client.DownloadFileAsync(new Uri(memory_quest.RelativePath), save_path);
                    pro_event_proxy = null;
                    client = null;
                    save_path = null;
                }
                else
                {
                    WWW www = new WWW(memory_quest.RelativePath);
                    memory_quest.Www = www;
                    yield return www;
                    bool flag3 = !string.IsNullOrEmpty(www.error) || www.bytes == null;
                    if (flag3)
                    {
                        this.Log(string.Format("res request error occurr, res url:{0}, error:{1}", memory_quest.RelativePath, www.error));
                        callback(false, null);
                    }
                    else
                    {
                        byte[] bytes = www.bytes;
                        callback(true, bytes);
                        bytes = null;
                    }
                    www.Dispose();
                    www = null;
                    www = null;
                }
            }
            yield break;
        }

    }
}
