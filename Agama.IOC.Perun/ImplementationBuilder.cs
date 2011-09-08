using System;
using System.Linq;
using System.Linq.Expressions;

namespace Agama.IOC.Perun
{
    internal class ImplementationBuilder<TPluginType> : IImplementationBuilder
    {

        private readonly ScoppingRegistration _scoppingRegistration;
        private readonly Func<BuildingContext, TPluginType> _factoryMethod;
        private readonly IPerunScope _scope;
        private readonly ScopedValuesCollection _scopedValues;


        public ImplementationBuilder(ScoppingRegistration scoppingRegistration, Func<BuildingContext, TPluginType> factoryMethod, IPerunScope scope)
        {

            _scoppingRegistration = scoppingRegistration;
            _factoryMethod = factoryMethod;
            _scope = scope;
            _scopedValues = new ScopedValuesCollection(scoppingRegistration);


        }
      


        public IPerunScope Scope
        {
            get
            {
                return _scope;
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
                return _factoryMethod(ctx);

            var result = (TPluginType)_scopedValues.FindValueByScope(scopeObj);
            if (!Object.Equals(result, default(TPluginType)))
                return result;

            result = _factoryMethod(ctx);
            _scopedValues.RegisterScopedObject(scopeObj, result);
            return result;
        }
        object IImplementationBuilder.Get(BuildingContext ctx)
        {
            return Get(ctx);
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
            
        }

        ~ImplementationBuilder()
        {
            Dispose(false);
        }
        #endregion



    }

    internal class ImplementationBuilder : IImplementationBuilder
    {

        public readonly OpenedImplementationBuilder Creator;
        private readonly ScoppingRegistration _scoppingRegistration;
        private readonly Type _pluginType;
        private readonly Func<BuildingContext, object> _factoryMethod;
        private readonly IPerunScope _scope;
        private readonly OpenedImplementationBuilder _creator;
        private readonly ScopedValuesCollection _scopedValues;


        public ImplementationBuilder(ScoppingRegistration scoppingRegistration, Type pluginType, Func<BuildingContext, object> factoryMethod, IPerunScope scope, OpenedImplementationBuilder creator = null)
        {
            Creator = creator;

            _scoppingRegistration = scoppingRegistration;
            _pluginType = pluginType;
            _factoryMethod = factoryMethod;
            _scope = scope;
            _creator = creator;
            _scopedValues = new ScopedValuesCollection(scoppingRegistration);


        }

        public IPerunScope Scope
        {
            get
            {
                return _scope;
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
                return _factoryMethod(ctx);



            var result = _scopedValues.FindValueByScope(scopeObj);
            if (result != null)
                return result;

            result = _factoryMethod(ctx);
            _scopedValues.RegisterScopedObject(scopeObj, result);
            return result;
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
            
        }

        ~ImplementationBuilder()
        {
            Dispose(false);
        }
        #endregion
    }
}