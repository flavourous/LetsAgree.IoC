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
    [TestFixture]
    public class Decoration : MvxIoCSupportingTest
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
            IMvxSimpleDecoratingRegistry reg = new MvxSimpleIocWithDecoration(a => a.GetTypes().Where(x => x.GetConstructors().Any()));
            reg.Register<Irt, rt>();
            reg.Register<Irt, decrt>().AsDecorator();
            var c = reg.GenerateContainer();
            Assert.IsInstanceOf<decrt>(c.Resolve<Irt>());
        }
    }
}
