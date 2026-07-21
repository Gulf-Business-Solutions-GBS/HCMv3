---
description: "Add a complete CRUD permission set for a feature (constants, registration, localization)"
allowed-tools: Read, Write, Edit, Glob, Grep
argument-hint: "$FEATURE_NAME - The feature name (e.g., Products)"
---

# Generate Permissions

## Usage

`/generate:permission $ARGUMENTS`

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

## Workflow

1. Apply `aspnetzero-permission-patterns` skill
2. Search existing permissions to avoid duplicates
3. Search existing localization keys to avoid duplicates
4. Add constants to `AppPermissions.cs`
5. Add registration to `AppAuthorizationProvider.cs`
6. Add localization keys to `HCMv3.xml`

## Options

- `--host-only` — Set `multiTenancySides: MultiTenancySides.Host`
- `--tenant-only` — Set `multiTenancySides: MultiTenancySides.Tenant`
- `--admin` — Place under `Pages_Administration_{Feature}` instead of `Pages_{Feature}`

## Example

```
/generate:permission Products
/generate:permission Products --admin
/generate:permission TenantSettings --host-only
```
