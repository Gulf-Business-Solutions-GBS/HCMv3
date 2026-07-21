---
description: "Audit permission consistency: AppPermissions vs AppAuthorizationProvider vs [AbpAuthorize] vs Angular"
allowed-tools: Read, Glob, Grep
---

# Permissions Audit

## Usage

`/review:permissions`

## What It Does

Audits permission consistency across all layers:

1. **AppPermissions.cs** — All defined permission constants
2. **AppAuthorizationProvider.cs** — All registered permissions
3. **[AbpAuthorize] usages** — All permission references in app services
4. **Angular templates** — All permission pipe usages

## Checks

| Check | What It Finds |
|-------|---------------|
| Orphaned constants | Constants in `AppPermissions` not registered in `AppAuthorizationProvider` |
| Missing constants | Permissions used in `[AbpAuthorize]` that don't exist in `AppPermissions` |
| Unregistered permissions | Constants that exist but aren't registered in the provider |
| Angular mismatches | Permission strings in Angular that don't match any constant value |
| Missing localization | Permission display names without localization keys |

## Output

```markdown
## Permission Audit Results

### Orphaned Constants (defined but not registered)
- `Pages_Products_Export` — not in AppAuthorizationProvider

### Missing References
- `[AbpAuthorize("Pages.Reports.View")]` in ReportAppService — not in AppPermissions

### Angular Mismatches
- `'Pages.Products.Export' | permission` in products.component.html — no matching constant

### Summary: X issues found
```
