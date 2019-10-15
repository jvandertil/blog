+++
author = "Jos van der Til"
title = "Introducing FdbServer"
date  = 2018-09-12T21:00:00+02:00
type = "post"
tags = [ "FoundationDB", "FdbServer" ]
+++

As a side project I've been working on my own implementation of a FoundationDB client for .NET.
Obviously I want to have tests to verify things are working properly, and for local development that isn't such a problem since I usually have a FoundationDB server installed.
However, when I want to run these tests on a build server I do not want to enforce installing FoundationDB in the build environment, things should just work.
Also when people checkout the repository and want to run the tests, they shouldn't strictly need to have FoundationDB installed.

That is why I created the [FdbServer](https://www.github.com/jvandertil/FdbServer) library. 

<!--more-->

## What does it do?
This library will download the FoundationDB server executables and mimic the installation procedure.
It will provide you with a cluster file which you use to connect to the temporary database.
After running all the tests you can call the Destroy method to clean up the FoundationDB instance.

At the time of writing you can't specify where it will install it, nor can you continue a session that you left previously.
For my use case I don't need this anyway.

## How does it work?

The library will download the FoundationDB executable from GitHub (which I had to repackage unfortunately), and place these in a temporary folder.
It will then create a cluster file, and some additional folders for data and logging.
The last step is starting the database and initializing it so that it will accept writes.

Unfortunately the executables are not distributed by Apple as a ZIP file or other easy extractable format but only as an installer.
Thus I have to repackage the executables I need into a ZIP file and host these somewhere.
To ensure atleast some protection the SHA512 hashes are hardcoded into the library, but it still requires trusting me and my machine.

## How do I use it?

You should create an instance of the database using the FdbServerBuilder and store this somewhere globally for the tests.
The builder will download and unpack everything so it should not be called multiple times if you want your tests to execute quickly.

If you use XUnit the easiest way would be to create Fixture that holds the instance and pass that to the tests that require it.
For example:
```cs
using System;
using FdbServer;

public sealed class FdbFixture : IAsyncLifetime
{
    private IFdbServer _server;

    public string ClusterFile => _server.ClusterFile;

    public async Task InitializeAsync()
    {
        var builder = new FdbServerBuilder()
            .WithVersion(FdbServerVersion.v5_2_5);

        _server = await builder.BuildAsync();

        _server.Start();
        _server.Initialize();
    }

    public Task DisposeAsync()
    {
        // Terminate the server process
        _server.Stop();

        // Clean up files
        _server.Destroy();

        return Task.CompletedTask;
    }
}
```

Then create a Collection to indicate the dependency on the fixture:
```cs
    using Xunit;

    [CollectionDefinition("IntegrationTest")]
    public class IntegrationTestCollection : ICollectionFixture<FdbFixture>
    {
    }
```

And use it in your integration tests:
```cs
    using Xunit;

    [Collection("IntegrationTest")]
    public class FdbTest
    {
        private readonly FdbFixture _fdb;

        public FdbTest(FdbFixture fdb)
        {
            _fdb = fdb;
        }

        [Fact]
        public void Test()
        {
            // Use the _fdb.ClusterFile to connect to the FoundationDB server.
            Assert.NotNull(_fdb.ClusterFile);
        }
    }
```

And with that you can write integration tests for FoundationDB that should run in most places.
Since the library requires starting extra processes and listening on a loopback port, I am not sure if it will work on cloud CI services such as AppVeyor.

**NOTE: Only Windows is supported currently**

## How do I get it?

The package is available for installation through [NuGet](https://www.nuget.org/packages/FdbServer) and the source is available on [GitHub](https://www.github.com/jvandertil/FdbServer).
