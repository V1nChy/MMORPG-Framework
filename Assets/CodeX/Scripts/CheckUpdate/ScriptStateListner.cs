using System;
using LuaInterface;
using GFW;

namespace CodeX
{
	public class ScriptStateListner : IGameStateListner
	{
        protected LuaFunction m_state_enter_func;

        protected LuaFunction m_state_update_func;

        protected LuaFunction m_state_exit_func;

		public ScriptStateListner(LuaFunction state_enter_func, LuaFunction state_update_func, LuaFunction state_exit_func)
		{
			this.m_state_enter_func = state_enter_func;
			this.m_state_update_func = state_update_func;
			this.m_state_exit_func = state_exit_func;
		}

		public void Dispose()
		{
		}

		public override void OnStateEnter(GameState pCurState)
		{
			this.m_state_enter_func.Call(new object[]
			{
				pCurState.GetName()
			});
		}

		public override void OnStateQuit(GameState pCurState)
		{
			this.m_state_exit_func.Call(new object[]
			{
				pCurState.GetName()
			});
		}

		public override void OnStateUpdate(GameState pCurState, float elapseTime)
		{
			this.m_state_update_func.Call(new object[]
			{
				pCurState.GetName(),
				elapseTime
			});
		}

		public override void Free()
		{
			this.Dispose();
		}
	}
}
