using System;
using System.Collections.Generic;
using UnityEngine;
using CodeX;
using GFW.ManagerSystem;
using GFW;
using LuaInterface;
using UnityEngine.UI;

namespace CodeX
{
    public class UIAgentManager : ManagerModule
    {
        private ResourceManager m_ResMgr;

        private Dictionary<string, Transform> m_tagRoot = new Dictionary<string, Transform>();

        private static string[] tags = new string[]
        {
            "NameBoard",
            "Dynamic_NameBoard",
            "Scene",
            "Main",
            "UI",
            "Activity",
            "Top",
            "SceneRoot",
        };

        public override void Start()
        {
            base.Start();
            m_ResMgr = GameSystem.Instance.GetManager<ResourceManager>();
            for (int i = 0; i < UIAgentManager.tags.Length; i++)
            {
                GameObject go = GameObject.FindWithTag(UIAgentManager.tags[i]);
                if (go != null)
                {
                    this.m_tagRoot.Add(UIAgentManager.tags[i], go.transform);
                }
            }
        }

        public Transform GetUILayer(string layer)
        {
            Transform root = null;
            GameObject go = GameObject.FindWithTag(layer);
            if (go)
                root = go.transform;
            return root;
        }

        public Transform GetTagRoot(string tag)
        {
            Transform tra;
            this.m_tagRoot.TryGetValue(tag, out tra);
            return tra;
        }

        public GameObject NewObject(GameObject prefab)
        {
            try
            {
                return GameObject.Instantiate<GameObject>(prefab);
            }
            catch (Exception e)
            {
                LogMgr.LogError(e.Message);
            }
            return null;
        }

        public void CreateView(string base_file, string layout_file, string layerName, LuaFunction func = null)
        {
            string abName = base_file.ToLower() + AppConst.ExtName;
            Transform root = this.GetTagRoot(layerName);
            if (root == null)
            {
                if (func != null)
                {
                    func.Call();
                }
            }
            else
            {
                m_ResMgr.LoadPrefab(abName, layout_file, delegate (UnityEngine.Object[] objs)
                {
                    if (objs == null || objs.Length == 0)
                    {
                        if (func != null)
                        {
                            func.Call();
                        }
                    }
                    else
                    {
                        GameObject prefab = objs[0] as GameObject;
                        if (prefab == null)
                        {
                            if (func != null)
                            {
                                func.Call();
                            }
                        }
                        else
                        {
                            GameObject go = NewObject(prefab);
                            if (go == null)
                            {
                                if (func != null)
                                {
                                    func.Call();
                                }
                            }
                            else
                            {
                                go.layer = LayerMask.NameToLayer("UI");
                                go.transform.SetParent(root);
                                go.transform.localScale = Vector3.one;
                                go.transform.localPosition = Vector3.zero;
                                if (func != null)
                                {
                                    func.Call(go);
                                }
                            }
                        }
                    }
                });
            }
        }

        public static void SetUIDepth(GameObject go, bool isUI, int order)
        {
            if (isUI)
            {
                Canvas canvas = go.GetComponent<Canvas>();
                GraphicRaycaster caster = go.GetComponent<GraphicRaycaster>();
                if (canvas == null)
                {
                    canvas = go.AddComponent<Canvas>();
                }
                if (caster == null)
                {
                    caster = go.AddComponent<GraphicRaycaster>();
                }
                canvas.overrideSorting = true;
                canvas.sortingOrder = order;
            }
            else
            {
                Renderer[] renders = go.GetComponentsInChildren<Renderer>();
                foreach (Renderer render in renders)
                {
                    render.sortingOrder = order;
                }
            }
        }

        public static void ShowProgressView(AsyncOperation async, LuaFunction func = null)
        {
            Action callback = () => 
            {
                if(func != null)
                {
                    func.Call();
                }
            };
            UIViewHelper.ShowProgressView(async, callback);
        }
    }
}
