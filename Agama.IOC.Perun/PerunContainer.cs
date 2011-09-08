using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Agama.Services.Api.IOC;

namespace Agama.IOC.Perun
{

    public class BuildingContext
    {
        /// <summary>
        /// Type being now constructed.
        /// </summary>
        public readonly Type ResolvingType;
        

        public BuildingContext(Type resolvingType, PerunContainer container)
        {
            ResolvingType = resolvingType;
            Container = container;
        }


        public IImplementationBuilder  CurrentBuilder 
        {
            get;internal set;
        }

        public PerunContainer Container 
        {
            get;
            private  set;
        }
    }

    //public class BuildingContext<T>
    //{
    //    public readonly Type ResolvingType;
    //    public BuildingContext()
    //    {
    //        ResolvingType = typeof (T);
    //    }
    //    public IImplementationBuilder CurrentBuilder
    //    {
    //        get;
    //        internal set;
    //    }
    //}


   

    public class PerunContainer : IIocContainer, IPerunScope
    {
        
        internal readonly Dictionary<Type,List<IImplementationBuilder>> _all = new Dictionary<Type, List<IImplementationBuilder>>();
        private ScoppingRegistration _scoppings = new ScoppingRegistration();
        private readonly Dictionary<Type, Type> _cache= new Dictionary<Type, Type>();

        public PerunContainer()
        {
          
            
            var t = Expression.Constant(this);
            //rozsireni pro podporu Func
            this.RegisterType(typeof(Func<>),ctx =>
                                                      {
                                                         var targetType = ctx.ResolvingType.GetGenericArguments()[0];
                                                         Func<object> res = delegate()
                                                                              {
                                                                                  var fce = GetFuncExpressionForResolvingType(targetType).Compile();
                                                                                  return fce;
                                                                              };
                                                          return res; //fce vracící fci
                                                         
                                                         // Type fType = Expression.GetFuncType(targetType);
                                                         //var exp = Expression.Call(t, "GetService",new Type[] {targetType});
                                                         //LambdaExpression result = Expression.Lambda(fType, exp);
                                                         //Delegate compiled = result.Compile();
                                                         //return compiled;
                                                     },new InnerTypeDependencyScope());



            
            
            //rozsireni pro podporu Lazy
            this.RegisterType(typeof(Lazy<>), ctx =>
            {
                var targetType = ctx.ResolvingType.GetGenericArguments()[0];
                
                var expr = GetFuncExpressionForResolvingType(targetType);
                var ci = ctx.ResolvingType.GetConstructor(new Type[] {typeof (Func<>).MakeGenericType(targetType), typeof (bool)});
                Type fType = Expression.GetFuncType(ctx.ResolvingType);
                var createdFunc = Expression.Lambda(fType,Expression.New(ci, expr, Expression.Constant(true)));
                var func = (Func<object>) createdFunc.Compile();

                
               //re-registration for next quick resolving. //todo refaktorovat, navic nefunguje ==
               //Type basePluginType= null;
               //if (!_cache.TryGetValue(ctx.ResolvingType,out  basePluginType) )
               //{
               //    InnerTypeDependencyScope innerScope = ctx.CurrentBuilder.Scope as InnerTypeDependencyScope;
               //    RegisterType(ctx.ResolvingType, func, innerScope!=null ? innerScope.FinalScope : ctx.CurrentBuilder.Scope/* todo tady by mel byt natvrdo prevzaty scope*/);
               //    _cache.Add(ctx.ResolvingType,typeof(Lazy<>));
               //}
                return func;
                //var result = func.Invoke();
                //return result;

            }, new InnerTypeDependencyScope());
            
            //test
            //var g = this.GetService<Lazy<IList<string>>>();
            //var j = g.Value;
            //var h = this.GetService<Lazy<IList<string>>>();
            //var k = h.Value;
            
        }

       

        /// <summary>
        /// Vrací výraz pro resolvovací funkcí.
        /// </summary>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        private LambdaExpression GetFuncExpressionForResolvingType(Type pluginType)
        {
            var This = Expression.Constant(this); //opravdu konstanta ?
            Type fType = Expression.GetFuncType(pluginType);
            var exp = Expression.Call(This, "GetService", new Type[] { pluginType });
            LambdaExpression result = Expression.Lambda(fType, exp);
            return result;
                                                         
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
            var This = Expression.Constant(this); //todo opravdu konstanta ?
            IEnumerable<Expression> exprs =
                ci.GetParameters().Select(x => Expression.Call(This, "GetService", new Type[] { x.ParameterType }));
            var tmp = exprs.ToList(); //todo: odstranit
            var createFunc = Expression.Lambda<Func<T>>(System.Linq.Expressions.Expression.New(ci, exprs));
            return createFunc.Compile();
        }

       

        public bool IsConfiguredFor(Type interfaceType)
        {
            return _all.ContainsKey(interfaceType);
        }

        public bool IsConfiguredFor<T>()
        {
            return _all.ContainsKey(typeof(T));
        }

        public T GetService<T>()
        {
            return (T) GetService(typeof (T));
        }

        public object GetService(Type t)
        {

            List<IImplementationBuilder> impls;
            if (!_all.TryGetValue(t, out impls) ) 
            {
                //resolving generic by opened generic deftype 
                if (t.IsGenericType)
                {
                    var gd = t.GetGenericTypeDefinition();
                    if (!_all.TryGetValue(gd, out impls))
                    {
                        return null;
                    }
                }
            }
            var ctx = new BuildingContext(t, this);
            var result = impls[0].Get(ctx); //first is default
            return result;
        }

      





        public IEnumerable<T> GetServices<T>()
        {
            return GetServices(typeof (T)).Cast<T>();
           
        }

