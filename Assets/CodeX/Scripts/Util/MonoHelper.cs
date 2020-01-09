using System;
using System.Collections;
using UnityEngine;

namespace GFW
{
    public delegate void MonoUpdaterEvent();

    public class MonoHelper:MonoBehaviour
    {
        private const string m_EntityName = "AppMain";
        private static MonoHelper m_instance;

        public static MonoHelper Instance
        {
            get
            {
                if (m_instance == null)
                {
                    GameObject obj = GameObject.Find(m_EntityName);
                    if (obj == null)
                    {
                        obj = new GameObject(m_EntityName);
                    }
                    GameObject.DontDestroyOnLoad(obj);

                    m_instance = obj.GetComponent<MonoHelper>();
                    if (m_instance == null)
                    {
                        m_instance = obj.AddComponent<MonoHelper>();
                    }
                }
                return m_instance;
            }
        }

        private event MonoUpdaterEvent UpdateEvent;
        private event MonoUpdaterEvent FixedUpdateEvent;

        public static void AddUpdateListener(MonoUpdaterEvent listener)
        {
            if (Instance != null)
            {
                Instance.UpdateEvent += listener;
            }
        }

        public static void RemoveUpdateListener(MonoUpdaterEvent listener)
        {
            if (Instance != null)
            {
                Instance.UpdateEvent -= listener;
            }
        }

        public static void AddFixedUpdateListener(MonoUpdaterEvent listener)
        {
            if (Instance != null)
            {
                Instance.FixedUpdateEvent += listener;
            }
        }

        public static void RemoveFixedUpdateListener(MonoUpdaterEvent listener)
        {
            if (Instance != null)
            {
                Instance.FixedUpdateEvent -= listener;
            }
        }

        void Update()
        {
            if (UpdateEvent != null)
            {
                UpdateEvent();
            }
        }

        void FixedUpdate()
        {
            if (FixedUpdateEvent != null)
            {
                FixedUpdateEvent();
            }
        }

        //===========================================================

        public static new Coroutine StartCoroutine(IEnumerator routine)
        {
            MonoBehaviour mono = Instance;
            return mono.StartCoroutine(routine);
        }
    }
}
