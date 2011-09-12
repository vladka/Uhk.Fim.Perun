using System;

namespace Agama.Perun
{
    //public delegate void EventHandler<out TEventArgs>(object sender, GettingScopedInstanceEventArgs e) where TEventArgs : EventArgs;
    //public delegate void EventHandler<out TEventArgs>(object sender, TEventArgs e) where TEventArgs : EventArgs;
    //public delegate void EventHandler<out TEventArgs>(object sender, BeforeReleaseComponentEventArgs e) where TEventArgs : EventArgs;

    
    /// <summary>
    /// Generic builder used for resolving fully specified componenets (not for opened generic types)
    /// </summary>
    public class ImplementationBuilder<TPluginType> : IImplementationBuilder<TPluginType>
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
            _scopedValues = new ScopedValuesCollection(_container._scoppings,this);
            _pluginType = typeof (TPluginType); //to be quick

        }

        /// <summary>
        /// Name of this plugin-info. 
        /// Using names to resolve service it is bad pattern!!. 
        /// You should use Func, which depends on circumstances.
        /// </summary>
        public string Name { get; set; }

        #region Event declaration...

        readonly object objectLock = new Object();
        private event EventHandler<GettingScopedInstanceEventArgs> _afterGotScoped;
        
        event EventHandler<GettingScopedInstanceEventArgs> IConfiguredPluginInfo.AfterGotScoped
        {
            add { lock(objectLock) {_afterGotScoped += value;}}
            remove{lock (objectLock){_afterGotScoped -= value;}}
        }
        
        private event EventHandler<GettingScopedInstanceEventArgs<TPluginType>> _afterGotScoped2; //todo:rename
        public event EventHandler<GettingScopedInstanceEventArgs<TPluginType>> AfterGotScoped
        {
            add { lock(objectLock) {_afterGotScoped2 +=  value;}}
            remove{lock (objectLock){_afterGotScoped2 -= value;}}
        }
        
        
        private void OnAfterGetScopedInstance(GettingScopedInstanceEventArgs<TPluginType> args)
        {
            if (_afterGotScoped != null)
                _afterGotScoped(this, args);
            if (_afterGotScoped2 != null)
                _afterGotScoped2(this, args);
        }
        
        //------
        readonly object objectLock2 = new Object();
        private event EventHandler<AfterBuiltComponentEventArgs> _afterBuiltNewComponent;
        event EventHandler<AfterBuiltComponentEventArgs> IConfiguredPluginInfo.AfterBuiltNewComponent
        {
            add { lock(objectLock2) {_afterBuiltNewComponent += value;}}
            remove{lock (objectLock2){_afterBuiltNewComponent -= value;}}
        }
        private event EventHandler<AfterBuiltComponentEventArgs<TPluginType>> _afterBuiltNewComponent2;
        public event EventHandler<AfterBuiltComponentEventArgs<TPluginType>> AfterBuiltNewComponent
         {
             add { lock (objectLock2) { _afterBuiltNewComponent2 +=  value; } }
            remove{lock (objectLock2){_afterBuiltNewComponent2 -= value;}}
        }
        private void OnAfterBuiltNewComponent(AfterBuiltComponentEventArgs<TPluginType> args)
        {
            if (_afterBuiltNewComponent != null)
                _afterBuiltNewComponent(this, args);
            if (_afterBuiltNewComponent2 != null)
                _afterBuiltNewComponent2(this, args);
        }
        
        //--------
        readonly object objectLock3 = new Object();
        private event EventHandler<BeforeReleaseComponentEventArgs> _beforeReleaseComponent;
        
        event EventHandler<BeforeReleaseComponentEventArgs> IConfiguredPluginInfo.BeforeReleaseComponent
         {
            add { lock(objectLock3) {_beforeReleaseComponent += value;}}
            remove{lock (objectLock3){_beforeReleaseComponent -= value;}}
        }
        private event EventHandler<BeforeReleaseComponentEventArgs<TPluginType>> _beforeReleaseComponent2;
        public event EventHandler<BeforeReleaseComponentEventArgs<TPluginType>> BeforeReleaseComponent
         {
            add { lock(objectLock3) {_beforeReleaseComponent2 += value;}}
            remove{lock (objectLock3){_beforeReleaseComponent2 -= value;}}
        }
        
        private void OnBeforeReleaseComponent(BeforeReleaseComponentEventArgs<TPluginType> args)
        {
            if (_beforeReleaseComponent != null)
                _beforeReleaseComponent(this, args);
            if (_beforeReleaseComponent2 != null)
                _beforeReleaseComponent2(this, args);
        }
        #endregion
        //------------
        public void ReleaseComponent(object instanceToRelease)
        {
            var args = new BeforeReleaseComponentEventArgs<TPluginType>((TPluginType)instanceToRelease);
            
            OnBeforeReleaseComponent(args);

            if (args.RunDispose)
            {
                var disposable = instanceToRelease as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }

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
                var args = new AfterBuiltComponentEventArgs<TPluginType>(_factoryMethod(ctx));
                OnAfterBuiltNewComponent(args);
                return (TPluginType) args.Component;
            }

            var result = (TPluginType)_scopedValues.FindValueByScope(scopeObj);
            if (!Object.Equals(result, default(TPluginType))) //if not null
            {
                var args2 = new GettingScopedInstanceEventArgs<TPluginType>(result);
                OnAfterGetScopedInstance(args2);
                return (TPluginType) args2.Component;
            }

            result = _factoryMethod(ctx);
            var args3 = new AfterBuiltComponentEventArgs<TPluginType>(result);
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
            _container.UnRegister(this);
            
            _afterBuiltNewComponent = null;
            _afterBuiltNewComponent2 = null;
            _afterGotScoped = null;
            _afterGotScoped2 = null;
            _beforeReleaseComponent = null;
            _beforeReleaseComponent2 = null;
            
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
            _scopedValues = new ScopedValuesCollection(_container._scoppings,this);


        }

        /// <summary>
        /// Name of this plugin-info. 
        /// Using names to resolve service it is bad pattern!!. 
        /// You should use Func, which depends on circumstances.
        /// </summary>
        public string Name { get; set; }

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
        public event EventHandler<BeforeReleaseComponentEventArgs> BeforeReleaseComponent;
        private void OnBeforeReleaseComponent(BeforeReleaseComponentEventArgs args)
        {
            if (BeforeReleaseComponent != null)
                BeforeReleaseComponent(this, args);
        }

        public void ReleaseComponent(object instanceToRelease)
        {
            var args = new BeforeReleaseComponentEventArgs(instanceToRelease);

            OnBeforeReleaseComponent(args);

            if (args.RunDispose)
            {
                var disposable = instanceToRelease as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }

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