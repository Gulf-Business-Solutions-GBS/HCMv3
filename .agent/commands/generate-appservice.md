# Generate Application Service

Generate complete CRUD application service: interface, DTOs, Mapperly mappers, implementation, permissions, and localization.

## Usage

`/generate-appservice {EntityName}` — Entity name in PascalCase (e.g., Product)

## Prerequisites

- Entity must already exist in Core project
- DbSet must be registered in DbContext

## What It Does

1. **DTOs** in `Application.Shared/{Feature}/Dto/`
   - `{Feature}ListDto.cs`, `{Feature}EditDto.cs`, `Create{Feature}Dto.cs`
   - `Get{Feature}ForEditOutput.cs`, `Get{Feature}sInput.cs`
2. **Interface** in `Application.Shared/{Feature}/I{Feature}AppService.cs`
3. **Mapperly mappers** in `Application/Mappers/{Feature}Mappers.cs`
4. **Implementation** in `Application/{Feature}/{Feature}AppService.cs`
5. **Permissions** in `AppPermissions.cs` + `AppAuthorizationProvider.cs`
6. **Localization** in `HCMv3.xml` (searches for duplicates first)
7. **Build verification**

## Workflow

1. Read entity class to discover properties
2. Create DTOs with Data Annotations
3. Create Mapperly mappers (`MapperBase<TSource, TDest>`)
4. Create app service interface and implementation
5. Add CRUD permission set (View, Create, Edit, Delete)
6. Add localization keys (search first!)
7. Build: `dotnet build aspnet-core/HCMv3.All.sln`

## Example

```
/generate-appservice Product
```

## Next Steps

- `/generate-tests Product` — Generate test class
- `/generate-angular-crud Product` — Generate Angular UI
- `/generate-react-crud Product` — Generate React UI
