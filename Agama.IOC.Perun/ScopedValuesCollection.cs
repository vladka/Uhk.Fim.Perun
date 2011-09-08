using System;
using System.Collections.Generic;

namespace Agama.IOC.Perun
{
    /// <summary>
    /// Kolekce hodnot pro jednotlivé scopy patrici jedne instanci <see cref="IImplementationBuilder"/>
    /// </summary>
    public class ScopedValuesCollection  : IDisposable
    {
        
        private readonly Dictionary<WeakReference,object> _all = new Dictionary<WeakReference, object>();

        private readonly ScoppingRegistration _scoppingRegistration;

        public ScopedValuesCollection(ScoppingRegistration scoppingRegistration)
        {
            _scoppingRegistration = scoppingRegistration;
        }

        /// <summary>
        /// Return instance of object holded by scope determined by scope context object.
        /// </summary>
        /// <param name="scopeValue">scope's object (value of <see cref="IPerunScope.Context"/>)</param>
        /// <returns></returns>
        public object FindValueByScope(object scopeValue)
        {
            foreach (KeyValuePair<WeakReference, object> keyValuePair in _all)
            {
                if (keyValuePair.Key.IsAlive && keyValuePair.Key.Target == scopeValue)
                    return keyValuePair.Value;
            }
            return null;
        }

        /// <summary>
        /// Zaregistruje instanci k prislusnemu scopu. 
        /// </summary>
        /// <param name="scopeValue"></param>
        /// <param name="value"></param>
        public void RegisterScopedObject(object scopeValue,object instance)
        {
            var a = new WeakReference(scopeValue);
            _all.Add(a, instance);
            _scoppingRegistration.Add(a,this);
        }

        /// <summary>
        /// Odstrani konkretni instanci z evidence pro tento typ scopu. Zavolani dispose, pokud je objekt typu <see cref="IDisposable"/>.
        /// Vola se jakmile konci nejaky scope.
        /// </summary>
        /// <param name="key"></param>
        public void Remove(WeakReference key)
        {
            object value;
            if (_all.TryGetValue(key, out  value))
            {
                _all.Remove(key);
                var disposable = value as IDisposable;
                if (disposable!=null)
                    disposable.Dispose();

            }
        }

        public void RemoveAll()
        {
            foreach (KeyValuePair<WeakReference, object> pair in _all)
            {
                
                this._scoppingRegistration.RemoveFor(pair.Key,this);
                
                var disposable = pair.Value as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        #region Dispose Block
        /// <summary>
        /// Returns <c>true</c>, if object is disposed.
        /// </summary>
        public bool Disposed { get; private set; }
        /// <summary>
        /// Implemetation of <see cref="IDisposable.Dispose"/>.
        /// It calls Dispose on every holded instance (if is <see cref="IDisposable"/>).
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
            
            RemoveAll();
            Disposed = true;
        }

        ~ScopedValuesCollection()
        {
            Dispose(false);
        }
        #endregion
    }
}