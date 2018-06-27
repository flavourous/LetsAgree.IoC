using System;
using System.Reflection;

namespace LetsAgree.IOC
{
    // Generics like this scare me, more than once they have made code nearly impossible to change.  But,
    // I want to achieve compile time compliance of library ioc needs and ioc framework capabilities.

    // INoConfig here is important, e.g.
    // Requirment:    ISingletonConfig<INoConfig>
    // Satisfied By:  IMyConfig : ISingletonConfig<IMyConfig>, ISelectionConfig<IMyConfig>, IDecoratorConfig<INoConfig>

    // Implimentation Notes
    // ====================
    // Usually the easiest to use implimention is one that doesnt register the same config type at the same level:
    //  interface IMyConfig : ISingletonConfig<IDecoratorConfig<INoConfig>>, ISingletonConfig<ICollectionConfig<INoConfig>>
    //  ^^ that looks like a nice way to express available method trees (e.g. you cant call singleton twice), but it ends up
    //     but calling ends up with the "Which AsSingleton do you mean?" diamond problem
    // IoC frameworks seem to do one of the equivilants of...
    //  interface IMyConfig : ISingletonConfig<INoConfig>,... { }
    //  interface IMyConfig : ISingletonConfig<IMyConfig>,... { }
    // ...either you can only configure it in one way, or you can do any config including invalid/redundant configs.

    public interface INoConfig { } 
    public interface ISingletonConfig<out Continue> : INoConfig where Continue : INoConfig
    {
        /// <summary>
        /// Only one instance of this `Interface` will ever be made.
        /// </summary>
        /// <returns></returns>
        Continue AsSingleton();
    }
    public interface IDecoratorConfig<out Continue> : INoConfig where Continue : INoConfig
    {
        /// <summary>
        /// This `Concrete` : `Interface` contains a constructor parameter of type `Interface` for chaining with existing `Interface`. 
        /// There should be (only) one non-decorator `Interface` registered to provide closure.
        /// </summary>
        /// <returns></returns>
        Continue AsDecorator();
    }
    public interface ICollectionConfig<out Continue> : INoConfig where Continue : INoConfig
    {
        /// <summary>
        /// This will be (only) resolved in `Interface`[]
        /// </summary>
        /// <returns></returns>
        Continue AsCollection();
    }
    public interface ISelectionConfig<out Continue> : INoConfig where Continue : INoConfig
    {
        Continue EndingWith(String name);
    }

    public interface IDynamicRegistration<out Config> where Config : INoConfig
    {
        Config Register(Type service, Type impl);
    }
    public interface IDynamicLocatorRegistration<out Config> where Config : INoConfig
    {
        Config Register(Type service, Func<Object> creator);
    }

    // TODO: Do the constraints on Service/Implimentation belong here? Should we split again into different interfaces?
    public interface IGenericRegistration<out Config> where Config : INoConfig
    {
        Config Register<Service, Implimentation>() 
            where Implimentation : class, Service 
            where Service : class;
    }
    public interface IGenericLocatorRegistration<out Config> where Config : INoConfig
    {
        Config Register<Service>(Func<Service> implimentation) 
            where Service : class;
    }

    public interface IScanningRegistration<out Config> where Config : INoConfig
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