+++
author = "Jos van der Til"
title = "Reducing GuidCombGenerator allocations"
date  = 2019-10-28T15:00:00+01:00
type = "post"
tags = [ "Span", "Performance", "NHibernate" ]
draft = true
+++

Recently at work I had to implement some functionality that required the use of `Guid` identifiers that were stored in SQL Server.
The `Guid`s were generated in the application and used as an alternative key / external identifier for other systems.
To avoid excessive index fragmentation, we opted to use the Guid Comb pattern using a generator from the NHibernate project.

{{% notice %}}
The generator code in this blog post is derived from the [NHibernate source code](https://github.com/nhibernate/nhibernate-core/blob/ac39173567b31bcfad475ca32687c9faf0d37f87/src/NHibernate/Id/GuidCombGenerator.cs) and is LGPL licensed.
{{% /notice %}}

For fun I took a look at the code to see if there were some optimizations I could make, and there were a couple fun things that I could do.

The original version of the code looks like this.
I added the inputs as parameters to make it a bit easier to verify that the optimizations are not changing the generated comb GUID later on.
```cs
private static readonly long BaseDateTicks = new DateTime(1900, 1, 1).Ticks;

private static Guid GenerateComb(Guid guid, DateTime now)
{
    byte[] guidArray = guid.ToByteArray();

    // Get the days and milliseconds which will be used to build the byte string
    TimeSpan days = new TimeSpan(now.Ticks - BaseDateTicks);
    TimeSpan msecs = now.TimeOfDay;

    // Convert to a byte array
    // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
    byte[] daysArray = BitConverter.GetBytes(days.Days);
    byte[] msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));

    // Reverse the bytes to match SQL Servers ordering
    Array.Reverse(daysArray);
    Array.Reverse(msecsArray);

    // Copy the bytes into the guid
    Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
    Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

    return new Guid(guidArray);
}
```

To know if the changes are helping or hurting, benchmarks of the code are required.
Using the excellent [BenchmarkDotNet](https://benchmarkdotnet.org/) library we can be quite certain that we are measuring correctly.
The initial benchmark code looks like this.
```cs
[MemoryDiagnoser,
 ReturnValueValidator(failOnError: true)]
public class Benchmarks
{
    private Guid _guid;
    private DateTime _now;

    [GlobalSetup]
    public void Setup()
    {
        _guid = Guid.NewGuid();
        _now = DateTime.UtcNow;
    }

    [Benchmark(Baseline = true)]
    public Guid GenerateComb()
    {
        return GenerateComb(_guid, _now);
    }
}
```
Note that we are returning the generated `Guid`, and that we added a `[ReturnValueValidator]` to the benchmarks class.
When our optimized version is run the return values will be compared, and if they do not match then the benchmarks will fail (`failOnError = true`).
The memory that is allocated is also interesting, which is measured using the `[MemoryDiagnoser]`.

For completeness, I ran the benchmarks in the following environment
```shell
BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), 64bit RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), 64bit RyuJIT
```

Which results in the following baseline:
{{% table %}}
|       Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
| GenerateComb | 43.91 ns | 0.2358 ns | 0.2206 ns |  1.00 | 0.0249 |     - |     - |     104 B |
{{% /table %}}

Looking at the code we can simplify the writing of the results by merging the writing and reversing of the `msecs` and `days` arrays.
```cs
private static Guid GenerateCombOptimized(Guid guid, DateTime now)
{
    byte[] guidArray = guid.ToByteArray();

    // Get the days and milliseconds which will be used to build the byte string
    TimeSpan days = new TimeSpan(now.Ticks - BaseDateTicks);
    TimeSpan msecs = now.TimeOfDay;

    // Convert to a byte array
    // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
    byte[] daysArray = BitConverter.GetBytes(days.Days);
    byte[] msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));

    // Reverse the bytes to match SQL Servers ordering
    // Copy the bytes into the guid
    guidArray[15] = msecsArray[0];
    guidArray[14] = msecsArray[1];
    guidArray[13] = msecsArray[2];
    guidArray[12] = msecsArray[3];
    guidArray[11] = daysArray[0];
    guidArray[10] = daysArray[1];

    return new Guid(guidArray);
}
```

The benchmark is really simple:
```cs
[Benchmark]
public Guid GenerateCombOptimized()
{
    return GenerateCombOptimized(_guid, _now);
}
```

These small changes have a massive impact, reducing the runtime of the method by 50%.
{{% table %}}
|                Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|          GenerateComb | 81.91 ns | 1.0139 ns | 0.9484 ns |  1.00 | 0.0331 |     - |     - |     104 B |
| GenerateCombOptimized | 40.83 ns | 0.5435 ns | 0.5084 ns |  0.50 | 0.0331 |     - |     - |     104 B |
{{% /table %}}

The next step is to reduce the number of allocations we need to generate a single GUID.

In unsafe code you can use the `stackalloc` keyword to allocate memory on the stack, avoiding the need for the garbage collector to manage that memory.
However, with `Span<T>` you can use `stackalloc` in managed code to allocate it into a `Span` directly!
We can use this technique to avoid allocating the scratch buffers.
Since we will be using the `MemoryMarshal` class to write the values (using `ref`) into the allocated `Span<byte>` instances, we will move the fields from the `TimeSpan`s into locals.

```
private static Guid GenerateCombSpanScratch(Guid guid, DateTime now)
{
    byte[] guidArray = guid.ToByteArray();

    // Get the days and milliseconds which will be used to build the byte string
    int days = new TimeSpan(now.Ticks - BaseDateTicks).Days;
    double msecs = now.TimeOfDay.TotalMilliseconds;

    // Convert to a byte array
    Span<byte> daysArray = stackalloc byte[4];
    MemoryMarshal.Write(daysArray, ref days);

    // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
    long msecsSql = (long)(msecs / 3.333333);

    Span<byte> msecsArray = stackalloc byte[8];
    MemoryMarshal.Write(msecsArray, ref msecsSql);

    // Reverse the bytes to match SQL Servers ordering
    // Copy the bytes into the guid
    guidArray[15] = msecsArray[0];
    guidArray[14] = msecsArray[1];
    guidArray[13] = msecsArray[2];
    guidArray[12] = msecsArray[3];
    guidArray[11] = daysArray[0];
    guidArray[10] = daysArray[1];

    return new Guid(guidArray);
}
```

And a new benchmark
```cs
[Benchmark]
public Guid GenerateCombSpanScratch()
{
    return GenerateCombSpanScratch(_guid, _now);
}
```

This reduces the amount of allocations and improves our runtime again!
{{% table %}}
|                  Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|            GenerateComb | 84.73 ns | 0.8703 ns | 0.7715 ns |  1.00 | 0.0331 |     - |     - |     104 B |
|   GenerateCombOptimized | 40.87 ns | 0.3838 ns | 0.3205 ns |  0.48 | 0.0331 |     - |     - |     104 B |
| GenerateCombSpanScratch | 31.02 ns | 0.4770 ns | 0.4229 ns |  0.37 | 0.0127 |     - |     - |      40 B |
{{% /table %}}

To remove the final allocations we will use the new API's introduced in .NET Core 3.0 (and .NET Standard 2.1) for the `Guid` type.
The constructor `Guid(ReadOnlySpan<byte>)` and `TryWriteBytes(Span<byte>)`, which looks like this.
```cs
private static Guid GenerateCombSpan(Guid guid, DateTime now)
{
    Span<byte> guidArray = stackalloc byte[16];
    guid.TryWriteBytes(guidArray);

    // Get the days and milliseconds which will be used to build the byte string
    int days = new TimeSpan(now.Ticks - BaseDateTicks).Days;
    double msecs = now.TimeOfDay.TotalMilliseconds;

    // Convert to a byte array
    Span<byte> daysArray = stackalloc byte[4];
    MemoryMarshal.Write(daysArray, ref days);

    // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
    long msecsSql = (long)(msecs / 3.333333);

    Span<byte> msecsArray = stackalloc byte[8];
    MemoryMarshal.Write(msecsArray, ref msecsSql);

    // Reverse the bytes to match SQL Servers ordering
    // Copy the bytes into the guid
    guidArray[15] = msecsArray[0];
    guidArray[14] = msecsArray[1];
    guidArray[13] = msecsArray[2];
    guidArray[12] = msecsArray[3];
    guidArray[11] = daysArray[0];
    guidArray[10] = daysArray[1];

    return new Guid(guidArray);
}
```

And for completeness the benchmark:
```cs
[Benchmark]
public Guid GenerateCombSpan()
{
    return GenerateCombSpan(_guid, _now);
}
```