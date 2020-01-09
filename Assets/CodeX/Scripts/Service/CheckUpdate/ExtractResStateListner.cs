using System;
using System.Collections;
using System.IO;
using UnityEngine;
using GFW;

namespace CodeX
{
    public class ExtractResStateListner : IGameStateListner
    {
        public bool m_finish_state = false;
        public string[] m_files = null;
        public int m_file_count = 0;

        public override void OnStateEnter(GameState pCurState)
        {
            this.Log("ExtractResStateListner@OnStateEnter()");
            string update_mess = GameConfig.Instance.GetValue("GameResExtract");
            ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "UpdateMessage", GameConfig.Instance["GameResExtract"]);
            this.CheckExtractResource();
        }

        public void CheckExtractResource()
        {
            bool debugMode = AppConst.DebugMode;
            if (debugMode)
            {
                this.ToNextState();
            }
            else
            {
                MonoHelper.StartCoroutine(this.OnExtractResource());
            }
        }

        public void ToNextState()
        {
            bool updateMode = AppConst.UpdateMode;
            if (updateMode)
            {
                CheckUpdateService.Instance.ToUpdateVersionFileState();
            }
            else
            {
                CheckUpdateService.Instance.ToPreResState();
            }
        }

        private IEnumerator OnExtractResource()
        {
            string dataPath = AppUtil.DataPath;
            string resPath = AppUtil.AppContentPath();
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
            string filename = GameConfig.Instance.GetValue("FireTxt");
            string infile = resPath + filename;
            string outfile = dataPath + filename;
            if (File.Exists(outfile))
            {
                File.Delete(outfile);
            }
            this.Log(infile);
            this.Log(outfile);
            bool has_data = false;
            if (Application.platform == RuntimePlatform.Android)
            {
                WWW www = new WWW(infile);
                yield return www;
                if (www.isDone)
                {
                    File.WriteAllBytes(outfile, www.bytes);
                    has_data = true;
                }
                www.Dispose();
                www = null;
            }
            else
            {
                if (File.Exists(infile))
                {
                    File.Copy(infile, outfile, true);
                    has_data = true;
                }
            }

            if (has_data)
            {
                this.m_files = File.ReadAllLines(outfile);
                bool flag7 = this.m_files == null || this.m_files.Length == 0;
                if (flag7)
                {
                    this.m_finish_state = true;
                    this.m_files = null;
                }
                else
                {
                    foreach (string file in this.m_files)
                    {
                        infile = resPath + file;
                        outfile = dataPath + file;
                        float process = (float)this.m_file_count * 100f / (float)this.m_files.Length;
                        process = (float)Math.Ceiling((double)process);
                        string message = GameConfig.Instance.GetValue("GameResExtract") + process + "%";
                        ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "UpdateMessage", message);
                        ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "UpdateProgress", process);
                        bool flag8 = File.Exists(outfile);
                        if (flag8)
                        {
                            File.Delete(outfile);
                        }
                        string dir = Path.GetDirectoryName(outfile);
                        bool flag9 = !Directory.Exists(dir);
                        if (flag9)
                        {
                            Directory.CreateDirectory(dir);
                        }
                        bool flag10 = Application.platform == RuntimePlatform.Android;
                        if (flag10)
                        {
                            WWW www2 = new WWW(infile);
                            yield return www2;
                            bool flag11 = www2.isDone && string.IsNullOrEmpty(www2.error);
                            if (flag11)
                            {
                                CompressHelper.DeCompressFromByte(dataPath, www2.bytes);
                            }
                            else
                            {
                                Debug.LogWarning("init data error:" + www2.error);
                            }
                            this.m_file_count++;
                            www2.Dispose();
                            www2 = null;
                            bool lowSystemMode = AppConst.LowSystemMode;
                            if (lowSystemMode)
                            {
                                yield return new WaitForSeconds(0.2f);
                            }
                            www2 = null;
                        }
                        else
                        {
                            bool flag12 = File.Exists(infile);
                            if (flag12)
                            {
                                File.Copy(infile, outfile, true);
                                CompressHelper.DeCompressFromFile(outfile, dataPath);
                                this.m_file_count++;
                            }
                            bool lowSystemMode2 = AppConst.LowSystemMode;
                            if (lowSystemMode2)
                            {
                                yield return new WaitForSeconds(0.2f);
                            }
                            else
                            {
                                yield return new WaitForSeconds(0.05f);
                            }
                        }
                        bool flag13 = File.Exists(outfile);
                        if (flag13)
                        {
                            File.Delete(outfile);
                        }
                        bool flag14 = this.m_file_count >= this.m_files.Length;
                        if (flag14)
                        {
                            this.m_finish_state = true;
                            message = GameConfig.Instance.GetValue("ExractResSuc");
                            ModuleManager.Instance.SendMessage(ModuleDef.LaunchModule, "SendMessageCommand", "UpdateMessage", GameConfig.Instance.GetValue("ExractResSuc"));
                        }
                        message = null;
                        dir = null;
                    }
                    string[] array = null;
                }
            }
            else
            {
                this.m_finish_state = true;
            }
            yield break;
        }

        // Token: 0x06000EA9 RID: 3753 RVA: 0x00002AC4 File Offset: 0x00000CC4
        public override void OnStateQuit(GameState pCurState)
        {
        }

        // Token: 0x06000EAA RID: 3754 RVA: 0x0009E86C File Offset: 0x0009CA6C
        public override void OnStateUpdate(GameState pCurState, float elapseTime)
        {
            if (this.m_finish_state)
            {
                this.ToNextState();
            }
        }

        // Token: 0x06000EAB RID: 3755 RVA: 0x00002AC4 File Offset: 0x00000CC4
        public override void Free()
        {
        }
    }
}
