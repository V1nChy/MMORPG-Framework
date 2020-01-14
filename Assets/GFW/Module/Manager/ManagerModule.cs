using System.Collections;
using System.Reflection;
using UnityEngine;

namespace GFW
{
    public abstract class ManagerModule : BasicModule
    {
        protected MonoBehaviour m_Mono;

        /////管理器生命周期
        public virtual void Awake(MonoBehaviour mono)
        {
            m_Mono = mono;
        }

        public virtual void Start()
        {
        }

        public virtual void Update()
        {
        }

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            return m_Mono.StartCoroutine(routine);
        }
    }
}