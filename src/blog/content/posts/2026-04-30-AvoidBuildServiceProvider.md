+++
author = "Jos van der Til"
title  = "Avoiding BuildServiceProvider() with IConfigureOptions"
date   = 2026-09-04T20:30:00+02:00
type   = "post"
tags   = [ "CSharp", ".NET", "ASP.NET" ]
+++

I wanted to configure ASP.NET Core Data Protection to persist keys to Redis using the `Microsoft.AspNetCore.DataProtection.StackExchangeRedis` package.
Using Aspire, it was easy to set up a Redis container in development, and the hosting package makes it simple to add a Redis client with logging, metrics, and resilience.
The problem was that `PersistKeysToStackExchangeRedis` only accepts a concrete `IConnectionMultiplexer` instance. There is no overload that takes a factory or resolves from the container, so the setup looked like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddRedisClient("redis");  // registers IConnectionMultiplexer

builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(???, "DataProtection-Keys");
```

The `IConnectionMultiplexer` is registered, but the container has not been built yet, so there is nothing to hand off to `PersistKeysToStackExchangeRedis`.
That is unfortunate, because it means we cannot take advantage of the Aspire setup without duplicating it ourselves.
I was not the only one to run into this. There is a tracked issue ([dotnet/aspnetcore#61768](https://github.com/dotnet/aspnetcore/issues/61768)), and it looks like it may only land in .NET 12 or later.

## The quick fix

The first thing I have seen people do is build the service provider early just to resolve the dependency:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddRedisClient("redis");

// DON'T DO THIS
var sp = builder.Services.BuildServiceProvider();
var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();

builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(multiplexer, "DataProtection-Keys");
```

This will work, but it is far from ideal.

Calling `BuildServiceProvider()` creates a second, fully independent DI container. The app now has two: the temporary one built during configuration, and the real one the framework builds when `builder.Build()` is called. Services registered as singletons are constructed once per container, so any singleton resolved through the temporary container is a different object from the one the rest of the application uses.

For a Redis multiplexer, that means two separate connections are opened: two sets of sockets, two background reconnect threads, and potentially two different authentication flows. It also means an extra cleanup step at the end of the application lifetime.

This is obviously wasteful. Luckily, the ASP.NET Core team already thought about this problem and introduced a couple of useful primitives for the configuration system.

## Using IConfigureOptions\<T\>

ASP.NET Core's options infrastructure has a clean mechanism for deferred configuration. The `IConfigureOptions<T>` interface has a single method:

```csharp
public interface IConfigureOptions<in TOptions>
{
    void Configure(TOptions options);
}
```

Implementations are registered as services in DI like anything else, which means they can declare dependencies in their constructor.
The framework resolves them from the real service provider the first time the options are needed.

That makes it a good fit for Data Protection configuration:

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
        options.XmlRepository = new RedisXmlRepository(
            _multiplexer.GetDatabase(),
            "DataProtection-Keys");
    }
}
```

Registration is straightforward:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisClient("redis");
builder.Services.AddDataProtection();
builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>, RedisDataProtectionSetup>();
```

When Data Protection first needs to read or write keys, it resolves `IOptions<KeyManagementOptions>`. That triggers the options configuration pipeline, which calls `RedisDataProtectionSetup.Configure` with `IConnectionMultiplexer` resolved from the real, complete container.

### Short version
Requiring a service from the DI container in a configuration is a common problem, so there is also a short way to do it through `ConfigureOptions<T>`:

```cs
builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
{
    return new ConfigureOptions<KeyManagementOptions>(options =>
    {
        options.XmlRepository = new RedisXmlRepository(() => services.GetRequiredService<IConnectionMultiplexer>().GetDatabase(), "DataProtection-Keys");
    });
});
```

{{< notice >}}
If you need to configure named options, use `IConfigureNamedOptions<T>` instead. It has the same idea, but it also receives the option name.
{{< /notice >}}

## Conclusion
The ideal fix is for `PersistKeysToStackExchangeRedis` to accept a factory delegate:

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(
        sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase(),
        "DataProtection-Keys");
```

That API is at least another year out, so this is the workaround for now.

My favorite approach is the shorthand inline configuration. It avoids creating extra classes for a simple dependency lookup from the container.
If the configuration gets more complex, or the registration code becomes harder to follow, splitting it out into a dedicated class might make sense.
Just keep an eye on locality and discoverability if you do that.
