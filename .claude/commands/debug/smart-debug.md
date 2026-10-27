---
description: "AI-powered root cause analysis for runtime errors, build failures, and integration issues"
allowed-tools: Read, Bash, Glob, Grep
argument-hint: "$ERROR - The error message, exception, or symptom to investigate"
---

# Smart Debug

## Usage

`/debug:smart-debug $ARGUMENTS`

## What It Does

1. **Parses** the error message or stack trace
2. **Identifies** the originating layer (Core, Application, EF, Angular)
3. **Searches** relevant code in the failure path
4. **Diagnoses** root cause using common ABP/ASP.NET Zero error patterns
5. **Proposes** targeted fix with minimal changes

## Common Patterns

| Error Pattern | Likely Root Cause |
|---------------|-------------------|
| `AbpAuthorizationException` | Permission not granted or `[AbpAuthorize]` missing |
| `UserFriendlyException` | Business rule — read the message |
| `Duplicate entry in localization` | Same key in XML file twice |
| `EntityNotFoundException` | Wrong ID or entity soft-deleted |
| Migration build error | Model change not migrated |
| NSwag `404` | Backend changed, proxies stale |
| Angular `undefined` property | DTO shape changed, proxy not regenerated |

## Options

- `--fix` — After diagnosis, apply the proposed fix automatically

## Example

```
/debug:smart-debug "AbpAuthorizationException: Current user did not login to the application!"
/debug:smart-debug "The entity type 'Product' was not found in the model" --fix
```
