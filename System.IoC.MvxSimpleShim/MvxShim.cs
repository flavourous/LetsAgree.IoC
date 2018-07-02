using MvvmCross.Platform;
using MvvmCross.Platform.IoC;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LetsAgree.IOC.MvxSimpleShim
{
    public interface IMvxSimpleContainer :
                                      IBasicContainer,
                                      IGenericContainer
    { }
    public interface IMvxSimpleConfig :
                                   ISingletonConfig<INoConfig>
    { }

    public interface IMvxScanningConfig :
                                   ISingletonConfig<INoConfig>,
                                   ISelectionConfig<IMvxScanningConfig>
    { }
    public interface IMvxSimpleRegistry :
                                IContainerGeneration<IMvxSimpleContainer>,
                                IDynamicRegistration<IMvxSimpleConfig>,
                                IGenericRegistration<IMvxSimpleConfig>,
                                IDynamicLocatorRegistration<IMvxSimpleConfig>,
                                IGenericLocatorRegistration<IMvxSimpleConfig>,
                                IScanningRegistration<IMvxScanningConfig>
    { }
    public class PPQueue<T> : Queue<T> { public T PP(T item) { Enqueue(item); return item; } }
    public class MvxRegistry : IMvxSimpleRegistry
    {
        readonly PPQueue<MvxConfig> ss = new PPQueue<MvxConfig>();
        readonly Func<Assembly, IEnumerable<Type>> CreatableTypes;
        public MvxRegistry(Func<Assembly, IEnumerable<Type>> CreatableTypes)
        {
            this.CreatableTypes = CreatableTypes;
        }

        public IMvxSimpleContainer GenerateContainer()
        {
            while(ss.Count > 0) ss.Dequeue().Register();
            return new MvxContainer();
        }

        public IMvxSimpleConfig Register(Type service, Type impl) 
            => ss.PP(new MvxDynConfig(service, impl));

        public IMvxSimpleConfig Register<Service, Implimentation>() 
            where Implimentation : class, Service 
            where Service : class 
            => ss.PP(new MvxGenConfig<Service,Implimentation>());

        public IMvxSimpleConfig Register(Type service, Func<object> creator) 
            => ss.PP(new MvxLocatorConfig(service, creator));

        public IMvxSimpleConfig Register<Service>(Func<Service> implimentation) 
            where Service : class 
            => ss.PP(new MvxLocatorConfig<Service>(implimentation));

        public IMvxScanningConfig RegisterAssembly(Assembly a) 
            => (MvxScannerConfig)ss.PP(new MvxScannerConfig(CreatableTypes(a)));
    }
    abstract class MvxConfig : IMvxSimpleConfig
    {
        public abstract void Register();
        protected bool singleton = false;
        public INoConfig AsSingleton()
        {
            singleton = true;
            return null;
        }
    }
    class MvxScannerConfig : MvxConfig, IMvxScanningConfig
    {
        private IEnumerable<Type> found;
        public MvxScannerConfig(IEnumerable<Type> found) => this.found = found;

        public IMvxScanningConfig EndingWith(string name)
        {
            found = found.EndingWith(name);
            return this;
        }

        public override void Register()
        {
            if (singleton) found.AsInterfaces().RegisterAsLazySingleton();
            else found.AsInterfaces().RegisterAsDynamic();
        }
    }
    class MvxLocatorConfig : MvxConfig
    {
        readonly Type s;
        readonly Func<Object> loc;
        public MvxLocatorConfig(Type s, Func<Object> loc)
        {
            this.s = s;
            this.loc = loc;
        }
        public override void Register()
        {
            if (singleton) Mvx.RegisterSingleton(s, loc);
            else Mvx.RegisterType(s, loc);
        }
    }
    class MvxLocatorConfig<T> : MvxConfig where T : class
    {
        readonly Func<T> loc;
        public MvxLocatorConfig(Func<T> loc) => this.loc = loc;
        public override void Register()
        {
            if (singleton) Mvx.LazyConstructAndRegisterSingleton(loc);
            else Mvx.RegisterType(loc);
        }
    }
    class MvxDynConfig : MvxConfig
    {
        readonly Type s, i;
        public MvxDynConfig(Type s, Type i) { this.s = s; this.i = i; }
        public override void Register()
        {
            if (singleton) Mvx.RegisterSingleton(s, () => Mvx.IocConstruct(i));
            else Mvx.RegisterType(s, i);
        }
    }
    class MvxGenConfig<S,I> : MvxConfig where S : class where I : class, S  
    {
        public override void Register()
        {
            if (singleton) Mvx.RegisterSingleton<S>(() => Mvx.IocConstruct<I>());
            else Mvx.RegisterType<S, I>();
        }
    }
    class MvxContainer : IMvxSimpleContainer
    {
        public object Resolve(Type t) => Mvx.Resolve(t);
        public T Resolve<T>() where T : class => Mvx.Resolve<T>();
        public bool TryResolve(Type t, out object service) => Mvx.TryResolve(t, out service);
        public bool TryResolve<T>(out T service) where T : class => Mvx.TryResolve(out service);
    }
}