+++
title = "Efficiently Filtering Multi-Column Identifiers with Entity Framework"
date = 2025-07-26
tags = ["entity framework", "sql server", "performance" ]
draft = false
+++

When working with data spanning multiple systems, you’ll often encounter composite keys—records identified by more than one column. Filtering by these keys in Entity Framework can be unwieldy, especially if identifiers overlap across sources.

The typical approach is to build a query with a large number of OR-ed conditions, like:

```sql
WHERE (SourceSystem = 'A' AND ExternalId = 'X')
   OR (SourceSystem = 'B' AND ExternalId = 'Y')
   -- and so on
```

But this isn’t ideal:
- The SQL query grows rapidly for large sets.
- Query plan caching is defeated because every key set generates a new query.
- It’s awkward to generate in code.

## A Better Approach: Using OPENJSON

Here’s how you can use JSON serialization and `OPENJSON` to efficiently filter by composite keys in SQL Server:

```csharp
public class DatabaseKey
{
    public string SourceSystem { get; set; }
    public string ExternalId { get; set; }
}

var keys = new[]
{
    new DatabaseKey { SourceSystem = "CRM", ExternalId = "12345" },
    new DatabaseKey { SourceSystem = "ERP", ExternalId = "54321" }
};

var serializedKeys = JsonSerializer.Serialize(keys);

context.AggregatedData.FromSqlInterpolated($"""
    SELECT [data].*
    FROM [dbo].[AggregatedData] AS [data]
    WHERE EXISTS (
        SELECT 1
          FROM OPENJSON({serializedKeys})
               WITH (
                [SourceSystem] VARCHAR(50),
                [ExternalId] VARCHAR(50)
               ) AS [json]
        WHERE [json].[SourceSystem] = [data].[SourceSystem]
          AND [json].[ExternalId] = [data].[ExternalId]
    )
""");
```

This produces JSON like:

```json
[
  { "SourceSystem": "CRM", "ExternalId": "12345" },
  { "SourceSystem": "ERP", "ExternalId": "54321" }
]
```

You can then use this `serializedKeys` string in your SQL query as described above.

### Why This Works

- The query plan is stable since the structure doesn’t change.
- SQL Server efficiently joins against the parsed key set.
- The C# code is simple—just serialize your keys and pass them in.

## When to Use

If you’re filtering by many multi-column identifiers and want to keep your queries efficient and maintainable, this technique is worth considering.
