using System.Collections.Generic;

namespace Agama.IOC.Perun
{

    public abstract class BindingContextScope : IPerunScope
    {
        private BuildingContext _bindingContext;
        public IPerunScope FinalScope { get; set; }

        public BuildingContext BindingContext
        {
            get { return _bindingContext; }
            set
            {
                _bindingContext = value;
                OnSetBindingContext();
            }
        }

        protected void OnSetBindingContext()
        {
            Context = UpdateContext();
        }

        /// <summary>
        /// Metoda se volá jakmile je znám  kontext (<see cref="BindingContext"/>).
        /// Metoda vrací značkovací objekt Contextu (<see cref="IPerunScope.Context"/>).
        /// </summary>
        /// <returns></returns>
        protected abstract object UpdateContext();

        public virtual object Context
        {
            get;
            protected set;
        }
    }

    /// <summary>
    /// Scope, který závisí na vnitřním typu
    /// </summary>
    public class InnerTypeDependencyScope : BindingContextScope
    {
        
        
        protected override object UpdateContext()
        {
            List<IImplementationBuilder> innerImpls;

            var type = BindingContext.ResolvingType.GetGenericArguments()[0];
            if (!BindingContext.Container._all.TryGetValue(type, out innerImpls))
            {
                if (!BindingContext.Container._all.TryGetValue(type.GetGenericTypeDefinition(), out innerImpls))
                    return null;
            }
            FinalScope = innerImpls[0].Scope;
            return FinalScope.Context;

        }

    }
}