using System;

namespace Agama.Perun
{
    /// <summary>
    /// Transient scope = no scope. Use this as singleton (<see cref="Instance"/>)
    /// New component are constructed everytime.
    /// </summary>
    public class TransientScope : IPerunScope
    {

        private static readonly Lazy<TransientScope> _instance
            = new Lazy<TransientScope>(() => new TransientScope());

        // private to prevent direct instantiation.
        private TransientScope()
        {
        }

        // accessor for instance
        public static TransientScope Instance
        {
            get
            {
                return _instance.Value;
            }
        }
        public object Context
        {
            get { return null; }
        }
    }
}