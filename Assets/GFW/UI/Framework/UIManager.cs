using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GFW.Unity;

namespace GFW
{
    public class UIManager:ServiceModule<UIManager>
    {
        class UIPageTrack
        {
            public string name;
            public object arg;
        }

        private Stack<UIPageTrack> m_pageTrackStack;
        private UIPageTrack m_pageCurrentTrack;
        private Dictionary<string, UIPanel> m_mapLoadedPanel;

        public UIManager()
        {
            m_pageTrackStack = new Stack<UIPageTrack>();
            m_mapLoadedPanel = new Dictionary<string, UIPanel>();
        }

        #region - 内部使用函数
        private UIPanel FindUI(string resName)
        {
            UIPanel ui = null;
            m_mapLoadedPanel.TryGetValue(resName, out ui);
            return ui;
        }
        private T LoadUI<T>(string resName) where T : UIPanel
        {
            T ui = (T)FindUI(resName);
            if (ui == null)
            {
                GameObject original = UIRes.LoadPrefab(resName);
                if (original != null)
                {
                    GameObject go = GameObject.Instantiate(original);
                    ui = go.EnsureAddComponent<T>();
                    if (ui != null)
                    {
                        go.name = resName;
                        UIRoot.AddChild(ui, ui.UILayer);
                        m_mapLoadedPanel.Add(resName, ui);
                    }
                    else
                    {
                        LogMgr.LogError("No Find Component<{0}>", typeof(T).Name);
                    }
                }
                else
                {
                    LogMgr.LogError("Res Not Found: {0}",resName);
                }
            }

            return ui;
        }
        private T OpenUI<T>(string resName, object arg = null) where T : UIPanel
        {
            T ui = LoadUI<T>(resName);
            if (ui != null)
            {
                ui.Open(arg);
            }
            return ui;
        }
        private void CloseUI(string resName, object arg = null)
        {
            UIPanel ui = FindUI(resName);
            if (ui.IsOpen)
                ui.Close();
        }
        private void CloseAllLoadedPanels()
        {
            foreach (var item in m_mapLoadedPanel)
            {
                if (item.Value.IsOpen)
                {
                    item.Value.Close();
                }
            }
        }
        private void OpenPageWorker(string resName, object arg)
        {
            m_pageCurrentTrack = new UIPageTrack();
            m_pageCurrentTrack.name = resName;
            m_pageCurrentTrack.arg = arg;

            //关闭当前Page时打开的所有UI
            CloseAllLoadedPanels();
            UIPanel ui = FindUI(resName);
            if(ui != null)
            {
                ui.Open(arg);
            }
        }
        #endregion

        /// <summary>
        /// *UI管理器初始化
        /// </summary>
        /// <param name="uiResRoot">ui prefab目录</param>
        public void Init(string uiResRoot = "ui/")
        {
            UIRes.UIResRoot = uiResRoot;
        }

        public T Open<T>(object arg = null) where T : UIPanel
        {
            string resName = typeof(T).Name;
            return Open<T>(resName, arg);
        }
        public T Open<T>(string resName, object arg = null) where T : UIPanel
        {
            T ui = default(T);
            Type type = typeof(T);
            if(type.IsSubclassOf(typeof(UIPage)))
            {
                ui = OpenPage<T>(resName, arg);
            }
            else if(type.IsSubclassOf(typeof(UIWindow)))
            {
                ui = OpenWindow<T>(resName, arg);
            }
            else if (type.IsSubclassOf(typeof(UIWidget)))
            {
                ui = OpenWidget<T>(resName, arg);
            }
            else
            {
                ui = OpenUI<T>(resName, arg);
            }
            return ui;
        }
        public T OpenPage<T>(string resName, object arg = null) where T : UIPanel
        {
            T ui = OpenUI<T>(resName, arg);
            if (ui != null)
            {
                if (m_pageCurrentTrack != null)
                {
                    m_pageTrackStack.Push(m_pageCurrentTrack);
                }
                OpenPageWorker(resName, arg);
            }
            return ui;
        }
        public void GoBackPage()
        {
            if (m_pageTrackStack.Count > 0)
            {
                var track = m_pageTrackStack.Pop();
                OpenPageWorker(track.name, track.arg);
            }
        }
        public T OpenWindow<T>(string resName, object arg = null) where T: UIPanel
        {
            T ui = OpenUI<T>(resName, arg);
            return ui;
        }
        public T OpenWidget<T>(string resName, object arg = null) where T : UIPanel
        {
            T ui = OpenUI<T>(resName, arg);
            return ui;
        }

        public void Close(string resName)
        {
            CloseUI(resName);
        }

        public void CloseAll()
        {
            CloseAllLoadedPanels();
        }
    }
}
