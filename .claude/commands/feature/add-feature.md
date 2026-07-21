---
description: "End-to-end feature generation: entity, DTOs, mappers, app service, permissions, localization, tests"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
argument-hint: "$FEATURE_NAME - Feature name in PascalCase (e.g., Product)"
---

# Add Feature

## Usage

`/feature:add-feature $ARGUMENTS`

## What It Does

Generates a complete CRUD feature end-to-end following the `crud-implementation.md` flow:

1. **Design** — Prompts for entity properties, PK type, tenancy
2. **Entity** — Creates entity in Core
3. **EF Config** — Adds DbSet, configures entity, creates migration
4. **DTOs** — Creates List, Edit, Create DTOs in Application.Shared
5. **Mappers** — Creates Mapperly mappers in Application
6. **Permissions** — Adds to AppPermissions + AppAuthorizationProvider
7. **Localization** — Adds keys to HCMv3.xml
8. **App Service** — Creates interface and implementation
9. **Tests** — Creates xUnit test class
10. **Build & Test** — Verifies compilation and tests pass

## Prerequisites

None — this command creates everything from scratch.

## Workflow

1. Parse feature name from `$ARGUMENTS`
2. Prompt user for entity properties (name:type pairs)
3. Delegate to `aspnetzero-developer` agent for backend
4. Delegate to `qa-engineer` agent for tests
5. Verify: `dotnet build` + `dotnet test`

## Options

- Properties inline: `Product --properties "Name:string,Price:decimal"`
- `--no-test` — Skip test generation
- `--with-angular` — Also generate Angular CRUD page
- `--with-react` — Also generate React CRUD page

## Example

```
/feature:add-feature Product --properties "Name:string,Price:decimal,Description:string,IsActive:bool"
/feature:add-feature Product --properties "Name:string,Price:decimal" --with-angular
```

## Generated Files

```
aspnet-core/
  src/
    Core/{Feature}/{Entity}.cs
    Application.Shared/{Feature}/Dto/*.cs (5 files)
    Application.Shared/{Feature}/I{Entity}AppService.cs
    Application/{Feature}/{Entity}AppService.cs
    Application/Mappers/{Feature}Mappers.cs
    EntityFrameworkCore/.../Migrations/...
  test/
    Tests/{Feature}/{Entity}AppService_Tests.cs

angular/ (if --with-angular)
  src/app/{area}/{feature}/*.component.ts/html (4 files)
```

## Skills Used

All backend skills + optionally Angular/React skills. See `flows/crud-implementation.md`.
