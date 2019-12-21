using System;
using System.Collections.Generic;
using LuaInterface;

namespace GFW
{
    public enum StateComposeType
    {
        SCT_NORMAL,
        SCT_SUB,
        SCT_COMPOSE
    }

    public enum StateCallbackType
    {
        CALLT_NONE,
        CALLT_MULIT_FUNC_OBJ,
        CALLT_FUNC_IN_TABLE,
        CALLT_C_LISTNER
    }

	public class GameState
	{
        protected GameStateMachine m_parent_machine;
        protected uint m_state_id = 0u;
        protected string m_state_name;
        protected ushort m_sink_id;
        protected StateComposeType m_compose_type;

        protected List<uint> m_out_state_id_list = new List<uint>();
        protected List<string> m_out_state_name_list = new List<string>();

        public List<uint> m_sub_state_id_list = new List<uint>();

        public uint m_cur_run_sub = 0u;

        public uint m_previous_sub = 0u;

        public uint m_parent_state_id = 0u;

        protected IGameStateListner m_listner;

        protected uint m_action_flag;

        protected bool m_can_re_enter = false;
        protected bool m_is_state_lock = false;

        protected StateCallbackType m_callback_type;

        #region - 构造析构
        public GameState(GameStateMachine parent_machine, uint state_id, string state_name, ushort sink_id, StateComposeType sct)
		{
			this.m_parent_machine = parent_machine;
            this.m_state_id = state_id;
            this.m_state_name = state_name;
            this.m_sink_id = sink_id;
            this.m_compose_type = sct;

			this.m_action_flag = uint.MaxValue;
			this.m_can_re_enter = false;
			this.m_is_state_lock = false;
			this.m_listner = null;
			this.m_parent_state_id = ushort.MaxValue;
		}
		public virtual void Dispose()
		{
			this.ClearCallbackInfo();
		}
        protected void ClearCallbackInfo()
        {
            if (this.m_listner != null)
            {
                this.m_listner.Free();
            }
            this.m_listner = null;
        }
        #endregion

        public void SetCallbackAsCListner(IGameStateListner listner)
        {
            this.ClearCallbackInfo();
            this.m_listner = listner;
        }

		public void Enter()
		{
			this.RunEnterFunction();
            if (this.m_compose_type == StateComposeType.SCT_COMPOSE)
			{
				GameState cur_sub_state = this.m_parent_machine.FindState(this.m_cur_run_sub);
                if (cur_sub_state != null)
				{
					cur_sub_state.Enter();
				}
			}
		}
        protected void RunEnterFunction()
        {
            if (this.m_listner != null)
            {
                this.m_listner.OnStateEnter(this);
            }
            this.m_parent_machine.OnStateEnterNotify(this);
        }

		public void Excute(float elapsedTime)
		{
			this.RunExcuteFunction(elapsedTime);
            if (this.m_compose_type == StateComposeType.SCT_COMPOSE)
			{
				GameState cur_sub_state = this.m_parent_machine.FindState(this.m_cur_run_sub);
                if (cur_sub_state != null)
				{
					cur_sub_state.Excute(elapsedTime);
				}
			}
		}
        protected void RunExcuteFunction(float elapse_time)
        {
            if (this.m_listner != null)
            {
                this.m_listner.OnStateUpdate(this, elapse_time);
            }
        }

		public void Exit()
		{
            if (this.m_compose_type == StateComposeType.SCT_COMPOSE)
			{
				GameState cur_sub_state = this.m_parent_machine.FindState(this.m_cur_run_sub);
                if (cur_sub_state != null)
				{
					cur_sub_state.Exit();
				}
			}
			this.RunExitFunction();
		}
        protected void RunExitFunction()
        {
            if (this.m_listner != null)
            {
                this.m_listner.OnStateQuit(this);
            }
            this.m_parent_machine.OnStateLeaveNotify(this);
        }

