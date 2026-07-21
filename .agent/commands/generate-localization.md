# Generate Localization

Add localization entries to HCMv3.xml with duplicate detection.

## Usage

`/generate-localization {key=value pairs}` — Comma-separated key=value pairs

## CRITICAL

**ABP throws `AbpException` on duplicate keys.** This command always searches first.

## What It Does

1. **Searches** existing keys in all XML files to prevent duplicates
2. **Adds** new entries to `HCMv3.xml` (English)
3. **Reports** which keys already existed and were skipped

## File Location

```
aspnet-core/src/HCMv3.Core/Localization/HCMv3/HCMv3.xml
```

## Search Command

```powershell
Select-String -Pattern 'name="KeyName"' -Path aspnet-core/src/HCMv3.Core/Localization/HCMv3/*.xml
```

## Example

```
/generate-localization Products=Products,CreateNewProduct=Create new product,EditProduct=Edit product
```

Adds (if not already present):
```xml
<text name="Products">Products</text>
<text name="CreateNewProduct">Create new product</text>
<text name="EditProduct">Edit product</text>
```
