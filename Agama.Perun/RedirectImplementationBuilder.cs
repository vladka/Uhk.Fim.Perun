using System;

namespace Agama.Perun
{
    /// <summary>
    /// Pouze presmerovaci implementace, ktera deleguje volani na otevrenou definici
    /// </summary>
    public sealed class RedirectImplementationBuilder : IImplementationBuilder<object>
    {
        private readonly Type _pluginType;
        public readonly OpenedImplementationBuilder Target;

        internal RedirectImplementationBuilder(Type pluginType,OpenedImplementationBuilder target)
        {
            _pluginType = pluginType;
            Target = target;
        }

        /// <summary>
        /// Name of this plugin-info. 
        /// Using names to resolve service it is bad pattern!!. 
        /// You should use Func, which depends on circumstances.
        /// </summary>
        public string Name { get; set; }

        public event EventHandler<GettingScopedInstanceEventArgs<object>> AfterGotScoped;
        private void OnAfterGetScopedInstance(GettingScopedInstanceEventArgs<object>args)
        {
            if (AfterGotScoped != null)
                AfterGotScoped(this, args);
        }
        public event EventHandler<AfterBuiltComponentEventArgs<object>> AfterBuiltNewComponent;
        private void OnAfterBuiltNewComponent(AfterBuiltComponentEventArgs<object> args)
        {
            if (AfterBuiltNewComponent != null)
                AfterBuiltNewComponent(this, args);
        }

        public event EventHandler<BeforeReleaseComponentEventArgs<object>> BeforeReleaseComponent;
        private void OnBeforeReleaseComponent(BeforeReleaseComponentEventArgs<object> args)
        {
            if (BeforeReleaseComponent != null)
                BeforeReleaseComponent(this, args);
        }

        public void ReleaseComponent(object instanceToRelease)
        {
            //nemas smysl pro tento typ

        }

        public object Get(BuildingContext ctx)
        {
            return  Target.Get(ctx);
        }

        public IPerunScope Scope
        {
            get { return Target.Scope; }
        }

        public Type PluginType
        {
            get { return this._pluginType; }
        }

        /// <summary>
        /// Nedela nic v teto implementaci
        /// </summary>
        public void UnRegister()
        {
            //nedela nic, protoze RedirectImplementation nejde vyrobit primo uzivatelem, ale je vyrabena automaticky.
            //neni tedy potrena ji oderegistrovát.
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