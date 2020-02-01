using System;
using LuaInterface;

namespace GFW
{
    public class ScriptAllStateListner : IGameStateListner
    {
        protected GameStateMachine m_state_machine;
        protected LuaFunction m_state_enter_func;
        protected LuaFunction m_state_exit_func;
        public ScriptAllStateListner(GameStateMachine state_machine, LuaFunction state_enter_func, LuaFunction state_exit_func)
        {
            this.m_state_enter_func = state_enter_func;
            this.m_state_exit_func = state_exit_func;
            this.m_state_machine = state_machine;
        }

        public void Dispose()
        {
        }

        public override void OnStateEnter(GameState pCurState)
        {
            ushort sink_id = pCurState.GetSinkId();
            string sink_name = this.m_state_machine.GetSinkNameFromId(sink_id);
            this.m_state_enter_func.Call(new object[]
			{
				pCurState.GetName(),
				sink_name
			});
        }

        public override void OnStateQuit(GameState pCurState)
        {
            ushort sink_id = pCurState.GetSinkId();
            string sink_name = this.m_state_machine.GetSinkNameFromId(sink_id);
            this.m_state_exit_func.Call(new object[]
			{
				pCurState.GetName(),
				sink_name
			});
        }

        public override void OnStateUpdate(GameState pCurState, float elapseTime)
        {
        }

        public override void Free()
        {
            this.Dispose();
        }
    }
}