        public IEnumerable<object> GetServices(Type t)
        {
            BuildingContext ctx = null; 
            List<IImplementationBuilder> impls;
            List<OpenedImplementationBuilder> implsToSkip = null; 
            if (_all.TryGetValue(t, out impls))
            {
                ctx = new BuildingContext(t, this);
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
            if (t.IsGenericType)
            {
                var gd = t.GetGenericTypeDefinition();

                if (_all.TryGetValue(gd, out impls))
                {
                    if (ctx==null)
                        ctx = new BuildingContext(t,this);
                    
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

        public void RegisterInstance<T>(T o) where T : class
        {
            RegisterType<T>(x=>o,LifeTimeType.Singleton); //todo: upravit rozhrani aby slo zadat scope - mozna odstranit  - jde pomoci extensiony
        }

        public void RegisterInstance(Type t, object o) //todo: upravit rozhrani aby slo zadat scope - mozna odstranit  - jde pomoci extensiony
        {
            RegisterType(t,x=>o,LifeTimeType.Singleton);
        }

        public void RegisterType<TReal>(LifeTimeType lifeTimeType)
        {
            RegisterType<TReal>(CreateFunc<TReal, TReal>(), lifeTimeType);
        }

        public void RegisterType<TInterface, TReal>(LifeTimeType lifeTimeType) where TReal : TInterface
        {
            RegisterType<TInterface>(CreateFunc<TInterface,TReal>(), lifeTimeType);
        }

       public void RegisterType<TInterface>(Func<TInterface> builder, LifeTimeType lifeTimeType)
       {
           RegisterType<TInterface>(CreateFunc(builder),lifeTimeType);
       }

        public void RegisterType<TInterface>(Func<BuildingContext,TInterface> builder, LifeTimeType lifeTimeType)
        {
            RegisterType<TInterface>(builder, GetScope(lifeTimeType));
        }

        public void RegisterType<TInterface>(Func<BuildingContext,TInterface> builder,IPerunScope scope)
        {
            List<IImplementationBuilder> implementators;
            var interfaceType = typeof (TInterface);
            if (!_all.TryGetValue(interfaceType, out implementators))
            {
                implementators = new List<IImplementationBuilder>();
                _all.Add(interfaceType, implementators);
            }

            var impl = new ImplementationBuilder<TInterface>(_scoppings, builder,scope);
            implementators.Add(impl);
        }

       

        public void RegisterType(Type type, Type @interface, LifeTimeType lifeTimeType)
        {
            RegisterType(@interface ?? type,CreateFunc(type),lifeTimeType);
        }

       

        /// <summary>
        /// Pomocná metoda, která má za ukol obalit puvodni funkci, tak aby zavisela na kontextu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="innerFunc"></param>
        /// <returns></returns>
        private Func<BuildingContext, T> CreateFunc<T>(Func<T> innerFunc )
        {
            return ctx => innerFunc(); //carrying 
        }


        private Func<BuildingContext,object> CreateFunc(Type type)
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
            return this.GetBuildUpFunc<TInterface>(typeof (TReal));
        }

        
        public void RegisterType(Type interfaceType, Func<object> builder, LifeTimeType lifeTimeType)
        {
            RegisterType(interfaceType, CreateFunc(builder), this.GetScope(lifeTimeType));
        }

        public void RegisterType(Type interfaceType, Func<object> builder,IPerunScope scope)
        {
            RegisterType(interfaceType, CreateFunc(builder), scope);
        }
       
        public void RegisterType(Type interfaceType, Func<BuildingContext,object> builder,LifeTimeType lifeTimeType)
        {
            RegisterType(interfaceType, builder, GetScope(lifeTimeType));
        }

        public void RegisterType(Type interfaceType, Func<BuildingContext,object> builder,IPerunScope scope)
        {
           

            IImplementationBuilder impl;
            if (interfaceType.IsGenericTypeDefinition)
                impl = new OpenedImplementationBuilder(_scoppings, interfaceType, builder, scope);
            else impl = new ImplementationBuilder(_scoppings, interfaceType, builder, scope);

            RegisterInternal(interfaceType,impl);

            
        }

        internal void RegisterInternal(Type interfaceType, IImplementationBuilder builder, Tuple<OpenedImplementationBuilder, ImplementationBuilder> callerToReplace = null)
        {
            List<IImplementationBuilder> implementators;
            if (!_all.TryGetValue(interfaceType, out implementators))
            {
                implementators = new List<IImplementationBuilder>();
                _all.Add(interfaceType, implementators);


                if (interfaceType.IsGenericType && (!interfaceType.IsGenericTypeDefinition))
                {
                    //pokud definujeme genericky typ, ale uz je definovan predpis pro otevreny genericky typ, 
                    //tak tento otevreny musi zustat jako defaultni
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
                    return;
                }
            }

            implementators.Add(builder);//jinak ji pridame na konec
        }

        public void EjectServices(LifeTimeType lifeTimeType)
        {
            var scopeObject = GetScope(lifeTimeType).Context;
            if (scopeObject != null)
            {
                _scoppings.RemoveAll(x=>x.Equals(scopeObject));
                
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
       


        public bool? IsCleanupRequiredForScope(LifeTimeType lifeTimeType)
        {
            return null;
        }


        private IPerunScope GetScope(LifeTimeType lifeTimeType)
        {
            switch (lifeTimeType)
            {
                case LifeTimeType.Singleton:
                    return this;
                case LifeTimeType.Thread:
                    return ThreadScope.Instance;
                case LifeTimeType.Transient:
                    return TransientScope.Instance;
                case LifeTimeType.HttpContext:
                    return HttpContextScope.Instance; //todo:
                default: throw new NotImplementedException();
            }
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
