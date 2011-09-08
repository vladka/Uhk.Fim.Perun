using System;

namespace Agama.Perun
{
    internal class OpenedImplementationBuilder : IImplementationBuilder
    {

        private readonly ScoppingRegistration _scoppingRegistration;
        private readonly Type _pluginType;
        private readonly Func<BuildingContext, object> _factoryMethod;
        private readonly IPerunScope _scope;
        


        public OpenedImplementationBuilder(ScoppingRegistration scoppingRegistration, Type pluginType, Func<BuildingContext, object> factoryMethod, IPerunScope scope)
        {

            _scoppingRegistration = scoppingRegistration;
            _pluginType = pluginType;
            _factoryMethod = factoryMethod;
            _scope = scope;
            


        }

        public IPerunScope Scope
        {
            get
            {
                return _scope;
            }
        }

      

        //tato metoda probiha pouze poprve pri prvni vyrobe konkretniho generickeho typu
        public object Get(BuildingContext ctx)
        {
            //todo vytvorit instanci a zaregistrovat do scopu
            ctx.CurrentBuilder = this;

            var lz = _scope as BindingContextScope;
            if (lz != null)
                lz.BindingContext = ctx;

            var finalFunc = (Func<object>) _factoryMethod(ctx);
            
            var closedBuilder = new ImplementationBuilder(_scoppingRegistration,ctx.ResolvingType,c => finalFunc(),lz!=null ? lz.FinalScope : _scope,this);

            ctx.Container.RegisterInternal(ctx.ResolvingType, closedBuilder,new Tuple<OpenedImplementationBuilder,ImplementationBuilder> (this,closedBuilder));

            var result = ctx.Container.GetService(ctx.ResolvingType);
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
            
            //todo: najit vsechny uzavrene implementatory a ty take Disposovat

            Disposed = true;
        }

        ~OpenedImplementationBuilder()
        {
            Dispose(false);
        }
        #endregion
    }
}