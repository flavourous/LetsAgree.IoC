using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IoC.StructureMapShim.Test
{
    class Root<C,L,R,N>
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

    [TestFixture]
    public class Composition
    {
        [Test]
        public void Compose()
        {
            var reg = new SMRegistry();
            Root<IConfigSpec, ILocatorConfigSpec, IRegSpec, IContainerSpec>.Compose(reg);
        }
    }
}
