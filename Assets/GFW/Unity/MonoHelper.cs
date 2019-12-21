using System;
using System.Collections;
using UnityEngine;

namespace GFW
{
    public delegate void MonoUpdaterEvent();

    public class MonoHelper : MonoSingleton<MonoHelper>
    {
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

        public static void StartCoroutine(IEnumerator routine)
        {
            MonoBehaviour mono = Instance;
            mono.StartCoroutine(routine);
        }
    }
}
