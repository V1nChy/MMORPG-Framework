using UnityEngine;

namespace GFW.ManagerSystem
{
    public class MonoBase : MonoBehaviour
    {
        protected virtual void Awake()
        {
            this.Log(this.GetType().Name + " Awake");
        }

        protected virtual void Start()
        {
            this.Log(this.GetType().Name + " Start");
        }
    }
}
