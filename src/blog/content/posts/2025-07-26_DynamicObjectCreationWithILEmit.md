+++
author = "Jos van der Til"
title = "Faster dynamic object creation with IL Emit"
date  = 2022-01-20T21:00:00+01:00
type = "post"
tags = [ ".NET", "C#" ]
draft = true
+++

You have a couple of classes that look like this:
```cs
public record SomeId
{
    private const string Prefix = "something/";
    private const string GuidFormat = "D";

    private readonly Guid _value;

    public string Value { get; }

    public SomeId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException();
        }

        Value = $"{Prefix}{value.ToString(GuidFormat)}";
        _value = value;
    }
}
```

and you want to create these dynamically.

The easiest way is to use the `Activator` class like this:
```cs
var id = (SomeId)Activator.CreateInstance(typeof(SomeId), _value)
```

This is obviously the most flexible way of creating instances dynamically, but if we know that all our types have a common constructor, for example: `Type(Guid id)`. Then we can optimize for this and using IL Emit we do significantly faster job.

```cs
public class IdActivator
{
    private static readonly Type[] GuidType = new[] { typeof(Guid) };

    private static readonly ConcurrentDictionary<Type, IdFactory> _cache = new();

    public static TId Create<TId>(Guid value)
    {
        return (TId)_cache.GetOrAdd(typeof(TId), (type) => CreateCtor<TId>())(value);
    }

    private static IdFactory CreateCtor<TId>()
    {
        // This is the type we want to construct.
        var type = typeof(TId);

        // Find the constructor that accepts a single Guid value
        ConstructorInfo constructor = type.GetConstructor(GuidType);

        if (constructor is null)
        {
            throw new InvalidOperationException($"{typeof(TId).Name} does not have a public constructor that accepts a single Guid parameter.");
        }

        // This block generates IL that corresponds roughly to:
        // TId CreateInstance(Guid value) => new TId(value);
        var dynamicMethod = new DynamicMethod("CreateInstance", type, GuidType, true);
        ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Newobj, constructor);
        ilGenerator.Emit(OpCodes.Ret);
        return (IdFactory)dynamicMethod.CreateDelegate(typeof(IdFactory));
    }

    private delegate object IdFactory(Guid value);
}
```

```cs
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
[ReturnValueValidator(failOnError: true)]
public class BenchmarkObjectCreation
{
    private readonly Guid _value = Guid.NewGuid();

    [Benchmark(Baseline = true)]
    public SomeId DirectConstructor()
    {
        return new SomeId(_value);
    }

    [Benchmark]
    public SomeId ActivatorCreateInstance()
    {
        return (SomeId)Activator.CreateInstance(typeof(SomeId), _value);
    }

    [Benchmark]
    public SomeId IdActivatorCreate()
    {
        return IdActivator.Create<SomeId>(_value);
    }
}
```