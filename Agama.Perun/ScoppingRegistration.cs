using System;
using System.Collections.Generic;
using System.Threading;

namespace Agama.Perun
{
    /// <summary>
    /// Tato instance existuje vzdy jedna pro cely kontejner.
    /// Udrzuje slovnik, kde klicem je WeakReference na scopovaný objekt,
    /// a hodnoty jsou odkazy na hodnoty evidované pro jednotlivé typy pluginů.
    /// </summary>
    public class ScoppingRegistration : IDisposable
    {
        public ScoppingRegistration()
        {
            _async = new Thread(AsyncCleanup);
            _async.Start();
            
        }

        private Thread _async;
        private bool _disposed; //TODO: udelat disposable
        private readonly Dictionary<WeakReference, List<ScopedValuesCollection>> _all =
            new Dictionary<WeakReference, List<ScopedValuesCollection>>();

        private void AsyncCleanup()
        {
            Console.WriteLine("Cleaning...");
            while (!_disposed)
            {
                Thread.Sleep(1000);
                var toDel = new List<WeakReference>();
                foreach (KeyValuePair<WeakReference, List<ScopedValuesCollection>> pair in _all)
                {
                    if (pair.Key.IsAlive)
                        continue;
                    foreach (var scopedValues in pair.Value)
                    {
                        scopedValues.Remove(pair.Key);
                    }
                    toDel.Add(pair.Key);
                }
                foreach (var key in toDel)
                {
                    _all.Remove(key);
                }
            }
        }

      
        /// <summary>
        /// Odstrani vsechny instance drzene objektem scopu splnujici <paramref name="match"/>.
        /// </summary>
        /// <param name="match">Optional. If null, every instances from every scopes are removed.</param>
        public void RemoveAll(Predicate<object> match ) 
        {
            foreach (KeyValuePair<WeakReference, List<ScopedValuesCollection>> pair in _all)
            {
                if ((!pair.Key.IsAlive) || (match != null && (!match(pair.Key.Target)))) 
                    continue;
                foreach (var scopedValues in pair.Value)
                {
                    scopedValues.Remove(pair.Key);
                }
                _all.Remove(pair.Key); //zruseni celeho paru
            }
        }
        
      
        public void Add(WeakReference key, ScopedValuesCollection value)
        {
            List<ScopedValuesCollection> refs;
            if (!_all.TryGetValue(key, out refs))
            {
                refs = new List<ScopedValuesCollection>();
                _all.Add(key,refs);
            }
            refs.Add(value);
        }
        public void RemoveFor(WeakReference key, ScopedValuesCollection scopedValuesCollection)
        {
            List<ScopedValuesCollection> refs;
            if (_all.TryGetValue(key, out refs))
            {
                refs.Remove(scopedValuesCollection);
                if (refs.Count == 0)
                    _all.Remove(key);//zruseni celeho paru
                
                    
            }
        }

        


       
      
        #region Dispose Block
        /// <summary>
        /// Returns <c>true</c>, if object is disposed.
        /// </summary>
        public bool Disposed { get { return _disposed; } }
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
            if (_disposed)
                return;

            _disposed = true;
            _async.Join();
            RemoveAll(x => true);
            
            
            
        }

        ~ScoppingRegistration()
        {
            Dispose(false);
        }
        #endregion

        
    }
}