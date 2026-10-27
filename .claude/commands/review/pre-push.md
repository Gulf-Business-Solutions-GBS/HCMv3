---
description: "Pre-push review: build, test, layer checks, permission audit, localization completeness"
allowed-tools: Read, Bash, Glob, Grep
---

# Pre-Push Review

## Usage

`/review:pre-push`

## What It Does

Runs a comprehensive checklist before pushing code:

1. **Build check**: `dotnet build aspnet-core/HCMv3.All.sln`
2. **Test check**: `dotnet test aspnet-core/test/HCMv3.Tests/`
3. **Layer violation check**: Scan for cross-layer imports
4. **Permission check**: Verify all `[AbpAuthorize]` references exist in `AppPermissions`
5. **Localization check**: Verify all `L("Key")` calls have matching XML entries
6. **TODO/HACK check**: Scan for leftover `TODO`, `HACK`, `FIXME` comments
7. **DTO validation check**: Verify DTOs have Data Annotations

## Output

```markdown
## Pre-Push Review Results

- [PASS] Build succeeds
- [PASS] Tests pass (X/X)
- [WARN] Found TODO in ProductAppService.cs:45
- [FAIL] Missing localization key: "ProductNotFound"

### Summary: X passed, X warnings, X failures
```

## When to Use

Run before pushing to remote. Catches common issues that would fail in CI.
