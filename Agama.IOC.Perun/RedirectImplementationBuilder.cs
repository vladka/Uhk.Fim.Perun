using System;

namespace Agama.IOC.Perun
{
    /// <summary>
    /// Pouze presmerovaci implementace, ktera deleguje volani na otevrenou definici
    /// </summary>
    internal class RedirectImplementationBuilder : IImplementationBuilder
    {
        public readonly OpenedImplementationBuilder Target;

        public RedirectImplementationBuilder(OpenedImplementationBuilder target)
        {
            Target = target;
        }


        public object Get(BuildingContext ctx)
        {
            return  Target.Get(ctx);
        }

        public IPerunScope Scope
        {
            get { return Target.Scope; }
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
            Target.Dispose();
        }

        ~RedirectImplementationBuilder()
        {
            Dispose(false);
        }
        #endregion
    }
}