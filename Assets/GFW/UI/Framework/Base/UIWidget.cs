using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFW
{
    /// <summary>
    /// 抽象UI挂件基类
    /// </summary>
    public abstract class UIWidget:UIPanel
    {
        /// <summary>
        /// 打开UI的参数
        /// </summary>
        protected object m_openArg;

        /// <summary>
        /// 调用它以打开UIWidget
        /// </summary>
        public sealed override void Open(object arg = null)
        {
            LogMgr.Log("Open() arg:{0}", arg);
            m_openArg = arg;
            if(!this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
            }
            OnOpen(arg);
        }

        /// <summary>
        /// 调用它以关闭UIWidget
        /// </summary>
        public sealed override void Close(object arg = null)
        {
            LogMgr.Log("Close() arg:{0}", arg);
            if(this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(false);
            }
            OnClose(arg);
        }
    }
}
