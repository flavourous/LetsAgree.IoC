using MvvmCross.Platform;
using MvvmCross.Platform.IoC;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LetsAgree.IOC.MvxSimpleShim
{
    public interface IMvxContainerSpec :
                                      IBasicContainer,
                                      IGenericContainer
    { }
    public interface IMvxConfigSpec :
                                   ISingletonConfig
    { }
    public interface IRegSpec :
                                IContainerGeneration<IMvxContainerSpec>,
                                IDynamicRegistration<IMvxConfigSpec>,
                                IGenericRegistration<IMvxConfigSpec>,
                                IDynamicLocatorRegistration<IMvxConfigSpec>,
                                IGenericLocatorRegistration<IMvxConfigSpec>,
                                IScanningRegistraction<IMvxConfigSpec>
    { }

    public class MvxRegistry : IRegSpec
    {
        readonly PPQueue<MvxConfig> ss = new PPQueue<MvxConfig>();
        readonly Func<Assembly, IEnumerable<Type>> CreatableTypes;
        public MvxRegistry(Func<Assembly, IEnumerable<Type>> CreatableTypes)
        {
            this.CreatableTypes = CreatableTypes;
        }

        public IMvxContainerSpec GenerateContainer()
        {
            while(ss.Count > 0) ss.Dequeue().Register();
            return new MvxContainer();
        }

        public IMvxConfigSpec Register(Type service, Type impl) 
            => ss.PP(new MvxDynConfig(service, impl));

        public IMvxConfigSpec Register<Service, Implimentation>() 
            where Implimentation : class, Service 
            where Service : class 
            => ss.PP(new MvxGenConfig<Service,Implimentation>());

        public IMvxConfigSpec Register(Type service, Func<object> creator) 
            => ss.PP(new MvxLocatorConfig(service, creator));

        public IMvxConfigSpec Register<Service>(Func<Service> implimentation) 
            where Service : class 
            => ss.PP(new MvxLocatorConfig<Service>(implimentation));

        public IMvxConfigSpec RegisterAssembly(Assembly a) 
            => ss.PP(new MvxScannerConfig(CreatableTypes(a)));
    }
    class PPQueue<T> : Queue<T> { public T PP(T item) { Enqueue(item); return item; } }
    abstract class MvxConfig :IMvxConfigSpec
    {
        public abstract void Register();
        protected bool singleton = false;
        public void AsSingleton() => singleton = true;
    }
    class MvxScannerConfig : MvxConfig 
    {
        readonly IEnumerable<Type> found;
        public MvxScannerConfig(IEnumerable<Type> found) => this.found = found;
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
            if (singleton) Mvx.RegisterSingleton(s, i);
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
    class MvxContainer : IMvxContainerSpec
    {
        public object Resolve(Type t) => Mvx.Resolve(t);
        public T Resolve<T>() where T : class => Mvx.Resolve<T>();
        public bool TryResolve(Type t, out object service) => Mvx.TryResolve(t, out service);
        public bool TryResolve<T>(out T service) where T : class => Mvx.TryResolve(out service);
    }
}