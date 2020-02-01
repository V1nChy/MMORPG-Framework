using System;
using System.Collections.Generic;
using LuaInterface;

namespace GFW
{
	public class GameStateMachine
	{
        private class GameStateSink
        {
            public ushort id;

            public string sink_name;

            public uint cur_state_id = ushort.MaxValue;

            public uint previous_state_id = 0u;

            public List<uint> contain_state_id_list = new List<uint>();
        }

        private const ushort SINK_ID_CAPACITY = 16;
        private List<ushort> m_free_sink_id_list = new List<ushort>(SINK_ID_CAPACITY);
        private List<GameStateMachine.GameStateSink> m_state_sink_array = new List<GameStateMachine.GameStateSink>();

        private const ushort STATE_ID_CAPACITY = 64;
        private List<uint> m_free_state_id_list = new List<uint>();
        private List<GameState> m_state_array = new List<GameState>();

        private IGameStateListner mAllStateListener;

		public GameStateMachine()
		{
			mAllStateListener = null;
            //初始化分支
            for (ushort i = 0; i < SINK_ID_CAPACITY; i++)
            {
                m_free_sink_id_list.Add(i);
                m_state_sink_array.Add(null);
            }

            //初始化状态
            for (ushort i = 0; i < STATE_ID_CAPACITY; i++)
            {
                m_free_state_id_list.Add(i);
                m_state_array.Add(null);
            }
		}

		public void Dispose()
		{
			for (int i = 0; i < 16; i += 1)
			{
                if (m_state_sink_array[i] != null)
				{
					m_state_sink_array[i] = null;
				}
			}
			m_state_sink_array.Clear();

			int j = 0;
			while (j < 64)
			{
                if (m_state_array[j] != null)
				{
					m_state_array[j] = null;
				}
				j++;
			}
			this.m_state_array.Clear();

            if (mAllStateListener != null)
			{
				mAllStateListener.Free();
				mAllStateListener = null;
			}
		}

		public void UpdateNow(float elapse_time)
		{
			for (int i = 0; i < m_state_sink_array.Count; i++)
			{
				GameStateMachine.GameStateSink sink = m_state_sink_array[i];
                if (sink != null)
				{
					GameState state = FindState(sink.cur_state_id);
                    if (state != null)
					{
						state.Excute(elapse_time);
					}
				}
			}
		}

        #region - 创建分支
        public bool CreateSink(ushort sink_id)
        {
            string sink_name = GetDefaultSinkNameFromId(sink_id);
            return this.CreateSinkImpl(sink_id, sink_name);
        }
        public bool CreateSink(string sink_name)
        {
            if (m_free_sink_id_list.Count > 0)
            {
                ushort sink_id = m_free_sink_id_list[0];
                return CreateSinkImpl(sink_id, sink_name);
            }
            return false;
        }
        private bool CreateSinkImpl(ushort sink_id, string sink_name)
        {
            if (sink_id < SINK_ID_CAPACITY && this.m_state_sink_array[(int)sink_id] == null)
            {
                GameStateMachine.GameStateSink sink = new GameStateMachine.GameStateSink();
                sink.id = sink_id;
                sink.sink_name = sink_name;
                sink.cur_state_id = ushort.MaxValue;
                sink.previous_state_id = ushort.MaxValue;
                this.m_state_sink_array[(int)sink_id] = sink;
                this.m_free_sink_id_list.Remove(sink_id);
                return true;
            }
            return false;
        }
        #endregion

