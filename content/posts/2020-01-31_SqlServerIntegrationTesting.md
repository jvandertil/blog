+++
author = "Jos van der Til"
title = "SQL Server integration testing"
date  = 2020-01-31T11:00:00+01:00
type = "post"
tags = [ ".NET", "ASP.NET", "CSharp", "SqlServer", "Testing" ]
draft = true
+++

Recently I wanted to verify that my data access layer could properly read and write to a SQL Server database, and I wanted to have these tests automated.
I wanted to answer these questions:
1. Can my `DbContext` roundtrip entities to the database and back?
2. Can my migration scripts be applied to the database correctly?
3. Does the schema in my migration scripts match the expected schema in my code?

Since I was using SQL Server I could utilize SQL Server [LocalDb](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) 
that comes with Visual Studio.
And to keep performance acceptable I do not want to create and destroy a database for each test, so I also need a way to reset the database after a test has run.

Let us first try to create a new database at the start of a test, and destroy it afterwards. For this post I will be using [xUnit](https://github.com/xunit/xunit).

## Creating and destroying a LocalDB database

Lets start with a simple fixture that will create a database, and clean it up when it is disposed.
For the database name I use the application name suffixed with a `Guid` value for uniqueness.
I will expose the database to my tests as an opened `SqlConnection`, but you could expose the connection string just as easy.
While not strictly necessary I used `async` methods to initialize and dispose of the database.

{{< notice >}}
I am using string interpolation `$""` to format the database name into the query. 
Do not use this to put user input in queries!
{{< /notice >}}

```cs
using Xunit;

public class SqlServerFixture : IAsyncLifetime
{
    private const string LocalDbConnectionString = "Data Source=(localdb)\\MSSQLLocalDB; Integrated Security=True;";

    private readonly string _dbName;    
    
    // Expose the opened connection
    public SqlConnection Connection { get; private set; }

    public SqlServerFixture()
    {
        _dbName = "blogpost-" + Guid.NewGuid().ToString();
    }

    public async Task InitializeAsync()
    {
        Connection = new SqlConnection(_connectionString);
        await Connection.OpenAsync();

        // Create a new database and switch to it.
        await ExecuteDbCommandAsync($"CREATE DATABASE [{_dbName}]");
        await ExecuteDbCommandAsync($"USE [{_dbName}]");
    }

    public async Task DisposeAsync()
    {
        // Delete recovery information
        await ExecuteDbCommandAsync($"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'{_dbName}'");
        await ExecuteDbCommandAsync("USE [master]");

        // Ensure that the database is not in use
        await ExecuteDbCommandAsync($"ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
        await ExecuteDbCommandAsync("USE [master]");

        // Drop the database
        await ExecuteDbCommandAsync($"DROP DATABASE [{_dbName}]");

        await Connection.DisposeAsync();
    }

    private async Task ExecuteDbCommandAsync(string commandText)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = commandText;
        await cmd.ExecuteNonQueryAsync();
    }
}
```

When this fixture is injected you can use the provided database connection, for example use a `IClassFixture` to share the database in a test class.
The `IClassFixture` will notify the xUnit runner that we want a new instance of the `SqlServerFixture` class for the lifetime of this test class.

```cs
using Xunit;

public class ConnectionTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    private SqlConnection Connection => _fixture.Connection;

    public ConnectionTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
	}

    [Fact]
    public async Task Can_Connect()
    {
        using var command = Connection.CreateCommand();
        cmd.CommandText = "SELECT 1";

        var result = await cmd.ExecuteScalarAsync();

        Assert.Equal(1, result);
	}
}
```

Now that we can create, connect to, and destroy a database lets get a schema deployed.

## Deploying database schema with DbUp
When deploying database I might be old fashioned, but I greatly prefer migration scripts over a 'desired state' like approach such as DACPAC.
Generally, the 'desired state' approach has either perceived (does your DBA trust automatic database state migration?) or real issues when data has to be migrated, requiring a manual migration script.

In any case, I am using DbUp to run SQL scripts embedded into a console application.
I created the following class to wrap the DbUp logic, and I generally call this from console applications `Main` method.

```cs
using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;

public class SqlServerMigrationRunner
{
    private readonly UpgradeEngine _upgrader;

    public SqlServerMigrationRunner(string connectionString)
    {
        _upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(SqlServerMigrationRunner).Assembly)
            .LogTo(new ConsoleUpgradeLog())
            .Build();
    }

    public DatabaseUpgradeResult PerformUpgrade()
    {
        return _upgrader.PerformUpgrade();
    }
}
```

Let's add a call to this to the `SqlServerFixture`, first adding a reference to the project that hosts the DbUp scripts.

```cs
public class SqlServerFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        Connection = new SqlConnection(_connectionString);
        await Connection.OpenAsync();

        // Create a new database and switch to it.
        await ExecuteDbCommandAsync($"CREATE DATABASE [{_dbName}]");
        await ExecuteDbCommandAsync($"USE [{_dbName}]");

        // Create a new connection string to the database
        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = _dbName
        };

        var runner = new SqlServerMigrationRunner(builder.ToString());
        var result = runner.PerformUpgrade();

        if (!result.Successful)
        {
            throw result.Error;  
		}
    }

    // Rest of the class omitted for brevity.....
}
```