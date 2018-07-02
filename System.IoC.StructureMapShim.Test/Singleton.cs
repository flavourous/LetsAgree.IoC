using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IoC.StructureMapShim.Test
{
    
    [TestFixture]
    public class Singleton
    {
        [Test]
        public void OneFromManyImplimintationRegistrations()
        {
            var reg = new SMRegistry();
            reg.Register<ITesti, t1>().AsSingleton().AsCollection();
            reg.Register<ITesti, t1>().AsSingleton();
            reg.Register<t1, t1>().AsSingleton();
            var c = reg.GenerateContainer();
            Assert.AreSame(c.Resolve<ITesti>(), c.Resolve<t1>());
            Assert.AreSame(c.Resolve<ITesti[]>()[0], c.Resolve<t1>());
            Assert.AreSame(c.Resolve<ITesti>(), c.Resolve<ITesti[]>()[0]);
        }
    }
}
