using LetsAgree.IOC.Extensions;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LetsAgree.IOC.StructureMapShim
{
    // Supported specs
    public interface IContainerSpec : 
                                      IBasicContainer, 
                                      IGenericContainer { }
    public interface IConfigSpec :
                                   IDecoratorConfig<IConfigSpec>,
                                   ICollectionConfig<IConfigSpec>,
                                   ISingletonConfig<IConfigSpec>
                                    { }
    public interface ILocatorConfigSpec :
                                   ICollectionConfig<ILocatorConfigSpec>,
                                   ISingletonConfig<ILocatorConfigSpec>
        { }
    public interface IRegSpec :         
                                IContainerGeneration<IContainerSpec>,
                                IDynamicRegistration<IConfigSpec>, 
                                IGenericRegistration<IConfigSpec>, 
                                IDynamicLocatorRegistration<ILocatorConfigSpec>,
                                IGenericLocatorRegistration<ILocatorConfigSpec>,
                                IScanningRegistration<INoConfig> { }

    // Shims to StructureMap
    public class SMRegistry : IRegSpec
    {
        Registry registry = new Registry();
        Stack<SMConfig> toRegister = new Stack<SMConfig>();

        public IContainerSpec GenerateContainer()
        {
            foreach (var tr in toRegister)
                tr.Register(registry);

            foreach (var tp in ToCollect)
                Registrars[tp.Key](registry);

            return new SMContainer(registry);
        }

        Dictionary<Type, List<Func<IContext, Object>>> ToCollect = new Dictionary<Type, List<Func<IContext, object>>>();
        Dictionary<Type, Action<Registry>> Registrars = new Dictionary<Type, Action<Registry>>();
        SMConfig Push(SMConfig s)
        {
            s.ToCollect = ToCollect;
            s.Registrars = Registrars;
            toRegister.Push(s);
            return s;
        }

        public IConfigSpec Register(Type service, Type impl) => Push(SMConfig.Create(service, impl));
        public ILocatorConfigSpec Register(Type service, Func<object> creator) => Push(SMConfig.Create(service, creator));
        public IConfigSpec Register<Service, Implimentation>() where Implimentation : class, Service where Service : class => Push(SMConfig.Create<Service, Implimentation>());
        public ILocatorConfigSpec Register<Service>(Func<Service> implimentation) where Service : class => Push(SMConfig.Create(implimentation));

        public INoConfig RegisterAssembly(Assembly a)
        {
            registry.Scan(x =>
            {
                x.Assembly(a);
                x.WithDefaultConventions();
            });
            return new NoConfig();
        }
    }

    class NoConfig : INoConfig { }

    class SMConfig : IConfigSpec, ILocatorConfigSpec
    {
        public Dictionary<Type, List<Func<IContext, Object>>> ToCollect;
        public Dictionary<Type, Action<Registry>> Registrars;
        public void AddCollect<T>(Func<IContext, Object> c)
        {
            var t = typeof(T);
            if (!ToCollect.ContainsKey(t))
                ToCollect[t] = new List<Func<IContext, Object>>();
            ToCollect[t].Add(c);
            Registrars[t] = reg => reg.For(t.MakeArrayType())
                                      .Use(con => ToCollect[t].Select(x => (T)x(con)).ToArray())
                                      .Singleton();
        }
        readonly IRegistrar registrar;
        private SMConfig(IRegistrar registrar) => this.registrar = registrar;
        public static SMConfig Create(Type service, Type impl)
        {
            var closed = typeof(TypedRegistrar<,>).MakeGenericType(service, impl);
            var instance = Activator.CreateInstance(closed) as IRegistrar;
            return new SMConfig(instance);
        }
        public static SMConfig Create<S, I>() where I : class, S
        {
            return new SMConfig(new TypedRegistrar<S, I>());
        }
        public static SMConfig Create(Type service, Func<object> locator)
        {
            var ct = typeof(LocateRegistrar<>).MakeGenericType(service);
            var inst = Activator.CreateInstance(ct, locator) as IRegistrar;
            return new SMConfig(inst);
        }
        public static SMConfig Create<S>(Func<S> locator)
        {
            return new SMConfig(new LocateRegistrar<S>(locator));
        }
        bool decorate, singleton, collection;
        public SMConfig AsDecorator()
        {
            decorate = true;
            return null;
        }
        public SMConfig AsSingleton()
        {
            singleton = true;
            return this;
        }
        public SMConfig AsCollection()
        {
            collection = true;
            return this;
        }
        public void Register(Registry reg) => registrar.Register(reg, this);

        IConfigSpec IDecoratorConfig<IConfigSpec>.AsDecorator() => AsDecorator();
        IConfigSpec ICollectionConfig<IConfigSpec>.AsCollection() => AsCollection();
        IConfigSpec ISingletonConfig<IConfigSpec>.AsSingleton() => AsSingleton();
        ILocatorConfigSpec ICollectionConfig<ILocatorConfigSpec>.AsCollection() => AsCollection();
        ILocatorConfigSpec ISingletonConfig<ILocatorConfigSpec>.AsSingleton() => AsSingleton();

        interface IRegistrar { void Register(Registry reg, SMConfig c); }
        class TypedRegistrar<S, I> : IRegistrar where I : class, S
        {
            readonly ResolveBuilder builder;
            public TypedRegistrar()
            {
                builder = new ResolveBuilder();
                builder.UseType(typeof(I));
                builder.UseConstructor(x => true);
            }

            public void Register(Registry reg, SMConfig c)
            {
                if (c.collection)
                {
                    S singleton = default(S);
                    bool made = false;
                    c.AddCollect<S>(x =>
                    {
                        if (c.singleton)
                        {
                            if (made) return singleton;
                            builder.UseResolver(x.GetInstance);
                            singleton = (I)builder.Build(); // because we never registered I
                            made = true;
                            return singleton;
                        }
                        else
                        {
                            builder.UseResolver(x.GetInstance);
                            return (I)builder.Build();
                        }

                    });
                }
                else
                {
                    var fors = reg.For<S>();
                    var next = c.decorate ? fors.DecorateAllWith<I>() : fors.Use<I>();
                    if (c.singleton) next = next.Singleton();
                }
            }
        }
        class LocateRegistrar<S> : IRegistrar
        {
            readonly Func<S> locator;
            public LocateRegistrar(Func<S> locator) => this.locator = locator;
            public LocateRegistrar(Func<Object> locator) => this.locator = () => (S)locator();
            public void Register(Registry reg, SMConfig c)
            {
                var fors = reg.For<S>();
                if (c.decorate) throw new NotImplementedException("Can't decorate a locator!");
                if (c.collection)
                {
                    c.AddCollect<S>(x => locator());
                }
                else
                {
                    var use = fors.Use(x => locator());
                    if (c.singleton) use.Singleton();
                }
            }
        }
    }
    class SMContainer : IContainerSpec
    {
        readonly Container c;
        public SMContainer(Registry sr)
        {
            c = new Container(sr);
        }

        public object Resolve(Type t)
        {
            return c.GetInstance(t);
        }

        public T Resolve<T>() where T : class
        {
            return c.GetInstance<T>();
        }

        public bool TryResolve(Type t, out object service)
        {
            service = c.TryGetInstance(t);
            return service != null;
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            service = c.TryGetInstance<T>();
            return !EqualityComparer<T>.Default.Equals(service, default(T));
        }
    }
}
