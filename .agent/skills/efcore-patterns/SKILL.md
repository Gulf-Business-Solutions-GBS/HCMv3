---
name: efcore-patterns
description: "Entity Framework Core patterns for ASP.NET Zero (DbContext, repositories, migrations)"
---

# EF Core Patterns for ASP.NET Zero

## Quick Start

After creating an entity in Core, register it in the **EntityFrameworkCore** project:

### 1. Add DbSet to DbContext

```csharp
// aspnet-core/src/HCMv3.EntityFrameworkCore/EntityFrameworkCore/HCMv3DbContext.cs
// Add property inside the class:

public DbSet<Product> Products { get; set; }
```

### 2. Configure Entity (if needed)

```csharp
// In OnModelCreating method of HCMv3DbContext:

modelBuilder.Entity<Product>(b =>
{
    b.ToTable("Products");
    b.HasIndex(e => e.TenantId);
    b.Property(e => e.Name).HasMaxLength(256).IsRequired();
    b.Property(e => e.Price).HasPrecision(18, 2);
});
```

### 3. Create Migration

```bash
# From the EntityFrameworkCore project directory
cd aspnet-core/src/HCMv3.EntityFrameworkCore
dotnet ef migrations add Added_Product_Entity
```

## DbContext Structure

```
aspnet-core/src/HCMv3.EntityFrameworkCore/
  EntityFrameworkCore/
    HCMv3DbContext.cs              # Main DbContext
    HCMv3EntityFrameworkCoreModule.cs
    Repositories/
      HCMv3RepositoryBase.cs      # Custom repository base
```

The DbContext inherits from `AbpZeroDbContext<Tenant, Role, User, HCMv3DbContext>`.

## Custom Repository (When Needed)

Only create custom repositories when `IRepository<T>` doesn't suffice (complex queries):

```csharp
// Interface in Core project
namespace HCMv3.{Feature};

public interface IProductRepository : IRepository<Product>
{
    Task<List<Product>> GetExpensiveProductsAsync(decimal minPrice);
}

// Implementation in EntityFrameworkCore project
namespace HCMv3.EntityFrameworkCore.Repositories;

public class ProductRepository : HCMv3RepositoryBase<Product>, IProductRepository
{
    public ProductRepository(IDbContextProvider<HCMv3DbContext> dbContextProvider)
        : base(dbContextProvider) { }

    public async Task<List<Product>> GetExpensiveProductsAsync(decimal minPrice)
    {
        return await GetAll()
            .Where(p => p.Price >= minPrice)
            .OrderByDescending(p => p.Price)
            .ToListAsync();
    }
}
```

## Fluent API Common Configurations

| Configuration | Code |
|--------------|------|
| Table name | `b.ToTable("Products")` |
| Required string | `b.Property(e => e.Name).HasMaxLength(256).IsRequired()` |
| Decimal precision | `b.Property(e => e.Price).HasPrecision(18, 2)` |
| Index | `b.HasIndex(e => e.TenantId)` |
| Composite index | `b.HasIndex(e => new { e.TenantId, e.Name })` |
| Unique index | `b.HasIndex(e => e.Code).IsUnique()` |
| Default value | `b.Property(e => e.IsActive).HasDefaultValue(true)` |
| One-to-many | `b.HasMany(e => e.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId)` |

## Migration Commands

All commands must be run from the **EntityFrameworkCore project directory**:

```bash
cd aspnet-core/src/HCMv3.EntityFrameworkCore

# 1. Build first (always required before migrations)
dotnet build

# 2. Create migration
dotnet ef migrations add {MigrationName}

# 3. Apply migration to database
dotnet ef database update

# Remove last migration (only if NOT yet applied)
dotnet ef migrations remove
```

## ASP.NET Zero: Applying Migrations

ASP.NET Zero has a dedicated **Migrator** console project at:
`aspnet-core/src/HCMv3.Migrator/`

In production or staging, run the Migrator to apply pending migrations:

```bash
dotnet run --project aspnet-core/src/HCMv3.Migrator
```

In development, `dotnet ef database update` from the EntityFrameworkCore directory is sufficient.

## Constraints

- Never put business logic in repositories
- Custom repos inherit from `HCMv3RepositoryBase<TEntity>`
- Never access DbContext from Application layer
- Use Fluent API for configuration, not data annotations on entities

## Reference Files

- **DbContext**: `aspnet-core/src/HCMv3.EntityFrameworkCore/EntityFrameworkCore/HCMv3DbContext.cs`
- **Repository base**: `aspnet-core/src/HCMv3.EntityFrameworkCore/EntityFrameworkCore/Repositories/HCMv3RepositoryBase.cs`
- **Custom repository**: `aspnet-core/src/HCMv3.EntityFrameworkCore/EntityFrameworkCore/Authorization/Users/UserRepository.cs`

## Checklist

- [ ] `DbSet<TEntity>` added to `HCMv3DbContext`
- [ ] Entity configured in `OnModelCreating` (if needed)
- [ ] Migration created with `dotnet ef migrations add` (from EntityFrameworkCore directory)
- [ ] Migration applied with `dotnet ef database update` (from EntityFrameworkCore directory)
- [ ] Build succeeds after migration
- [ ] Custom repository only if `IRepository<T>` is insufficient
