using System.Threading;
using System;

namespace Agama.Perun
{
    public static class FuncExtension
    {

        /// <summary>
        /// Converts func to IPerunScope
        /// </summary>
        /// <param name="funcReturningScopeObject"></param>
        /// <returns></returns>
        public static IPerunScope ToPerunScope(this Func<object> funcReturningScopeObject)
        {
            return new FuncScope(funcReturningScopeObject);
        }
    }


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