using System;
using GFW;

namespace CodeX
{
    public class LoadViewHelper:ServiceModule<LoadViewHelper>
    {
        UIProgressPage page;

        public void Open()
        {
            page = UIManager.Instance.Open<UIProgressPage>();
        }

        public void Close()
        {
            page.Close();
        }

        public void SetProgress(float value)
        {
            page.SetProgress(value);
        }
    }
}
