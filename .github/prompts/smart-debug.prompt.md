# Smart Debug

AI-powered root cause analysis for runtime errors, build failures, and integration issues.

## Usage

Provide the error message, exception, or symptom as context when invoking this command.

## What It Does

1. **Parses** the error message or stack trace
2. **Identifies** the originating layer (Core, Application, EF, Angular, React)
3. **Searches** relevant code in the failure path
4. **Diagnoses** root cause using common ABP/ASP.NET Zero error patterns
5. **Proposes** targeted fix with minimal changes

## Common Error Patterns

| Error Pattern | Likely Root Cause |
|---------------|-------------------|
| `AbpAuthorizationException` | Permission not granted or `[AbpAuthorize]` missing |
| `UserFriendlyException` | Business rule — read the message |
| `Duplicate entry in localization` | Same key in XML file twice |
| `EntityNotFoundException` | Wrong ID or entity soft-deleted |
| Migration build error | Model change not migrated |
| NSwag `404` | Backend changed, proxies stale |
| Angular `undefined` property | DTO shape changed, proxy not regenerated |
| `AbpValidationException` | DTO validation failure — check Data Annotations |
| `AbpDbConcurrencyException` | Concurrent entity modification |
| `?Id=[object Object]` in URL | NSwag GET/DELETE: pass raw `id` number, not `new EntityDto({ id })` |

## Diagnostic Steps

1. **Read the error** — exact message and stack trace
2. **Identify the layer** — which project does the top of the stack point to?
3. **Search for the code** — find the method/class mentioned in the stack
4. **Check dependencies** — permissions, localization, DbContext, mappers
5. **Propose fix** — minimal change to resolve the issue

## Example Usage

```
/smart-debug "AbpAuthorizationException: Current user did not login to the application!"
/smart-debug "The entity type 'Product' was not found in the model"
/smart-debug "Duplicate key 'Products' in localization XML"
/smart-debug "AbpValidationException: Method arguments are not valid! ?Id=[object Object]"
```
