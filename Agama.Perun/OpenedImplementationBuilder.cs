using System;

namespace Agama.Perun
{
   
    /// <summary>
    /// Builder for opened generics types.
    /// It is used only for creating concrete builder
    ///  (<see cref="ImplementationBuilder"/> or <see cref="ImplementationBuilder{TPluginType}"/>).
    /// 
    /// </summary>
    public sealed class OpenedImplementationBuilder : IImplementationBuilder
    {

        private readonly ScoppingRegistration _scoppingRegistration;
        private readonly PerunContainer _container;
        private readonly Type _pluginType;
        private readonly Func<BuildingContext, object> _factoryMethod;
        private readonly IPerunScope _scope;
        


        internal  OpenedImplementationBuilder(PerunContainer container, Type pluginType, Func<BuildingContext, object> factoryMethod, IPerunScope scope)
        {
            _container = container;
            _pluginType = pluginType;
            _factoryMethod = factoryMethod;
            _scope = scope;
        }

        public IPerunScope Scope
        {
            get
            {
                return _scope;
            }
        }

        /// <summary>
        /// Type for what is this instance defined
        /// </summary>
        public Type PluginType
        {
            get
            {
                return _pluginType;
            }
        }

        public event EventHandler<GettingScopedInstanceEventArgs> AfterGotScoped;
        private void OnAfterGetScopedInstance(GettingScopedInstanceEventArgs args)
        {
            if (AfterGotScoped != null)
                AfterGotScoped(this, args);
        }
        public event EventHandler<AfterBuiltComponentEventArgs> AfterBuiltNewComponent;
        private void OnAfterBuiltNewComponent(AfterBuiltComponentEventArgs args)
        {
            if (AfterBuiltNewComponent != null)
                AfterBuiltNewComponent(this, args);
        }
        //tato metoda probiha pouze poprve pri prvni vyrobe konkretniho generickeho typu
        public object Get(BuildingContext ctx)
        {
            //todo vytvorit instanci a zaregistrovat do scopu
            ctx.CurrentBuilder = this;

            var lz = _scope as BindingContextScope;
            if (lz != null)
                lz.BindingContext = ctx;

            var finalFunc = (Func<object>) _factoryMethod(ctx);
            
            var closedBuilder = new ImplementationBuilder(_container,ctx.ResolvingType,c => finalFunc(),lz!=null ? lz.FinalScope : _scope,this);

            ctx.Container.RegisterInternal(ctx.ResolvingType, closedBuilder,new Tuple<OpenedImplementationBuilder,ImplementationBuilder> (this,closedBuilder));

            var result = ctx.Container.GetService(ctx.ResolvingType);
            return result;
            
        }

        /// <summary>
        /// Pokusi se vyjmout definici. Pouze ji vyjme a neprovadi dispose na drzenych objektech.
        /// Defakto dojde pouze k odstraneni definice, ale veskere zijici komponenty jsou ponechany nazivu, dokud plati jejich scope.
        /// (Porovnej s <see cref="Dispose"/>, která naopak ruší sebe včetně toho, že volá Dispose na všech držených komponentách.)
        /// </summary>
        public void UnRegister()
        {
            _container.UnRegister(this);
            
        }

        #region Dispose Block
        /// <summary>
        /// Returns <c>true</c>, if object is disposed.
        /// </summary>
        public bool Disposed { get; private set; }
        /// <summary>
        /// Implemetation of <see cref="IDisposable.Dispose"/>.
        /// It calls Dispose on every scope-holded instance (if is <see cref="IDisposable"/>).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Disposed)
                return;
            
            //todo: najit vsechny uzavrene implementatory a ty take Disposovat

            Disposed = true;
        }

        ~OpenedImplementationBuilder()
        {
            Dispose(false);
        }
        #endregion
    }
}