        #region - 创建状态
        public GameState CreateNormalState(uint state_id, ushort sink_id)
        {
            string state_name = GetDefaultStateNameFromId(state_id);
            return this.CreateStateImpl(state_id, state_name, sink_id, StateComposeType.SCT_NORMAL, ushort.MaxValue);
        }
        public GameState CreateNormalState(string state_name, string sink_name)
        {
            GameStateMachine.GameStateSink sink = FindSink(sink_name);
            if (sink != null)
            {
                if (m_free_state_id_list.Count > 0)
                {
                    uint state_id = m_free_state_id_list[0];
                    return this.CreateStateImpl(state_id, state_name, sink.id, StateComposeType.SCT_NORMAL, ushort.MaxValue);
                }
            }
            return null;
        }
        private GameState CreateStateImpl(uint state_id, string state_name, ushort sink_id, StateComposeType com_type)
        {
            return this.CreateStateImpl(state_id, state_name, sink_id, com_type, ushort.MaxValue);
        }
        private GameState CreateStateImpl(uint state_id, string state_name, ushort sink_id, StateComposeType com_type, uint parent_id)
        {
            GameState state = null;
            GameStateMachine.GameStateSink sink = FindSink(sink_id);
            if (sink != null && state_id < STATE_ID_CAPACITY && m_state_array[(int)state_id] == null)
            {
                if (com_type == StateComposeType.SCT_COMPOSE || com_type == StateComposeType.SCT_NORMAL)
                {
                    state = new GameState(this, state_id, state_name, sink_id, com_type);
                    sink.contain_state_id_list.Add(state_id);
                }
                else if (com_type == StateComposeType.SCT_SUB)
                {
                    GameState parent_state = this.FindState(parent_id);
                    if (parent_state != null && parent_state.GetComposeType() == StateComposeType.SCT_COMPOSE && parent_state.GetSinkId() == sink_id)
                    {
                        state = new GameState(this, state_id, state_name, sink_id, com_type);
                        state.m_parent_state_id = parent_id;
                        parent_state.m_sub_state_id_list.Add(state_id);
                    }
                }
                if (state != null)
                {
                    this.m_state_array[(int)state_id] = state;
                    this.m_free_state_id_list.Remove(state_id);
                }
            }
            return state;
        }
        #endregion

        public bool ChangeState(uint state_id)
		{
			return ChangeState(state_id, false);
		}
		public bool ChangeState(uint state_id, bool ignore_state_lock)
		{
			GameState state = this.FindState(state_id);
            if (state != null)
			{
                if (IsInState(state_id))
				{
                    if (state.IsStateReEnter())
					{
						state.Exit();
						state.Enter();
						return true;
					}
					return false;
				}
				else
				{
					GameStateMachine.GameStateSink sink = FindSink(state.GetSinkId());
					GameState cur_sink_state = FindState(sink.cur_state_id);
                    if (cur_sink_state != null && cur_sink_state.IsStateLock() && !ignore_state_lock)
					{
						return false;
					}

                    if (state.GetComposeType() == StateComposeType.SCT_SUB)
					{
						GameState parent_state = this.FindState(state.m_parent_state_id);
						if (this.IsInState(parent_state.GetId()))
						{
							GameState cur_run_state = this.FindState(parent_state.m_cur_run_sub);
							if (state.CanChangeFromState(parent_state.m_cur_run_sub))
							{
								parent_state.m_previous_sub = parent_state.m_cur_run_sub;
								parent_state.m_cur_run_sub = state_id;
								if (cur_run_state != null)
								{
									cur_run_state.Exit();
								}
								state.Enter();
								return true;
							}
						}
						else
						{
							if (parent_state.CanChangeFromState(sink.cur_state_id))
							{
								parent_state.m_previous_sub = parent_state.m_cur_run_sub;
								parent_state.m_cur_run_sub = state_id;
								sink.previous_state_id = sink.cur_state_id;
								sink.cur_state_id = parent_state.GetId();
								if (cur_sink_state != null)
								{
									cur_sink_state.Exit();
								}
								if (parent_state != null)
								{
									parent_state.Enter();
								}
								return true;
							}
						}
					}
					else
					{
                        if (state.CanChangeFromState(sink.cur_state_id))
						{
							sink.previous_state_id = sink.cur_state_id;
							sink.cur_state_id = state_id;
                            if (cur_sink_state != null)
							{
								cur_sink_state.Exit();
							}
							state.Enter();
							return true;
						}
					}
				}
			}
			return false;
		}
		public bool ChangeState(string state_name)
		{
			return this.ChangeState(state_name, false);
		}
		public bool ChangeState(string state_name, bool ignore_state_lock)
		{
			return this.ChangeState(GetStateIdFromName(state_name), ignore_state_lock);
		}
		public bool ChangeStateForce(uint state_id)
		{
			GameState state = this.FindState(state_id);
            bool result = true;
            if (state != null)
			{
                if (IsInState(state_id))
				{
                    if (state.IsStateReEnter())
					{
						state.Exit();
						state.Enter();
					}
					else
					{
						result = false;
					}
				}
				else
				{
					GameStateMachine.GameStateSink sink = FindSink(state.GetSinkId());
					GameState cur_sink_state = FindState(sink.cur_state_id);
                    if (state.GetComposeType() == StateComposeType.SCT_SUB)
					{
						GameState parent_state = this.FindState(state.m_parent_state_id);
                        if (this.IsInState(parent_state.GetId()))
						{
							GameState cur_run_state = this.FindState(parent_state.m_cur_run_sub);
							if (cur_run_state != null)
							{
								cur_run_state.Exit();
							}
							parent_state.m_previous_sub = parent_state.m_cur_run_sub;
							parent_state.m_cur_run_sub = state_id;
							state.Enter();
						}
						else
						{
							parent_state.m_previous_sub = parent_state.m_cur_run_sub;
							parent_state.m_cur_run_sub = state_id;
							sink.previous_state_id = sink.cur_state_id;
							sink.cur_state_id = parent_state.GetId();
                            if (cur_sink_state != null)
							{
								cur_sink_state.Exit();
							}
                            if (parent_state != null)
							{
								parent_state.Enter();
							}
						}
					}
					else
					{
						sink.previous_state_id = sink.cur_state_id;
						sink.cur_state_id = state_id;
                        if (cur_sink_state != null)
						{
							cur_sink_state.Exit();
						}
						state.Enter();
					}
				}
			}
			else
			{
				result = false;
			}
			return result;
		}
		public bool ChangeStateForce(string state_name)
		{
			return ChangeStateForce(GetStateIdFromName(state_name));
		}

