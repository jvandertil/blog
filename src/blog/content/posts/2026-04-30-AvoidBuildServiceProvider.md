+++
title = "Avoiding BuildServiceProvider() using IConfigureOptions"
date  = 2026-04-30T12:00:00+02:00
type  = "post"
tags  = [ "CSharp", ".NET", "ASP.NET" ]
+++

I wanted to configure ASP.NET Core Data Protection to persist keys to Redis using an `IConnectionMultiplexer` that was registered in DI. The `PersistKeysToStackExchangeRedis` extension method only accepts a concrete `IConnectionMultiplexer` instance — there is no overload that takes a factory or resolves from the container. The situation looked like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisClient("redis");  // registers IConnectionMultiplexer

builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(???, "DataProtection-Keys");
```

The `IConnectionMultiplexer` is registered, but the container isn't built yet, so there's nothing to hand off to `PersistKeysToStackExchangeRedis`. This is a tracked issue ([dotnet/aspnetcore#61768](https://github.com/dotnet/aspnetcore/issues/61768)), but there is no fix yet.

In this post I'll walk through the workaround I landed on, and explain why the obvious first attempt is wrong.

## The obvious workaround (and why it's wrong)

The first thing that comes to mind is building the service provider early just to resolve the dependency:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisClient("redis");

// DON'T DO THIS
var sp = builder.Services.BuildServiceProvider();
var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();

builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(multiplexer, "DataProtection-Keys");
```

This compiles, it might even pass your tests, and then it quietly breaks things in production.

Calling `BuildServiceProvider()` creates a second, fully independent DI container. The app now has two: the temporary one you built mid-configuration, and the real one the framework builds when `builder.Build()` is called. Services registered as singletons are constructed once per container, so any singleton resolved through the temporary container is a distinct object from the one the rest of the application uses.

For a Redis multiplexer, this means two separate connections are opened — two sets of sockets, two background reconnect threads, and potentially two separate authentication flows. The multiplexer you passed to `PersistKeysToStackExchangeRedis` is essentially orphaned. It will never be properly disposed because the temporary container is never disposed.

ASP.NET Core actually generates a compiler warning when you call `BuildServiceProvider()` from application code, specifically because this pattern causes these problems.

## The right approach: IConfigureOptions\<T\>

ASP.NET Core's options infrastructure has a clean mechanism for deferred configuration. The `IConfigureOptions<T>` interface has a single method:

```csharp
public interface IConfigureOptions<in TOptions>
{
    void Configure(TOptions options);
}
```

Implementations of this interface are registered as services in DI like anything else, which means they can declare dependencies in their constructor. The framework resolves them from the real service provider the first time the options are needed — not during startup.

For the Data Protection scenario, I created a small setup class:

```csharp
internal sealed class RedisDataProtectionSetup : IConfigureOptions<KeyManagementOptions>
{
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisDataProtectionSetup(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }

    public void Configure(KeyManagementOptions options)
    {
        options.XmlRepository = new StackExchangeRedisXmlRepository(
            _multiplexer.GetDatabase(),
            "DataProtection-Keys");
    }
}
```

`StackExchangeRedisXmlRepository` is the built-in `IXmlRepository` implementation from the `Microsoft.AspNetCore.DataProtection.StackExchangeRedis` package. The important thing is that `IConnectionMultiplexer` comes from the constructor — no manual resolution, no temporary containers.

Registration is simple:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisClient("redis");
builder.Services.AddDataProtection();
builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>, RedisDataProtectionSetup>();
```

When the Data Protection system first needs to read or write keys, it resolves `IOptions<KeyManagementOptions>`. That triggers the options configuration pipeline, which calls `RedisDataProtectionSetup.Configure` with `IConnectionMultiplexer` resolved from the real, complete container. One container, one connection, no leaks.

{{< notice >}}
If you need to configure named options, use `IConfigureNamedOptions<T>` instead. It has the same idea but also receives the option name.
{{< /notice >}}

## Why not just fix the library?

The ideal fix is for `PersistKeysToStackExchangeRedis` to provide an overload that accepts a factory delegate:

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(
        sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase(),
        "DataProtection-Keys");
```

That overload has been proposed ([dotnet/aspnetcore#61768](https://github.com/dotnet/aspnetcore/issues/61768)) and may land in a future release. Until then, `IConfigureOptions<T>` is the cleanest workaround available — and it's a useful pattern to know for any library that's missing a DI-friendly overload.

I hope this saves someone a debugging session.
