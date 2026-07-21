---
name: mapperly-patterns
description: "Mapperly entity-DTO mapping patterns using MapperBase from Abp.Mapperly"
---

# Mapperly Mapping Patterns

## Quick Start

Mappers live in the **Application** project under `Mappers/`:

```csharp
// aspnet-core/src/HCMv3.Application/Mappers/{Feature}Mappers.cs
using Abp.Mapperly;
using HCMv3.{Feature};
using HCMv3.{Feature}.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class ProductToProductListDtoMapper : MapperBase<Product, ProductListDto>
{
    public override partial ProductListDto Map(Product source);
    public override partial void Map(Product source, ProductListDto destination);
}

[Mapper]
public partial class ProductEditDtoToProductMapper : MapperBase<ProductEditDto, Product>
{
    public override partial Product Map(ProductEditDto source);
    public override partial void Map(ProductEditDto source, Product destination);
}

[Mapper]
public partial class ProductToProductEditDtoMapper : MapperBase<Product, ProductEditDto>
{
    public override partial ProductEditDto Map(Product source);
    public override partial void Map(Product source, ProductEditDto destination);
}
```

## Key Conventions

| Convention | Detail |
|------------|--------|
| Base class | `MapperBase<TSource, TDest>` from `Abp.Mapperly` |
| Attribute | `[Mapper]` from `Riok.Mapperly.Abstractions` |
| Class | `partial` — Mapperly generates the mapping code at compile time |
| Naming | `{Source}To{Dest}Mapper` (e.g., `ProductToProductListDtoMapper`) |
| File location | `Application/Mappers/{Feature}Mappers.cs` |
| Namespace | `HCMv3.Mappers` (flat, not per-feature) |
| Multiple mappers per file | Yes — group all mappers for a feature in one file |

## Two Methods Per Mapper

Each mapper provides two overrides:

```csharp
// Creates a new TDest from TSource
public override partial TDest Map(TSource source);

// Updates an existing TDest from TSource
public override partial void Map(TSource source, TDest destination);
```

## Manual Mapping (When Needed)

When the target has protected setters (e.g., `EntityDto` base has protected `Id` setter) or calculated properties:

```csharp
[Mapper]
public partial class ProductToProductInfoDtoMapper : MapperBase<Product, ProductInfoDto>
{
    // Manual mapping - not partial
    public override ProductInfoDto Map(Product source)
    {
        return new ProductInfoDto
        {
            Id = source.Id,
            Name = source.Name,
            IsExpensive = source.Price > 1000
        };
    }

    public override void Map(Product source, ProductInfoDto destination)
    {
        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.IsExpensive = source.Price > 1000;
    }
}
```

## Ignoring Properties

```csharp
[Mapper]
public partial class UserToUserListDtoMapper : MapperBase<User, UserListDto>
{
    [MapperIgnoreTarget(nameof(UserListDto.Password))]
    public override partial UserListDto Map(User source);

    [MapperIgnoreTarget(nameof(UserListDto.Password))]
    public override partial void Map(User source, UserListDto destination);
}
```

## Using Mappers in App Services

Mappers are auto-registered. Use `ObjectMapper` from the ABP base class:

```csharp
// Entity -> DTO
var dto = ObjectMapper.Map<ProductListDto>(entity);

// DTO -> Entity (create)
var entity = ObjectMapper.Map<Product>(createDto);

// DTO -> Entity (update existing)
ObjectMapper.Map(editDto, existingEntity);

// List mapping
var dtos = ObjectMapper.Map<List<ProductListDto>>(entities);
```

## Typical Mappers Per Feature

| Mapper | Direction | Purpose |
|--------|-----------|---------|
| `{Entity}To{Entity}ListDtoMapper` | Entity → ListDto | For list/table display |
| `{Entity}To{Entity}EditDtoMapper` | Entity → EditDto | For GetForEdit |
| `{Entity}EditDtoTo{Entity}Mapper` | EditDto → Entity | For Update |
| `Create{Entity}DtoTo{Entity}Mapper` | CreateDto → Entity | For Create |

## Reference Files

- **Multiple mappers in one file**: `aspnet-core/src/HCMv3.Application/Mappers/EditionMappers.cs`
- **Manual mapping example**: Lines 46-74 in `EditionMappers.cs` (protected setter workaround)

## Checklist

- [ ] File in `Application/Mappers/{Feature}Mappers.cs`
- [ ] Namespace: `HCMv3.Mappers`
- [ ] `[Mapper]` attribute + `partial` class
- [ ] Inherits `MapperBase<TSource, TDest>`
- [ ] Both `Map` overrides declared
- [ ] Manual mapping for protected setters or calculated properties
- [ ] All needed directions covered (Entity↔DTO)
