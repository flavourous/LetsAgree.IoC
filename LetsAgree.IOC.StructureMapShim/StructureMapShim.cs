using StructureMap;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LetsAgree.IOC.StructureMapShim
{
    // Supported specs
    public interface IContainerSpec : 
                                      IBasicContainer, 
                                      IGenericContainer { }
    public interface IConfigSpec : 
                                   ISingletonConfig, 
                                   IDecoratorConfig { }
    public interface ILocatorConfigSpec : 
                                          ISingletonConfig { }
    public interface IRegSpec :         
                                IContainerGeneration<IContainerSpec>,
                                IDynamicRegistration<IConfigSpec>, 
                                IGenericRegistration<IConfigSpec>, 
                                IDynamicLocatorRegistration<ILocatorConfigSpec>,
                                IGenericLocatorRegistration<ILocatorConfigSpec>,
                                IScanningRegistraction<INoConfig>

    {
    }

    // Shims to StructureMap
    public class SMRegistry : IRegSpec
    {
        Registry registry = new Registry();
        Stack<SMConfig> toRegister = new Stack<SMConfig>();

        public IContainerSpec GenerateContainer()
        {
            foreach (var tr in toRegister) tr.Register(registry);
            return new SMContainer(registry);
        }

        SMConfig Push(SMConfig s)
        {
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
        readonly IRegistrar registrar;
        private SMConfig(IRegistrar registrar) => this.registrar = registrar;
        public static SMConfig Create(Type service, Type impl)
        {
            var closed = typeof(TypedRegistrar<,>).MakeGenericType(service, impl);
            var instance = Activator.CreateInstance(closed) as IRegistrar;
            return new SMConfig(instance);
        }
        public static SMConfig Create<S, I>() where I : S
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
        bool decorate, singleton;
        public void AsDecorator() => decorate = true;
        public void AsSingleton() => singleton = true;
        public void Register(Registry reg) => registrar.Register(reg, this);
        interface IRegistrar { void Register(Registry reg, SMConfig c); }
        class TypedRegistrar<S, I> : IRegistrar where I : S
        {
            public void Register(Registry reg, SMConfig c)
            {
                var fors = reg.For<S>();
                if (c.decorate) fors.DecorateAllWith<I>();
                else
                {
                    var use = fors.Use<I>();
                    if (c.singleton) use.Singleton();
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
                if (c.decorate) throw new NotImplementedException("no thanks");
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
