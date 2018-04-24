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
        [Test]
        public void Use()
        {
            Assert.Throws<NotImplementedException>(() => UseDI(new MyDi()));
        }

        static void UseDI<C, R>(IRegistryCreator<C,R> creator)
            where C : ISingletonConfig, IDecoratorConfig
            where R : IBasicRegistration<C>, IGenericRegistration<C>
        {
            var reg = creator.GenerateRegistry();
            reg.Register<int, bool>();
            reg.Register(typeof(string), typeof(Assembly));
        }

        class MyDi : IRegistryCreator<IConfigSpec, IRegistrySpec<IConfigSpec>>
        {
            public IRegistrySpec<IConfigSpec> GenerateRegistry()
            {
                return (IRegistrySpec<IConfigSpec>)new MyReg();
            }
        }
        public interface IConfigSpec : 
            ISingletonConfig, 
            IDecoratorConfig
        {
        }
        public interface IRegistrySpec<T> :
            IBasicRegistration<T>,
            IGenericRegistration<T>
            where T : IRegisterConfig
        {
        }
        class MyReg : IRegistrySpec<MyConfig>
        {
            public IContainer GenerateContainer()
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
