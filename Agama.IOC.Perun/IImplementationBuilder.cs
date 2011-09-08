using System;

namespace Agama.IOC.Perun
{
    public interface  IImplementationBuilder:IDisposable
    {

        object Get(BuildingContext ctx);
        IPerunScope Scope {get;}
        
    }
}