using System;

namespace Agama.Perun
{
    internal interface  IImplementationBuilder< T>:IConfiguredPluginInfo<T>
    {

        object Get(BuildingContext ctx);
        void ReleaseComponent(object instanceToRelease);
       
        
    }
}