using System;
using UnityEngine;
using UnityEngine.UI;
using GFW;

namespace CodeX
{
    public class UIProgressPage : UIPage
    {
        private Image m_BarImg;

        private Action m_OnFinsh;

        private bool m_IsFinish;
        private float m_bTime;
        private float m_Duration = 1;
        private float m_Progress = 0;

        private AsyncOperation m_Async;

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
                if (arg is AsyncOperation)
                {
                    m_Async = arg as AsyncOperation;
                }
                else
                {
                    m_Duration = (float)arg;
                }
            }
        }

        private void Update()
        {
            if (m_IsFinish)
                return;

            if (m_Async != null)
            {
                m_Progress = m_Async.progress;
            }
            else
            {
                float per = (Time.time - m_bTime) / m_Duration;
                per = per > 1 ? 1 : per;
                m_Progress = per;
            }

            Vector2 size = new Vector2(1200 * m_Progress, 28);
            m_BarImg.rectTransform.sizeDelta = size;
            if (m_Progress >= 1)
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
