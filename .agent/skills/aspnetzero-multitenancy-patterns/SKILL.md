---
name: aspnetzero-multitenancy-patterns
description: "Multi-tenancy implementation patterns for ASP.NET Zero"
---

# ASP.NET Zero Multi-Tenancy Patterns

## Quick Reference

| Interface | When | TenantId | Auto-Filtered |
|-----------|------|----------|---------------|
| `IMustHaveTenant` | Entity always belongs to a tenant | `int` (required) | Yes |
| `IMayHaveTenant` | Entity can be host-level or tenant-level | `int?` (nullable) | Yes |
| Neither | Host-only entity | None | No |

## Entity Pattern

```csharp
// Tenant-scoped entity
public class Product : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }
    public string Name { get; set; }
}

// Optional tenant entity (host or tenant)
public class SharedDocument : FullAuditedEntity, IMayHaveTenant
{
    public int? TenantId { get; set; }
    public string Title { get; set; }
}
```

## App Service: Setting TenantId

**Critical**: After mapping DTO → Entity, always set TenantId manually:

```csharp
[AbpAuthorize(AppPermissions.Pages_Products_Create)]
public async Task CreateProduct(CreateProductDto input)
{
    var product = ObjectMapper.Map<Product>(input);
    product.TenantId = AbpSession.GetTenantId(); // REQUIRED
    await _productRepository.InsertAsync(product);
}
```

## Permission Multi-Tenancy Sides

```csharp
// In AppAuthorizationProvider.cs:

// Available to both host and tenants (default)
var products = pages.CreateChildPermission(
    AppPermissions.Pages_Products, L("Products"));

// Host-only
pages.CreateChildPermission(
    AppPermissions.Pages_Tenants,
    L("Tenants"),
    multiTenancySides: MultiTenancySides.Host);

// Tenant-only
pages.CreateChildPermission(
    AppPermissions.Pages_TenantDashboard,
    L("Dashboard"),
    multiTenancySides: MultiTenancySides.Tenant);
```

## Testing Multi-Tenancy

```csharp
[MultiTenantFact]
public async Task Should_Create_Product_For_Current_Tenant()
{
    // LoginAsHostAdmin() sets up the session
    await _productAppService.CreateProduct(new CreateProductDto
    {
        Name = "Tenant Product"
    });

    await UsingDbContextAsync(async ctx =>
    {
        var product = await ctx.Products
            .FirstOrDefaultAsync(p => p.Name == "Tenant Product");
        product.TenantId.ShouldBe(AbpSession.TenantId.Value);
    });
}
```

## ABP Automatic Filtering

ABP automatically filters queries by `TenantId`:
- `IMustHaveTenant`: Only returns entities where `TenantId == AbpSession.TenantId`
- `IMayHaveTenant`: Only returns entities where `TenantId == AbpSession.TenantId` or `TenantId == null`

To disable filtering temporarily:

```csharp
using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MustHaveTenant))
{
    // Query across all tenants
    var allProducts = await _productRepository.GetAllListAsync();
}
```

## Reference Files

- **Permission with sides**: `aspnet-core/src/HCMv3.Core/Authorization/AppAuthorizationProvider.cs`
- **Multi-tenant entity**: `aspnet-core/src/HCMv3.Core/MultiTenancy/Payments/SubscriptionPayment.cs`

## Checklist

- [ ] Entity implements `IMustHaveTenant` or `IMayHaveTenant`
- [ ] `TenantId` property declared on entity
- [ ] `TenantId` set from `AbpSession.GetTenantId()` in Create methods
- [ ] Permissions use correct `multiTenancySides`
- [ ] Tests use `[MultiTenantFact]` attribute
