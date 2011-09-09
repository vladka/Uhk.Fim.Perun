using System;
using System.Web;

namespace Agama.Perun
{
    /// <summary>
    /// Lifecycle per web request: There will be one instance per one  <see cref="HttpContext.Current"/>.
    /// </summary>
    public class HttpContextScope : IPerunScope
    {

        private static readonly Lazy<HttpContextScope> _instance
            = new Lazy<HttpContextScope>(() => new HttpContextScope());

        // private to prevent direct instantiation.
        private HttpContextScope()
        {
        }

        // accessor for instance
        public static HttpContextScope Instance
        {
            get
            {
                return _instance.Value;
            }
        }
        public object Context
        {
            get { return HttpContext.Current; }
        }
    }
}