		public bool ChangeStateTest(uint state_id)
		{
			return this.ChangeStateTest(state_id, false);
		}
		public bool ChangeStateTest(uint state_id, bool ignore_state_lock)
		{
			GameState state = this.FindState(state_id);
			if (state != null)
			{
				if (this.IsInState(state_id))
				{
					return state.IsStateReEnter();
				}
				GameStateMachine.GameStateSink sink = this.FindSink(state.GetSinkId());
				GameState cur_sink_state = this.FindState(sink.cur_state_id);
				if (cur_sink_state != null && cur_sink_state.IsStateLock() && !ignore_state_lock)
				{
					return false;
				}
				if (state.GetComposeType() == StateComposeType.SCT_SUB)
				{
					GameState parent_state = this.FindState(state.m_parent_state_id);
					if (this.IsInState(parent_state.GetId()))
					{
						if (state.CanChangeFromState(parent_state.m_cur_run_sub))
						{
							return true;
						}
					}
					else
					{
						if (parent_state.CanChangeFromState(sink.cur_state_id))
						{
							return true;
						}
					}
				}
				else
				{
					if (state.CanChangeFromState(sink.cur_state_id))
					{
						return true;
					}
				}
			}
			return false;
		}
		public bool ChangeStateTest(string state_name)
		{
			return this.ChangeStateTest(state_name, false);
		}
		public bool ChangeStateTest(string state_name, bool ignore_state_lock)
		{
			return this.ChangeStateTest(this.GetStateIdFromName(state_name), ignore_state_lock);
		}

