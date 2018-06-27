using LetsAgree.IOC.MvxSimpleShim;
using MvvmCross.Platform;
using MvvmCross.Test.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsAgree.IOC.Extensions.MvxSimpleShim.Test
{
    public interface Irt { }
    public class rt : Irt { }
    public class decrt : Irt
    {
        public decrt(Irt next)
        {

        }
    }

    public interface ITesti { }
    public class t1 : ITesti { }
    public class t2 : ITesti { }
    public class t3 : ITesti { }


    [TestFixture]
    public class ExtensionTests : MvxIoCSupportingTest
    {
        protected override void AdditionalSetup()
        {
            base.AdditionalSetup();
            MvvmCross.Platform.IoC.MvxSimpleIoCContainer.Initialize();
        }
        [Test]
        public void TestDecoration()
        {
            base.Setup();
            using (var registryScope = new MvxSimpleIocImprovedCreator(a => a.GetTypes().Where(x => x.GetConstructors().Any())))
            {
                var reg = registryScope.Registry;
                reg.Register<Irt, rt>();
                reg.Register<Irt, decrt>().AsDecorator();
                var c = reg.GenerateContainer();
                Assert.IsInstanceOf<decrt>(c.Resolve<Irt>());
            }
        }
        [Test]
        public void TestCollection()
        {
            base.Setup();
            using (var registryScope = new MvxSimpleIocImprovedCreator(a => a.GetTypes().Where(x => x.GetConstructors().Any())))
            {
                var reg = registryScope.Registry;

                reg.Register<ITesti, t1>().AsSingleton().AsCollection();
                reg.Register<ITesti, t2>().AsCollection();
                reg.Register<ITesti, t3>();
                var c = reg.GenerateContainer();
                var all = c.Resolve<ITesti[]>();
                Assert.AreEqual(2, all.Length);
            }
        }
       
        class Root<C, L, R, N>
            where C : ISingletonConfig<C>, IDecoratorConfig<C>, ICollectionConfig<C>
            where L : ISingletonConfig<L>, ICollectionConfig<L>
            where N : IBasicContainer
            where R : IGenericRegistration<C>, IGenericLocatorRegistration<L>
        {
            public static void Compose(R registry)
            {
                // YAY
            }
        }

        [Test]
        public void TestComposition()
        {
            base.Setup();
            using (var registryScope = new MvxSimpleIocImprovedCreator(a => a.GetTypes().Where(x => x.GetConstructors().Any())))
            {
                var reg = registryScope.Registry;
                Root<IMvxImprovedConfig, IMvxImprovedLocatorConfig, IMvxImprovedRegistry, IMvxSimpleContainer>.Compose(reg);
            }
        }
    }
}
