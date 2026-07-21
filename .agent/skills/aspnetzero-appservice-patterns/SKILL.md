---
name: aspnetzero-appservice-patterns
description: "Application service CRUD patterns for ASP.NET Zero"
---

# ASP.NET Zero Application Service Patterns

## Quick Start

### Interface (Application.Shared)

```csharp
// aspnet-core/src/HCMv3.Application.Shared/{Feature}/I{Feature}AppService.cs
namespace HCMv3.{Feature};

public interface IProductAppService : IApplicationService
{
    Task<PagedResultDto<ProductListDto>> GetProducts(GetProductsInput input);
    Task<GetProductForEditOutput> GetProductForEdit(EntityDto input);
    Task CreateProduct(CreateProductDto input);
    Task UpdateProduct(ProductEditDto input);
    Task DeleteProduct(EntityDto input);
}
```

### Implementation (Application)

```csharp
// aspnet-core/src/HCMv3.Application/{Feature}/ProductAppService.cs
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;           // Required for .IsNullOrWhiteSpace() extension method
using Abp.Linq.Extensions;      // Required for .WhereIf() and .PageBy()
using Microsoft.EntityFrameworkCore;
using HCMv3.Authorization;
using HCMv3.{Feature}.Dto;

namespace HCMv3.{Feature};

public class ProductAppService : HCMv3AppServiceBase, IProductAppService
{
    private readonly IRepository<Product> _productRepository;

    public ProductAppService(IRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    [AbpAuthorize(AppPermissions.Pages_Products)]
    [HttpPost] // Required on list methods with custom input: ABP maps Get* to HTTP GET, so NSwag would emit GetProductsInput as query-string args instead of a typed DTO class — breaking service-proxies.ts. Do NOT add [HttpPost] to GetForEdit or other Get* methods that take EntityDto/NullableIdDto.
    public async Task<PagedResultDto<ProductListDto>> GetProducts(GetProductsInput input)
    {
        var query = _productRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                p => p.Name.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Name" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ProductListDto>(
            totalCount,
            ObjectMapper.Map<List<ProductListDto>>(products)
        );
    }

    [AbpAuthorize(AppPermissions.Pages_Products_Create, AppPermissions.Pages_Products_Edit)]
    public async Task<GetProductForEditOutput> GetProductForEdit(EntityDto input)
    {
        var product = await _productRepository.GetAsync(input.Id);
        var productEditDto = ObjectMapper.Map<ProductEditDto>(product);

        return new GetProductForEditOutput { Product = productEditDto };
    }

    [AbpAuthorize(AppPermissions.Pages_Products_Create)]
    public async Task CreateProduct(CreateProductDto input)
    {
        var product = ObjectMapper.Map<Product>(input);
        product.TenantId = AbpSession.GetTenantId();
        await _productRepository.InsertAsync(product);
    }

    [AbpAuthorize(AppPermissions.Pages_Products_Edit)]
    public async Task UpdateProduct(ProductEditDto input)
    {
        var product = await _productRepository.GetAsync(input.Id.Value);
        ObjectMapper.Map(input, product);
    }

    [AbpAuthorize(AppPermissions.Pages_Products_Delete)]
    public async Task DeleteProduct(EntityDto input)
    {
        await _productRepository.DeleteAsync(input.Id);
    }
}
```

## Key Patterns

| Pattern | Detail |
|---------|--------|
| Base class | `HCMv3AppServiceBase` |
| Authorization | `[AbpAuthorize(AppPermissions.Pages_...)]` on every public method |
| Repository | `IRepository<TEntity>` or `IRepository<TEntity, TPrimaryKey>` |
| Mapping Entity → DTO | `ObjectMapper.Map<TDto>(entity)` |
| Mapping DTO → Entity (create) | `ObjectMapper.Map<TEntity>(dto)` then set `TenantId` |
| Mapping DTO → Entity (update) | `ObjectMapper.Map(dto, existingEntity)` |
| Paging | `.OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "DefaultField" : input.Sorting).PageBy(input)` |
| Filtering | `.WhereIf(!filter.IsNullOrWhiteSpace(), predicate)` |
| Error messages | `throw new UserFriendlyException(L("LocalizationKey"))` |

## Critical Rules

1. **Always set TenantId** after mapping DTO → Entity: `entity.TenantId = AbpSession.GetTenantId()`
2. **Always map Entity → DTO** when returning data (never return entities)
3. **GetForEdit must require an identifier** — do not return empty DTO if Id is not sent
4. **In Create/Update**, map DTO → Entity first, then apply business rules
5. **Never access DbContext directly** — use `IRepository<T>`
6. **Never use `??` for sorting fallback** — Angular/PrimeNG sends `sorting: ""` (empty string), not `null`. `input.Sorting ?? "Name"` does not protect against empty string. Dynamic LINQ throws `ParseException` on `.OrderBy("")`. Always use: `.OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "DefaultField" : input.Sorting)`
7. **Always add `[HttpPost]` to every `Get*` method that takes a custom input DTO** — ABP routes `Get*` methods as HTTP GET by default, which causes NSwag to serialize input as individual query-string parameters instead of a typed body DTO. This means the input class (e.g. `GetProductsInput`) will NOT be generated in `service-proxies.ts`, breaking React/Angular clients. `[HttpPost]` forces NSwag to emit the input class as a body type so it appears in `service-proxies.ts`.

## Method Naming

| Method | Returns | Permission |
|--------|---------|------------|
| `Get{Feature}s(input)` | `PagedResultDto<{Feature}ListDto>` | Pages_{Feature} |
| `Get{Feature}ForEdit(EntityDto)` | `Get{Feature}ForEditOutput` | Pages_{Feature}_Create or _Edit |
| `Create{Feature}(dto)` | `Task` | Pages_{Feature}_Create |
| `Update{Feature}(dto)` | `Task` | Pages_{Feature}_Edit |
| `Delete{Feature}(EntityDto)` | `Task` | Pages_{Feature}_Delete |

## Reference Files

- **Full example**: `aspnet-core/src/HCMv3.Application/Editions/EditionAppService.cs`
- **Base class**: `aspnet-core/src/HCMv3.Application/HCMv3AppServiceBase.cs`
- **Interface**: `aspnet-core/src/HCMv3.Application.Shared/Editions/IEditionAppService.cs`

## Checklist

- [ ] Interface in Application.Shared
- [ ] Implementation in Application, inherits `HCMv3AppServiceBase`
- [ ] `[AbpAuthorize]` on every public method
- [ ] `ObjectMapper` for all entity/DTO conversions
- [ ] `TenantId` set manually after DTO → Entity mapping
- [ ] `WhereIf` for optional filters
- [ ] Sorting with `.IsNullOrWhiteSpace()` guard (not `??`): `input.Sorting.IsNullOrWhiteSpace() ? "Default" : input.Sorting`
- [ ] Paging with `PageBy(input)`
- [ ] Localized error messages via `L("Key")`
