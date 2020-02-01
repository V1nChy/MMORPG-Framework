using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LuaFramework
{
    public class LuaClickListener : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
    {
        public delegate void VoidDelegate(GameObject go, float x, float y);

        public LuaClickListener.VoidDelegate onClick;

        public static LuaClickListener Get(GameObject go)
        {
            LuaClickListener listener = go.GetComponent<LuaClickListener>();
            if (listener == null)
            {
                listener = go.AddComponent<LuaClickListener>();
            }
            return listener;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (this.onClick != null)
            {
                this.onClick(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }
    }
}
