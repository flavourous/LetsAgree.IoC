using System;
using System.Reflection;

namespace LetsAgree.IOC
{
    // Generics like this scare me, more than once they have made code nearly impossible to change.  But,
    // we want to achieve compile time compliance of library ioc needs and ioc framework capabilities.

    // Implimenting NoConfig here is important, e.g.
    // Requirment:    ISingletonConfig<INoConfig>
    // Satisfied By:  IMyConfig : ISingletonConfig<IMyConfig>, ISelectionConfig<IMyConfig>, IDecoratorConfig<INoConfig>

    // Generally in these docs, `T` is the concrete type implimenting resolvable service type `I`
   
    public interface INoConfig { } 
    public interface ISingletonConfig<out Continue> : INoConfig
    {
        /// <summary>
        /// Only one instance of this service will ever be made.
        /// </summary>
        /// <returns></returns>
        Continue AsSingleton();
    }
    public interface IDecoratorConfig<out Continue> : INoConfig
    {
        /// <summary>
        /// This T : I contains a constructor parameter of type I for chaining with existing I. 
        /// There should be (only) one non-decorator I registered to provide closure.
        /// </summary>
        /// <returns></returns>
        Continue AsDecorator();
    }
    public interface ICollectionConfig<out Continue> : INoConfig
    {
        /// <summary>
        /// This will be resolved in I[]
        /// </summary>
        /// <returns></returns>
        Continue AsCollection();
    }
    public interface ISelectionConfig<out Continue> : INoConfig
    {
        Continue EndingWith(String name);
    }

    public interface IDynamicRegistration<out Config> 
    {
        Config Register(Type service, Type impl);
    }
    public interface IDynamicLocatorRegistration<out Config> 
    {
        Config Register(Type service, Func<Object> creator);
    }

    // TODO: Do the constraints on Service/Implimentation belong here? Should we split again into different interfaces?
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

    public interface IScanningRegistration<out Config> 
    {
        Config RegisterAssembly(Assembly a);
    }

    // TODO: should we have containers at all? should resolution by constructor injection be encouraged instead?
    public interface IContainerGeneration<out Container>
    {
        Container GenerateContainer();
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