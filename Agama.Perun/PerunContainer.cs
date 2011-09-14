using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Agama.Perun
{
    /// <summary>
    /// DI container.
    /// </summary>
    public partial class PerunContainer :  IPerunScope, IDisposable
    {

        internal readonly ConcurrentDictionary<Type, List<IImplementationBuilder>> _all = new ConcurrentDictionary<Type, List<IImplementationBuilder>>();
        internal ScoppingRegistration _scoppings = new ScoppingRegistration();
        private readonly Dictionary<Type, Type> _cache= new Dictionary<Type, Type>();
        /// <summary>
        /// Version of data, every registration or remove increments this number
        /// </summary>
        private long _serial = 0;
        
        /// <summary>
        /// Creates new DI container. If <paramref name="configurationManager"/> is not specified, the default one is used.
        /// </summary>
        /// <param name="configurationManager"></param>
        public PerunContainer(PerunContainerConfigurationManager configurationManager = null)
        {
            ConfigurationManager = configurationManager ?? new PerunContainerConfigurationManager(this);
            
            ConfigurationManager.Configure();

            _serial = 0; //we set it to zero after configurationManager running.
        }

        /// <summary>
        /// Gets current configuration manager used for this container.
        /// </summary>
        /// <remarks>
        /// (This mannager is set when you create <see cref="PerunContainer"/>, see constructor)
        /// </remarks>
        public readonly PerunContainerConfigurationManager ConfigurationManager;

        /// <summary>
        /// Events is osccured when 'GetService' method returns NULL.
        /// If this event is occured, you can register missing component/plugin, because
        /// container will try resolve component again.
        /// </summary>
        public event EventHandler<AfterMissedComponentEventArgs> AfterMissedComponent;

        /// <summary>
        /// Version of data, every registration or remove components increments this number.
        /// If this number is changed, something has been changed.
        /// </summary>
        public long Serial
        {
            get
            {
                return _serial;
            }
        }
       

        #region 'Query' methods
        /// <summary>
        /// Returns <c>true</c> if this plugin type is registered.
        /// </summary>
        /// <param name="pluginType">pluginType (ussually interface type)</param>
        /// <returns></returns>
        public bool IsConfiguredFor(Type pluginType)
        {
            return _all.ContainsKey(pluginType);//todo: co genericke otevrene definice
        }

        /// <summary>
        ///  Returns <c>true</c> if this plugin type is registered.
        /// </summary>
        /// <typeparam name="T">pluginType (ussually interface type)</typeparam>
        /// <returns></returns>
        public bool IsConfiguredFor<T>()
        {
            return _all.ContainsKey(typeof(T));//todo: co genericke otevrene definice
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
        /// Get service by plugin-info.
        /// </summary>
        /// <param name="configuredInfo"></param>
        /// <returns></returns>
        public object GetService(IConfiguredPluginInfo configuredInfo)
        {

            var ob = configuredInfo as OpenedImplementationBuilder;
            if (ob != null)
                throw new ArgumentException("Resolving service from opened generic definition is not possible.");
            var ctx = new BuildingContext(configuredInfo.PluginType, this);
            var result = ((IImplementationBuilder)configuredInfo).Get(ctx);
            return result;


        }

        /// <summary>
        /// Enumerates service determined by their predicate
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object> GetServices(Predicate<IConfiguredPluginInfo> match)
        {

            foreach (var listOfBuilders in _all)
            {
                foreach (var implementationBuilder in listOfBuilders.Value)
                {
                    if (implementationBuilder is RedirectImplementationBuilder) continue;
                    if (implementationBuilder is OpenedImplementationBuilder) continue;
                    var ctx = new BuildingContext(implementationBuilder.PluginType,this);
                    var res = implementationBuilder.Get(ctx);
                    if (res != null)
                        yield return res;
                }
            }
            yield break;
        }

       
        /// <summary>
        /// Returns default service determined by their pluginType
        /// </summary>
        /// <param name="pluginType">pluginType (ussually interface type)</param>
        /// <returns></returns>
        public object GetService(Type pluginType)
        {
            return GetService(pluginType, true);
        }
        
        private object GetService(Type pluginType,bool raiseEvent)
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
                        if (raiseEvent)
                        {
                            var args = new AfterMissedComponentEventArgs(pluginType);
                            if (AfterMissedComponent != null)
                            {
                                AfterMissedComponent(this, args);
                                return GetService(pluginType, false);
                            }
                        }
                        return null;
                    }
                }
                else
                {
                    if (raiseEvent)
                    {
                        var args = new AfterMissedComponentEventArgs(pluginType);
                        if (AfterMissedComponent != null)
                        {
                            AfterMissedComponent(this, args);
                            return GetService(pluginType, false);
                        }
                    }
                    return null;
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

        public IConfiguredPluginInfo<TReal> RegisterType<TReal>(IPerunScope scope = null)
        {
            return (IConfiguredPluginInfo<TReal>) RegisterType<TReal>(CreateFunc<TReal>(), scope ?? TransientScope.Instance);
        }
        public IConfiguredPluginInfo<TInterface> RegisterType<TInterface, TReal>(IPerunScope scope = null) where TReal : TInterface
        {
            var inner = CreateFunc<TReal,TInterface>();
            
            return RegisterType<TInterface>(inner, scope ?? TransientScope.Instance);
        }

        /// <summary>
        /// Experimental.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TReal"></typeparam>
        /// <param name="scope"></param>
        /// <returns></returns>
        public IConfiguredPluginInfo<TReal> ____RegisterTypeConcrete<TInterface, TReal>(IPerunScope scope = null) where TReal : TInterface
        {
            var inner = CreateFunc<TReal>();
            Func<BuildingContext, TReal> outer = ctx => inner();
            return RegisterType<TInterface,TReal>(outer, scope ?? TransientScope.Instance);
        }

        public  IConfiguredPluginInfo<TInterface> RegisterType<TInterface>(Func<TInterface> builder, IPerunScope scope = null)
       {
           return  RegisterType<TInterface>(CreateFunc(builder), scope ?? TransientScope.Instance);
           
       }

       

        public IConfiguredPluginInfo<TInterface> RegisterType<TInterface>(Func<BuildingContext,TInterface> builder,IPerunScope scope = null)
        {
            List<IImplementationBuilder> implementators;
            var interfaceType = typeof (TInterface);
            while (true)
            {
                if (!_all.TryGetValue(interfaceType, out implementators))
                {
                    implementators = new List<IImplementationBuilder>();
                    if (_all.TryAdd(interfaceType, implementators))
                        continue; //else the cycle goes again

                }
                Interlocked.Increment(ref _serial);
                break;
            }

            var impl = new ImplementationBuilder<TInterface>(this, builder,scope ?? TransientScope.Instance);
            implementators.Add(impl);
            return impl;
        }

      

        public IConfiguredPluginInfo<TReal> RegisterType<TInterface,TReal>(Func<BuildingContext, TReal> builder, IPerunScope scope = null) where TReal : TInterface
        {
            List<IImplementationBuilder> implementators;
            var interfaceType = typeof(TInterface);
            while (true)
            {
                if (!_all.TryGetValue(interfaceType, out implementators))
                {
                    implementators = new List<IImplementationBuilder>();
                    if (!_all.TryAdd(interfaceType, implementators))
                         continue;; //else the cycle goes again

                }
                Interlocked.Increment(ref _serial);
                break;
            }

            var impl = new ImplementationBuilder<TInterface,TReal>(this, builder, scope ?? TransientScope.Instance);
            implementators.Add(impl);
            return impl;
        }



        public IConfiguredPluginInfo RegisterType(Type type, Type @interface = null, IPerunScope scope = null)
        {
           return RegisterType(@interface ?? type,CreateFunc(type),scope ?? TransientScope.Instance);
        }



        public IConfiguredPluginInfo RegisterType(Type interfaceType, Func<object> builder, IPerunScope scope = null)
        {
            return RegisterType(interfaceType, CreateFunc(builder), scope ?? TransientScope.Instance);
        }



        public IConfiguredPluginInfo RegisterType(Type interfaceType, Func<BuildingContext, object> builder, IPerunScope scope = null)
        {
           

            IImplementationBuilder impl;
            if (interfaceType.IsGenericTypeDefinition)
                impl = new OpenedImplementationBuilder(this, interfaceType, builder, scope ?? TransientScope.Instance);
            else impl = new ImplementationBuilder(this, interfaceType, builder, scope ?? TransientScope.Instance);

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

        /// <summary>
        /// Unregister all components by given pluginType.
        /// Every IDisposable component is disposed. 
        /// (<seealso cref="UnRegister"/> which keep components alive).
        /// If pluginType is set by opened generic definition (e.g. typeof(IList&lt;&gt;), this this plugin is kept.
        /// </summary>
        /// <param name="pluginType"></param>
        public void DisposeService(Type pluginType)
        {
            List<IImplementationBuilder> implementators;
            if (_all.TryGetValue(pluginType, out implementators))
            {
                var toDispose = (from i in implementators
                                 let ib = i as ImplementationBuilder
                                 where ib == null || ib.Creator == null
                                 select i).ToList();
                //to avoid Ienumerable modified exception
                toDispose.ForEach(x=>x.Dispose());

                
            }
            
        }

        
        /// <summary>
        /// Try to remove definition, all components keep alive.
        /// Pokusi se vyjmout definici. Pouze ji vyjme a neprovadi dispose na drzenych objektech, jinak by doslo k zacykleni.
        /// Defakto dojde pouze k odstraneni definice, ale veskere zijici komponenty jsou ponechany nazivu, dokud plati jejich scope.
        /// Pokud builder je typu <see cref="OpenedImplementationBuilder"/>, pak jsou odregistrovani i vsichni uzavrene genericke definice vychazejici z tohoto buildru.
        /// </summary>
        /// <param name="builder"></param>
        internal void UnRegister(IImplementationBuilder builder)
        {
            List<IImplementationBuilder> implementators;
            if (!this._all.TryGetValue(builder.PluginType, out implementators))
                return;
            var ob = builder as OpenedImplementationBuilder;
            if (ob != null)
            {
                //odregistrovani i konkretnich generickych potomku vychazejicich z otevrene genericke definice
                List<IImplementationBuilder> toRemoveList = (from i in implementators
                                                         let ib = i as ImplementationBuilder
                                                         where ib != null && ib.Creator == builder
                                                         select i).ToList();
                toRemoveList.ForEach(x=>implementators.Remove(x));
                return;
            }

            //=> it is ImplementationBuilder, it is only once
            var toRemove = implementators.Find(x => builder == x);
            if (toRemove == null)
                return;
            implementators.Remove(toRemove);

            if (implementators.Count == 0)
            {
                List<IImplementationBuilder> tmp;
                if (this._all.TryRemove(builder.PluginType,out tmp ))
                    Interlocked.Increment(ref _serial);
            }


        }


        #endregion
        internal IImplementationBuilder RegisterInternal(Type interfaceType, IImplementationBuilder builder, Tuple<OpenedImplementationBuilder, ImplementationBuilder> callerToReplace = null)
        {
            List<IImplementationBuilder> implementators;
            while (true)
            {
                if (!_all.TryGetValue(interfaceType, out implementators))
                {
                    implementators = new List<IImplementationBuilder>();
                    if (!_all.TryAdd(interfaceType, implementators))
                        continue; //try again, parallel access
                    
                    Interlocked.Increment(ref _serial);

                    if (callerToReplace == null && interfaceType.IsGenericType &&
                        (!interfaceType.IsGenericTypeDefinition))
                    {
                        //pokud definujeme genericky typ, ale uz je definovan predpis pro otevreny genericky typ, 
                        //tak tento otevreny musi zustat jako defaultni
                        //Tuto vetev vsak nevolame, pokud jde o zakladani volane otevrene definice (callerToReplace==null)
                        List<IImplementationBuilder> openedImplementators;
                        if (_all.TryGetValue(interfaceType.GetGenericTypeDefinition(), out openedImplementators))
                        {
                            implementators.Add(
                                new RedirectImplementationBuilder(interfaceType,
                                                                  (OpenedImplementationBuilder) openedImplementators[0]));
                        }
                    }
                }
                else
                {
                    //jakmile otevřená definice má svoji konkrétní implementaci, nahradíme puvodni 'redirector'
                    if (callerToReplace != null)
                    {
                        int indexToReplace = implementators.FindIndex(x =>
                                                                          {
                                                                              var openedRedirector =
                                                                                  x as RedirectImplementationBuilder;
                                                                              return (openedRedirector != null &&
                                                                                      openedRedirector.Target ==
                                                                                      callerToReplace.Item1);

                                                                          }
                            );
                        if (indexToReplace >= 0)
                            implementators[indexToReplace] = callerToReplace.Item2;
                        return null; //neni potreba nic vracet pokud callerToReplace!=null
                    }
                }
                break;
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

        private Func<TReal> CreateFunc<TReal>()
        {
            return this.GetBuildUpFunc<TReal>(typeof(TReal));
        }
        private Func<TPlugin> CreateFunc<TReal,TPlugin>()
        {
            return this.GetBuildUpFunc<TPlugin>(typeof(TReal));
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
