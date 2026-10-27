---
description: "Scaffold a new domain entity with DbContext registration and EF Core migration"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
argument-hint: "$ENTITY_NAME - The entity name in PascalCase (e.g., Product)"
---

# Generate Entity

## Usage

`/generate:entity $ARGUMENTS`

## What It Does

1. **Creates entity class** in `aspnet-core/src/HCMv3.Core/{Feature}/{EntityName}.cs`
2. **Adds DbSet** to `HCMv3DbContext`
3. **Configures entity** in `OnModelCreating` (if needed)
4. **Creates EF Core migration**
5. **Builds** to verify

## Workflow

1. Parse `$ARGUMENTS` for entity name and options
2. Ask user for:
   - Properties (name:type pairs)
   - Primary key type (default: `int`)
   - Base class (default: `FullAuditedEntity`)
   - Multi-tenancy (default: `IMustHaveTenant`)
3. Apply `aspnetzero-entity-patterns` skill
4. Apply `efcore-patterns` skill
5. Build: `dotnet build aspnet-core/HCMv3.All.sln`

## Options

- Entity name is required (first argument)
- Properties can be specified inline: `Product --properties "Name:string,Price:decimal,IsActive:bool"`
- `--pk guid` or `--pk long` to change primary key type
- `--no-tenant` to skip multi-tenancy interface

## Property Type Reference

| Shorthand | C# Type | Notes |
|-----------|---------|-------|
| `string` | `string` | Add `[StringLength]` on DTO |
| `int` | `int` | |
| `long` | `long` | |
| `decimal` | `decimal` | Add `.HasPrecision(18,2)` in Fluent API |
| `bool` | `bool` | |
| `DateTime` | `DateTime` | |
| `Guid` | `Guid` | |
| `double` | `double` | |

## Example

```
/generate:entity Product --properties "Name:string,Price:decimal,Description:string,IsActive:bool"
```

Creates:
- `Core/Products/Product.cs` — entity with properties
- DbSet in `HCMv3DbContext`
- EF Core migration

## Next Steps After Entity

- `/generate:appservice Product` — create full CRUD service
- `/generate:permission Products` — add permission set
