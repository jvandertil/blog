+++
author = "Jos van der Til"
title = "Faster dynamic object creation with IL Emit"
date  = 2025-07-26T21:00:00+02:00
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

## Using IL Emit for Fast Object Creation

The idea is simple: when you have many types that follow the same constructor signature, you can use IL Emit to create a delegate for instantiating them, avoiding the overhead of reflection on every object creation.

Here’s a factory that uses IL Emit for fast construction:

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

## Benchmarking the Approaches

To see the performance difference, let’s use BenchmarkDotNet to compare direct construction, `Activator.CreateInstance`, and our IL Emit approach:

```cs
[MemoryDiagnoser]
[SimpleJob()]
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
- **IdActivator (IL Emit)** is almost as fast as direct construction, and far faster than Activator.

Here’s a typical result (yours may vary based on hardware and runtime):

```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4652/24H2/2024Update/HudsonValley)
AMD Ryzen 9 7950X 4.50GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                  | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| DirectConstructor       |  17.25 ns | 0.260 ns | 0.217 ns |  1.00 |    0.02 | 0.0153 |     256 B |        1.00 |
| ActivatorCreateInstance | 108.07 ns | 0.552 ns | 0.517 ns |  6.26 |    0.08 | 0.0324 |     544 B |        2.12 |
| IdActivatorCreate       |  26.11 ns | 0.221 ns | 0.207 ns |  1.51 |    0.02 | 0.0153 |     256 B |        1.00 |


### Cold Start results
```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4652/24H2/2024Update/HudsonValley)
AMD Ryzen 9 7950X 4.50GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-NBOIBG : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

LaunchCount=50  RunStrategy=ColdStart  

```
| Method                  | Mean      | Error    | StdDev     | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |----------:|---------:|-----------:|----------:|------:|--------:|----------:|------------:|
| DirectConstructor       |  8.950 μs | 3.721 μs |  79.913 μs | 0.9000 μs | 11.55 |  111.39 |     256 B |        1.00 |
| ActivatorCreateInstance | 16.066 μs | 4.123 μs |  88.542 μs | 7.9000 μs | 20.73 |  123.60 |    1472 B |        5.75 |
| IdActivatorCreate       | 20.905 μs | 8.960 μs | 192.424 μs | 1.6000 μs | 26.97 |  268.21 |     256 B |        1.00 |



### Summary

If you need dynamic object creation for types with a predictable constructor, IL Emit is a powerful tool. You get nearly the same performance as direct instantiation, while retaining the flexibility to create different types dynamically.

Use this approach for performance-critical scenarios where Activator is too slow, and there’s a common constructor pattern across your types.

Let me know if you have questions or want to see how this can be extended for other constructor signatures!