using System;

namespace Agama.Perun
{
    internal interface  IImplementationBuilder:IResolver
    {

        object Get(BuildingContext ctx);
       
        
    }
}