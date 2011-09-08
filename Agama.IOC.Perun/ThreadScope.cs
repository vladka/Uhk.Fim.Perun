using System;
using System.Threading;

namespace Agama.IOC.Perun
{
    public class ThreadScope : IPerunScope
    {

        private static readonly Lazy<ThreadScope> _instance
            = new Lazy<ThreadScope>(() => new ThreadScope());

        // private to prevent direct instantiation.
        private ThreadScope()
        {
        }

        // accessor for instance
        public static ThreadScope Instance
        {
            get
            {
                return _instance.Value;
            }
        }
        public object Context
        {
            get { return Thread.CurrentThread; }
        }
    }
}