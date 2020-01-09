using System;

namespace GFW
{
    public abstract class ServiceModule<T> : Module where T : ServiceModule<T>,new()
    {
        protected static T ms_instance = default(T);

        public static T Instance
        {
            get
            {
                if(ms_instance == null)
                {
                    ms_instance = new T();
                }

                return ms_instance;
            }
        }

        public ServiceModule()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {

        }
    }
}
