using System;

namespace Agama.Perun
{
    internal interface  IImplementationBuilder:IConfiguredPluginInfo
    {

        object Get(BuildingContext ctx);
        void ReleaseComponent(object instanceToRelease);
       
        
    }

    internal interface IImplementationBuilder<T> : IImplementationBuilder,IConfiguredPluginInfo<T>
    {

       // object Get(BuildingContext ctx);
       // void ReleaseComponent(object instanceToRelease);


    }
}