using System;

namespace Agama.Perun
{
    /// <summary>
    /// Pouze presmerovaci implementace, ktera deleguje volani na otevrenou definici
    /// </summary>
    public sealed class RedirectImplementationBuilder : IImplementationBuilder
    {
        public readonly OpenedImplementationBuilder Target;

        internal RedirectImplementationBuilder(OpenedImplementationBuilder target)
        {
            Target = target;
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