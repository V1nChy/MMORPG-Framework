using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFW
{
    public class UIRoot : MonoBehaviour
    {
        public const string NAME = "UIRoot";
        private static GameObject root;
        private static Dictionary<string, GameObject> m_dicLayer;
        public static GameObject Root
        {
            get
            {
                if (root == null)
                {
                    root = GameObject.Find(NAME);
                    if (root == null)
                    {
                        LogManager.LogError("UIRoot GameObject Is Not Exist!");
                    }
                    if (root.GetComponent<UIRoot>() == null)
                    {
                        root.AddComponent<UIRoot>();
                    }
                }
                return root;
            }
        }

        /// <summary>
        /// 从UIRoot下通过名字&类型寻找一个组件对象
        /// </summary>
        public static T Find<T>(string name) where T:MonoBehaviour
        {
            GameObject obj = Find(name);
            if(obj != null)
            {
                return obj.GetComponent<T>();
            }
            LogManager.LogWarning("Component:{0} Not Exist！", typeof(T).Name);
            return null;
        }

        void Awake()
        {
            DontDestroyOnLoad(root.transform.root.gameObject);
        }

        /// <summary>
        /// 在UIRoot下通过名字寻找一个GameObject对象
        /// </summary>
        public static GameObject Find(string name)
        {
            Transform obj = null;
            if (Root != null)
            {
                obj = Root.transform.Find(name);
            }
            if(obj != null)
            {
                return obj.gameObject;
            }
            LogManager.LogWarning("UIRoot@Find() ,UI:{0} Not Exist！", name);
            return null;
        }

        public static void AddChild(UIPanel child, string layerName)
        {
            if(Root == null || child == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(layerName))
                layerName = "Top";

            GameObject parent = GetLayer(layerName);
            child.transform.SetParent(parent.transform, false);
        }
        public static GameObject GetLayer(string layerName)
        {
            if (m_dicLayer == null)
                m_dicLayer = new Dictionary<string, GameObject>();
            GameObject layer;
            if (!m_dicLayer.ContainsKey(layerName))
            {
                layer = Find(layerName);
                if(layer != null)
                {
                    m_dicLayer[layerName] = layer;
                }
                else
                {
                    LogManager.LogError("UIRoot@GetLayer, {0} Is Not Exist!", layerName);
                }
            }
            else
            {
                layer = m_dicLayer[layerName];
            }
            return layer;
        }
    }
}
