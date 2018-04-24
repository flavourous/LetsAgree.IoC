using System;
using System.Reflection;

namespace LetsAgree.IOC
{
    public interface IRegistryCreator<out C, out R>
        where R : IRegister<C>
        where C : IRegisterConfig
    {
        R GenerateRegistry();
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

    public interface IRegister<out C> where C : IRegisterConfig
    {
        IContainer GenerateContainer();
    }
    public interface IBasicRegistration<out C> : IRegister<C> where C : IRegisterConfig
    {
        C Register(Type service, Type impl);
        C Register(Type service, Func<Object> creator);
    }
    public interface IGenericRegistration<out C> : IRegister<C> where C : IRegisterConfig
    {
        C Register<Service, Implimentation>();
        C Register<Service>(Func<Service> implimentation);
    }
    public interface IScanningRegistraction<out C> : IRegister<C> where C : IRegisterConfig
    {
        void RegisterAssembly(Assembly a);
    }
    
    public interface IContainer
    {
        object Resolve(Type t);
        T Resolve<T>();
        bool TryResolve(Type t, out object service);
        bool TryResolve<T>(out T service);
    }
}