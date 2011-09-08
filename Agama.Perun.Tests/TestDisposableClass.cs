using System;

namespace Agama.Perun.Tests
{
    public interface ITestDisposable : IDisposable
    {
        /// <summary>
        /// Action called when instance is finalized or disposed
        /// </summary>
        Action BeforeDisposeAction { get; set; }
        /// <summary>
        /// Track whether Dispose has been called.
        /// </summary>
        bool Disposed { get; }
    }
    
    /// <summary>
    /// Pomocná trida slouzici  k vyvolani udalosti, jakmile je instance teto tridy dispoznuta nebo destruovana Garbage Collectorem.
    /// </summary>
    public class TestDisposableClass : ITestDisposable
    {
        public Action BeforeDisposeAction { get; set; }

        private string name;
        
        public TestDisposableClass()
        {
            name = Guid.NewGuid().ToString();
        }
        
       

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        
        private bool disposed = false;
        /// <summary>
        /// Track whether Dispose has been called.
        /// </summary>
        public bool Disposed { get { return disposed; } }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
             Console.WriteLine(" Dispossing object "+this.name);
            else
            {
                Console.WriteLine(" Finalizing object "+this.name);
            }
            
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                if (BeforeDisposeAction!=null)
                    BeforeDisposeAction();
                BeforeDisposeAction = null;
              
                disposed = true;

            }
        }

        ~TestDisposableClass()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }
    }
}