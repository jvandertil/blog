+++
author = "Jos van der Til"
title = "Faster dynamic object creation with IL Emit"
date  = 2025-07-26T21:00:00+02:00
type = "post"
tags = [ ".NET", "C#" ]
draft = true
+++

Dynamic object creation comes up often in .NET infrastructure—think serializers, frameworks, and code that needs to work with types discovered at runtime. 
The flexible approach is to use reflection, but that comes at a performance cost, especially on hot paths.

In this post, I’ll show how you can use IL Emit to speed up dynamic creation when your types share a predictable constructor signature.

{{< notice >}}
This is an advanced performance technique. 
It's rarely needed in typical application code; consider it only if object creation is a proven bottleneck.
{{< /notice >}}

## Background

I originally implemented this technique around 2020-2022, before C# introduced static abstract members in interfaces. At that time, using IL Emit was one of the best ways to achieve fast dynamic object creation without incurring the overhead of reflection—provided your types shared a common constructor signature. If you're maintaining or reviewing older infrastructure code, you may still encounter this pattern.

These days, .NET has evolved and there are newer, more idiomatic approaches available (see the Modern Alternative below). Still, understanding this technique remains useful for legacy code, or in scenarios where you don't control all the types involved.

## Typical Scenario

Suppose you have classes like this:
```cs
public record SomeId
{
    private readonly Guid _value;

    public SomeId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException();
        }

        _value = value;
    }

    public Guid ToGuid() 
        => _value;
}
```

You want to create instances dynamically, knowing only the type and a `Guid` at runtime.

The easiest and most straightforward way is to use the `Activator` class like this:
```cs
var id = (SomeId)Activator.CreateInstance(typeof(SomeId), _value)
```

This works for any constructor signature, but incurs overhead from reflection, boxing, and type checking.
But if we know that all our types have a common constructor, for example: `Type(Guid id)`, then we can optimize for this.

## Using IL Emit for Fast Object Creation

IL Emit lets you generate and compile methods at runtime, producing delegates that instantiate your objects almost as fast as direct constructor calls. 
This is much faster than reflection once the delegate is compiled.

The idea is simple: when you have many types that follow the same constructor signature, you use IL Emit to create a delegate for instantiating them, avoiding the overhead of reflection on every object creation.

{{< notice >}}
The first call to compile the delegate is expensive. Only use this approach when you'll create many instances over time, so the initial cost is amortized.
{{< /notice >}}

Here’s a thread-safe factory using IL Emit:

```cs
public class IdActivator
{
    private delegate object IdFactory(Guid value);

    private static readonly Type[] ConstructorParameters = [typeof(Guid)];
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
        ConstructorInfo? constructor = type.GetConstructor(ConstructorParameters);
        if (constructor is null)
        {
            throw new InvalidOperationException($"{type.Name} does not have a public constructor that accepts a single Guid parameter.");
        }

        // This block generates IL that corresponds roughly to:
        // TId CreateInstance(Guid value) => new TId(value);
        var dynamicMethod = new DynamicMethod("CreateInstance", type, ConstructorParameters, true);
        ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Newobj, constructor);
        ilGenerator.Emit(OpCodes.Ret);

        // Compile and return the delegate
        return (IdFactory)dynamicMethod.CreateDelegate(typeof(IdFactory));
    }
}
```

Some notes about this code:
- Only works for types with a public constructor accepting a single `Guid`.
- Delegates are cached for performance.

## Benchmarking the Approaches

Let’s use [BenchmarkDotNet](https://benchmarkdotnet.org/) to compare three approaches:

- Direct construction (the baseline)
- `Activator.CreateInstance`
- IL Emit-based factory (IdActivator)

```cs
[MemoryDiagnoser]
[SimpleJob()]
[SimpleJob(RunStrategy.ColdStart, iterationCount: 1, launchCount: 5)]
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

### Results

Here's the results on my machine:

```
BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4652/24H2/2024Update/HudsonValley)
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-YWWROQ : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

```

**Normal (steady-state) results:**

| Method                  | RunStrategy | Mean             | Error           | StdDev         | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |------------ |-----------------:|----------------:|---------------:|------:|--------:|-------:|----------:|------------:|
| DirectConstructor       | Default     |         3.156 ns |       0.0581 ns |      0.0454 ns |  1.00 |    0.02 | 0.0019 |      32 B |        1.00 |
| ActivatorCreateInstance | Default     |        95.615 ns |       1.5014 ns |      1.3309 ns | 30.30 |    0.58 | 0.0191 |     320 B |       10.00 |
| IdActivatorCreate       | Default     |        12.207 ns |       0.2717 ns |      0.4230 ns |  3.87 |    0.14 | 0.0019 |      32 B |        1.00 |

**Cold-start results (includes delegate compilation):**

| Method                  | RunStrategy | Mean         | Error      | StdDev    | Ratio | RatioSD |
|------------------------ |------------ |-------------:|-----------:|----------:|------:|--------:|
| DirectConstructor       | ColdStart   |   151.340 μs |  10.569 μs |  2.745 μs |  1.00 |    0.02 |
| ActivatorCreateInstance | ColdStart   |   262.600 μs |  35.240 μs |  9.152 μs |  1.74 |    0.06 |
| IdActivatorCreate       | ColdStart   | 1,333.800 μs | 148.963 μs | 38.685 μs |  8.82 |    0.27 |

You can see in the results that:
- Direct construction is always fastest.
- Activator is flexible but slow (and allocates much more).
- IL Emit is almost as fast as direct construction after the first call, with allocations identical to direct construction.
- The cold-start penalty for IL Emit is significant (due to delegate compilation), but only paid once.
- IL Emit allocations per instance are identical to direct construction.

## Modern Alternative: static abstract Factory Method

Since the time I first wrote this draft (in 2022), C# and .NET have evolved. If you control your types and are on .NET 7 or newer, you can now use a `static abstract` factory method on a common interface. This has several advantages: it's compile-time safe, AOT-friendly, and doesn't require IL Emit or reflection.

```csharp
public interface IIdFactory<T>
{
    static abstract T Create(Guid value);
}

public record SomeId(Guid Value) : IIdFactory<SomeId>
{
    public static SomeId Create(Guid value) => new(value);
}

// Usage:
public static T CreateId<T>(Guid value) where T : IIdFactory<T>
    => T.Create(value);
```

If you can use this pattern, it's the cleanest and fastest option for this problem.

## When Should You Use IL Emit?

- You need to create many instances dynamically, and performance is critical.
- All types share a predictable constructor.
- You can't use static abstract factories (older .NET, or you don't control the types).

If you need to support more complex signatures, or your code must run under AOT (where IL Emit isn't supported), consider using static interface members, source generators, or build-time code generation.

## Summary

IL Emit was a powerful solution for fast dynamic object creation when your types fit the pattern, especially before C# gained static abstract interface members. 
These days, the static abstract factory pattern is preferred where possible. 
Still, understanding IL Emit remains valuable for maintaining and reasoning about legacy code, or for specialized cases where modern language features aren't available.

If you've identified dynamic creation as a bottleneck and your scenario matches, IL Emit is a tool worth knowing about—even if it's less often needed today.