        public bool CanChangeFromState(uint state_id)
        {
            bool can_change = false;
            GameState old_state = this.m_parent_machine.FindState(state_id);
            if (old_state != null)
            {
                if (old_state.GetSinkId() == this.GetSinkId())
                {
                    uint resultId = old_state.m_out_state_id_list.Find((uint user) => user == this.m_state_id);
                    string resultName = old_state.m_out_state_name_list.Find((string user) => user == this.m_state_name);
                    if (resultId != 0u || resultName != null)
                    {
                        can_change = true;
                    }
                }
            }
            else
            {
                can_change = true;
            }
            return can_change;
        }

		public ushort GetSinkId()
		{
			return this.m_sink_id;
		}
		public uint GetId()
		{
			return this.m_state_id;
		}
		public string GetName()
		{
			return this.m_state_name;
		}
		public StateComposeType GetComposeType()
		{
			return this.m_compose_type;
		}
		public IGameStateListner GetListner()
		{
			return this.m_listner;
		}

		public void AddOutStateId(uint stateId)
		{
			this.m_out_state_id_list.Add(stateId);
		}
		public void RemoveOutStateId(uint state_id)
		{
			this.m_out_state_id_list.Remove(state_id);
		}
		public void AddOutStateName(string state_name)
		{
			this.m_out_state_name_list.Add(state_name);
		}
		public void RemoveOutStateName(string state_name)
		{
			this.m_out_state_name_list.Remove(state_name);
		}

		public void SetActionFlag(uint actionFlag)
		{
			this.m_action_flag = actionFlag;
		}
		public uint GetActionFlag()
		{
			uint tmp_act_flag = this.m_action_flag;
			bool flag = this.m_compose_type == StateComposeType.SCT_COMPOSE;
			if (flag)
			{
				GameState cur_sub_state = this.m_parent_machine.FindState(this.m_cur_run_sub);
				bool flag2 = cur_sub_state != null;
				if (flag2)
				{
					tmp_act_flag = cur_sub_state.GetActionFlag();
				}
			}
			return tmp_act_flag;
		}
		public void SetSingleActionFlag(uint aFlag)
		{
			this.SetSingleActionFlag(aFlag, true);
		}
		public void SetSingleActionFlag(uint aFlag, bool bSet)
		{
			if (bSet)
			{
				this.m_action_flag |= aFlag;
			}
			else
			{
				this.m_action_flag &= ~aFlag;
			}
		}
		public bool IsStateLock()
		{
			bool flag = this.m_compose_type == StateComposeType.SCT_COMPOSE;
			bool result;
			if (flag)
			{
				GameState cur_sub_state = this.m_parent_machine.FindState(this.m_cur_run_sub);
				bool flag2 = cur_sub_state != null;
				result = (flag2 && cur_sub_state.IsStateLock());
			}
			else
			{
				result = this.m_is_state_lock;
			}
			return result;
		}
		public void SetIsStateLock(bool isLock)
		{
			this.m_is_state_lock = isLock;
		}
		public bool IsStateReEnter()
		{
            if (this.m_compose_type == StateComposeType.SCT_COMPOSE)
			{
				GameState cur_sub_state = this.m_parent_machine.FindState(this.m_cur_run_sub);
                return cur_sub_state == null || cur_sub_state.IsStateReEnter();
			}
			else
			{
				return this.m_can_re_enter;
			}
		}
		public void SetStateCanReEnter(bool canReEnter)
		{
			this.m_can_re_enter = canReEnter;
		}
		public void DestroyAllSubStates()
		{
			bool flag = this.m_compose_type == StateComposeType.SCT_COMPOSE;
			if (flag)
			{
				foreach (uint item in this.m_sub_state_id_list)
				{
					this.m_parent_machine.DestroyState(item);
				}
				this.m_sub_state_id_list.Clear();
			}
		}
		public void SetCallbackAsMultiFuctions(LuaFunction enter_func, LuaFunction excute_func, LuaFunction exit_func)
		{
			ScriptStateListner listner = new ScriptStateListner(enter_func, excute_func, exit_func);
			this.SetCallbackAsCListner(listner);
		}
		public void SetCallbackAsTable(LuaTable table_func)
		{
			ScriptStateTableListner listner = new ScriptStateTableListner(table_func);
			this.SetCallbackAsCListner(listner);
		}
	}
}
