---
description: "Generate EF Core migration after verifying the build succeeds"
allowed-tools: Bash, Read
argument-hint: "$MIGRATION_NAME - The migration name (e.g., Added_Product_Entity)"
---

# Generate Migration

## Usage

`/generate:migration $ARGUMENTS`

## What It Does

1. **Builds** the solution to verify no compilation errors
2. **Creates** EF Core migration from the EntityFrameworkCore project
3. **Reports** the generated migration files

## Workflow

```bash
# Step 1: Build
dotnet build aspnet-core/HCMv3.All.sln

# Step 2: Create migration (from EF project directory)
cd aspnet-core/src/HCMv3.EntityFrameworkCore
dotnet ef migrations add $MIGRATION_NAME
```

## Example

```
/generate:migration Added_Product_Entity
/generate:migration Updated_Product_Added_Price_Column
```

## Notes

- Always build before creating a migration
- Migration name should describe the change (e.g., `Added_{Entity}_Entity`)
- Review the generated migration file before applying
- To apply: `dotnet ef database update` from EF project directory
- To remove last migration: `dotnet ef migrations remove`
