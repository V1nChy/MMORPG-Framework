using System;
using LuaInterface;

namespace GFW
{
    public class ScriptStateTableListner : IGameStateListner
    {
        protected LuaTable m_state_func_table;
        protected bool m_is_table_valid;

        public ScriptStateTableListner(LuaTable state_func_table)
        {
            this.m_state_func_table = state_func_table;
            this.m_is_table_valid = true;
        }

        public void Dispose()
        {
        }

        public override void OnStateEnter(GameState pCurState)
        {
            LuaFunction cur_func = this.m_state_func_table.GetLuaFunction("StateEnter");
            cur_func.Call(new object[]
			{
				pCurState.GetName()
			});
        }

        public override void OnStateQuit(GameState pCurState)
        {
            LuaFunction cur_func = this.m_state_func_table.GetLuaFunction("StateQuit");
            cur_func.Call(new object[]
			{
				pCurState.GetName()
			});
        }

        public override void OnStateUpdate(GameState pCurState, float elapseTime)
        {
            LuaFunction cur_func = this.m_state_func_table.GetLuaFunction("StateUpdate");
            cur_func.Call(new object[]
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
