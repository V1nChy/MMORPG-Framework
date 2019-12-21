namespace GFW
{
    public class LuaModule : BusinessModule
    {
        private object m_args;

        /// <summary>
        /// 构造函数传入Lua模块名
        /// </summary>
        internal LuaModule(string name) : base(name) { }

        public override void Create(object args = null)
        {
            this.Log("创建Lua模块 name = " + Name);
            m_args = args;

            //EventTable mgrEvent = GetEventTable();
            //TODO 需要映射到Lua脚本中
        }

        protected override void Start(object arg)
        {
            base.Start(arg);
            GameSystem.Instance.ExecuteCommand("LuaCommand", Name, "StartModule");
        }

        /// <summary>
        /// 调用它以卸载Lua脚本
        /// </summary>
        public override void Release()
        {
            this.Log("Release Lua = " + Name);
        }
    }
}
