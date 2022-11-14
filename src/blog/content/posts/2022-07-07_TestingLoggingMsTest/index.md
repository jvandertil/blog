+++
author = "Jos van der Til"
title = "Capturing application logging in MsTest"
date  = 2022-07-07T22:00:00+02:00
type = "post"
tags = [ "CSharp", ".NET", "Testing", "Logging" ]
+++

In a lot of projects I have been on I've seen the following approaches when it comes to application logging in test:

1. The most popular option: It is completely ignored, either by pumping it into a mock or a `NullLogger`
2. It is tested by verifying that the correct log messages are written. This is usually done to satisfy a 'strict' mocking framework.

Neither of these options are ideal in my opinion.
The first option totally hides the logging, making it hard to see if it is actually valuable.
The second option adds too much noise into the tests, since verifying that the message is written doesn't tell me much.

I would suggest to insert a logger that hooks into the testing framework so that the logging is available for debugging purposes.
This way, when a test fails unexpectedly, you can use the logging to try and pin down the problem. Is that is hard to do with just the logging you have?
Great! You just found a gap in the logging to find out what the application is doing.

To facilitate that I will show some infrastructure classes I use in my testing to get this done using [MsTest](https://github.com/microsoft/testfx) and the [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/) package.

{{< notice >}}
In MsTest prior to v2.2.9 you could write to the `TestContext` directly and have it work. Since v2.2.9 they use an `AsyncLocal` internally breaking this when you want to capture logging from another async context (such as when using the `WebApplicationFactory` from the [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing)), I filed an issue [here](https://github.com/microsoft/testfx/issues/1083). Kudos to [@gaurav137](https://github.com/gaurav137) for his/her suggestion to use a `BufferBlock` instead of logging to a file! I have adapted my solution to use [System.Threading.Channels](https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/).
{{< /notice >}}

Note that since a `Task` is used to capture the async context of the test you will need to dispose of the logger in your tests to stop the `Task` running in the background.
```cs
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[DebuggerStepThrough]
internal class MsTestLogger : ILogger
{
    private readonly ChannelWriter<string> _logs;
    private readonly string _categoryName;

    public MsTestLogger(ChannelWriter<string> logs, string categoryName)
    {
        _logs = logs;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NoopDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        WriteLog($"{logLevel}: {_categoryName} [{eventId}] {formatter(state, exception)}");

        if (exception is not null)
        {
            WriteLog(exception.ToString());
        }
    }

    private void WriteLog(string message)
    {
        bool writtenWithinTimeout = SpinWait.SpinUntil(() => _logs.TryWrite(message), TimeSpan.FromSeconds(1));

        if (!writtenWithinTimeout)
        {
            // Since we created an unbounded channel we don't expect this to fail, but if it does we want to know.
            throw new TimeoutException("Timed out while writing to log channel.");
        }
    }

    /// <summary>
    /// Creates a new logger for the given test context.
    /// </summary>
    public static DisposableMsTestLogger<T> Create<T>(TestContext context)
    {
        var logs = Channel.CreateUnbounded<string>();

        // Workaround for the AsyncLocal issue. Assumption being invocation of this constructor captures
        // the right execution context that is unaffected by code under test where we cannot assume that
        // execution context gets preserved when Log() gets invoked.
        Task.Run(async () =>
        {
            await foreach (var message in logs.Reader.ReadAllAsync())
            {
                context.WriteLine(message);
            }
        });

        return new DisposableMsTestLogger<T>(logs.Writer);
    }

    private class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new NoopDisposable();

        public void Dispose()
        {
        }
    }
}

[DebuggerStepThrough]
internal class MsTestLogger<T> : MsTestLogger, ILogger<T>
{
    public MsTestLogger(ChannelWriter<string> logs)
        : base(logs, typeof(T).Name)
    {
    }
}

[DebuggerStepThrough]
internal class DisposableMsTestLogger<T> : MsTestLogger<T>, IDisposable
{
    private readonly ChannelWriter<string> _logs;

    public DisposableMsTestLogger(ChannelWriter<string> logs)
        : base(logs)
    {
        _logs = logs;
    }

    public void Dispose()
    {
        _logs.Complete();
    }
}
```

The reason for the `DisposableMsTestLogger` and the `Create<T>` method are that I also have an implementation of an `ILoggerFactory` that can be used when integration testing an application.
Note that this also uses a `Task` to capture the async context of the test and thus should be disposed when the test is completed.
```cs
[DebuggerStepThrough]
internal class MsTestLoggerFactory : ILoggerFactory
{
    private readonly Channel<string> _logs;
    private readonly TestContext _context;

    public MsTestLoggerFactory(TestContext context)
    {
        _context = context;

        _logs = Channel.CreateUnbounded<string>();

        // Workaround for the AsyncLocal issue. Assumption being invocation of this constructor captures
        // the right execution context that is unaffected by code under test where we cannot assume that
        // execution context gets preserved when Log() gets invoked.
        Task.Run(async () =>
        {
            await foreach (var message in _logs.Reader.ReadAllAsync())
            {
                _context.WriteLine(message);
            }
        });
    }

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new MsTestLogger(_logs.Writer, categoryName);
    }

    public void Dispose()
    {
        _logs.Writer.Complete();
    }
}
```

In Visual Studio the output will be shown in the 'Test Details' window, example screenshot below.

{{< figure src="screenshot.png" alt="Partial screenshot of the Test Details window showing captured logging.">}}
