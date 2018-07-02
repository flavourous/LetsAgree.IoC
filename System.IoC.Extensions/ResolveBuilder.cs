using System;
using System.Linq;
using System.Reflection;

namespace System.IoC.Extensions
{
    public class ResolveBuilder
    {
        Type buildType;
        Func<Type, Object> buildResolver;
        ConstructorInfo buildCtor;
        Type[] buildParams;

        public void UseType(Type t) => this.buildType = t;

        public void UseResolver(Func<Type, Object> resolve) => this.buildResolver = resolve;

        public void UseConstructor(Func<Type[], bool> constructorSelector)
        {
            var actrs = buildType.GetConstructors()
                      .ToArray();
            var ctrs = actrs.Select(x => new { c = x, p = x.GetParameters().Select(y => y.ParameterType).ToArray() })
                            .Where(x => constructorSelector(x.p))
                            .ToArray();
            if (ctrs.Length > 1) throw new InvalidOperationException("Multiple public constructors match");
            if (ctrs.Length == 0)
            {
                var ex = new InvalidOperationException($"No public constructors of {buildType.Name} match. ({actrs.Length} total)");
                ex.Data["implimentation"] = buildType;
                throw ex;
            }
            buildCtor = ctrs.Single().c;
            buildParams = ctrs.Single().p;
        }

        public object Build() => buildCtor.Invoke(buildParams.Select(buildResolver).ToArray());
    }
}