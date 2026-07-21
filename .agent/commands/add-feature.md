# Add Feature

End-to-end feature generation: entity, DTOs, mappers, app service, permissions, localization, tests.

## Usage

`/add-feature {FeatureName}` — Feature name in PascalCase (e.g., Product)

## What It Does

Generates a complete CRUD feature end-to-end:

1. **Entity** — Creates entity in Core
2. **EF Config** — Adds DbSet, configures entity, creates migration
3. **DTOs** — Creates List, Edit, Create DTOs in Application.Shared
4. **Mappers** — Creates Mapperly mappers in Application
5. **Permissions** — Adds to AppPermissions + AppAuthorizationProvider
6. **Localization** — Adds keys to HCMv3.xml (searches first!)
7. **App Service** — Creates interface and implementation
8. **Tests** — Creates xUnit test class
9. **Build & Test** — Verifies compilation and tests pass

## Workflow

1. Parse feature name
2. Ask for entity properties (name:type pairs)
3. Ask for PK type (default: int), multi-tenancy (default: IMustHaveTenant)
4. Generate all files following the CRUD implementation flow
5. Build: `dotnet build aspnet-core/HCMv3.All.sln`
6. Test: `dotnet test aspnet-core/test/HCMv3.Tests/`

## Options

- Properties inline: `Product --properties "Name:string,Price:decimal"`
- `--no-test` — Skip test generation
- `--with-angular` — Also generate Angular CRUD page
- `--with-react` — Also generate React CRUD page
- `--pk guid` or `--pk long` — Change primary key type
- `--no-tenant` — Skip multi-tenancy interface

## Generated Files

New files created (`+`) and existing files modified (`~`):

```
aspnet-core/src/
  Core/{Feature}/{Entity}.cs                                         [+]
  Core.Shared/Authorization/AppPermissions.cs                        [~] adds Pages_{Feature} constants
  Core/Authorization/AppAuthorizationProvider.cs                     [~] registers permissions
  Core/Localization/HCMv3/HCMv3.xml              [~] adds localization keys
  Application.Shared/{Feature}/Dto/*.cs (5 DTOs)                    [+]
  Application.Shared/{Feature}/I{Entity}AppService.cs               [+]
  Application/{Feature}/{Entity}AppService.cs                        [+]
  Application/Mappers/{Feature}Mappers.cs                            [+]
  EntityFrameworkCore/HCMv3DbContext.cs                    [~] adds DbSet
  EntityFrameworkCore/Migrations/...                                 [+]
aspnet-core/test/
  Tests/{Feature}/{Entity}AppService_Tests.cs                        [+]
```

## Example

```
/add-feature Product --properties "Name:string,Price:decimal,Description:string,IsActive:bool"
/add-feature Product --properties "Name:string,Price:decimal" --with-angular
```
