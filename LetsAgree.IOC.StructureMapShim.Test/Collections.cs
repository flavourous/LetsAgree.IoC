using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsAgree.IOC.StructureMapShim.Test
{
    public interface ITesti { }
    public class t1 : ITesti { }
    public class t2 : ITesti { }
    public class t3 : ITesti { }

    [TestFixture]
    public class Collections
    {
        [Test]
        public void Collects()
        {
            var reg = new SMRegistry();
            reg.Register<ITesti, t1>().AsSingleton().AsCollection();
            reg.Register<ITesti, t2>().AsCollection();
            reg.Register<ITesti, t3>();
            var c = reg.GenerateContainer();
            var all = c.Resolve<ITesti[]>();
            Assert.AreEqual(2, all.Length);
        }
    }
}
