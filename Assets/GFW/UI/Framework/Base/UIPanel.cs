using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFW
{
    public abstract class UIPanel:MonoBehaviour
    {
        protected string m_uiLayer;
        public string UILayer { get { return this.m_uiLayer; } }

        #region - 外部调用
        /// <summary>
        /// 当前UI是否显示
        /// </summary>
        public bool IsOpen { get { return this.gameObject.activeSelf; } }

        /// <summary>
        /// 当UI打开时调用（UIManager中）
        /// </summary>
        public virtual void Open(object arg = null)
        {
            LogMgr.Log("arg:{0}",arg);
        }

        /// <summary>
        /// 当UI关闭时调用（UIManager中）
        /// </summary>
        public virtual void Close(object arg = null)
        {
            LogMgr.Log("arg:{0}", arg);
        }

        #endregion

        #region - 定义Panel派生类的具体行为（Open->OnOpen）
        /// <summary>
        /// 当UI打开时，会响应这个函数
        /// </summary>
        protected virtual void OnOpen(object arg = null)
        {
            LogMgr.Log("arg:{0}", arg);
        }
        /// <summary>
        /// 当UI关闭时，会响应这个函数
        /// </summary>
        protected virtual void OnClose(object arg = null)
        {
            LogMgr.Log("OnClose()");
        }
        #endregion
    }
}
