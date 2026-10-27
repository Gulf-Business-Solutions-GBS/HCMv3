---
description: "Generate complete CRUD application service: interface, DTOs, Mapperly mappers, implementation, permissions, and localization"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
argument-hint: "$ENTITY_NAME - The entity to generate CRUD service for (e.g., Product)"
---

# Generate Application Service

## Usage

`/generate:appservice $ARGUMENTS`

## What It Does

Creates a complete CRUD application service with all supporting files:

1. **DTOs** in `Application.Shared/{Feature}/Dto/`
   - `{Feature}ListDto.cs`
   - `{Feature}EditDto.cs`
   - `Create{Feature}Dto.cs`
   - `Get{Feature}ForEditOutput.cs`
   - `Get{Feature}sInput.cs`

2. **Interface** in `Application.Shared/{Feature}/I{Feature}AppService.cs`

3. **Mapperly mappers** in `Application/Mappers/{Feature}Mappers.cs`

4. **Implementation** in `Application/{Feature}/{Feature}AppService.cs`

5. **Permissions** in:
   - `Core.Shared/Authorization/AppPermissions.cs`
   - `Core/Authorization/AppAuthorizationProvider.cs`

6. **Localization** in `Core/Localization/HCMv3/HCMv3.xml`

7. **Build verification**

## Prerequisites

- Entity must already exist in Core project
- DbSet must be registered in DbContext

## Workflow

1. Read the entity class to discover properties
2. Apply `aspnetzero-dto-patterns` — create DTOs with Data Annotations
3. Apply `mapperly-patterns` — create Mapperly mapper classes
4. Apply `aspnetzero-appservice-patterns` — create interface and implementation
5. Apply `aspnetzero-permission-patterns` — add CRUD permission set
6. Apply `aspnetzero-localization-patterns` — add localization keys (search first!)
7. Build: `dotnet build aspnet-core/HCMv3.All.sln`

## Skills Used

| Step | Skill |
|------|-------|
| DTOs | `aspnetzero-dto-patterns` |
| Mappers | `mapperly-patterns` |
| App Service | `aspnetzero-appservice-patterns` |
| Permissions | `aspnetzero-permission-patterns` |
| Localization | `aspnetzero-localization-patterns` |

## Example

```
/generate:appservice Product
```

## Next Steps

- `/generate:tests Product` — generate test class
- `/generate:angular-crud Product` — generate Angular UI
- `/generate:react-crud Product` — generate React UI