		public bool IsInState(uint state_id)
		{
			bool is_in_state = false;
			GameState state = FindState(state_id);
            if (state != null)
			{
				GameStateMachine.GameStateSink sink = FindSink(state.GetSinkId());
                if (state.GetComposeType() == StateComposeType.SCT_SUB)
				{
					GameState parent_state = FindState(state.m_parent_state_id);
                    if (parent_state != null && parent_state.GetId() == sink.cur_state_id)
					{
						is_in_state = (parent_state.m_cur_run_sub == state_id);
					}
				}
				else
				{
					is_in_state = (sink.cur_state_id == state_id);
				}
			}
			return is_in_state;
		}

		public bool IsInState(string state_name)
		{
			return this.IsInState(this.GetStateIdFromName(state_name));
		}

		public GameState FindState(uint state_id)
		{
			GameState state = null;
            if (state_id != ushort.MaxValue)
			{
                if (state_id < m_state_array.Count)
				{
					state = m_state_array[(int)state_id];
				}
			}
			return state;
		}

		public GameState FindState(string name)
		{
			GameState state = null;
			for (int i = 0; i < m_state_array.Count; i++)
			{
                if (m_state_array[i] != null && m_state_array[i].GetName() == name)
				{
					state = m_state_array[i];
					break;
				}
			}
			return state;
		}

		public uint GetSinkRunState(ushort sink_id)
		{
			uint find_id = ushort.MaxValue;
			GameStateMachine.GameStateSink sink = this.FindSink(sink_id);
			if (sink != null)
			{
				find_id = sink.cur_state_id;
			}
			return find_id;
		}

		public string GetSinkRunState(string sink_name)
		{
			return GetStateNameFromId(GetSinkRunState(GetSinkIdFromName(sink_name)));
		}

		public uint GetCurActionFlag()
		{
			return uint.MaxValue;
		}

		public void SinkToNullState(ushort sink_id)
		{
			GameStateMachine.GameStateSink sink = this.FindSink(sink_id);
			if (sink != null)
			{
				sink.previous_state_id = sink.cur_state_id;
				GameState state = this.FindState(sink.cur_state_id);
				if (state != null)
				{
					state.Exit();
				}
				sink.cur_state_id = ushort.MaxValue;
			}
		}

		public void SinkToNullState(string sink_name)
		{
			this.SinkToNullState(this.GetSinkIdFromName(sink_name));
		}

		public GameState CreateComposeState(uint state_id, ushort sink_id)
		{
			string state_name = this.GetDefaultStateNameFromId(state_id);
			return this.CreateStateImpl(state_id, state_name, sink_id, StateComposeType.SCT_COMPOSE, ushort.MaxValue);
		}
        public GameState CreateComposeState(string state_name, string sink_name)
        {
            GameStateMachine.GameStateSink sink = FindSink(sink_name);
            GameState result = null;
            if (sink != null)
            {
                if (m_free_state_id_list.Count > 0)
                {
                    uint id = m_free_state_id_list[0];
                    result = CreateStateImpl(id, state_name, sink.id, StateComposeType.SCT_COMPOSE, ushort.MaxValue);
                }
            }
            return result;
        }

		public GameState CreateSubState(uint state_id, ushort sink_id, uint parent_id)
		{
			string state_name = this.GetDefaultStateNameFromId(state_id);
			return this.CreateStateImpl(state_id, state_name, sink_id, StateComposeType.SCT_SUB, parent_id);
		}
		public GameState CreateSubState(string state_name, string sink_name, string parent_name)
		{
			GameStateMachine.GameStateSink sink = this.FindSink(sink_name);
			GameState result;
			if (sink == null)
			{
				result = null;
			}
			else
			{
				GameState parent_state = this.FindState(parent_name);
				if (parent_state == null)
				{
					result = null;
				}
				else
				{
					if (this.m_free_state_id_list.Count != 0)
					{
						uint id = this.m_free_state_id_list[0];
						result = this.CreateStateImpl(id, state_name, sink.id, StateComposeType.SCT_SUB, parent_state.GetId());
					}
					else
					{
						result = null;
					}
				}
			}
			return result;
		}

