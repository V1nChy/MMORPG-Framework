using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LuaFramework
{
    public class LuaEventListener : EventTrigger
    {
        public delegate void VoidDelegate(GameObject go, float x, float y);
        public LuaEventListener.VoidDelegate onClick;
        public LuaEventListener.VoidDelegate onDown;
        public LuaEventListener.VoidDelegate onEnter;
        public LuaEventListener.VoidDelegate onExit;
        public LuaEventListener.VoidDelegate onUp;
        public LuaEventListener.VoidDelegate onSelect;
        public LuaEventListener.VoidDelegate onUpdateSelect;
        public LuaEventListener.VoidDelegate onDrag;
        public LuaEventListener.VoidDelegate onDragBegin;
        public LuaEventListener.VoidDelegate onDragEnd;
        public LuaEventListener.VoidDelegate onDrop;

        public int pre_event_type = -1;
        public int curr_event_type = 0;

        public static LuaEventListener Get(GameObject go)
        {
            LuaEventListener listener = go.GetComponent<LuaEventListener>();
            if (listener == null)
            {
                listener = go.AddComponent<LuaEventListener>();
            }
            return listener;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (this.onClick != null)
            {
                this.pre_event_type = this.curr_event_type;
                this.curr_event_type = 1;
                this.onClick(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (this.onDown != null)
            {
                this.pre_event_type = this.curr_event_type;
                this.curr_event_type = 2;
                this.onDown(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (this.onEnter != null)
            {
                this.pre_event_type = this.curr_event_type;
                this.curr_event_type = 3;
                this.onEnter(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (this.onExit != null)
            {
                this.pre_event_type = this.curr_event_type;
                this.curr_event_type = 4;
                this.onExit(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (this.onUp != null)
            {
                this.pre_event_type = this.curr_event_type;
                this.curr_event_type = 5;
                this.onUp(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (this.onDrag != null)
            {
                this.pre_event_type = this.curr_event_type;
                this.curr_event_type = 8;
                this.onDrag(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (this.onDragBegin != null)
            {
                this.pre_event_type = this.curr_event_type;
                this.curr_event_type = 9;
                this.onDragBegin(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (this.onDragEnd != null)
            {
                this.pre_event_type = this.curr_event_type;
                this.curr_event_type = 10;
                this.onDragEnd(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }

        public override void OnDrop(PointerEventData eventData)
        {
            if (this.onDrop != null)
            {
                this.pre_event_type = this.curr_event_type;
                this.curr_event_type = 10;
                this.onDrop(base.gameObject, eventData.position.x, eventData.position.y);
            }
        }
    }
}
