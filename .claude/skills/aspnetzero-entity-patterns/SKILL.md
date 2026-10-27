---
name: aspnetzero-entity-patterns
description: "Domain entity creation patterns for ASP.NET Zero Core project"
---

# ASP.NET Zero Entity Patterns

## Quick Start

Create entities in the **Core** project under a feature folder:

```csharp
// aspnet-core/src/HCMv3.Core/{Feature}/{EntityName}.cs
namespace HCMv3.{Feature};

public class Product : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    public string Description { get; set; }

    public decimal Price { get; set; }
}
```

## Base Class Selection

| Base Class | Provides | When to Use |
|------------|----------|-------------|
| `Entity<TPrimaryKey>` | Just `Id` | Simple lookup tables |
| `AuditedEntity<TPrimaryKey>` | `CreationTime`, `CreatorUserId`, `LastModificationTime`, `LastModifierUserId` | Entities needing audit trail |
| `FullAuditedEntity<TPrimaryKey>` | Above + `IsDeleted`, `DeleterUserId`, `DeletionTime` | **Default choice** - soft delete + full audit |
| `FullAuditedEntity` | Same with `int` PK | Default when int PK is fine |

**Default**: Inherit from `FullAuditedEntity` (int PK) unless explicitly told otherwise.

## Primary Key Types

| Type | Usage |
|------|-------|
| `int` (default) | Use `FullAuditedEntity` (no generic param) |
| `long` | Use `FullAuditedEntity<long>` |
| `Guid` | Use `FullAuditedEntity<Guid>` |

## Multi-Tenancy Interfaces

| Interface | When | Effect |
|-----------|------|--------|
| `IMustHaveTenant` | Entity always belongs to a tenant | Adds `TenantId` (required), auto-filtered |
| `IMayHaveTenant` | Entity can be host-level or tenant-level | Adds `TenantId` (nullable), auto-filtered |
| Neither | Host-only entity | No tenant filtering |

## File Location

```
aspnet-core/src/HCMv3.Core/
  {Feature}/
    {EntityName}.cs           # Entity class
    {DomainService}.cs        # Domain service (if needed)
    I{DomainService}.cs       # Domain service interface (if needed)
```

**Namespace**: `HCMv3.{Feature}`

## Constraints

- Never put DTOs in Core project
- Never reference Application or EntityFrameworkCore projects
- Never use EF Core attributes (use Fluent API in EntityFrameworkCore project)
- Always use `FullAuditedEntity` unless explicitly instructed otherwise
- For tenant-scoped entities, always implement `IMustHaveTenant` or `IMayHaveTenant`

## Reference Files

- **Existing entity**: `aspnet-core/src/HCMv3.Core/Editions/SubscribableEdition.cs`
- **Entity with multi-tenancy**: `aspnet-core/src/HCMv3.Core/MultiTenancy/Payments/SubscriptionPayment.cs`

## Checklist

- [ ] Entity in Core project under feature folder
- [ ] Correct base class (default: `FullAuditedEntity`)
- [ ] Multi-tenancy interface if tenant-scoped
- [ ] Properties with appropriate types
- [ ] Namespace matches folder: `HCMv3.{Feature}`
