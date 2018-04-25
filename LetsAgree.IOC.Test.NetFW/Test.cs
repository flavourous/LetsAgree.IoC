using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LetsAgree.IOC.Test.NetFW
{
    [TestFixture]
    public class UseTest
    {
        // An injection point, the composition root, expressing IOC requirments
        static void UseDI<C, R, T>(IRegistryCreator<C,R,T> creator)
            where R : IBasicRegistration<C,T>, IGenericRegistration<C,T>
            where C : ISingletonConfig, IDecoratorConfig
            where T : IBasicContainer, IGenericContainer
        {
            // And then doing some random stuff
            var reg = creator.GenerateRegistry();
            reg.Register<int, bool>();
            reg.Register(typeof(string), typeof(Assembly));
            var ct = reg.GenerateContainer();
            ct.TryResolve(out int lol);
            lol += 1;
            ct.TryResolve(out myIOCConstructedClassHonestly);
        }
        static ICustomTypeProvider myIOCConstructedClassHonestly;

        public abstract class MyLibrary
        {
            // Repeating constraints is annoying, but it allows users to send 3rd party DI frameworks to 3rd party Libraries without any fuss.
            // (generic paramaters on classes cannot be inferred)
            public static MyLibrary Create<C,R,T>(IRegistryCreator<C, R, T> c)
                where R : IBasicRegistration<C, T>, IGenericRegistration<C, T>
                where C : ISingletonConfig, IDecoratorConfig
                where T : IBasicContainer, IGenericContainer
            {
                return new MYDiClass<C, R, T>(c);
            }
            class MYDiClass<C, R, T> : MyLibrary
                where R : IBasicRegistration<C, T>, IGenericRegistration<C, T>
                where C : ISingletonConfig, IDecoratorConfig
                where T : IBasicContainer, IGenericContainer
            {
                public MYDiClass(IRegistryCreator<C, R, T> creator)
                {
                    // And then doing some random stuff
                    var reg = creator.GenerateRegistry();
                    reg.Register<int, bool>();
                    reg.Register(typeof(string), typeof(Assembly));
                    var ct = reg.GenerateContainer();
                    ct.TryResolve(out int lol);
                    lol += 1;
                    ct.TryResolve(out myIOCConstructedClassHonestly);
                }
            }
        }


        [Test]
        public void Use()
        {
            // Injecting our below implimentation
            Assert.Throws<NotImplementedException>(() => UseDI(new MyDIFramework()));
            Assert.Throws<NotImplementedException>(() => MyLibrary.Create(new MyDIFramework()));
        }

        // Implimentation capabilities
        public interface IConfigSpec : 
            ISingletonConfig, 
            IDecoratorConfig
        {
        }
        public interface IContainerSpec :
            IBasicContainer,
            IGenericContainer
        {
        }
        public interface IRegistrySpec<C,T> :
            IBasicRegistration<C,T>,
            IGenericRegistration<C,T>
            where C : IRegisterConfig
            where T : IContainer
        {
        }

        // Implimentation
        class MyDIFramework : IRegistryCreator<IConfigSpec, IRegistrySpec<IConfigSpec, IContainerSpec>, IContainerSpec>
        {
            public IRegistrySpec<IConfigSpec, IContainerSpec> GenerateRegistry()
            {
                return (IRegistrySpec<IConfigSpec, IContainerSpec>)new MyRegistry();
            }
        }
        class MyRegistry : IRegistrySpec<MyConfig, MyContainer>
        {
            public MyContainer GenerateContainer()
            {
                throw new NotImplementedException();
            }

            public MyConfig Register(Type service, Type impl)
            {
                throw new NotImplementedException();
            }

            public MyConfig Register(Type service, Func<object> creator)
            {
                throw new NotImplementedException();
            }

            public MyConfig Register<Service, Implimentation>()
            {
                throw new NotImplementedException();
            }

            public MyConfig Register<Service>(Func<Service> implimentation)
            {
                throw new NotImplementedException();
            }
        }
        class MyContainer : IContainerSpec
        {
            public object Resolve(Type t)
            {
                throw new NotImplementedException();
            }

            public T Resolve<T>()
            {
                throw new NotImplementedException();
            }

            public bool TryResolve(Type t, out object service)
            {
                throw new NotImplementedException();
            }

            public bool TryResolve<T>(out T service)
            {
                throw new NotImplementedException();
            }
        }
        class MyConfig : IConfigSpec
        {
            public void AsDecorator()
            {
                throw new NotImplementedException();
            }

            public void AsSingleton()
            {
                throw new NotImplementedException();
            }
        }
    }
}
