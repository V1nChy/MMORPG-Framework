using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LuaFramework
{
    public class LuaDragListener : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler, IEndDragHandler
    {
        public delegate void VoidDelegate(GameObject go, float x, float y);
        public LuaDragListener.VoidDelegate onDrag;
        public LuaDragListener.VoidDelegate onDragBegin;
        public LuaDragListener.VoidDelegate onDragEnd;

        public static LuaDragListener Get(GameObject go)
        {
            LuaDragListener listener = go.GetComponent<LuaDragListener>();
            if (listener == null)
            {
                listener = go.AddComponent<LuaDragListener>();
            }
            return listener;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (this.onDragBegin != null)
            {
                this.onDragBegin(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (this.onDrag != null)
            {
                this.onDrag(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (this.onDragEnd != null)
            {
                this.onDragEnd(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }
    }
}
