﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GFW
{
    /// <summary>
    /// 抽象UI窗口基类
    /// </summary>
    public abstract class UIWindow:UIPanel
    {
        public delegate void CloseEvent(object arg = null);

        //=======================================================================
        /// <summary>
        /// 关闭按钮，大部分窗口都会有关闭按钮
        /// </summary>
        [SerializeField]
        private Button m_btnClose;

        public event CloseEvent onClose;

        /// <summary>
        /// 打开UI的参数
        /// </summary>
        protected object m_openArg;

        /// <summary>
        /// 该UI的当前实例是否曾经被打开过
        /// </summary>
        private bool m_isOpenedOnce;

        /// <summary>
        /// 当UI可用时调用
        /// </summary>
        protected void OnEnable()
        {
            LogMgr.Log("OnEnable()");
            if(m_btnClose != null)
            {
                m_btnClose.onClick.AddListener(OnBtnClose);
            }

        }

        /// <summary>
        /// 当UI不可用时调用
        /// </summary>
        protected void OnDisable()
        {
            LogMgr.Log("OnDisable()");

            if(m_btnClose != null)
            {
                m_btnClose.onClick.RemoveAllListeners();
            }
        }

        /// <summary>
        /// 当点击关闭按钮时调用
        /// 但是并不是每一个Window都有关闭按钮
        /// </summary>
        private void OnBtnClose()
        {
            LogMgr.Log("OnBtnClose()");
            Close(0);
        }

        public sealed override void Open(object arg = null)
        {
            LogMgr.Log("Open() arg:{0}", arg);
            m_openArg = arg;
            m_isOpenedOnce = false;
            if(!this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
            }
            OnOpen(arg);
            m_isOpenedOnce = true;
        }

        public sealed override void Close(object arg = null)
        {
            LogMgr.Log("Close()");
            if(this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(false);
            }

            OnClose(arg);
            if (onClose != null)
            {
                onClose(arg);
                onClose = null;
            }
        }
    }
}
