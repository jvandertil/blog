+++
author = "Jos van der Til"
title = "Reducing GuidCombGenerator allocations"
date  = 2019-11-01T19:00:00+01:00
type = "post"
tags = [ "Span", "Performance", "NHibernate" ]
+++

Recently at work I had to implement some functionality that required the use of `Guid` identifiers that were stored in SQL Server.
The `Guid`s were generated in the application and used as an alternative key / external identifier for other systems.
To avoid excessive index fragmentation, we opted to use the GuidComb variant using a generator from the NHibernate project.

The `GuidCombGenerator` generates `Guid` values that have a timestamp embedded into the last 6 bytes. For example:
```plaintext
6c5fd568-c982-4bc3-8c06-aaf8013db1a1
10c6a438-2afa-4bf7-90ce-aaf8013db1c1
82c9da6c-fdd0-4fc8-970a-aaf8013db1df
```

These `Guid` values are optimized for SQL Server, using this same implementation with other database servers is not guaranteed to work.
But for SQL Server this method significantly reduces index fragmentation when you index the `Guid` values.

{{% notice %}}
The generator code in this blog post is derived from the [NHibernate source code](https://github.com/nhibernate/nhibernate-core/blob/ac39173567b31bcfad475ca32687c9faf0d37f87/src/NHibernate/Id/GuidCombGenerator.cs) and is LGPL licensed.
{{% /notice %}}

For fun I took a look at the code to see if there were some optimizations I could make, and there were a couple fun things that I could do.

The original version of the code looks like this.
I added the parameters to make it a bit easier to verify that the optimizations are not changing the generated `Guid` later on.
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

To know if the changes are helping or hurting, I needed some benchmarks of the code.
Using the excellent [BenchmarkDotNet](https://benchmarkdotnet.org/) library you can create these very quickly and easily, avoiding common pitfalls.

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
The generated `Guid` value is returned, and the `[ReturnValueValidator]` will check that all benchmark methods return the same value.
If any benchmark returns a different value the benchmarks will fail (`failOnError = true`).
Since the value of the `Guid` is partially random, and partially a timestamp, you can see why adding the inputs as parameters is required.
The memory that is allocated is also interesting, so I added the `[MemoryDiagnoser]` as well.

For completeness, I ran the benchmarks in the following environment. So your result may differ depending on runtime, CPU, and other factors.
```ini
BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), 64bit RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), 64bit RyuJIT
```

The baseline of the generator is shown below, and looking at the code some improvements are possible.
{{% table %}}
|                  Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|            GenerateComb | 43.98 ns | 0.1643 ns | 0.1536 ns |  1.00 | 0.0249 |     - |     - |     104 B |
{{% /table %}}

You can see that the `days` and `msecs` variables are converted into `byte[]`, the contents are reversed and then copied into the `guidArray`.
Also only 6 bytes are copied and the positions where these values are placed are constant.
Instead of rearranging the bytes physically in memory using `Array.Reverse` and copying them into place using `Array.Copy`, you can do both steps simultaneously manually. 

Note that both the original code, as well as the optimized code assume a little-endian system. 
It is a safe bet in modern environments, but be aware that this assumption is there. 

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

```cs
[Benchmark]
public Guid GenerateCombOptimized()
{
    return GenerateCombOptimized(_guid, _now);
}
```

This simply change results in a significant improvement of the performance of this method.
I also think the new version is slightly more understandable as well. So I'd consider this a win-win.

{{% table %}}
|                  Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|            GenerateComb | 43.98 ns | 0.1643 ns | 0.1536 ns |  1.00 | 0.0249 |     - |     - |     104 B |
|   GenerateCombOptimized | 23.63 ns | 0.1767 ns | 0.1653 ns |  0.54 | 0.0249 |     - |     - |     104 B |
{{% /table %}}

The next step is to reduce the number of allocations needed to generate a single `Guid` using the new `Span` and `MemoryMarshal` types.

In unsafe code there is a trick to allocate memory on the stack, which is not managed by the garbage collector (GC), using the `stackalloc` keyword.
While stack space is limited, it is useful for small arrays (say < 256 bytes) since you avoid the need for the garbage collector (GC) to clean up the memory once you are done.
Stack based memory is cleaned up automatically once you return from the method in which you allocated it.

Since C# 7.2 you can use `stackalloc` in managed code aswell, with the limitation that you have to allocate it into a `Span<T>`.
I will use this technique to avoid allocating the scratch buffers that hold `msecs` and `days`.

Since I will be using the `MemoryMarshal` class to write the values (using `ref`) into the allocated `Span<byte>` instances, I need to move the fields I used from the `TimeSpan`s into local variables.

```cs
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

```cs
[Benchmark]
public Guid GenerateCombSpanScratch()
{
    return GenerateCombSpanScratch(_guid, _now);
}
```

This reduces the amount of allocations managed by the GC by 60% while slightly improving the runtime performance as well.
If you are wondering why the total memory usage is reduced by 64 bytes, while the arrays are only allocating 12 bytes in total (`int` is 4 bytes, and `long` is 8 bytes),
it is because an array is an object, and objects have some overhead associated with them. 

Each array has an object header, a method table pointer, and the length variable. These are all the size of a pointer.
Since the benchmarks are run in a x64 environment, the pointer size is 8 bytes. 
Thus each array carries an overhead of 3 &times; 8 = 24 bytes. This totals up to 60 bytes (24 + 8 + 24 + 4).

This leaves 4 bytes unaccounted for. This is because the .NET runtime tries to align memory when allocating, so at a minimum it will allocate an array no smaller than the native pointer size (4 bytes on 32 bit, and 8 bytes on 64 bit).

{{% table %}}
|                  Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|            GenerateComb | 43.98 ns | 0.1643 ns | 0.1536 ns |  1.00 | 0.0249 |     - |     - |     104 B |
|   GenerateCombOptimized | 23.63 ns | 0.1767 ns | 0.1653 ns |  0.54 | 0.0249 |     - |     - |     104 B |
| GenerateCombSpanScratch | 16.87 ns | 0.0834 ns | 0.0780 ns |  0.38 | 0.0095 |     - |     - |      40 B |
{{% /table %}}

To remove the final allocations I will use the new API's introduced in .NET Core 3.0 (and .NET Standard 2.1) for the `Guid` type.
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

```cs
[Benchmark]
public Guid GenerateCombSpan()
{
    return GenerateCombSpan(_guid, _now);
}
```

These changes completely eliminate the need for heap allocations while generating a new `Guid`.
The mean runtime has regressed slightly, but since the GC is no longer doing any work the method runs a lot for consistently (StdDev is improved significantly).

{{% table %}}
|                  Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|            GenerateComb | 43.98 ns | 0.1643 ns | 0.1536 ns |  1.00 | 0.0249 |     - |     - |     104 B |
|   GenerateCombOptimized | 23.63 ns | 0.1767 ns | 0.1653 ns |  0.54 | 0.0249 |     - |     - |     104 B |
| GenerateCombSpanScratch | 16.87 ns | 0.0834 ns | 0.0780 ns |  0.38 | 0.0095 |     - |     - |      40 B |
|        GenerateCombSpan | 18.12 ns | 0.0052 ns | 0.0049 ns |  0.41 |      - |     - |     - |         - |
{{% /table %}}

All in all, I am quite satisfied with the results. 
Even though the generator was quite fast already (44 nanoseconds / invocation), the most important part is that I could reduce the amount of memory allocated per call.
As you saw, `Span<T>` makes it a lot easier to manipulate memory in a safe way. Before you would have to introduce `unsafe` code, and you probably wouldn't be able to eliminate all of them.

If you are deploying code that is invoked millions of times a day, these small savings can quickly add up.
