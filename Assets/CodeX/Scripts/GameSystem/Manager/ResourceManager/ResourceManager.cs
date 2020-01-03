using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LuaInterface;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using GFW;
using GFW.ManagerSystem;

namespace CodeX
{
    public class AssetsLoadLevel
    {
        public static uint LOW = 0u;
        public static uint NORMAL = 1u;
        public static uint HIGHT = 2u;
    }

    public class ResourceManager : Manager
    {
        private class AssetBundleInfo
        {
            public RealAssetBundle m_AssetBundle = new RealAssetBundle();
            public int m_ReferencedCount;

            public AssetBundleInfo(AssetBundle assetBundle, int ref_count = 0)
            {
                this.m_AssetBundle.assetBundle = assetBundle;
                this.m_ReferencedCount = ref_count;
            }
        }

        private class LoadAssetRequest
        {
            public Type assetType;
            public string[] assetNames;
            public LuaFunction luaFunc;
            public Action<Object[]> sharpFunc;
            public uint level;
            public LoadAssetRequest() { }
            public LoadAssetRequest(Type type, string[] names, LuaFunction luaFunc, Action<Object[]> sharpFunc, uint level)
            {
                this.assetType = type;
                this.assetNames = names;
                this.luaFunc = luaFunc;
                this.sharpFunc = sharpFunc;
                this.level = level;
            }
        }

        private class UpdateLoadTask
        {
            public string abName;
            public Type abType;
            public UpdateLoadTask(string abName, Type abType)
            {
                this.abName = abName;
                this.abType = abType;
            }
        }

        /// <summary>
        /// 依赖关系文件
        /// </summary>
        private AssetBundleManifest m_AssetBundleManifest = null;

        /// <summary>
        /// 依赖关系字典
        /// </summary>
        private Dictionary<string, string[]> m_Dependencies = new Dictionary<string, string[]>();

        /// <summary>
        /// 已被加载的ab
        /// </summary>
        private Dictionary<string, AssetBundleInfo> m_LoadedAssetBundles = new Dictionary<string, AssetBundleInfo>();

        /// <summary>
        /// 资源加载请求字典
        /// </summary>
        private Dictionary<string, List<LoadAssetRequest>> m_LoadRequests = new Dictionary<string, List<ResourceManager.LoadAssetRequest>>();
        private List<UpdateLoadTask> m_updateLoadAssetsLow = new List<UpdateLoadTask>();
        private List<UpdateLoadTask> m_updateLoadAssetsNormal = new List<UpdateLoadTask>();
        private List<UpdateLoadTask> m_updateLoadAssetsHight = new List<UpdateLoadTask>();
        /// <summary>
        /// 正在进行更新加载的队列
        /// </summary>
        private List<string> m_update_load_list = new List<string>();
        /// <summary>
        /// 单独优先加载的资源标志
        /// </summary>
        private string m_single_hight_load_res;

        /// <summary>
        /// 已经检查过md5的Bundle文件
        /// </summary>
        private List<string> m_has_check_update_res_list = new List<string>();
        private List<string> m_has_check_need_update_list = new List<string>();

        /// <summary>
        /// 已下载的bundle
        /// </summary>
        private Dictionary<string, AssetBundle> m_download_www = new Dictionary<string, AssetBundle>();
        private Dictionary<string, byte[]> m_download_byte = new Dictionary<string, byte[]>();
        private Dictionary<string, bool> m_download_history = new Dictionary<string, bool>();
        private float m_last_save_download_history_time = 0f;
        private int m_last_save_download_history_count = 0;

        private AsyncOperation m_clear_memory_obj = null;

        private float m_last_check_bundle_time = 0f;
        public uint m_max_load_ab_count = 6u;
        public uint m_max_download_count = 4u;
        public uint m_cur_download_count = 0u;

        public uint m_download_timeout = 20u;
        public float m_timeout_progress = 0.2f;
        public bool m_start_res_download_outtime_check = true;

        public bool m_use_new_doanlod_mode = false;
        public bool m_open_cache_download_mode = true;

        #region - 初始化设置
        public void Initialize()
        {
            this.CollectAllMemory();
            string url = string.Format("{0}{1}", AppUtil.GetRelativePath(), AppConst.StreamingAssets);
            WWW download;
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                download = WWW.LoadFromCacheOrDownload(url, Util.RandomNum(10000, 99999));
            }
            else
            {
                download = new WWW(url);
            }
            if (download != null && download.assetBundle != null)
            {
                Object request = download.assetBundle.LoadAsset("AssetBundleManifest", typeof(AssetBundleManifest));
                this.m_AssetBundleManifest = (request as AssetBundleManifest);
            }
            Debug.Log("ResourceManager@Initialize()");
        }

        public void SetLoadAssetsMaxCount(uint count)
        {
            this.m_max_load_ab_count = count;
        }

        public void SetDownloadMaxCount(uint count)
        {
            this.m_max_download_count = count;
        }

        public void SetDownloadFileTimeout(uint time)
        {
            this.m_download_timeout = time;
        }

        public void SetNewDownloadMode(bool state)
        {
            this.m_use_new_doanlod_mode = state;
        }

        public void SetCacheDownloadMode(bool state)
        {
            this.m_open_cache_download_mode = state;
        }
        #endregion

