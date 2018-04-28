using System;
using System.Reflection;

namespace LetsAgree.IOC
{
    public interface INoConfig { }
    public interface ISingletonConfig : INoConfig
    {
        void AsSingleton();
    }
    public interface IDecoratorConfig : INoConfig
    {
        void AsDecorator();
    }

    public interface IContainerGeneration<out Container>
    {
        Container GenerateContainer();
    }
    public interface IDynamicRegistration<out Config> 
    {
        Config Register(Type service, Type impl);
    }
    public interface IDynamicLocatorRegistration<out Config> 
    {
        Config Register(Type service, Func<Object> creator);
    }
    public interface IGenericRegistration<out Config> 
    {
        Config Register<Service, Implimentation>() 
            where Implimentation : class, Service 
            where Service : class;
    }
    public interface IGenericLocatorRegistration<out Config>  
    {
        Config Register<Service>(Func<Service> implimentation) 
            where Service : class;
    }
    public interface IScanningRegistraction<out Config> 
    {
        Config RegisterAssembly(Assembly a);
    }

    public interface IBasicContainer 
    {
        object Resolve(Type t);
        bool TryResolve(Type t, out object service);
    }
    public interface IGenericContainer 
    { 
        T Resolve<T>() where T : class;
        bool TryResolve<T>(out T service) where T : class;
    }
}