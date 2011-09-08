using System;
using System.Threading;

namespace Agama.Perun
{
    /// <summary>
    /// Class providing life cycles per thread.
    /// </summary>
    public class ThreadScope : IPerunScope
    {

        private static readonly Lazy<ThreadScope> _instance
            = new Lazy<ThreadScope>(() => new ThreadScope());

        // private to prevent direct instantiation.
        private ThreadScope()
        {
        }

        /// <summary>
        ///  accessor for instance (singleton)
        /// </summary>
        public static ThreadScope Instance
        {
            get
            {
                return _instance.Value;
            }
        }
        /// <summary>
        /// <see cref="IPerunScope.Context"/>
        /// </summary>
        public object Context
        {
            get { return Thread.CurrentThread; }
        }
    }
}