using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Agama.Perun;
using Agama.Services.Api.IOC;

namespace Agama.IOC.Perun
{

   

    public class PerunAdapter : IIocContainer 
    {
        
        
        public PerunAdapter()
        {
            _ioc = new PerunContainer();
        }

        private readonly PerunContainer _ioc;

        private IPerunScope GetScope(LifeTimeType lifeTimeType)
        {
            switch (lifeTimeType)
            {
                case LifeTimeType.Singleton:
                    return _ioc;
                case LifeTimeType.Thread:
                    return ThreadScope.Instance;
                case LifeTimeType.Transient:
                    return TransientScope.Instance;
                case LifeTimeType.HttpContext:
                    return HttpContextScope.Instance; //todo:
                default: throw new NotImplementedException();
            }
        }
       

        public bool IsConfiguredFor(Type interfaceType)
        {
            return _ioc.IsConfiguredFor(interfaceType);
        }

        public bool IsConfiguredFor<T>()
        {
            return _ioc.IsConfiguredFor<T>();
        }

        public T GetService<T>()
        {
            return _ioc.GetService<T>();
        }

        public object GetService(Type t)
        {
            return _ioc.GetService(t);
        }

        public IEnumerable<T> GetServices<T>()
        {
            return _ioc.GetServices<T>();
           
        }

        public IEnumerable<object> GetServices(Type t)
        {
            return _ioc.GetServices(t);
        }

      

        public void RegisterType<TReal>(LifeTimeType lifeTimeType)
        {
            _ioc.RegisterType<TReal>(GetScope(lifeTimeType));
        }

        public void RegisterType<TInterface, TReal>(LifeTimeType lifeTimeType) where TReal : TInterface
        {
            _ioc.RegisterType<TInterface,TReal>(GetScope(lifeTimeType));
        }

       public void RegisterType<TInterface>(Func<TInterface> builder, LifeTimeType lifeTimeType)
       {
           _ioc.RegisterType<TInterface>(builder,GetScope(lifeTimeType));
       }

       

        public void RegisterType(Type type, Type @interface, LifeTimeType lifeTimeType)
        {
          _ioc.RegisterType(type,@interface, GetScope(lifeTimeType));
        }

       

        
        public void RegisterType(Type interfaceType, Func<object> builder, LifeTimeType lifeTimeType)
        {
           _ioc.RegisterType(interfaceType, builder,GetScope(lifeTimeType));
        }

      
       

        public void EjectServices(LifeTimeType lifeTimeType)
        {
          _ioc.EjectServices( GetScope(lifeTimeType));
        }

      

        public bool? IsCleanupRequiredForScope(LifeTimeType lifeTimeType)
        {
            return null;
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
            _ioc.Dispose();
            
        }

        ~PerunAdapter()
        {
            Dispose(false);
        }
        #endregion

    }
}
