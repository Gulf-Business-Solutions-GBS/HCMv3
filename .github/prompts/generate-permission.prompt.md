# Generate Permissions

Add a complete CRUD permission set for a feature (constants, registration, localization).

## Usage

`/generate-permission {FeatureName}` — Feature name (e.g., Products)

## What It Does

1. **Adds constants** to `Core.Shared/Authorization/AppPermissions.cs`
2. **Registers permissions** in `Core/Authorization/AppAuthorizationProvider.cs`
3. **Adds localization keys** to `Core/Localization/HCMv3/HCMv3.xml`

## Generated Permissions

For feature `Products`:

| Constant | Value | Localization Key |
|----------|-------|-----------------|
| `Pages_Products` | `"Pages.Products"` | `Products` |
| `Pages_Products_Create` | `"Pages.Products.Create"` | `CreatingNewProduct` |
| `Pages_Products_Edit` | `"Pages.Products.Edit"` | `EditingProduct` |
| `Pages_Products_Delete` | `"Pages.Products.Delete"` | `DeletingProduct` |

## Options

- `--host-only` — `multiTenancySides: MultiTenancySides.Host`
- `--tenant-only` — `multiTenancySides: MultiTenancySides.Tenant`
- `--admin` — Place under `Pages_Administration_{Feature}`

## Example

```
/generate-permission Products
/generate-permission Products --admin
/generate-permission TenantSettings --host-only
```
