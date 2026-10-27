# Review Permissions

Audit permission consistency across all layers: AppPermissions vs AppAuthorizationProvider vs [AbpAuthorize] vs Angular/React.

## What It Does

1. **Scans `AppPermissions.cs`** — All defined permission constants
2. **Scans `AppAuthorizationProvider.cs`** — All registered permissions
3. **Scans `[AbpAuthorize]` usages** — All permission references in app services
4. **Scans Angular/React** — All permission pipe/hook usages in frontend

## Checks

| Check | What It Finds |
|-------|---------------|
| Orphaned constants | Constants in `AppPermissions` not registered in `AppAuthorizationProvider` |
| Missing constants | Permissions used in `[AbpAuthorize]` that don't exist in `AppPermissions` |
| Unregistered permissions | Constants that exist but aren't registered |
| Frontend mismatches | Permission strings in Angular/React that don't match any constant value |
| Missing localization | Permission display names without localization keys |

## Output Format

```markdown
## Permission Audit Results

### Orphaned Constants (defined but not registered)
- `Pages_Products_Export` — not in AppAuthorizationProvider

### Missing References
- `[AbpAuthorize("Pages.Reports.View")]` in ReportAppService — not in AppPermissions

### Frontend Mismatches
- `'Pages.Products.Export' | permission` in products.component.html — no matching constant

### Summary: X issues found
```

## Key Files

- `aspnet-core/src/HCMv3.Core.Shared/Authorization/AppPermissions.cs`
- `aspnet-core/src/HCMv3.Core/Authorization/AppAuthorizationProvider.cs`
- All `[AbpAuthorize]` usages in `aspnet-core/src/HCMv3.Application/`
- Angular: `| permission` and `| permissionAny` usages
- React: `isGranted()` and `isGrantedAny()` usages

## Example

```
/review-permissions
```
