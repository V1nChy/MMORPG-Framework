using CodeX;
using LuaFramework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class DownloadUpdateHistory : Singleton<DownloadUpdateHistory> {

    private Dictionary<string, bool> m_download_history = new Dictionary<string, bool>();
    private float m_last_save_download_history_time = 0f;
    private int m_last_save_download_history_count = 0;

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

    public void UpdateSaveDownloadHistory()
    {
        if (!(Time.time - this.m_last_save_download_history_time < 10f))
        {
            SaveDownloadHistory();
        }
    }

    public void SaveDownloadHistory()
    {
        if (!(this.m_download_history.Count == 0 || this.m_download_history.Count == this.m_last_save_download_history_count))
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
