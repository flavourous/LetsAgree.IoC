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
    public interface IMvxSimpleDecoratingRegistry :
        IDynamicRegistration<IMvxSimpleDecoratingConfig>,
        IGenericRegistration<IMvxSimpleDecoratingConfig>,
        IScanningRegistraction<IMvxSimpleDecoratingConfig>,
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
            drh.AnylyzeAndRegisterDecorators((s, i) => basic.Register(s, i), Mvx.IocConstruct);
            return new ProxyContainer { mc = basic.GenerateContainer() };
        }

        public IMvxSimpleDecoratingConfig Register(Type service, Type impl)
        {
            bool donothing = false;
            var makeDecorator = drh.ServiceRegisteredCallback(service, () =>
            {
                donothing = true;
                return impl;
            });
            return new FakedConfig((s, d) =>
            {
                if (donothing) return; // AsDecorator was called
                var reg = basic.Register(service, impl);
                if (s) reg.AsSingleton();
            }, () => makeDecorator());
        }

        public IMvxSimpleConfig Register<Service>(Func<Service> implimentation) where Service : class
        {
            // Types registered this way arent decoratable
            return basic.Register(implimentation);
        }

        IMvxSimpleDecoratingConfig IGenericRegistration<IMvxSimpleDecoratingConfig>.Register<Service, Implimentation>()
        {
            bool donothing = false;
            var makeDecorator = drh.ServiceRegisteredCallback(typeof(Service), () =>
            {
                donothing = true;
                return typeof(Implimentation);
            });
            return new FakedConfig((s, d) =>
            {
                if (donothing) return; // AsDecorator was called
                var reg = basic.Register<Service, Implimentation>();
                if (s) reg.AsSingleton();
            }, () => makeDecorator());
        }

        public IMvxSimpleDecoratingConfig RegisterAssembly(Assembly a)
        {
            var scanned = new HashSet<MvxTypeExtensions.ServiceTypeAndImplementationTypePair>
                (creatableTypes(a).EndingWith("Service").AsInterfaces());
            var decorateAll = scanned.SelectMany(p => p.ServiceTypes.Select(s =>
                                  drh.ServiceRegisteredCallback(s, () =>
                                  {
                                      p.ServiceTypes.Remove(s);
                                      return p.ImplementationType;
                                  })
                              )).ToArray();
            // It's gonna remove all service types if AsDecorator is called so no worries!
            return new FakedConfig((s, d) =>
            {
                if (s) scanned.RegisterAsLazySingleton();
                else scanned.RegisterAsDynamic();
            }, () =>
            {
                foreach (var act in decorateAll)
                    act();
            });
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
