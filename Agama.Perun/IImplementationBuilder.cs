using System;

namespace Agama.Perun
{
    
    /// <summary>
    /// Configured component beeing ready to be resolved
    /// </summary>
    public interface IResolver
    {
        /// <summary>
        /// Curenty used scope (Singleton, per Thread, per HttpContext...)
        /// </summary>
        IPerunScope Scope { get; }
        
        /// <summary>
        /// Event occurs when component is resolved from scope-cache before is served to consumer.
        /// </summary>
        event EventHandler<GettingScopedInstanceEventArgs> AfterGotScoped;
        
        /// <summary>
        /// Event occurs when component is constructed (by constructor or by given Func&lt;&gt;)
        /// before is served to consumer.
        /// </summary>
        event EventHandler<AfterBuiltComponentEventArgs> AfterBuiltNewComponent;
        

    }
    
    internal interface  IImplementationBuilder:IDisposable,IResolver
    {

        object Get(BuildingContext ctx);
       
        
    }
}