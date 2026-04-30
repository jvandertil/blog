+++
title = "Avoiding BuildServiceProvider() using IConfigureOptions"
date  = 2026-04-30T12:00:00+02:00
type  = "post"
tags  = [ "CSharp", ".NET", "ASP.NET" ]
+++

I ran into a frustrating situation recently: I needed to configure ASP.NET Core Data Protection to persist keys using a client that was registered in the DI container by a third-party package. The extension method provided by the Data Protection library only accepts a concrete instance — not a factory that gets the instance from the container. The dependency is registered in DI, so I can't get a hold of it until the container is built.

This is a surprisingly common problem. Libraries ship extension methods that accept concrete instances because the typical caller just `new`s up a client or uses a connection string. Once you're relying on DI for that dependency though, you're stuck.

In this post I'll walk through the workaround I landed on, and explain why the obvious first attempt is wrong.

## The problem

The setup I wanted looked roughly like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyStorageClient("my-storage");
builder.Services.AddDataProtection()
    .PersistKeysToMyStorage(/* need IMyStorageClient here */, "DataProtection-Keys");
```

`IMyStorageClient` is registered by `AddMyStorageClient`, but the container hasn't been built yet at this point. There's no way to get an instance of it to hand off to `PersistKeysToMyStorage`.

## The obvious workaround (and why it's wrong)

The first thing that comes to mind is building the service provider early just to resolve the dependency:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyStorageClient("my-storage");

// DON'T DO THIS
var sp = builder.Services.BuildServiceProvider();
var client = sp.GetRequiredService<IMyStorageClient>();

builder.Services.AddDataProtection()
    .PersistKeysToMyStorage(client, "DataProtection-Keys");
```

This compiles, it might even pass your tests, and then it quietly breaks things in production.

Calling `BuildServiceProvider()` creates a second, fully independent DI container. The app now has two: the temporary one you built mid-configuration, and the real one the framework builds when `builder.Build()` is called. Services registered as singletons are constructed once per container, so any singleton resolved through the temporary container is a distinct object from the one the rest of the application uses.

For a storage client, this means two separate clients are created — and depending on the implementation, two separate connection pools, two authentication flows, or two background threads. The first client, the one you passed to `PersistKeysToMyStorage`, is essentially orphaned. It will never be properly disposed because the temporary container is never disposed.

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
internal sealed class DataProtectionStorageSetup : IConfigureOptions<KeyManagementOptions>
{
    private readonly IMyStorageClient _client;

    public DataProtectionStorageSetup(IMyStorageClient client)
    {
        _client = client;
    }

    public void Configure(KeyManagementOptions options)
    {
        options.XmlRepository = new MyStorageXmlRepository(_client, "DataProtection-Keys");
    }
}
```

`MyStorageXmlRepository` is a straightforward `IXmlRepository` implementation that wraps the storage client. The important thing is that `IMyStorageClient` comes from the constructor — no manual resolution, no temporary containers.

Registration is simple:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyStorageClient("my-storage");
builder.Services.AddDataProtection();
builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>, DataProtectionStorageSetup>();
```

When the Data Protection system first needs to read or write keys, it resolves `IOptions<KeyManagementOptions>`. That triggers the options configuration pipeline, which calls `DataProtectionStorageSetup.Configure` with `IMyStorageClient` resolved from the real, complete container. One container, one client instance, no leaks.

{{< notice >}}
If you need to configure named options, use `IConfigureNamedOptions<T>` instead. It has the same idea but also receives the option name.
{{< /notice >}}

## Why not just fix the library?

The ideal fix is for the library to provide an overload that accepts a `Func<IServiceProvider, TDependency>` factory. Something like:

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToMyStorage(sp => sp.GetRequiredService<IMyStorageClient>(), "DataProtection-Keys");
```

Some libraries have added these overloads over time. When the overload is missing and upgrading the library isn't an option, `IConfigureOptions<T>` is the cleanest workaround available — and frankly, it's a useful pattern to know regardless.

I hope this saves someone a debugging session.
