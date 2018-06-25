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
    public interface IMvxImprovedDynConfig : ISingletonConfig<ICollectionConfig<INoConfig>>, IDecoratorConfig<INoConfig> { }
    public interface IMvxImprovedConfig : ISingletonConfig<ICollectionConfig<INoConfig>>, IDecoratorConfig<INoConfig>, ICollectionConfig<ISingletonConfig<INoConfig>> { }
    public interface IMvxImprovedLocatorConfig : ISingletonConfig<ICollectionConfig<INoConfig>>, ICollectionConfig<ISingletonConfig<INoConfig>> { }
    public interface IMvxScanningConfig : ISelectionConfig<IMvxScanningConfig>, ISingletonConfig<INoConfig> { }
    public interface IMvxImprovedRegistry :
        IDynamicRegistration<IMvxImprovedDynConfig>,
        IGenericRegistration<IMvxImprovedConfig>,
        IScanningRegistration<IMvxScanningConfig>,
        IGenericLocatorRegistration<IMvxImprovedLocatorConfig>,
        IContainerGeneration<IMvxSimpleContainer>
    { }

    /// <summary>
    /// Not incredibly smart about how it computes decoration, so nothing
    /// will be registered until this is disposed, at which point the stacks
    /// are analyzed and registered.  This also happens any time a container is generated.
    /// </summary>
    public class MvxSimpleIocImprovedCreator : IDisposable
    {
        readonly MvxSimpleIocImproved registry;
        public MvxSimpleIocImprovedCreator(Func<Assembly, IEnumerable<Type>> CreatableTypes)
        {
            registry = new MvxSimpleIocImproved(CreatableTypes);
        }
        public IMvxImprovedRegistry Registry => registry;
        public void Dispose() => registry.GenerateContainer();
    }

    internal class MvxSimpleIocImproved : IMvxImprovedRegistry
    {
        readonly Func<Assembly, IEnumerable<Type>> creatableTypes;
        readonly MvxRegistry basic;
        readonly DecoratingRegistryHelper drh = new DecoratingRegistryHelper();
        public MvxSimpleIocImproved(Func<Assembly, IEnumerable<Type>> CreatableTypes)
        {
            creatableTypes = CreatableTypes;
            basic = new MvxRegistry(CreatableTypes);
        }

        ProxyContainer lastContainer;
        public IMvxSimpleContainer GenerateContainer()
        {
            lastContainer = new ProxyContainer();
            drh.AnylyzeAndRegisterDecorators((s, i) => basic.Register(s, i), x => lastContainer.Resolve(x), Mvx.IocConstruct);
            while (ss.Count > 0) ss.Dequeue().Process();
            lastContainer.mc = basic.GenerateContainer();
            return lastContainer;
        }

        readonly Dictionary<Type, List<Object>> Collectables = new Dictionary<Type, List<Object>>();
        void AddCollectable<TInterface>(Func<TInterface> locator)
        {
            var tInterface = typeof(TInterface);
            if (!Collectables.ContainsKey(tInterface))
            {
                Collectables[tInterface] = new List<object> { locator };
                basic.Register(tInterface.MakeArrayType(), () => Collectables[tInterface].Select(x => ((Func<TInterface>)x)()).ToArray());
            }
            else Collectables[tInterface].Add(locator);
        }
        readonly PPQueue<FakedConfig> ss = new PPQueue<FakedConfig>();

        public IMvxImprovedDynConfig Register(Type service, Type impl)
        {
            bool donothing = false;
            var makeDecorator = drh.ServiceRegisteredCallback(service, skipRegistration =>
            {
                donothing = skipRegistration;
                return impl;
            });
            return ss.PP(new FakedConfig((s, d, c) =>
            {
                if (donothing) return; // AsDecorator was called
                var reg = basic.Register(service, impl);
                if (s) reg.AsSingleton();
            }, () => makeDecorator()));
        }

        public IMvxImprovedLocatorConfig Register<Service>(Func<Service> implimentation) where Service : class
        {
            // Types registered this way arent decoratable
            return ss.PP(new FakedConfig((s, d, c) =>
            {
                Type service = typeof(Service);
                if (c)
                {
                    Lazy<Service> singleton = new Lazy<Service>(implimentation);
                    AddCollectable<Service>(s ? () => singleton.Value : implimentation);
                }
                else
                {
                    var reg = basic.Register<Service>(implimentation);
                    if (s) reg.AsSingleton();
                }
            }, delegate { }));
        }

        IMvxImprovedConfig IGenericRegistration<IMvxImprovedConfig>.Register<Service, Implimentation>()
        {
            bool donothing = false;
            var makeDecorator = drh.ServiceRegisteredCallback(typeof(Service), skipRegistration =>
            {
                donothing = skipRegistration;
                return typeof(Implimentation);
            });
            return ss.PP(new FakedConfig((s, d, c) =>
            {
                if (donothing) return; // AsDecorator was called
                if (c)
                {
                    Func<Service> create = () => Mvx.IocConstruct<Implimentation>();
                    Lazy<Service> singleton = new Lazy<Service>(create);
                    AddCollectable<Service>(s ? () => singleton.Value : create);
                }
                else
                {
                    var reg = basic.Register<Service, Implimentation>();
                    if (s) reg.AsSingleton();
                }
            }, () => makeDecorator()));
        }

        public IMvxScanningConfig RegisterAssembly(Assembly a)
        {
            // pls use classes not lambda closure hax
            FakedScanningConfig config = null;
            config = new FakedScanningConfig((s, d, c) =>
            {
                // assert !d && !c
                IEnumerable<Type> filtering = creatableTypes(a);
                foreach (var end in config.ending)
                    filtering = filtering.EndingWith(end);
                var scanned =  new HashSet<MvxTypeExtensions.ServiceTypeAndImplementationTypePair>
                (filtering.AsInterfaces());
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
            });
            return (IMvxScanningConfig)ss.PP(config);
        }
    }

    class FakedScanningConfig : FakedConfig, IMvxScanningConfig
    {
        public FakedScanningConfig(Action<bool, bool, bool> process)
            : base(process, delegate { })
        {
        }

        public readonly List<String> ending = new List<string>();
        public IMvxScanningConfig EndingWith(string name)
        {
            ending.Add(name);
            return this;
        }

        INoConfig ISingletonConfig<INoConfig>.AsSingleton()
        {
            base.AsSingleton();
            return null;
        }
    }

    // TODO: The patternless lambda coding in here is getting out of control!
    class FakedConfig : IMvxImprovedConfig, IMvxImprovedDynConfig, IMvxImprovedLocatorConfig, ICollectionConfig<INoConfig>, ISingletonConfig<INoConfig>
    {
        public void Process() => process(singleton, decorator, collectable);
        readonly Action<bool, bool, bool> process;
        readonly Action makeDecorator;
        public FakedConfig(Action<bool, bool, bool> process, Action makeDecorator)
        {
            this.makeDecorator = makeDecorator;
            this.process = process;
        }
        bool decorator, singleton, collectable;
        public INoConfig AsDecorator()
        {
            decorator = true;
            makeDecorator();
            return null;
        }
        public ICollectionConfig<INoConfig> AsSingleton()
        {
            singleton = true;
            return this;
        }

        public ISingletonConfig<INoConfig> AsCollection()
        {
            collectable = true;
            return this;
        }

        INoConfig ISingletonConfig<INoConfig>.AsSingleton()
        {
            AsSingleton();
            return null;
        }

        INoConfig ICollectionConfig<INoConfig>.AsCollection()
        {
            AsCollection();
            return null;
        }
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
