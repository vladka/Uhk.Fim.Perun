using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Agama.Perun
{
    /// <summary>
    /// DI container.
    /// </summary>
    public partial class PerunContainer :  IPerunScope, IDisposable
    {
        
        internal readonly Dictionary<Type,List<IImplementationBuilder>> _all = new Dictionary<Type, List<IImplementationBuilder>>();
        private ScoppingRegistration _scoppings = new ScoppingRegistration();
        private readonly Dictionary<Type, Type> _cache= new Dictionary<Type, Type>();

        
        /// <summary>
        /// Creates new DI container. If <paramref name="configurationManager"/> is not specified, the default one is used.
        /// </summary>
        /// <param name="configurationManager"></param>
        public PerunContainer(PerunContainerConfigurationManager configurationManager = null)
        {
            ConfigurationManager = configurationManager ?? new PerunContainerConfigurationManager(this);
            
            ConfigurationManager.Configure();
        }

        /// <summary>
        /// Gets current configuration manager used for this container.
        /// </summary>
        /// <remarks>
        /// (This mannager is set when you create <see cref="PerunContainer"/>, see constructor)
        /// </remarks>
        public readonly PerunContainerConfigurationManager ConfigurationManager; 

       

        #region 'Query' methods
        /// <summary>
        /// Returns <c>true</c> if this plugin type is registered.
        /// </summary>
        /// <param name="pluginType">pluginType (ussually interface type)</param>
        /// <returns></returns>
        public bool IsConfiguredFor(Type pluginType)
        {
            return _all.ContainsKey(pluginType);
        }

        /// <summary>
        ///  Returns <c>true</c> if this plugin type is registered.
        /// </summary>
        /// <typeparam name="T">pluginType (ussually interface type)</typeparam>
        /// <returns></returns>
        public bool IsConfiguredFor<T>()
        {
            return _all.ContainsKey(typeof(T));
        }
        #endregion
        #region GetService(s) Methods

        /// <summary>
        /// Returns default service determined by their pluginType
        /// </summary>
        /// <typeparam name="T">pluginType (ussually interface type)</typeparam>
        /// <returns></returns>
        public T GetService<T>()
        {
            return (T) GetService(typeof (T));
        }

        /// <summary>
        /// Returns default service determined by their pluginType
        /// </summary>
        /// <param name="pluginType">pluginType (ussually interface type)</param>
        /// <returns></returns>
        public object GetService(Type pluginType)
        {

            List<IImplementationBuilder> impls;
            if (!_all.TryGetValue(pluginType, out impls) ) 
            {
                //resolving generic by opened generic deftype 
                if (pluginType.IsGenericType)
                {
                    var gd = pluginType.GetGenericTypeDefinition();
                    if (!_all.TryGetValue(gd, out impls))
                    {
                        return null;
                    }
                }
            }
            var ctx = new BuildingContext(pluginType, this);
            var result = impls[0].Get(ctx); //first is default
            return result;
        }

        
        /// <summary>
        /// Returns all services determined by their pluginType
        /// </summary>
        /// <typeparam name="T">pluginType (ussually interface type)</typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetServices<T>()
        {
            return GetServices(typeof (T)).Cast<T>();
        }

        /// <summary>
        /// Returns all services determined by their pluginType
        /// </summary>
        /// <param name="pluginType">pluginType (ussually interface type)</param>
        /// <returns></returns>
        public IEnumerable<object> GetServices(Type pluginType)
        {
            BuildingContext ctx = null; 
            List<IImplementationBuilder> impls;
            List<OpenedImplementationBuilder> implsToSkip = null; 
            if (_all.TryGetValue(pluginType, out impls))
            {
                ctx = new BuildingContext(pluginType, this);
                //protoze zavolani 'i.Get(ctx)' muze zpusobit pridani dalsich definic do kolekce impls, 
                //pouzivame 'for' a vzdy znovuvyhodnocujeme celkovy pocet
                // ReSharper disable ForCanBeConvertedToForeach
                for (int index = 0; index < impls.Count; index++)
                {
                    var i = impls[index];
                    var rd = i as RedirectImplementationBuilder;
                    
                    //protoze budeme prochazet i otevrene definice, tak je nebudeme volat znovu, pokud jiz byly redirectorovány.
                    if (rd != null )
                        (implsToSkip ?? (implsToSkip = new List<OpenedImplementationBuilder>())).Add(rd.Target);
                    else
                    {
                        //protoze budeme prochazet i otevrene definice, tak je nebudeme volat znovu, pokud tato definice vychazi z otevrene definice
                        var ib = i as ImplementationBuilder;
                        if (ib!=null && ib.Creator!=null)
                            (implsToSkip ?? (implsToSkip = new List<OpenedImplementationBuilder>())).Add(ib.Creator);
                    }
                    yield return i.Get(ctx);
                }
                // ReSharper restore ForCanBeConvertedToForeach
            }

            //resolving generic by opened generic deftype 
            if (pluginType.IsGenericType)
            {
                var gd = pluginType.GetGenericTypeDefinition();

                if (_all.TryGetValue(gd, out impls))
                {
                    if (ctx==null)
                        ctx = new BuildingContext(pluginType,this);
                    
                    //protoze zavolani 'i.Get(ctx)' muze zpusobit pridani dalsich definic do kolekce impls, 
                    //pouzivame 'for' a vzdy znovuvyhodnocujeme celkovy pocet
                    // ReSharper disable ForCanBeConvertedToForeach
                    for (int index = 0; index < impls.Count; index++)
                    {
                        var i = impls[index];
                        if (implsToSkip != null && implsToSkip.Contains(i))
                            continue;
                        yield return i.Get(ctx);
                    }
                    // ReSharper restore ForCanBeConvertedToForeach
                }
            }
            yield break;

        }
        #endregion

        #region RegisterType Methods..

        public IResolver RegisterType<TReal>(IPerunScope scope = null)
        {
            return RegisterType<TReal>(CreateFunc<TReal, TReal>(), scope ?? TransientScope.Instance);
        }

        public IResolver RegisterType<TInterface, TReal>(IPerunScope scope = null) where TReal : TInterface
        {
            return RegisterType<TInterface>(CreateFunc<TInterface,TReal>(), scope ?? TransientScope.Instance);
        }

        public IResolver RegisterType<TInterface>(Func<TInterface> builder, IPerunScope scope = null)
       {
           return RegisterType<TInterface>(CreateFunc(builder),scope ?? TransientScope.Instance);
           
       }

       

        public IResolver RegisterType<TInterface>(Func<BuildingContext,TInterface> builder,IPerunScope scope = null)
        {
            List<IImplementationBuilder> implementators;
            var interfaceType = typeof (TInterface);
            if (!_all.TryGetValue(interfaceType, out implementators))
            {
                implementators = new List<IImplementationBuilder>();
                _all.Add(interfaceType, implementators);
            }

            var impl = new ImplementationBuilder<TInterface>(_scoppings, builder,scope ?? TransientScope.Instance);
            implementators.Add(impl);
            return impl;
        }



        public IResolver RegisterType(Type type, Type @interface = null, IPerunScope scope = null)
        {
           return RegisterType(@interface ?? type,CreateFunc(type),scope ?? TransientScope.Instance);
        }



        public IResolver RegisterType(Type interfaceType, Func<object> builder, IPerunScope scope = null)
        {
            return RegisterType(interfaceType, CreateFunc(builder), scope ?? TransientScope.Instance);
        }



        public IResolver RegisterType(Type interfaceType, Func<BuildingContext, object> builder, IPerunScope scope = null)
        {
           

            IImplementationBuilder impl;
            if (interfaceType.IsGenericTypeDefinition)
                impl = new OpenedImplementationBuilder(_scoppings, interfaceType, builder, scope ?? TransientScope.Instance);
            else impl = new ImplementationBuilder(_scoppings, interfaceType, builder, scope ?? TransientScope.Instance);

            return RegisterInternal(interfaceType,impl);

            
        }
        #endregion

        #region Eject & Remove & Dispose Methods

        public void EjectServices(IPerunScope scope)
        {
            var scopeObject = scope.Context;
            if (scopeObject != null)
            {
                _scoppings.RemoveAll(x => x.Equals(scopeObject));

            }
        }

        public void DisposeService(Type interfaceType)
        {
            List<IImplementationBuilder> implementators;
            if (_all.TryGetValue(interfaceType, out implementators))
            {
                _all.Remove(interfaceType);

                foreach (var i in implementators)
                {
                    i.Dispose();
                }
            }

        }


        #endregion
        internal IImplementationBuilder RegisterInternal(Type interfaceType, IImplementationBuilder builder, Tuple<OpenedImplementationBuilder, ImplementationBuilder> callerToReplace = null)
        {
            List<IImplementationBuilder> implementators;
            if (!_all.TryGetValue(interfaceType, out implementators))
            {
                implementators = new List<IImplementationBuilder>();
                _all.Add(interfaceType, implementators);


                if (callerToReplace==null && interfaceType.IsGenericType && (!interfaceType.IsGenericTypeDefinition))
                {
                    //pokud definujeme genericky typ, ale uz je definovan predpis pro otevreny genericky typ, 
                    //tak tento otevreny musi zustat jako defaultni
                    //Tuto vetev vsak nevolame, pokud jde o zakladani volane otevrene definice (callerToReplace==null)
                    List<IImplementationBuilder> openedImplementators;
                    if (_all.TryGetValue(interfaceType.GetGenericTypeDefinition(), out openedImplementators))
                    {
                        implementators.Add(
                            new RedirectImplementationBuilder((OpenedImplementationBuilder) openedImplementators[0]));
                    }
                }
            }
            else
            {
                //jakmile otevřená definice má svoji konkrétní implementaci, nahradíme puvodni 'redirector'
                if (callerToReplace != null )
                {
                    int indexToReplace = implementators.FindIndex(x=>
                                            {
                                                var openedRedirector = x as RedirectImplementationBuilder;
                                                return (openedRedirector != null &&
                                                        openedRedirector.Target == callerToReplace.Item1);
                                                    
                                            }
                                            );
                    if (indexToReplace >= 0)
                        implementators[indexToReplace] = callerToReplace.Item2;
                    return null; //neni potreba nic vracet pokud callerToReplace!=null
                }
            }

            implementators.Add(builder);//jinak ji pridame na konec
            return builder;
        }


        /// <summary>
        /// Pomocná metoda, která má za ukol obalit puvodni funkci, tak aby zavisela na kontextu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="innerFunc"></param>
        /// <returns></returns>
        private Func<BuildingContext, T> CreateFunc<T>(Func<T> innerFunc)
        {
            return ctx => innerFunc(); //carrying 
        }


        private Func<BuildingContext, object> CreateFunc(Type type)
        {
            //todo : najit spravny ctor
            if (!type.IsGenericTypeDefinition)
            {
                //vytvoreni funkce na zaklade typu
                return CreateFunc(GetBuildUpFunc<object>(type));
            }
            else
            {
                Func<BuildingContext, object> f = delegate(BuildingContext ctx)
                {
                    var genType = type.MakeGenericType(ctx.ResolvingType.GetGenericArguments());
                    var createFunc = GetBuildUpFunc<object>(genType);
                    //this.RegisterType(ctx.ResolvingType,);
                    return createFunc;

                };
                return f;
            }

        }


         

        private Func<TInterface> CreateFunc<TInterface, TReal>()
        {
            return this.GetBuildUpFunc<TInterface>(typeof(TReal));
        }


        /// <summary>
        /// Vrací konstrukční funkci a to tak, ze vybira konstruktor s nejvetsim poctem parametru
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        private Func<T> GetBuildUpFunc<T>(Type t)
        {

            var ctors = t.GetConstructors();
            var ci = ctors.OrderByDescending(x => x.GetParameters().Length).FirstOrDefault(); //todo: co kdyz zadny neni
            var This = Expression.Constant(this);
            IEnumerable<Expression> exprs =
                ci.GetParameters().Select(x => Expression.Call(This, "GetService", new Type[] { x.ParameterType }));
            var createFunc = Expression.Lambda<Func<T>>(System.Linq.Expressions.Expression.New(ci, exprs));
            return createFunc.Compile();
        }





        object IPerunScope.Context
        {
            get { return this; }
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
            _scoppings.Dispose();
            
        }

        ~PerunContainer()
        {
            Dispose(false);
        }
        #endregion
    }
}
