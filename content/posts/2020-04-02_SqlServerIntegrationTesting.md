+++
author = "Jos van der Til"
title = "SQL Server integration testing"
date  = 2020-04-02T11:00:00+02:00
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
To keep performance acceptable I do not want to create and destroy a database for each test, so I need a way to reset the database after a test has run.

Let us first try to create a new database at the start of a test, and destroy it afterwards. For the tests in this post I will be using [xUnit](https://github.com/xunit/xunit).

## Creating and destroying a LocalDB database

Lets start with a simple fixture that will create a database, and clean it up when it is disposed.
For the database name I use the application name suffixed with a `Guid` value for uniqueness.
While not strictly necessary I used `async` methods to initialize and dispose of the database.

{{< notice >}}
I am using string interpolation (`$""`) to format the database name into the query. 
Do not use this to put user input in queries!
{{< /notice >}}

```cs
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;

public class DatabaseFixture : IAsyncLifetime
{
    private const string BaseConnectionString = "Data Source=(localdb)\MSSQLLocalDB;IntegratedSecurity=True;";

    private readonly string _dbName;

    public string ConnectionString { get; }

    public DatabaseFixture()
    {
        _dbName = "demo-for-blog" + "-" + Guid.NewGuid().ToString();

        var builder = new SqlConnectionStringBuilder(BaseConnectionString)
        {
            InitialCatalog = _dbName
        };

        ConnectionString = builder.ToString();
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqlConnection(BaseConnectionString);
        await connection.OpenAsync();

        await ExecuteDbCommandAsync(connection, $"CREATE DATABASE [{_dbName}]");
        await ExecuteDbCommandAsync(connection, $"USE [{_dbName}]");
    }

    public async Task DisposeAsync()
    {
        using var connection = new SqlConnection(BaseConnectionString);
        await connection.OpenAsync();

        await ExecuteDbCommandAsync(connection, $"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'{_dbName}'");
        await ExecuteDbCommandAsync(connection, "USE [master]");
        await ExecuteDbCommandAsync(connection, $"ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
        await ExecuteDbCommandAsync(connection, "USE [master]");
        await ExecuteDbCommandAsync(connection, $"DROP DATABASE [{_dbName}]");
    }

    private async Task ExecuteDbCommandAsync(SqlConnection connection, string commandText)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        await cmd.ExecuteNonQueryAsync();
    }
}
```

When this fixture is injected you can use it to create a connection to the database.
As an example, you can use an `IClassFixture<T>` to share the database in a test class.

{{< notice >}}
Due to the long time it takes to initialize and destroy databases you are generally better off using a [collection fixture](https://xunit.net/docs/shared-context#collection-fixture) instead of an `IClassFixture<T>`. 
These will share the database fixture across all tests in the collection, but this does limit the amount of parallelization xUnit can do.
Be sure to read the documentation!
{{< /notice >}}

The `ConnectionTests` class below demonstrates the basic usage of the `DatabaseFixture`
```cs
using Xunit;

public class ConnectionTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    private SqlConnection Connection { get; }

    public ConnectionTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        Connection = new SqlConnection(_fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await Connection.OpenAsync();
    }

    public async Task DisposeAsync()
    {
        await Connection.DisposeAsync();
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
When deploying database I greatly prefer migration scripts over a 'desired state' like approach such as DACPAC.
Generally, the 'desired state' approach has either perceived (does your DBA trust automatic database state migration?) or real issues when data has to be migrated, requiring a manual migration script.

In any case, I am using [DbUp](https://dbup.github.io/) to run SQL scripts embedded into a console application.
I created the following class to wrap the DbUp logic, and I generally call this from a console application `Main` method.

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

Let's add a call to this to the initialization logic of the `DatabaseFixture`. 
If you are using another form of database schema migration, adjust as necessary.
Although doing this when using something like DACPAC might give you considerable pain.

```cs
public class DatabaseFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        // Rest of method omitted for brevity.

        InitializeDatabaseSchema();
    }

    private void InitializeDatabaseSchema()
    {
        var runner = new SqlServerMigrationRunner(ConnectionString);
        var result = runner.PerformUpgrade();

        if (!result.Successful)
        {
            throw result.Error;
        }
    }

    // Rest of the class omitted for brevity.....
}
```

Now our database will have our database migration scripts applied after it is created.
I generally try to use the same code here as I do when running the console application that is run during application deployment.
This gives me the greatest confidence that everything will work as expected when I eventually have to deploy the code to production.

## Resetting database state
To ensure that tests do not influence each other you should give each test a clean database as a starting point.
Creating a new empty database for each test will increase the time your test suite needs to run considerably.
Generally I would advise to have all tests run against a single database instance and clean the database after each test.

I used the [Respawn](https://github.com/jbogard/Respawn) library created by [Jimmy Bogard](https://jimmybogard.com/) that intelligently deletes data from the database. 
It follows the relationships defined in your model and deletes from the 'leaf' tables inwards.
The `Checkpoint` class provides methods that you can use to delete all the data from the database.
There are a couple of options when you want to avoid clearing out certain tables or schemas, everything else will be deleted.

If you are dependent on seed data that is in tables that are also modified by tests, then you will have to reseed everytime you reset the database using Respawn.
(I am not sure if I would consider that test design as a 'test smell' since the test depends on things outside of its control.)

I placed the method to reset the database on the fixture as well, as it is convenient to have all the database fixture control in 1 place as a consumer.

```cs
using Respawn;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly Checkpoint _emptyDatabaseCheckpoint;

    public DatabaseFixture()
    {
        // Rest of constructor omitted for brevity.

        _emptyDatabaseCheckpoint = new Checkpoint
        {
            // Reseed identity columns
            WithReseed = true,
            TablesToIgnore = new[]
            {
                // DbUp journal does not need cleaning
                "SchemaVersions"
            },
        };
    }

    public async Task ResetDatabaseAsync()
    {
        await _emptyDatabaseCheckpoint.Reset(ConnectionString);
    }

    // Rest of class omitted for brevity.
}
```

From the test's `InitializeAsync` method you should call the `ResetDatabaseAsync` method on the fixture.
Now you are ready to write your own integration tests.

## Copy and paste example code

Below you will find some code that you can copy and paste in to your project to get up and running quickly.
The `InitializeDatabase` method is left `abstract`, you will need to plug in your own schema migrations there.

```cs
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Respawn;
using Xunit;

