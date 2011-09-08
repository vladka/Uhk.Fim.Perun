using System.Threading;

namespace Agama.Perun
{
    /// <summary>
    /// Interface used for all scope definitions.
    /// </summary>
    public interface IPerunScope
    {
        /// <summary>
        /// Return object which represents any scope.
        /// It should return e.g. <see cref="Thread.CurrentThread"/>, or HttpContext.Current ...
        /// </summary>
        /// <remarks>
        /// </remarks>
        object Context { get;}
    }
}