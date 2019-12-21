using UnityEngine;
using System.Collections;
using LuaFramework;

namespace GFW.ManagerSystem
{
    public class Manager : MonoBase, IManager
    {
        public virtual void DoStart()
        {
            this.Log(this.GetType().Name + "DoStart");
        }
    }
}
