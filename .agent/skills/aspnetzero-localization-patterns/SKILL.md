---
name: aspnetzero-localization-patterns
description: "XML-based localization patterns for ASP.NET Zero"
---

# ASP.NET Zero Localization Patterns

## Quick Start

Localization files are XML in the **Core** project:

```
aspnet-core/src/HCMv3.Core/Localization/HCMv3/
  HCMv3.xml              # English (default)
  HCMv3-tr.xml           # Turkish
  HCMv3-fr.xml           # French
  HCMv3-de.xml           # German
  ... (other languages)
```

## CRITICAL: Search Before Adding

**ABP throws `AbpException` if duplicate keys exist.** Always search first:

```powershell
# PowerShell
Select-String -Pattern 'name="YourKeyName"' -Path aspnet-core/src/HCMv3.Core/Localization/HCMv3/*.xml
```

## XML Format

```xml
<text name="Products">Products</text>
<text name="CreateNewProduct">Create new product</text>
<text name="EditProduct">Edit product</text>
<text name="DeleteProduct">Delete product</text>
<text name="ProductName">Product name</text>
<text name="ProductPrice">Price</text>
<text name="ProductDeleteWarningMessage">Product {0} will be deleted. Are you sure?</text>
```

### Placeholders

Use `{0}`, `{1}` etc. for dynamic values:

```xml
<text name="ThereAreXProductsWithName">There are {0} products with name {1}</text>
```

## Usage Patterns

### C# Backend

```csharp
// In app services (inherited from ApplicationService)
L("Products")                           // Simple key
L("ProductDeleteWarningMessage", name)  // With placeholder

// In exceptions
throw new UserFriendlyException(L("ProductNotFound"));
```

### Angular Templates

```html
{{ 'Products' | localize }}
{{ 'ProductDeleteWarningMessage' | localize: product.name }}
```

### Angular TypeScript

```typescript
this.l('Products');
this.l('ProductDeleteWarningMessage', product.name);
```

## Standard Keys Per Feature

When adding a new feature, create these keys:

| Key | English Value | Purpose |
|-----|---------------|---------|
| `{Feature}` | `{Feature}` | Page title, menu, permission name |
| `CreateNew{Feature}` | `Create new {feature}` | Create button label |
| `Edit{Feature}` | `Edit {feature}` | Edit form title |
| `{Feature}DeleteWarningMessage` | `{Feature} {0} will be deleted` | Delete confirmation |
| `SuccessfullyDeleted` | (already exists) | After delete |
| `SavedSuccessfully` | (already exists) | After save |

## Existing Common Keys (DO NOT duplicate)

These keys already exist — reuse them:

| Key | Value |
|-----|-------|
| `SavedSuccessfully` | Saved successfully |
| `SuccessfullyDeleted` | Successfully deleted |
| `AreYouSure` | Are you sure? |
| `Actions` | Actions |
| `Edit` | Edit |
| `Delete` | Delete |
| `Save` | Save |
| `Cancel` | Cancel |
| `Close` | Close |
| `Yes` | Yes |
| `No` | No |
| `CreationTime` | Creation time |
| `Name` | Name |

## Reference Files

- **English**: `aspnet-core/src/HCMv3.Core/Localization/HCMv3/HCMv3.xml`
- **Usage in app service**: `aspnet-core/src/HCMv3.Application/Editions/EditionAppService.cs` (line 116)

## Checklist

- [ ] Searched for existing keys before adding
- [ ] Added entries to `HCMv3.xml` (English)
- [ ] No duplicate `name` attributes
- [ ] Used proper placeholder format `{0}`, `{1}`
- [ ] Key names follow PascalCase convention
