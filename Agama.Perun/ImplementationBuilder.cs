using System;

namespace Agama.Perun
{
    /// <summary>
    /// Generic builder used for resolving fully specified componenets (not for opened generic types)
    /// </summary>
    public class ImplementationBuilder<TPluginType> : IImplementationBuilder
    {

        
        private readonly PerunContainer _container;
        private readonly Func<BuildingContext, TPluginType> _factoryMethod;
        private readonly IPerunScope _scope;
        private readonly ScopedValuesCollection _scopedValues;
        private readonly Type _pluginType;

        internal ImplementationBuilder(PerunContainer container, Func<BuildingContext, TPluginType> factoryMethod, IPerunScope scope)
        {
            _container = container;
            _factoryMethod = factoryMethod;
            _scope = scope;
            _scopedValues = new ScopedValuesCollection(_container._scoppings);
            _pluginType = typeof (TPluginType); //to be quick

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

        public TPluginType Get(BuildingContext ctx)
        {
            ctx.CurrentBuilder = this;

            var lz = _scope as BindingContextScope;
            if (lz != null)
                lz.BindingContext = ctx;


            var scopeObj = _scope.Context;
            if (scopeObj == null)
            {
                //scope cache is not needed
                var args = new AfterBuiltComponentEventArgs(_factoryMethod(ctx));
                OnAfterBuiltNewComponent(args);
                return (TPluginType) args.Component;
            }

            var result = (TPluginType)_scopedValues.FindValueByScope(scopeObj);
            if (!Object.Equals(result, default(TPluginType))) //if not null
            {
                var args2 = new GettingScopedInstanceEventArgs(result);
                OnAfterGetScopedInstance(args2);
                return (TPluginType) args2.Component;
            }

            result = _factoryMethod(ctx);
            var args3 = new AfterBuiltComponentEventArgs(result);
            OnAfterBuiltNewComponent(args3);
            _scopedValues.RegisterScopedObject(scopeObj, args3.Component);
            return (TPluginType) args3.Component;
        }
        object IImplementationBuilder.Get(BuildingContext ctx)
        {
            return Get(ctx);
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
            Disposed = true;
            _scopedValues.Dispose();
            AfterBuiltNewComponent = null;
            AfterGotScoped = null;
            
        }

        ~ImplementationBuilder()
        {
            Dispose(false);
        }
        #endregion



    }


    /// <summary>
    /// Builder used for resolving fully specified componenets (not for opened generic types)
    /// </summary>
    public class ImplementationBuilder : IImplementationBuilder
    {
        private readonly PerunContainer _container;
        public readonly OpenedImplementationBuilder Creator;
        private readonly Type _pluginType;
        private readonly Func<BuildingContext, object> _factoryMethod;
        private readonly IPerunScope _scope;
        private readonly OpenedImplementationBuilder _creator;
        private readonly ScopedValuesCollection _scopedValues;


        internal ImplementationBuilder(PerunContainer container, Type pluginType, Func<BuildingContext, object> factoryMethod, IPerunScope scope, OpenedImplementationBuilder creator = null)
        {
            _container = container;
            Creator = creator;

            
            _pluginType = pluginType;
            _factoryMethod = factoryMethod;
            _scope = scope;
            _creator = creator;
            _scopedValues = new ScopedValuesCollection(_container._scoppings);


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

        //public void Eject(bool dispose)
        //{
        //    //todo: co delat v pripade BindingContextScope?
        //    var scopeObj = _scope.Context;
        //    if (scopeObj != null)
        //    {
        //       var value =   _scopedValues.FindValueByScope(scopeObj);
        //       if (dispose)
        //       {
        //           var d = value as IDisposable;
        //           if (d != null)
        //               d.Dispose();
        //       }
        //        _scopedValues.Remove();

        //    }

        //}

       

        public object Get(BuildingContext ctx)
        {
            ctx.CurrentBuilder = this;

            var lz = _scope as BindingContextScope;
            if (lz != null)
                lz.BindingContext = ctx;

            var scopeObj = _scope.Context;
            if (scopeObj == null)
            {   //scope cache is not needed
                var args = new AfterBuiltComponentEventArgs(_factoryMethod(ctx));
                OnAfterBuiltNewComponent(args);
                return args.Component;
            }


            //component has been found in scope cache
            var result = _scopedValues.FindValueByScope(scopeObj);
            if (result != null)
            {
                var args2 = new GettingScopedInstanceEventArgs(result);
                OnAfterGetScopedInstance(args2);
                return args2.Component;
            }

            //component is not found in scope cache.
            result = _factoryMethod(ctx);
            var args3 = new AfterBuiltComponentEventArgs(result);
            OnAfterBuiltNewComponent(args3);
            _scopedValues.RegisterScopedObject(scopeObj, args3.Component);
            return args3.Component;
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

            Disposed = true;
            _scopedValues.Dispose();
            _container.UnRegister(this);
            AfterBuiltNewComponent = null;
            AfterGotScoped = null;
            
            
            
        }

        ~ImplementationBuilder()
        {
            Dispose(false);
        }
        #endregion
    }
}