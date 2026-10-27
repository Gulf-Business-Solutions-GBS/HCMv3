---
name: aspnetzero-permission-patterns
description: "Permission definition and authorization patterns for ASP.NET Zero"
---

# ASP.NET Zero Permission Patterns

## Quick Start

Adding permissions requires changes to **3 files** (+ localization):

### 1. Permission Constants (Core.Shared)

```csharp
// aspnet-core/src/HCMv3.Core.Shared/Authorization/AppPermissions.cs
// Add inside the class, in the appropriate section (COMMON, TENANT, or HOST)

public const string Pages_Products = "Pages.Products";
public const string Pages_Products_Create = "Pages.Products.Create";
public const string Pages_Products_Edit = "Pages.Products.Edit";
public const string Pages_Products_Delete = "Pages.Products.Delete";
```

### 2. Permission Registration (Core)

```csharp
// aspnet-core/src/HCMv3.Core/Authorization/AppAuthorizationProvider.cs
// Add inside SetPermissions method

var products = pages.CreateChildPermission(AppPermissions.Pages_Products, L("Products"));
products.CreateChildPermission(AppPermissions.Pages_Products_Create, L("CreatingNewProduct"));
products.CreateChildPermission(AppPermissions.Pages_Products_Edit, L("EditingProduct"));
products.CreateChildPermission(AppPermissions.Pages_Products_Delete, L("DeletingProduct"));
```

### 3. Localization Keys (Core)

```xml
<!-- aspnet-core/src/HCMv3.Core/Localization/HCMv3/HCMv3.xml -->
<text name="Products">Products</text>
<text name="CreatingNewProduct">Creating new product</text>
<text name="EditingProduct">Editing product</text>
<text name="DeletingProduct">Deleting product</text>
```

## Naming Convention

| Field | Format | Example |
|-------|--------|---------|
| Constant name | `Pages_{Area}_{Feature}_{Action}` | `Pages_Products_Create` |
| Constant value | `"Pages.{Area}.{Feature}.{Action}"` | `"Pages.Products.Create"` |
| Localization key | Gerund form | `"CreatingNewProduct"` |

For admin features: `Pages_Administration_{Feature}_{Action}`

## Multi-Tenancy Sides

```csharp
// Host-only permission
products.CreateChildPermission(
    AppPermissions.Pages_Products_Create,
    L("CreatingNewProduct"),
    multiTenancySides: MultiTenancySides.Host
);

// Tenant-only permission
products.CreateChildPermission(
    AppPermissions.Pages_Products_Create,
    L("CreatingNewProduct"),
    multiTenancySides: MultiTenancySides.Tenant
);

// Both (default - no parameter needed)
products.CreateChildPermission(
    AppPermissions.Pages_Products_Create,
    L("CreatingNewProduct")
);
```

## Usage in App Services

```csharp
[AbpAuthorize(AppPermissions.Pages_Products)]
public async Task<PagedResultDto<ProductListDto>> GetProducts(GetProductsInput input)

[AbpAuthorize(AppPermissions.Pages_Products_Create)]
public async Task CreateProduct(CreateProductDto input)

// Multiple permissions (any of them grants access)
[AbpAuthorize(AppPermissions.Pages_Products_Create, AppPermissions.Pages_Products_Edit)]
public async Task<GetProductForEditOutput> GetProductForEdit(NullableIdDto input)
```

## Hierarchy Structure

```
Pages (root)
  └── Administration
       ├── Roles (CRUD)
       ├── Users (CRUD + special actions)
       └── ...
  └── {YourFeature}
       ├── Create
       ├── Edit
       └── Delete
```

Permissions are hierarchical — granting `Pages_Products` grants all child permissions.

## Standard CRUD Permission Set

For any new feature, always create this standard set:

| Permission | Purpose |
|------------|---------|
| `Pages_{Feature}` | View/list (parent) |
| `Pages_{Feature}_Create` | Create new |
| `Pages_{Feature}_Edit` | Edit existing |
| `Pages_{Feature}_Delete` | Delete |

## Reference Files

- **Constants**: `aspnet-core/src/HCMv3.Core.Shared/Authorization/AppPermissions.cs`
- **Provider**: `aspnet-core/src/HCMv3.Core/Authorization/AppAuthorizationProvider.cs`

## Checklist

- [ ] Constants added to `AppPermissions.cs` (Core.Shared)
- [ ] Permissions registered in `AppAuthorizationProvider.cs` (Core)
- [ ] Localization keys added to `HCMv3.xml`
- [ ] `[AbpAuthorize]` on all app service methods
- [ ] Multi-tenancy side set if needed
- [ ] Hierarchy is correct (child under parent)
