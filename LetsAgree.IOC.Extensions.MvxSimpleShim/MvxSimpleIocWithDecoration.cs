using LetsAgree.IOC.MvxSimpleShim;
using MvvmCross.Platform;
using MvvmCross.Platform.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LetsAgree.IOC.Extensions.MvxSimpleShim
{
    // TODO: this wouldnt be needed if IOC.Extensions worked with the IOC interfaces alone
    public interface IMvxSimpleDecoratingConfig : ISingletonConfig, IDecoratorConfig { }
    public interface IMvxScanningConfig : IFluentSelectionConfig<IMvxScanningConfig>, IMvxSimpleDecoratingConfig { }
    public interface IMvxSimpleDecoratingRegistry :
        IDynamicRegistration<IMvxSimpleDecoratingConfig>,
        IGenericRegistration<IMvxSimpleDecoratingConfig>,
        IScanningRegistraction<IMvxScanningConfig>,
        IGenericLocatorRegistration<IMvxSimpleConfig>,
        IContainerGeneration<IMvxSimpleContainer>
    { }

    public class MvxSimpleIocWithDecoration : IMvxSimpleDecoratingRegistry
    {
        readonly Func<Assembly, IEnumerable<Type>> creatableTypes;
        readonly MvxRegistry basic;
        readonly DecoratingRegistryHelper drh = new DecoratingRegistryHelper();
        public MvxSimpleIocWithDecoration(Func<Assembly, IEnumerable<Type>> CreatableTypes)
        {
            creatableTypes = CreatableTypes;
            basic = new MvxRegistry(CreatableTypes);
        }

        public IMvxSimpleContainer GenerateContainer()
        {
            var proxyContainer = new ProxyContainer();
            drh.AnylyzeAndRegisterDecorators((s, i) => basic.Register(s, i), proxyContainer.Resolve, Mvx.IocConstruct);
            while (ss.Count > 0) ss.Dequeue().Process();
            proxyContainer.mc = basic.GenerateContainer();
            return proxyContainer;
        }

        readonly PPQueue<FakedConfig> ss = new PPQueue<FakedConfig>();

        public IMvxSimpleDecoratingConfig Register(Type service, Type impl)
        {
            bool donothing = false;
            var makeDecorator = drh.ServiceRegisteredCallback(service, skipRegistration =>
            {
                donothing = skipRegistration;
                return impl;
            });
            return ss.PP(new FakedConfig((s, d) =>
            {
                if (donothing) return; // AsDecorator was called
                var reg = basic.Register(service, impl);
                if (s) reg.AsSingleton();
            }, () => makeDecorator()));
        }

        public IMvxSimpleConfig Register<Service>(Func<Service> implimentation) where Service : class
        {
            // Types registered this way arent decoratable
            return basic.Register(implimentation);
        }

        IMvxSimpleDecoratingConfig IGenericRegistration<IMvxSimpleDecoratingConfig>.Register<Service, Implimentation>()
        {
            bool donothing = false;
            var makeDecorator = drh.ServiceRegisteredCallback(typeof(Service), skipRegistration =>
            {
                donothing = skipRegistration;
                return typeof(Implimentation);
            });
            return ss.PP(new FakedConfig((s, d) =>
            {
                if (donothing) return; // AsDecorator was called
                var reg = basic.Register<Service, Implimentation>();
                if (s) reg.AsSingleton();
            }, () => makeDecorator()));
        }

        public IMvxScanningConfig RegisterAssembly(Assembly a)
        {
            // pls use classes not lambda closure hax
            FakedScanningConfig config = null;
            config = new FakedScanningConfig((s, d) =>
            {
                IEnumerable<Type> filtering = creatableTypes(a);
                foreach (var c in config.ending)
                    filtering = filtering.EndingWith(c);
                var scanned =  new HashSet<MvxTypeExtensions.ServiceTypeAndImplementationTypePair>
                (filtering.AsInterfaces());
                // TODO: this doesnt actually work with the other registrar calls 
                if (d) foreach (var act in scanned.SelectMany(p => p.ServiceTypes.Select(v =>
                                         drh.ServiceRegisteredCallback(v, skipRegistration =>
                                         {
                                             if (skipRegistration)
                                                 p.ServiceTypes.Remove(v);
                                             return p.ImplementationType;
                                         })
                                     )))
                        act();
                if (s) scanned.RegisterAsLazySingleton();
                else scanned.RegisterAsDynamic();
            }, delegate { /* we'll just find out later ok (no not ok, not if you want to play with the TODO above) */ });
            return (IMvxScanningConfig)ss.PP(config);
        }
    }

    class FakedScanningConfig : FakedConfig, IMvxScanningConfig
    {
        public FakedScanningConfig(Action<bool, bool> process, Action makeDecorator)
            :base(process, makeDecorator)
        {
        }

        public readonly List<String> ending = new List<string>();
        public IMvxScanningConfig EndingWith(string name)
        {
            ending.Add(name);
            return this;
        }
    }

    class FakedConfig : IMvxSimpleDecoratingConfig
    {
        public void Process() => process(singleton, decorator);
        readonly Action<bool, bool> process;
        readonly Action makeDecorator;
        public FakedConfig(Action<bool, bool> process, Action makeDecorator)
        {
            this.process = process;
            this.makeDecorator = makeDecorator;
        }
        bool decorator, singleton;
        public void AsDecorator() { decorator = true; makeDecorator(); }
        public void AsSingleton() => singleton = true;
    }
    class ProxyContainer : IMvxSimpleContainer
    {
        public IOC.MvxSimpleShim.IMvxSimpleContainer mc;
        public object Resolve(Type t) => mc.Resolve(t);
        public T Resolve<T>() where T : class => mc.Resolve<T>();
        public bool TryResolve(Type t, out object service) => mc.TryResolve(t, out service);
        public bool TryResolve<T>(out T service) where T : class => mc.TryResolve<T>(out service);
    }
}
