# Generate Entity

Scaffold a new domain entity with DbContext registration and EF Core migration.

## Usage

`/generate-entity {EntityName}` — Entity name in PascalCase (e.g., Product)

## What It Does

1. **Creates entity class** in `aspnet-core/src/HCMv3.Core/{Feature}/{EntityName}.cs`
2. **Adds DbSet** to `HCMv3DbContext`
3. **Configures entity** in `OnModelCreating` (table, indexes, precision)
4. **Creates EF Core migration**
5. **Builds** to verify

## Workflow

1. Parse entity name and options
2. Ask for properties (name:type pairs), PK type, base class, multi-tenancy
3. Create entity in Core following `aspnetzero-entity-patterns`
4. Configure in EF Core following `efcore-patterns`
5. Create migration: `dotnet ef migrations add Added_{Entity}_Entity`
6. Build: `dotnet build aspnet-core/HCMv3.All.sln`

## Options

- `--properties "Name:string,Price:decimal,IsActive:bool"` — Define properties inline
- `--pk guid` or `--pk long` — Primary key type (default: int)
- `--no-tenant` — Skip `IMustHaveTenant` interface

## Property Types

| Shorthand | C# Type | Notes |
|-----------|---------|-------|
| `string` | `string` | Add `[StringLength]` on DTO |
| `int` | `int` | |
| `decimal` | `decimal` | `.HasPrecision(18,2)` in Fluent API |
| `bool` | `bool` | |
| `DateTime` | `DateTime` | |
| `Guid` | `Guid` | |

## Example

```
/generate-entity Product --properties "Name:string,Price:decimal,Description:string,IsActive:bool"
```

## Next Steps

- `/generate-appservice Product` — Create full CRUD service
- `/generate-permission Products` — Add permission set
