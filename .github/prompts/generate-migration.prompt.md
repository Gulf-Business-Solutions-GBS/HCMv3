# Generate Migration

Generate EF Core migration after verifying the build succeeds.

## Usage

`/generate-migration {MigrationName}` — Migration name (e.g., Added_Product_Entity)

## What It Does

1. **Builds** the solution to verify no compilation errors
2. **Creates** EF Core migration from the EntityFrameworkCore project
3. **Reports** the generated migration files

## Commands

```bash
# Step 1: Build
dotnet build aspnet-core/HCMv3.All.sln

# Step 2: Create migration (from EF project directory)
cd aspnet-core/src/HCMv3.EntityFrameworkCore
dotnet ef migrations add {MigrationName}
```

## Example

```
/generate-migration Added_Product_Entity
/generate-migration Updated_Product_Added_Price_Column
```

## Notes

- Always build before creating a migration
- Migration name should describe the change
- Review generated migration file before applying
- To apply: `dotnet ef database update` from EF project directory
- To remove last: `dotnet ef migrations remove`
