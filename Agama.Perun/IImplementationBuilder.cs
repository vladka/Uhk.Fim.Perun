using System;

namespace Agama.Perun
{
    public interface  IImplementationBuilder:IDisposable
    {

        object Get(BuildingContext ctx);
        IPerunScope Scope {get;}
        
    }
}