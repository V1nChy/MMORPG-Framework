using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GFW;

namespace CodeX
{
    public class GameSystemMono : MonoBehaviour {

        private string m_EntityName = "AppMain";
        private static GameSystemMono m_instance;

        public GameSystemMono Instance
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

                    m_instance = obj.GetComponent<GameSystemMono>();
                    if (m_instance == null)
                    {
                        m_instance = obj.AddComponent<GameSystemMono>();
                    }
                }
                return m_instance;
            }
        }

        public static void StartUp()
        {
            
        }

        private void Awake() {
            GameSystem.Instance.OnAwake(m_instance);
        }

        // Use this for initialization
        void Start() {
            GameSystem.Instance.OnStart();
        }

        // Update is called once per frame
        void Update() {
            GameSystem.Instance.OnUpdate();
        }

        private void OnDestroy()
        {
            GameSystem.Instance.Release();
        }
    }
}
