using System;
using UnityEngine;
using UnityEngine.UI;
using GFW;

namespace CodeX
{

    public class UIProgressPage : UIPage
    {
        public class ProgressInfo
        {
            public float progress;
            public float durTime;
            public float totTime;
            public bool auto;
            public ProgressInfo()
            {
                this.progress = 0;
                this.durTime = 0;
                this.totTime = 0;
                this.auto = false;
            }
            public ProgressInfo(float progress, float totTime, bool auto)
            {
                this.progress = progress;
                this.durTime = 0;
                this.totTime = totTime;
                this.auto = auto;
            }
        }

        private Image m_BarImg;

        private Action m_OnFinsh;

        private bool m_IsFinish;
        private float m_bTime;

        private ProgressInfo m_ProgressInfo;

        private void Awake()
        {
            m_BarImg = transform.Find("ProgressBar/bar").GetComponent<Image>();
        }

        protected override void OnOpen(object arg = null)
        {
            m_IsFinish = false;
            m_bTime = Time.time;

            if (arg != null)
            {
                if (arg is ProgressInfo)
                {
                    m_ProgressInfo = (ProgressInfo)arg;
                }
            }

            if(m_ProgressInfo == null)
            {
                m_ProgressInfo = new ProgressInfo(0, 2, true);
            }
        }

        private void Update()
        {
            if (m_IsFinish)
                return;

            if(m_ProgressInfo.auto)
            {
                m_ProgressInfo.durTime += Time.deltaTime;
                m_ProgressInfo.progress = m_ProgressInfo.durTime / m_ProgressInfo.totTime;
                m_ProgressInfo.progress = Mathf.Clamp(m_ProgressInfo.progress,0,1);
            }

            Vector2 size = new Vector2(1200 * m_ProgressInfo.progress, 28);
            m_BarImg.rectTransform.sizeDelta = size;
            if (m_ProgressInfo.progress >= 1)
            {
                m_IsFinish = true;
                if (m_OnFinsh != null)
                {
                    m_OnFinsh();
                    Close();
                }
            }
        }
        public void SetFinishCallback(Action callback)
        {
            m_OnFinsh = callback;
        }
    }
}
