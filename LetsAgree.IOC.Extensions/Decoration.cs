using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LetsAgree.IOC.Extensions
{
    // TODO: Can this be written in terms of LetsAgree.IOC interfaces alone, as a registry decorator or something?
    public delegate void MakeDecoratorCallback();
    public delegate Type DoNotRegisterCallback();
    public delegate void RegisterServiceDelegate(Type service, Func<object> creator);
    public delegate Object CreateWithIocDelegate(Type service);
    public class DecoratingRegistryHelper
    {
        // run extra registrations (that delegate to the container) before creating the container.
        public void AnylyzeAndRegisterDecorators(RegisterServiceDelegate registerService, CreateWithIocDelegate iocCreate)
        {
            foreach(var dstack in serviceStacks.Where(x=>x.Value.Any(y=>y.madeDecorator)))
            {
                if (dstack.Value.First().madeDecorator) throw new InvalidOperationException("root cant be decorator");
                if (dstack.Value.Skip(1).Any(x => !x.madeDecorator)) throw new InvalidOperationException("all following root must be decorator");

                // call do nit regiuster new
                var root = dstack.Value.First().service();
                var decorators = dstack.Value.Skip(1).Select(x => x.service()).ToArray();

                // regenerate a stack each time.. (e.g. what if not a singleton?)
                Func<Stack<DecTypeIocInfo>> decStack = () => new Stack<DecTypeIocInfo>(decorators.Select(x => new DecTypeIocInfo(dstack.Key, x)));

                // registring construction like this
                registerService(dstack.Key, () => RecusrivelyConstructStack(root, decStack(), iocCreate));
            }
        }

        class DecTypeIocInfo
        {
            readonly ConstructorInfo constructor;
            public readonly Type[] parameters;
            public readonly Type serviceType;
            public DecTypeIocInfo(Type t, Type decoratorService)
            {
                serviceType = t;
                var actrs = decoratorService.GetConstructors()
                             .ToArray();
                var ctrs = actrs.Select(x => new { c = x, p = x.GetParameters().Select(y => y.ParameterType).ToArray() })
                                .Where(x=>x.p.Contains(t))
                                .ToArray();
                if (ctrs.Length > 1) throw new InvalidOperationException("Multiple public constructors have the decorator service");
                if (ctrs.Length == 0)
                {
                    var ex = new InvalidOperationException($"No public constructors of {decoratorService.Name} have the decorator service. ({actrs.Length} total)");
                    ex.Data["implimentation"] = decoratorService;
                    ex.Data["interface"] = t;
                    throw ex;
                }
                constructor = ctrs.Single().c;
                parameters = ctrs.Single().p;
            }
            public Object Construct(object[] args) => constructor.Invoke(args);
        }

        Object RecusrivelyConstructStack(Type rootDecorated, Stack<DecTypeIocInfo> sStack, CreateWithIocDelegate iocCreate)
        {
            if (sStack.Count == 0)
                return iocCreate(rootDecorated);
            var cs = sStack.Pop();
            Func<Object> nextLevel = () => RecusrivelyConstructStack(rootDecorated, sStack, iocCreate);
            var args = cs.parameters.Select(x => x == cs.serviceType ? nextLevel() : iocCreate(x));
            return cs.Construct(args.ToArray());
        }

        class ssArg { public DoNotRegisterCallback service; public bool madeDecorator; }
        readonly Dictionary<Type, List<ssArg>> serviceStacks = new Dictionary<Type, List<ssArg>>();

        // nees to know the decorations and roots. top decorator will be registered, so root cannot be.
        public MakeDecoratorCallback ServiceRegisteredCallback(Type t, DoNotRegisterCallback c)
        {
            if (!serviceStacks.ContainsKey(t)) serviceStacks[t] = new List<ssArg>();
            var ssa = new ssArg { service = c };
            serviceStacks[t].Add(ssa);
            return () => ssa.madeDecorator = true;
        }
    }
}