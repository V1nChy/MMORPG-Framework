using System;
using UnityEngine;
using GFW;

namespace CodeX
{
    public static class UIViewHelper
    {
        public static void ShowProgressView(object arg = null, Action finishCallback = null)
        {
            UIProgressPage ui = UIManager.Instance.Open<UIProgressPage>(arg);
            if(finishCallback != null)
                ui.SetFinishCallback(finishCallback);
        }
    }
}