public abstract class DatabaseFixture : IAsyncLifetime
{
    private const string BaseConnectionString = "Data Source=(localdb)\\MSSQLLocalDB; Integrated Security=True;";

    private readonly string _dbName;
    private readonly Checkpoint _emptyDatabaseCheckpoint;

    public string ConnectionString { get; }

    public DatabaseFixture(string dbName)
    {
        _dbName = dbName + "-" + Guid.NewGuid().ToString();

        _emptyDatabaseCheckpoint = new Checkpoint
        {
            // Reseed identity columns
            WithReseed = true,
            TablesToIgnore = new[]
            {
                // DbUp journal does not need cleaning
                "SchemaVersions"
            },
        };

        var builder = new SqlConnectionStringBuilder(BaseConnectionString)
        {
            InitialCatalog = _dbName
        };

        ConnectionString = builder.ToString();
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqlConnection(BaseConnectionString);
        await connection.OpenAsync();

        await ExecuteDbCommandAsync(connection, $"CREATE DATABASE [{_dbName}]");
        await ExecuteDbCommandAsync(connection, $"USE [{_dbName}]");

        InitializeDatabase();
    }

    public async Task DisposeAsync()
    {
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await ExecuteDbCommandAsync(connection, $"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'{_dbName}'");
        await ExecuteDbCommandAsync(connection, "USE [master]");
        await ExecuteDbCommandAsync(connection, $"ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
        await ExecuteDbCommandAsync(connection, "USE [master]");
        await ExecuteDbCommandAsync(connection, $"DROP DATABASE [{_dbName}]");
    }

    public async Task ResetDatabaseAsync()
    {
        await _emptyDatabaseCheckpoint.Reset(ConnectionString);
    }

    protected abstract void InitializeDatabase();

    private async Task ExecuteDbCommandAsync(SqlConnection connection, string commandText)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        await cmd.ExecuteNonQueryAsync();
    }
}
```

## Running in Azure DevOps
I am using the code shown here successfully in Azure DevOps on a Microsoft hosted agent using the `windows-latest` image.
You should add the following command to your pipeline before running the tests.

```none
sqllocaldb start MSSQLLocalDB
```

This will ensure that the database engine is started before the tests are run, this is not always the case.