		public void DestroyState(uint state_id)
		{
			GameState state = this.FindState(state_id);
			if (state != null)
			{
				GameStateMachine.GameStateSink sink = this.FindSink(state.GetSinkId());
				uint cur_run_state_id = sink.cur_state_id;
				sink.contain_state_id_list.Remove(state_id);
				bool need_cancel_cur_state = false;
				if (this.IsInState(state_id))
				{
					state.Exit();
					need_cancel_cur_state = true;
				}
				if (state.GetComposeType() == StateComposeType.SCT_SUB)
				{
					GameState parent_state = this.FindState(state.m_parent_state_id);
					if (need_cancel_cur_state)
					{
						parent_state.m_cur_run_sub = ushort.MaxValue;
						parent_state.m_previous_sub = ushort.MaxValue;
					}
					parent_state.m_sub_state_id_list.Remove(state_id);
				}
				else
				{
					sink.contain_state_id_list.Remove(state_id);
					if (need_cancel_cur_state)
					{
						sink.cur_state_id = ushort.MaxValue;
						sink.previous_state_id = ushort.MaxValue;
					}
					if (state.GetComposeType() == StateComposeType.SCT_COMPOSE)
					{
						state.DestroyAllSubStates();
					}
				}
				state.Dispose();
				this.m_state_array[(int)state_id] = null;
				this.m_free_state_id_list.Add(state_id);
			}
		}

		public void DestroyState(string state_name)
		{
			this.DestroyState(this.GetStateIdFromName(state_name));
		}

		public string GetStateNameFromId(uint state_id)
		{
			string tmp_name = "";
			GameState state = FindState(state_id);
            if (state != null)
			{
				tmp_name = state.GetName();
			}
			return tmp_name;
		}

		public uint GetStateIdFromName(string name)
		{
			GameState state = FindState(name);
            uint result = ushort.MaxValue;
            if (state != null)
			{
				result = state.GetId();
			}
			return result;
		}

		public string GetSinkNameFromId(ushort sink_id)
		{
			string tmp_name = "";
			GameStateMachine.GameStateSink sink = this.FindSink(sink_id);
			if (sink != null)
			{
				tmp_name = sink.sink_name;
			}
			return tmp_name;
		}

		public ushort GetSinkIdFromName(string name)
		{
			GameStateMachine.GameStateSink sink = FindSink(name);
            ushort result = ushort.MaxValue;
            if (sink != null)
			{
				result = sink.id;
			}
			return result;
		}

		public void SetListener(LuaFunction enter_func, LuaFunction exit_func)
		{
			if (this.mAllStateListener != null)
			{
				this.mAllStateListener.Free();
				this.mAllStateListener = null;
			}
			this.mAllStateListener = new ScriptAllStateListner(this, enter_func, exit_func);
		}

		public void OnStateEnterNotify(GameState pCurState)
		{
			if (this.mAllStateListener != null)
			{
				this.mAllStateListener.OnStateEnter(pCurState);
			}
		}

		public void OnStateLeaveNotify(GameState pCurState)
		{
            if (this.mAllStateListener != null)
			{
				this.mAllStateListener.OnStateQuit(pCurState);
			}
		}

		private GameStateMachine.GameStateSink FindSink(ushort sink_id)
		{
			GameStateMachine.GameStateSink sink = null;
            if (sink_id != ushort.MaxValue)
			{
                if (this.m_state_sink_array.Count >= 0)
				{
					sink = this.m_state_sink_array[(int)sink_id];
				}
			}
			return sink;
		}

		private GameStateMachine.GameStateSink FindSink(string name)
		{
			GameStateMachine.GameStateSink sink = null;
			for (int i = 0; i < this.m_state_sink_array.Count; i++)
			{
                if (this.m_state_sink_array[i] != null && this.m_state_sink_array[i].sink_name == name)
				{
					sink = this.m_state_sink_array[i];
					break;
				}
			}
			return sink;
		}

		private string GetDefaultStateNameFromId(uint id)
		{
			return "_def_sink_" + (int)id;
		}

		private string GetDefaultSinkNameFromId(ushort id)
		{
			return "_def_sink_" + (int)id;
		}
	}
}
