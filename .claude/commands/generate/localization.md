---
description: "Add localization entries to HCMv3.xml with duplicate detection"
allowed-tools: Read, Edit, Glob, Grep
argument-hint: "$KEYS - Comma-separated key=value pairs (e.g., Products=Products,EditProduct=Edit product)"
---

# Generate Localization

## Usage

`/generate:localization $ARGUMENTS`

## What It Does

1. **Searches** existing keys in all XML files to prevent duplicates
2. **Adds** new entries to `HCMv3.xml` (English)
3. **Reports** which keys already existed and were skipped

## CRITICAL

**ABP throws `AbpException` on duplicate keys.** This command always searches first.

## Workflow

1. Apply `aspnetzero-localization-patterns` skill
2. Parse key=value pairs from arguments
3. Search each key in all localization XML files
4. Skip keys that already exist (report them)
5. Add new keys to `HCMv3.xml`

## File Location

```
aspnet-core/src/HCMv3.Core/Localization/HCMv3/HCMv3.xml
```

## Example

```
/generate:localization Products=Products,CreateNewProduct=Create new product,EditProduct=Edit product,DeleteProduct=Delete product
```

Adds (if not already present):
```xml
<text name="Products">Products</text>
<text name="CreateNewProduct">Create new product</text>
<text name="EditProduct">Edit product</text>
<text name="DeleteProduct">Delete product</text>
```

## Search Command Used

```powershell
Select-String -Pattern 'name="KeyName"' -Path aspnet-core/src/HCMv3.Core/Localization/HCMv3/*.xml
```
