using System;

namespace GFW
{
	public abstract class IGameStateListner
	{
		public virtual void OnStateEnter(GameState pCurState)
		{
		}

		public virtual void OnStateQuit(GameState pCurState)
		{
		}

		public virtual void OnStateUpdate(GameState pCurState, float elapseTime)
		{
		}

		public abstract void Free();
	}
}