        #region - 加载接口
        public void LoadPrefab(string abName, string assetName, Action<Object[]> func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                assetName
            }, typeof(GameObject), func, null, level);
        }

        public void LoadObject(string abName, string assetName, Action<Object[]> func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                assetName
            }, typeof(Object), func, null, level);
        }

        public void LoadPrefab(string abName, string[] assetNames, Action<Object[]> func, uint level = 1u)
        {
            this.LoadAsset(abName, assetNames, typeof(GameObject), func, null, level);
        }

        public void LoadPrefab(string abName, string[] assetNames, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, assetNames, typeof(GameObject), null, func, level);
        }

        public void LoadPrefab(string abName, string assetName, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                assetName
            }, typeof(GameObject), null, func, level);
        }

        public void LoadSprite(string abName, string spriteName, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                spriteName
            }, typeof(Sprite), null, func, level);
        }

        public void LoadSprites(string abName, string[] assetNames, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, assetNames, typeof(Sprite), null, func, level);
        }

        public void LoadSound(string abName, string assetName, Action<Object[]> func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                assetName
            }, typeof(AudioClip), func, null, level);
        }

        public void LoadTexture(string abName, string spriteName, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                spriteName
            }, typeof(Texture), null, func, level);
        }

        public void LoadFont(string abName, string fontName, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                fontName
            }, typeof(Font), null, func, level);
        }

        public void LoadTexture(string abName, string[] assetNames, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, assetNames, typeof(Texture), null, func, level);
        }

        public void LoadRenderTexture(string abName, string spriteName, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                spriteName
            }, typeof(RenderTexture), null, func, level);
        }

        public void LoadShader(string abName, string[] assetNames, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, assetNames, typeof(Shader), null, func, level);
        }

        public void LoadTextAsset(string abName, string assetNames, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                assetNames
            }, typeof(TextAsset), null, func, level);
        }

        public void LoadMaterial(string abName, string assetNames, LuaFunction func, uint level = 1u)
        {
            this.LoadAsset(abName, new string[]
            {
                assetNames
            }, typeof(Material), null, func, level);
        }
        #endregion

        private void LoadAsset(string abName, string[] assetNames, Type type, Action<Object[]> action = null, LuaFunction func = null, uint level = 0u)
        {
            abName = this.GetRealAssetPath(abName);
            #region - 判断是否为版本文件
            bool empty_file = true;
            if (AppConst.UpdateMode)
            {
                if (ResUpdateManager.Instance.IsVersionFile(abName))
                {
                    empty_file = false;
                }
            }
            else
            {
                string ab_path = AppUtil.DataPath + abName;
                if (File.Exists(ab_path))
                {
                    empty_file = false;
                }
            }
            if (empty_file)
            {
                if (action != null)
                {
                    action(null);
                }
                if (func != null)
                {
                    func.Call<object>(null);
                    func.Dispose();
                }
                LogManager.Log("assetsbundle is not exist:" + abName);
                return;
            }
            #endregion

            #region - 预加载过的资源直接返回资源
            AssetBundleInfo bundleInfo = this.GetLoadedAssetBundle(abName);
            if (bundleInfo != null)
            {
                List<Object> assetList = new List<Object>();
                RealAssetBundle rab = bundleInfo.m_AssetBundle;
                bool has_load_all = true;
                foreach (string assetPath in assetNames)
                {
                    if (!rab.CheckHasLoadedAsset(assetPath))
                    {
                        has_load_all = false;
                        break;
                    }
                    Object abr = rab.LoadAssetAsync(assetPath, type);
                    assetList.Add(abr);
                }
                if (has_load_all)
                {
                    bundleInfo.m_ReferencedCount++;
                    if (action != null)
                    {
                        action(assetList.ToArray());
                        action = null;
                    }
                    if (func != null)
                    {
                        func.Call(new object[]
                        {
                                assetList.ToArray()
                        });
                        func.Dispose();
                        func = null;
                    }
                    return;
                }
            }
            #endregion

            #region - 加载信息加入字典，插入到更新下载队列
            LoadAssetRequest request = new LoadAssetRequest(type, assetNames, func, action, level);
            List<LoadAssetRequest> requests = null;
            if (!this.m_LoadRequests.TryGetValue(abName, out requests))
            {
                requests = new List<LoadAssetRequest>();
                requests.Add(request);
                this.m_LoadRequests.Add(abName, requests);
                this.InsertUpdateLoad(abName, type, level);
            }
            else
            {
                requests.Add(request);
            }
            #endregion
        }

        public void Update()
        {
            if (!(m_clear_memory_obj != null && !m_clear_memory_obj.isDone))
            {
                m_clear_memory_obj = null;
                CheckNoUseAssets();
                LoadAssetsUpdate();
                SaveDownloadHistory();
            }
        }

        #region - 协程加载
        private void InsertUpdateLoad(string abName, Type type, uint level)
        {
            UpdateLoadTask task = new UpdateLoadTask(abName, type);
            if (level == AssetsLoadLevel.LOW)
            {
                this.m_updateLoadAssetsLow.Add(task);
            }
            else if (level == AssetsLoadLevel.NORMAL)
            {
                this.m_updateLoadAssetsNormal.Add(task);
            }
            else
            {
                this.m_updateLoadAssetsHight.Add(task);
            }
        }

        private void LoadAssetsUpdate()
        {
            if (this.m_update_load_list.Count < this.m_max_load_ab_count)
            {
                bool is_begin = BeginTask(this.m_updateLoadAssetsHight);
                is_begin = is_begin ? is_begin : BeginTask(this.m_updateLoadAssetsNormal);
                is_begin = is_begin ? is_begin : BeginTask(this.m_updateLoadAssetsLow);
            }
            else
            {
                if (this.m_updateLoadAssetsHight.Count > 0 && this.m_single_hight_load_res == null)
                {
                    UpdateLoadTask task = this.m_updateLoadAssetsHight[0];
                    this.m_updateLoadAssetsHight.Remove(task);

                    this.m_single_hight_load_res = task.abName;
                    this.m_update_load_list.Add(task.abName);
                    this.OnLoadAsset(task.abName, task.abType);
                }
            }
        }

        private bool BeginTask(List<UpdateLoadTask> taskList)
        {
            if (taskList.Count > 0)
            {
                UpdateLoadTask task = taskList[0];
                if (this.m_cur_download_count >= this.m_max_download_count)
                {
                    if (AppConst.UpdateMode && this.CheckUpdateState(task.abName, true))
                    {
                        taskList.Remove(task);
                        taskList.Add(task);
                        return false;
                    }
                }
                taskList.Remove(task);
                this.m_update_load_list.Add(task.abName);
                this.OnLoadAsset(task.abName, task.abType);
                return true;
            }
            return false;
        }

        private void OnLoadAsset(string abName, Type type)
        {
            AssetBundleInfo bundleInfo = this.GetLoadedAssetBundle(abName);
            if (bundleInfo == null)
            {
                StartCoroutine(this.OnLoadAssetBundle(abName, type, null));
            }
            else
            {
                this.LoadedCompleteCallback(abName, bundleInfo);
            }
        }

        private IEnumerator OnLoadAssetBundle(string abName, Type type, AssetBundle out_assetbundle = null)
        {
            #region 1.bundle检查更新
            AssetBundle download = out_assetbundle;
            if (AppConst.UpdateMode && download == null)
            {
                if (this.CheckUpdateState(abName, true))
                {
                    //下载更新bundle
                    if (m_use_new_doanlod_mode)
                    {
                        yield return this.StartCoroutine(this.RequestDownloadAssets(abName));
                    }
                    else
                    {
                        yield return this.StartCoroutine(this.DownloadAssets(abName));
                    }

                    //如果出现错误则中断
                    if (!this.m_download_www.TryGetValue(abName, out download))
                    {
                        this.RemoveWhenDownloadError(abName);
                        yield break;
                    }

                    if (!this.m_open_cache_download_mode && !this.m_download_byte.ContainsKey(abName))
                    {
                        this.m_download_www.Remove(abName);
                    }

                    if (AppConst.LowSystemMode)
                    {
                        yield return new WaitForSeconds(0.1f);
                    }
                    else
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
                else
                {
                    if (this.m_open_cache_download_mode)
                    {
                        this.m_download_www.TryGetValue(abName, out download);
                    }
                }
            }
            #endregion

            #region 2.加载依赖
            string[] dependencies = null;
            if (!this.m_Dependencies.TryGetValue(abName, out dependencies))
            {
                dependencies = this.m_AssetBundleManifest.GetAllDependencies(abName);
                this.m_Dependencies.Add(abName, dependencies);
            }
            if (dependencies != null && dependencies.Length != 0)
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    string depName = dependencies[i];
                    AssetBundleInfo bundleInfo = null;
                    if (this.m_LoadedAssetBundles.TryGetValue(depName, out bundleInfo))
                    {
                        bundleInfo.m_ReferencedCount++;
                    }
                    else
                    {
                        if (!this.m_LoadRequests.ContainsKey(depName))
                        {
                            AssetBundle depend_assetbundle = null;
                            if (AppConst.UpdateMode)
                            {
                                if (this.CheckUpdateState(depName, true))
                                {
                                    if (this.m_use_new_doanlod_mode)
                                    {
                                        yield return this.StartCoroutine(this.RequestDownloadAssets(depName));
                                    }
                                    else
                                    {
                                        yield return this.StartCoroutine(this.DownloadAssets(depName));
                                    }

                                    if (!(this.m_download_www.TryGetValue(depName, out depend_assetbundle)))
                                    {
                                        this.RemoveWhenDownloadError(abName);
                                        yield break;
                                    }
                                    if (!this.m_open_cache_download_mode && !this.m_download_byte.ContainsKey(depName))
                                    {
                                        this.m_download_www.Remove(depName);
                                    }
                                    if (AppConst.LowSystemMode)
                                    {
                                        yield return new WaitForSeconds(0.1f);
                                    }
                                    else
                                    {
                                        yield return new WaitForEndOfFrame();
                                    }
                                }
                                else
                                {
                                    if (this.m_open_cache_download_mode)
                                    {
                                        this.m_download_www.TryGetValue(depName, out depend_assetbundle);
                                    }
                                }
                            }

                            LoadAssetRequest request = new LoadAssetRequest();
                            if (!this.m_LoadRequests.ContainsKey(depName))
                            {
                                List<LoadAssetRequest> requests = new List<LoadAssetRequest>();
                                requests.Add(request);
                                this.m_LoadRequests.Add(depName, requests);
                                requests = null;
                            }
                            else
                            {
                                List<LoadAssetRequest> requests2 = this.m_LoadRequests[depName];
                                if (requests2 != null)
                                {
                                    requests2.Add(request);
                                }
                                requests2 = null;
                            }
                            this.m_update_load_list.Add(depName);
                            yield return this.StartCoroutine(this.OnLoadAssetBundle(depName, type, depend_assetbundle));
                            depend_assetbundle = null;
                            request = null;
                        }
                        else
                        {
                            yield return this.StartCoroutine(this.OnWaitForDepDownload(depName));
                        }
                    }
                    depName = null;
                    bundleInfo = null;
                }
            }
            #endregion

            #region 3.加载资源
            AssetBundleInfo localBundleInfo = this.GetLoadedAssetBundle(abName);
            if (localBundleInfo == null)
            {
                AssetBundle assetObj = null;
                if (download == null)
                {
                    string resPath = this.GetLocalResPath(abName);
                    try
                    {
                        assetObj = AssetBundle.LoadFromFile(resPath);
                    }
                    catch (Exception ex)
                    {
                        Exception e = ex;
                        string msg = "no msg";
                        if (e != null && e.Message != null)
                        {
                            msg = e.Message;
                        }
                        Debug.LogWarning("AssetBundle LoadFromFile Error:" + msg);
                        msg = null;
                    }
                    if (AppConst.LowSystemMode)
                    {
                        yield return new WaitForSeconds(0.03f);
                    }
                    else
                    {
                        if (AppConst.LoadResWaitNextFrame)
                        {
                            yield return new WaitForEndOfFrame();
                        }
                    }
                    if (AppConst.OpenDownloadLog)
                    {
                        Debug.Log(abName + " loaded");
                    }
                    resPath = null;
                }
                else
                {
                    assetObj = download;
                }

                if (assetObj != null)
                {
                    AssetBundleInfo bundleInfo = new AssetBundleInfo(assetObj, 0);
                    if (!this.m_LoadedAssetBundles.ContainsKey(abName))
                    {
                        this.m_LoadedAssetBundles.Add(abName, bundleInfo);
                    }
                    this.LoadedCompleteCallback(abName, bundleInfo);
                    bundleInfo = null;
                }
                assetObj = null;
            }
            else
            {
                this.LoadedCompleteCallback(abName, localBundleInfo);
            }

            if (!this.m_has_check_update_res_list.Contains(abName))
            {
                this.m_has_check_update_res_list.Add(abName);
            }
            yield break;
            #endregion
        }

        private IEnumerator RequestDownloadAssets(string assets_name)
        {
            float start_time = Time.time;
            this.m_cur_download_count += 1u;
            string realDepName = Util.GetAssetsBundlePath(assets_name);
            GameSystem.Instance.GetManager<DownLoadResManager>().CheckDownloadFile(realDepName);
            string local_path = string.Format("{0}{1}", AppUtil.DataPath, realDepName);
            string full_file_name = ResUpdateManager.Instance.GetUnityBundlePath(realDepName);
            int file_version = ResUpdateManager.Instance.GetFileVersion(full_file_name);
            string remote_path = string.Format("{0}{1}?v={2}", AppConst.CdnUrl, full_file_name.Remove(0, 1), file_version);
            bool openDownloadLog = AppConst.OpenDownloadLog;
            if (openDownloadLog)
            {
                this.Log(string.Format("assetbundle start download, res path:{0},", assets_name));
            }
            UnityWebRequest webRequest = UnityWebRequest.Get(remote_path);
            webRequest.timeout = (int)this.m_download_timeout;
            yield return webRequest.SendWebRequest();
            bool openDownloadLog2 = AppConst.OpenDownloadLog;
            if (openDownloadLog2)
            {
                this.Log(string.Format("res download last time {0},{1},cost time:{2:N3}", remote_path, assets_name, Time.time - start_time));
            }
            string error_state = null;
            bool flag = webRequest.isNetworkError || webRequest.isHttpError;
            if (flag)
            {
                bool flag2 = Application.internetReachability > NetworkReachability.NotReachable;
                if (flag2)
                {
                    float first_cost_time = Time.time - start_time;
                    start_time = Time.time;
                    bool useDeleteRequestMode = AppConst.UseDeleteRequestMode;
                    if (useDeleteRequestMode)
                    {
                        UnityWebRequest.Delete(remote_path);
                    }
                    webRequest.Dispose();
                    webRequest = null;
                    webRequest = UnityWebRequest.Get(remote_path);
                    webRequest.timeout = (int)this.m_download_timeout;
                    yield return webRequest.SendWebRequest();
                    Util.ThrowLuaException(string.Format("res download again {0},{1},{2},first cost time:{3:N3},again cost time:{4:N3}", new object[]
                    {
                        remote_path,
                        webRequest.error,
                        assets_name,
                        first_cost_time,
                        Time.time - start_time
                    }), null, 1);
                    bool flag3 = webRequest.isNetworkError || webRequest.isHttpError;
                    if (flag3)
                    {
                        error_state = "download";
                    }
                }
                else
                {
                    error_state = "download";
                }
            }
            AssetBundle ab = null;
            bool flag4 = error_state == null;
            if (flag4)
            {
                byte[] bytes = webRequest.downloadHandler.data;
                ab = AssetBundle.LoadFromMemory(bytes);
                bool flag5 = !this.m_open_cache_download_mode;
                if (flag5)
                {
                    Pathtool.DeleteToFile(local_path);
                    bool save_succeed = Pathtool.SaveDataToFile(local_path, bytes);
                    bool flag7 = save_succeed;
                    if (flag7)
                    {
                        bool openNoneImportResVersionSave = AppConst.OpenNoneImportResVersionSave;
                        if (openNoneImportResVersionSave)
                        {
                            string file_md5 = Md5Helper.Md5Buffer(bytes);
                            bool sqlite_succeed = ResUpdateManager.Instance.RepalceInfoFromRemote("unitybundle", full_file_name, file_md5);
                            bool flag8 = !sqlite_succeed;
                            if (flag8)
                            {
                                error_state = "md5";
                            }
                            file_md5 = null;
                        }
                    }
                    else
                    {
                        error_state = "save";
                    }
                    bytes = null;
                }
                else
                {
                    bool flag9 = !this.m_download_byte.ContainsKey(assets_name);
                    if (flag9)
                    {
                        this.m_download_byte.Add(assets_name, bytes);
                    }
                }
                bytes = null;
            }
            bool flag10 = error_state != null;
            if (flag10)
            {
                Util.ThrowLuaException(string.Format("res {0} error occurr, url:{1}, error:{2}", error_state, remote_path, webRequest.error), null, 1);
                this.m_cur_download_count -= 1u;
                webRequest.Dispose();
                webRequest = null;
                yield break;
            }
            bool flag11 = !this.m_download_www.ContainsKey(assets_name);
            if (flag11)
            {
                this.m_download_www.Add(assets_name, ab);
            }
            this.m_cur_download_count -= 1u;
            this.AddDownloadHistory(realDepName);
            bool useDeleteRequestMode2 = AppConst.UseDeleteRequestMode;
            if (useDeleteRequestMode2)
            {
                UnityWebRequest.Delete(remote_path);
            }
            webRequest.Dispose();
            webRequest = null;
            yield break;
        }

        private IEnumerator DownloadAssets(string assets_name)
        {
            this.m_cur_download_count += 1u;
            string real_name = Util.GetAssetsBundlePath(assets_name);
            GameSystem.Instance.GetManager<DownLoadResManager>().CheckDownloadFile(real_name);

            string local_path = string.Format("{0}{1}", AppUtil.DataPath, real_name);
            string full_file_name = ResUpdateManager.Instance.GetUnityBundlePath(real_name);
            int file_version = ResUpdateManager.Instance.GetFileVersion(full_file_name);
            string remote_path = string.Format("{0}{1}?v={2}", AppConst.CdnUrl, full_file_name.Remove(0, 1), file_version);
            if (AppConst.OpenDownloadLog)
            {
                Debug.Log(string.Format("assetbundle start download, res path:{0},", assets_name));
            }
            #region - 启动www下载，检查错误信息
            WWW www = new WWW(remote_path);
            string error_state = null;
            if (!this.m_start_res_download_outtime_check)
            {
                yield return www;
            }
            else
            {
                float curTime = 0f;
                while (www != null && !www.isDone)
                {
                    if (curTime > this.m_download_timeout && www.progress < this.m_timeout_progress)
                    {
                        error_state = "timeout";
                        break;
                    }
                    curTime += Time.deltaTime;
                    yield return new WaitForSeconds(0.05f);
                }
            }
            if (error_state == null && !string.IsNullOrEmpty(www.error))
            {
                error_state = "download";
            }
            #endregion
            #region - 保存文件到本地，检查错误信息
            if (error_state == null)
            {
                if (!this.m_open_cache_download_mode)
                {
                    byte[] bytes = www.bytes;
                    Pathtool.DeleteToFile(local_path);
                    bool save_succeed = Pathtool.SaveDataToFile(local_path, bytes);
                    if (save_succeed)
                    {
                        string file_md5 = Md5Helper.Md5Buffer(bytes);
                        bool sqlite_succeed = ResUpdateManager.Instance.RepalceInfoFromRemote("unitybundle", full_file_name, file_md5);
                        if (!sqlite_succeed)
                        {
                            error_state = "md5";
                        }
                        file_md5 = null;
                    }
                    else
                    {
                        error_state = "save";
                    }
                    bytes = null;
                }
                else
                {
                    if (!this.m_download_byte.ContainsKey(assets_name))
                    {
                        this.m_download_byte.Add(assets_name, www.bytes);
                    }
                }
            }
            #endregion

            #region - 处理结果
            if (error_state != null)
            {
                Util.ThrowLuaException(string.Format("res {0} error occurr, url:{1}, error:{2}", error_state, remote_path, www.error), null, 1);
                this.m_cur_download_count -= 1u;
                if (!error_state.Equals("timeout"))
                {
                    www.Dispose();
                }
                www = null;
                yield break;
            }
            else
            {
                if (!this.m_download_www.ContainsKey(assets_name))
                {
                    this.m_download_www.Add(assets_name, www.assetBundle);
                }
                this.m_cur_download_count -= 1u;
                this.AddDownloadHistory(real_name);
                www.Dispose();
                www = null;
                yield break;
            }
            #endregion
        }

        private IEnumerator OnWaitForDepDownload(string depName)
        {
            AssetBundleInfo bundle = null;
            while (bundle == null)
            {
                this.m_LoadedAssetBundles.TryGetValue(depName, out bundle);
                if (AppConst.LowSystemMode)
                {
                    yield return new WaitForSeconds(0.03f);
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            yield break;
        }

        private void RemoveWhenDownloadError(string abName)
        {
            List<ResourceManager.LoadAssetRequest> list = null;
            if (this.m_LoadRequests.TryGetValue(abName, out list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].sharpFunc != null)
                    {
                        list[i].sharpFunc(null);
                        list[i].sharpFunc = null;
                    }
                    if (list[i].luaFunc != null)
                    {
                        list[i].luaFunc.Call<object>(null);
                        list[i].luaFunc.Dispose();
                        list[i].luaFunc = null;
                    }
                }
                this.m_LoadRequests.Remove(abName);
            }
            if (this.m_update_load_list.Contains(abName))
            {
                this.m_update_load_list.Remove(abName);
            }
        }

        private void LoadedCompleteCallback(string abName, AssetBundleInfo bundleInfo)
        {
            if (abName.Equals(this.m_single_hight_load_res))
            {
                this.m_single_hight_load_res = null;
            }

            List<LoadAssetRequest> list = null;
            if (!this.m_LoadRequests.TryGetValue(abName, out list))
            {
                this.m_update_load_list.Remove(abName);
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    bundleInfo.m_ReferencedCount++;
                    if (list[i].sharpFunc != null || list[i].luaFunc != null)
                    {
                        string[] assetNames = list[i].assetNames;
                        List<Object> assetList = new List<Object>();
                        RealAssetBundle rab = bundleInfo.m_AssetBundle;
                        foreach (string assetPath in assetNames)
                        {
                            if (assetPath != null && assetPath != "")
                            {
                                Object res = rab.LoadAssetAsync(assetPath, list[i].assetType);
                                assetList.Add(res);
                            }
                            else
                            {
                                Debug.LogWarning("request empty res in abname " + abName);
                                assetList.Add(null);
                            }
                        }
                        if (list[i].sharpFunc != null)
                        {
                            list[i].sharpFunc(assetList.ToArray());
                            list[i].sharpFunc = null;
                        }
                        if (list[i].luaFunc != null)
                        {
                            list[i].luaFunc.Call(new object[]
                            {
                                assetList.ToArray()
                            });
                            list[i].luaFunc.Dispose();
                            list[i].luaFunc = null;
                        }
                    }
                }
                this.m_LoadRequests.Remove(abName);
                this.m_update_load_list.Remove(abName);
            }
        }
        #endregion

        #region - 资源卸载检测
        private void CheckNoUseAssets()
        {
            if (Time.time - this.m_last_check_bundle_time > 60f)
            {
                this.m_last_check_bundle_time = Time.time;
                List<string> delay_delete_ab = new List<string>();
                foreach (KeyValuePair<string, AssetBundleInfo> item in this.m_LoadedAssetBundles)
                {
                    AssetBundleInfo bundle = item.Value;
                    if (bundle.m_ReferencedCount <= 0)
                    {
                        string abName = item.Key;
                        delay_delete_ab.Add(abName);
                    }
                }

                if (delay_delete_ab.Count > 0)
                {
                    for (int i = 0; i < delay_delete_ab.Count; i++)
                    {
                        string abName2 = delay_delete_ab[i];
                        AssetBundleInfo bundle2 = this.GetLoadedAssetBundle(abName2);
                        LogManager.Log("-------------auto unload assets:" + abName2);
                        DeleteAB(abName2, bundle2, true);
                    }
                }
            }
        }

        private AssetBundleInfo GetLoadedAssetBundle(string abName)
        {
            AssetBundleInfo bundle = null;
            if (this.m_LoadedAssetBundles.TryGetValue(abName, out bundle))
            {
                string[] dependencies = null;
                if (this.m_Dependencies.TryGetValue(abName, out dependencies))
                {
                    foreach (string dependency in dependencies)
                    {
                        AssetBundleInfo dependentBundle;
                        this.m_LoadedAssetBundles.TryGetValue(dependency, out dependentBundle);
                        if (dependentBundle == null)
                        {
                            return null;
                        }
                    }
                }
            }
            return bundle;
        }

        public void UnloadAssetBundle(string abName, bool isThorough = false, int ref_count = 1)
        {
            abName = this.GetRealAssetPath(abName);
            this.UnloadAssetBundleInternal(abName, isThorough, ref_count);
        }

        private void UnloadAssetBundleInternal(string abName, bool isThorough = false, int ref_count = 1)
        {
            if (abName != null)
            {
                AssetBundleInfo bundle = null;
                this.m_LoadedAssetBundles.TryGetValue(abName, out bundle);
                if (bundle != null)
                {
                    bundle.m_ReferencedCount -= ref_count;
                    if (bundle.m_ReferencedCount <= 0)
                    {
                        if (!m_LoadRequests.ContainsKey(abName))
                        {
                            DeleteAB(abName, bundle, isThorough);
                        }
                    }
                }
                else
                {
                    CancelLoadRes(abName, null);
                }
            }
            else
            {
                LogManager.Log("UnloadAssetBundle Error:>>" + abName);
            }
        }

        private void DeleteAB(string abName, AssetBundleInfo abInfo, bool isThorough)
        {
            if (abInfo != null && abInfo.m_AssetBundle != null && !this.m_download_byte.ContainsKey(abName))
            {
                abInfo.m_AssetBundle.Unload(isThorough);
            }
            this.UnloadDependencies(abName, isThorough);
            this.m_LoadedAssetBundles.Remove(abName);
            if (AppConst.OpenDownloadLog)
            {
                Debug.Log(abName + " unloaded");
            }
        }

        private void UnloadDependencies(string abName, bool isThorough)
        {
            string[] dependencies = null;
            if (m_Dependencies.TryGetValue(abName, out dependencies))
            {
                for (int i = 1; i < dependencies.Length; i++)
                {
                    UnloadAssetBundleInternal(dependencies[i], isThorough, 1);
                }
            }
        }

        public void CancelLoadRes(string abName, string[] assetNames)
        {
            List<ResourceManager.LoadAssetRequest> requests = null;
            if (m_LoadRequests.TryGetValue(abName, out requests))
            {
                if (m_update_load_list.Contains(abName))
                {
                    if (assetNames != null)
                    {
                        for (int i = 0; i < requests.Count; i++)
                        {
                            ResourceManager.LoadAssetRequest now = requests[i];
                            if (now.assetNames == assetNames)
                            {
                                requests.Remove(now);
                                break;
                            }
                        }
                    }
                    else
                    {
                        requests.Clear();
                    }
                }

                if (requests.Count == 0)
                {
                    this.m_LoadRequests.Remove(abName);
                    bool find = false;
                    for (int j = 0; j < this.m_updateLoadAssetsLow.Count; j++)
                    {
                        UpdateLoadTask now2 = this.m_updateLoadAssetsLow[j];
                        if (now2.abName == abName)
                        {
                            this.m_updateLoadAssetsLow.Remove(now2);
                            find = true;
                            break;
                        }
                    }
                    if (!find)
                    {
                        for (int k = 0; k < this.m_updateLoadAssetsNormal.Count; k++)
                        {
                            UpdateLoadTask now3 = this.m_updateLoadAssetsNormal[k];
                            if (now3.abName == abName)
                            {
                                this.m_updateLoadAssetsNormal.Remove(now3);
                                find = true;
                                break;
                            }
                        }
                    }
                    if (!find)
                    {
                        for (int l = 0; l < this.m_updateLoadAssetsHight.Count; l++)
                        {
                            UpdateLoadTask now4 = this.m_updateLoadAssetsHight[l];
                            if (now4.abName == abName)
                            {
                                this.m_updateLoadAssetsHight.Remove(now4);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public AssetBundle GetAssetBundle(string abName)
        {
            abName = this.GetRealAssetPath(abName);
            bool flag = abName != null;
            if (flag)
            {
                AssetBundleInfo bundleInfo = this.GetLoadedAssetBundle(abName);
                bool flag2 = bundleInfo != null;
                if (flag2)
                {
                    return bundleInfo.m_AssetBundle.assetBundle;
                }
            }
            return null;
        }
        #endregion

        /// <summary>
        /// 检查是否是需要更新的bundle资源（因为res启动时已经检查，所以默认就是不用更新的）
        /// </summary>
        public bool CheckUpdateState(string abName, bool need_md5_check = true)
        {
            bool result = false;
            if (!AppConst.IgnoreGameUpdateState)
            {
                if (!this.m_has_check_update_res_list.Contains(abName))
                {
                    if (this.m_has_check_need_update_list.Contains(abName))
                    {
                        result = true;
                    }
                    else
                    {
                        string resName = Util.GetAssetsBundlePath(abName);
                        string full_path = ResUpdateManager.Instance.GetUnityBundlePath(resName);
                        bool need_update = ResUpdateManager.Instance.FileNeedUpdate(full_path, need_md5_check);
                        if (!need_update && need_md5_check && !this.m_has_check_update_res_list.Contains(abName))
                        {
                            this.m_has_check_update_res_list.Add(abName);
                        }
                        else
                        {
                            if (need_update && !this.m_has_check_need_update_list.Contains(abName))
                            {
                                Debug.Log("需要更新：" + abName);
                                this.m_has_check_need_update_list.Add(abName);
                            }
                        }
                        result = need_update;
                    }
                }
            }
            return result;
        }

        public void CollectAllMemory()
        {
            if (m_clear_memory_obj != null)
            {
                m_clear_memory_obj = null;
            }
            m_clear_memory_obj = Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private string GetRealAssetPath(string abName)
        {
            abName = abName.ToLower();
            if (!abName.EndsWith(".syrd"))
            {
                abName = string.Format("{0}{1}", abName, ".syrd");
            }

            if (!abName.Contains("mdata/"))
            {
                abName = string.Format("{0}{1}", "rdata/", abName);
            }
            return abName;
        }

        private string GetLocalResPath(string abName)
        {
            string resPath = null;
            if (!AppConst.DebugMode)
            {
                string real_abname = Util.GetAssetsBundlePath(abName);
                resPath = string.Format("{0}{1}", AppUtil.DataPath, real_abname);
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                resPath = string.Format("{0}{1}{2}{3}", new object[]
                {
                        "../../",
                        AppConst.StreamingAssets,
                        "/",
                        abName
                });
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                resPath = string.Format("{0}{1}{2}", AppConst.StreamingAssets, "/", abName);
            }
            return resPath;
        }

        public void GetDependResList(string abName, Action<bool, string[]> func)
        {
            if (abName != null && func != null)
            {
                abName = this.GetRealAssetPath(abName);
                string[] dependencies = this.m_AssetBundleManifest.GetAllDependencies(abName);
                if (dependencies != null)
                {
                    func(true, dependencies.ToArray<string>());
                }
                else
                {
                    func(false, null);
                }
            }
        }

        public void CopyFileFormApp(string app_path, string local_path, LuaFunction func)
        {
            base.StartCoroutine(this.CopyAppFile(app_path, local_path, func));
        }
        private IEnumerator CopyAppFile(string src, string des, LuaFunction func)
        {
            string local_path = AppUtil.DataPath + des;
            string dir = Path.GetDirectoryName(local_path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string app_path = AppUtil.AppContentPath() + src;
            bool result = false;
            if (Application.platform == RuntimePlatform.Android)
            {
                WWW www = new WWW(app_path);
                yield return www;
                if (www.isDone && www.bytes != null && www.bytes.Length != 0)
                {
                    File.WriteAllBytes(local_path, www.bytes);
                    result = true;
                }
                www.Dispose();
                www = null;
                www = null;
            }
            else
            {
                if (File.Exists(app_path))
                {
                    File.Copy(app_path, local_path, true);
                    result = true;
                }
            }

            if (func != null)
            {
                func.Call(new object[]
                {
                    src,
                    result
                });
                func.Dispose();
                func = null;
            }
            yield break;
        }

        #region - 后台静默下载
        public void WriteDownloadCacheFile()
        {
            base.StartCoroutine(this.WriteSingleDownloadCacheFile());
        }

        public int GetDownloadCacheFileCount()
        {
            return this.m_download_byte.Count;
        }

        public void WriteAllDownloadCacheFile()
        {
            int count = this.m_download_byte.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    this.WriteCacheFile();
                }
            }
        }

        private IEnumerator WriteSingleDownloadCacheFile()
        {
            if (this.m_download_byte.Count <= 0)
            {
                yield break;
            }
            this.WriteCacheFile();
            yield break;
        }

        private void WriteCacheFile()
        {
            KeyValuePair<string, byte[]> asset = this.m_download_byte.First<KeyValuePair<string, byte[]>>();
            if (!(asset.Key == null || asset.Value == null))
            {
                string res_name = Util.GetAssetsBundlePath(asset.Key);
                string local_path = string.Format("{0}{1}", AppUtil.DataPath, res_name);
                string full_file_name = ResUpdateManager.Instance.GetUnityBundlePath(res_name);
                string error_state = null;
                Pathtool.DeleteToFile(local_path);
                bool save_succeed = Pathtool.SaveDataToFile(local_path, asset.Value);
                if (save_succeed)
                {
                    string file_md5 = Md5Helper.Md5Buffer(asset.Value);
                    bool sqlite_succeed = ResUpdateManager.Instance.RepalceInfoFromRemote("unitybundle", full_file_name, file_md5);
                    if (!sqlite_succeed)
                    {
                        error_state = "md5";
                    }
                    else
                    {
                        if (AppConst.OpenDownloadLog)
                        {
                            Debug.Log("WriteCacheFile Succeed " + asset.Key);
                        }
                    }
                }
                else
                {
                    error_state = "save";
                }

                if (error_state != null)
                {
                    string error = string.Format("res {0} error occurr, url:{1}, error:{2}", error_state, full_file_name, "cache write error");
                    Util.ThrowLuaException(error, null, 1);
                    if (AppConst.OpenDownloadLog)
                    {
                        Debug.Log("WriteCacheFile Error " + asset.Key + " " + error_state);
                    }
                }
                this.m_download_byte.Remove(asset.Key);
                if (!this.m_LoadedAssetBundles.ContainsKey(asset.Key))
                {
                    AssetBundle bundle = null;
                    if (this.m_download_www.TryGetValue(asset.Key, out bundle))
                    {
                        bundle.Unload(true);
                    }
                }
                this.m_download_www.Remove(asset.Key);
            }
        }
        #endregion

        #region - 下载历史记录
        public void ReadDownloadHistory()
        {
            string res_file = AppUtil.DataPath + "downloadhistory.temp";
            if (File.Exists(res_file))
            {
                StreamReader sr = new StreamReader(res_file);
                string str;
                while ((str = sr.ReadLine()) != null)
                {
                    this.m_download_history.Add(str, true);
                }
                sr.Dispose();
                sr = null;
            }
        }

        public void AddDownloadHistory(string file_path)
        {
            string file_name = Path.GetFileNameWithoutExtension(file_path);
            bool temp;
            if (!this.m_download_history.TryGetValue(file_name, out temp))
            {
                this.m_download_history.Add(file_name, true);
            }
        }

        public bool ExistDownloadHistory(string file_path)
        {
            if (this.m_download_history.Count == 0)
                return false;

            string file_name = Path.GetFileNameWithoutExtension(file_path);
            return this.m_download_history.ContainsKey(file_name);
        }

        public void SaveDownloadHistory()
        {
            if (!(this.m_download_history.Count == 0 || this.m_download_history.Count == this.m_last_save_download_history_count))
            {
                if (!(Time.time - this.m_last_save_download_history_time < 10f))
                {
                    this.m_last_save_download_history_time = Time.time;
                    this.m_last_save_download_history_count = this.m_download_history.Count;
                    StringBuilder temp = new StringBuilder();
                    foreach (KeyValuePair<string, bool> resdata in this.m_download_history)
                    {
                        string key = resdata.Key;
                        temp.AppendFormat("{0}\n", key);
                    }
                    string data_file = string.Format("{0}/downloadhistory.temp", AppUtil.DataPath);
                    string content = temp.ToString();
                    if (content.Length > 0)
                    {
                        content = content.Substring(0, content.Length - 1);
                    }
                    File.WriteAllText(data_file, content);
                }
            }
        }
        #endregion

        public void PrintResourceLog(LuaFunction load_func)
        {
            List<string> load_file_list = new List<string>();
            foreach (KeyValuePair<string, AssetBundleInfo> assets in this.m_LoadedAssetBundles)
            {
                load_file_list.Add(assets.Key);
            }
            List<string> try_load_file_list = new List<string>();
            foreach (KeyValuePair<string, List<ResourceManager.LoadAssetRequest>> assets2 in this.m_LoadRequests)
            {
                try_load_file_list.Add(assets2.Key);
            }
            List<string> try_load_file_hight_list = new List<string>();
            foreach (UpdateLoadTask assets3 in this.m_updateLoadAssetsHight)
            {
                try_load_file_hight_list.Add(assets3.abName);
            }
            List<string> try_load_file_normal_list = new List<string>();
            foreach (UpdateLoadTask assets4 in this.m_updateLoadAssetsNormal)
            {
                try_load_file_normal_list.Add(assets4.abName);
            }
            List<string> try_load_file_low_list = new List<string>();
            foreach (UpdateLoadTask assets5 in this.m_updateLoadAssetsLow)
            {
                try_load_file_low_list.Add(assets5.abName);
            }
            bool flag = load_func != null;
            if (flag)
            {
                load_func.Call(new object[]
                {
                    load_file_list.Count,
                    load_file_list,
                    try_load_file_list.Count,
                    try_load_file_list,
                    this.m_update_load_list.Count,
                    this.m_update_load_list,
                    try_load_file_hight_list.Count,
                    try_load_file_hight_list,
                    try_load_file_normal_list.Count,
                    try_load_file_normal_list,
                    try_load_file_low_list.Count,
                    try_load_file_low_list
                });
                load_func.Dispose();
                load_func = null;
            }
        }
    }
}
