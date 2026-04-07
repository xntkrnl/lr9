using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lr9.Code
{
    abstract public class Singleton<T> where T : class, new()
    {
        private static T? _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new T();

                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        protected Singleton() { }
    }
}
