using System;
using System.Reflection;

namespace LetsAgree.IOC
{
    public interface IRegistryCreator<out Config, out Registry, out Container>
        where Registry : IRegister<Config, Container>
        where Config : IRegisterConfig
        where Container : IContainer
    {
        Registry GenerateRegistry();
    }

    public interface IRegisterConfig
    {
        
    }
    public interface ISingletonConfig : IRegisterConfig
    {
        void AsSingleton();
    }
    public interface IDecoratorConfig : IRegisterConfig
    {
        void AsDecorator();
    }

    public interface IRegister<out Config, out Container> 
        where Config : IRegisterConfig
        where Container : IContainer
    {
        Container GenerateContainer();
    }
    public interface IBasicRegistration<out Config, out Container> : IRegister<Config, Container>
        where Config : IRegisterConfig
        where Container : IContainer
    {
        Config Register(Type service, Type impl);
        Config Register(Type service, Func<Object> creator);
    }
    public interface IGenericRegistration<out Config, out Container> : IRegister<Config, Container> 
        where Config : IRegisterConfig
        where Container : IContainer
    {
        Config Register<Service, Implimentation>();
        Config Register<Service>(Func<Service> implimentation);
    }
    public interface IScanningRegistraction<out Config, out Container> : IRegister<Config, Container> 
        where Config : IRegisterConfig
        where Container : IContainer
    {
        void RegisterAssembly(Assembly a);
    }

    public interface IContainer
    {
    }
    public interface IBasicContainer : IContainer
    {
        object Resolve(Type t);
        bool TryResolve(Type t, out object service);
    }
    public interface IGenericContainer : IContainer
    { 
        T Resolve<T>();
        bool TryResolve<T>(out T service);
    }
}