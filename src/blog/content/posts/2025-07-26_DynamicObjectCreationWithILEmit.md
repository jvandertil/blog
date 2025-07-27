+++
author = "Jos van der Til"
title = "Faster dynamic object creation with IL Emit"
date  = 2025-07-26T21:00:00+02:00
type = "post"
tags = [ ".NET", "C#" ]
draft = true
+++

{{< notice >}}
This is an advanced performance trick. Ensure that you really need this before applying it.
{{< /notice >}}

Assume you have various classes that look like this:
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

and you need to create these dynamically.

The easiest and most straightforward way is to use the `Activator` class like this:
```cs
var id = (SomeId)Activator.CreateInstance(typeof(SomeId), _value)
```

This is obviously the most flexible way of creating instances dynamically. 
But if we know that all our types have a common constructor, for example: `Type(Guid id)`, then we can optimize for this.

## Using IL Emit for Fast Object Creation

The idea is simple: when you have many types that follow the same constructor signature, you use IL Emit to create a delegate for instantiating them, avoiding the overhead of reflection on every object creation.

{{< notice >}}
Ensure that you are calling the compiled delegate often enough that the first-call compilation penalty is acceptable!
{{< /notice >}}

Here’s a factory that uses IL Emit for fast construction:

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

## Benchmarking the Approaches

To see the performance difference, let’s use [BenchmarkDotNet](https://benchmarkdotnet.org/) to compare direct construction, `Activator.CreateInstance`, and our IL Emit approach:

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

When running the benchmark, you’ll see:

- **Direct construction** is always fastest, as it’s just a plain constructor call.
- **Activator.CreateInstance** is much slower because of reflection.
- **IdActivator (IL Emit)** is slower than direct construction, but far faster than `Activator`.

Here's the results on my machine:

```
BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4652/24H2/2024Update/HudsonValley)
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-YWWROQ : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                  | RunStrategy | Mean             | Error           | StdDev         | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |------------ |-----------------:|----------------:|---------------:|------:|--------:|-------:|----------:|------------:|
| DirectConstructor       | Default     |         3.156 ns |       0.0581 ns |      0.0454 ns |  1.00 |    0.02 | 0.0019 |      32 B |        1.00 |
| ActivatorCreateInstance | Default     |        95.615 ns |       1.5014 ns |      1.3309 ns | 30.30 |    0.58 | 0.0191 |     320 B |       10.00 |
| IdActivatorCreate       | Default     |        12.207 ns |       0.2717 ns |      0.4230 ns |  3.87 |    0.14 | 0.0019 |      32 B |        1.00 |

And the cold start results, which show that `Activator` is much faster on the first invocation, where as the compilation of the delegate takes a (relatively) significant amount of time.

| Method                  | RunStrategy | Mean         | Error      | StdDev    | Ratio | RatioSD |
|------------------------ |------------ |-------------:|-----------:|----------:|------:|--------:|
| DirectConstructor       | ColdStart   |   151.340 μs |  10.569 μs |  2.745 μs |  1.00 |    0.02 |
| ActivatorCreateInstance | ColdStart   |   262.600 μs |  35.240 μs |  9.152 μs |  1.74 |    0.06 |
| IdActivatorCreate       | ColdStart   | 1,333.800 μs | 148.963 μs | 38.685 μs |  8.82 |    0.27 |


### Summary

If you need dynamic object creation for types with a predictable constructor, IL Emit is a powerful tool. 
You get nearly the same performance as direct instantiation, while retaining the flexibility to create different types dynamically.

Use this approach for performance-critical scenarios where Activator is too slow, and there’s a common constructor pattern across your types.
