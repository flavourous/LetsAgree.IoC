using System;
using System.Collections.Generic;
using System.Linq;

namespace System.IoC.Extensions
{
    // TODO: Can this be written in terms of System.IoC interfaces alone, as a registry decorator or something?
    public delegate void MakeDecoratorCallback();
    public delegate Type GetServiceCallback(bool skipRegistration);
    public delegate void RegisterServiceDelegate(Type service, Func<object> creator);
    public delegate Object ResolveDelegate(Type service);
    public delegate Object IocConstructDelegate(Type service);
    public class DecoratingRegistryHelper
    {
        // run extra registrations (that delegate to the container) before creating the container.
        public void AnylyzeAndRegisterDecorators(RegisterServiceDelegate registerService, ResolveDelegate resolve, IocConstructDelegate create)
        {
            foreach(var dstack in serviceStacks.Where(x=>x.Value.Any(y=>y.madeDecorator)))
            {
                if (dstack.Value.First().madeDecorator) throw new InvalidOperationException("root cant be decorator");
                if (dstack.Value.Skip(1).Any(x => !x.madeDecorator)) throw new InvalidOperationException("all following root must be decorator");

                // call do nit regiuster new
                var root = dstack.Value.First().service(true);
                var decorators = dstack.Value.Skip(1).Select(x => x.service(true)).ToArray();

                // regenerate a stack each time.. (e.g. what if not a singleton?)
                Func<Stack<DecTypeIocInfo>> decStack = () => new Stack<DecTypeIocInfo>(decorators.Select(x => new DecTypeIocInfo(dstack.Key, x)));

                // registring construction like this
                registerService(dstack.Key, () => RecusrivelyConstructStack(root, decStack(), resolve, create));
            }
        }

        class DecTypeIocInfo
        {
            readonly ResolveBuilder constructBuilder = new ResolveBuilder();
            //public readonly Type[] parameters;
            public readonly Type serviceType;
            public DecTypeIocInfo(Type t, Type decoratorService)
            {
                serviceType = t;
                constructBuilder.UseType(decoratorService);
                constructBuilder.UseConstructor(x => x.Contains(t));
            }
            public Object Construct(Func<Type,Object> resolveScope)
            {
                constructBuilder.UseResolver(resolveScope);
                return constructBuilder.Build();
            }
        }

        Object RecusrivelyConstructStack(Type rootDecorated, Stack<DecTypeIocInfo> sStack, ResolveDelegate resolve, IocConstructDelegate create)
        {
            if (sStack.Count == 0)
                return create(rootDecorated);
            var cs = sStack.Pop();
            Func<Object> nextLevel = () => RecusrivelyConstructStack(rootDecorated, sStack, resolve, create);
            return cs.Construct(x => x == cs.serviceType ? nextLevel() : resolve(x));
        }

        class ssArg { public GetServiceCallback service; public bool madeDecorator; }
        readonly Dictionary<Type, List<ssArg>> serviceStacks = new Dictionary<Type, List<ssArg>>();

        // nees to know the decorations and roots. top decorator will be registered, so root cannot be.
        public MakeDecoratorCallback ServiceRegisteredCallback(Type t, GetServiceCallback c)
        {
            if (!serviceStacks.ContainsKey(t)) serviceStacks[t] = new List<ssArg>();
            var ssa = new ssArg { service = c };
            serviceStacks[t].Add(ssa);
            return () => ssa.madeDecorator = true;
        }
    }
}