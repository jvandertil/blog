+++
author = "Jos van der Til"
title = "Dynamically dispatching events to subclasses"
date  = 2019-10-25T15:00:00+01:00
type = "post"
tags = [ ".NET", "CSharp", "Performance" ]
draft = true
+++

Consider the following interfaces and classes:
```cs
public interface IDomainEvent { }

public class CustomerBecamePreferred : IDomainEvent { }

public class Customer 
{
  public IEnumerable<IDomainEvent> GetUncommittedEvents()
  {
    // Stub implementation
    yield return new CustomerBecamePreferred();
  }
}

public class BaseRepository 
{
  public void Save(Customer customer)
  {
    foreach (var e in customer.GetUncommittedEvents())
    {
       Apply(e); // ERROR: Does not compile.
    }

    SaveChanges();
  }
}

public class CustomerRepository
{
  private bool _preferred;

  private void Apply(CustomerBecamePreferred e) 
  {
    _preferred = true;
  }
}
```

## Using the dynamic keyword

## Using pure reflection

## Using reflection and IL emitting

```cs
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace FunWithDynamicDispatch
{
    public interface IDispatcher
    {
        Task HandleAsync(object instance);
    }

    public static class EmitDynamicDispatcher
    {
        private static readonly ConcurrentDictionary<Type, TypedDynamicDispatcher> _dispatchers = new ConcurrentDictionary<Type, TypedDynamicDispatcher>();

        public static IDispatcher For(object instance)
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var type = instance.GetType();

            var dispatcher = _dispatchers.GetOrAdd(type, t => new TypedDynamicDispatcher(t));

            return new InstanceTypedDynamicDispatcher(dispatcher, instance);
        }

        private sealed class InstanceTypedDynamicDispatcher : IDispatcher
        {
            private readonly TypedDynamicDispatcher _dispatcher;

            private readonly object _instance;

            public InstanceTypedDynamicDispatcher(TypedDynamicDispatcher dispatcher, object instance)
            {
                _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
                _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            }

            public Task HandleAsync(object obj)
            {
                return _dispatcher.HandleAsync(_instance, obj);
            }
        }

        private sealed class TypedDynamicDispatcher
        {
            private delegate Task Route(object instance, object arg);

            private readonly ConcurrentDictionary<Type, Route> _routes = new ConcurrentDictionary<Type, Route>();

            private readonly Type _type;

            public TypedDynamicDispatcher(Type type)
            {
                _type = type ?? throw new ArgumentNullException(nameof(type));
            }

            public Task HandleAsync(object instance, object obj)
            {
                var route = _routes.GetOrAdd(obj.GetType(), CreateRoute);

                return route.Invoke(instance, obj);
            }

            private Route CreateRoute(Type typeToRoute)
            {
                const BindingFlags attributes = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var method = _type.GetMethod("HandleAsync", attributes, null, new Type[] { typeToRoute }, null);

                var dynamicMethod = new DynamicMethod("Route", typeof(Task), new[] { typeof(object), typeof(object) }, true);

                var body = dynamicMethod.GetILGenerator();
                body.Emit(OpCodes.Ldarg_0);
                body.Emit(OpCodes.Ldarg_1);
                body.Emit(OpCodes.Call, method);
                body.Emit(OpCodes.Ret);

                return (Route)dynamicMethod.CreateDelegate(typeof(Route));
            }
        }
    }
}
```
