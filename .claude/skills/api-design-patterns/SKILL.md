---
name: api-design-patterns
description: "API and application service interface design patterns for ASP.NET Zero"
---

# API Design Patterns

## Interface Convention

```csharp
// aspnet-core/src/HCMv3.Application.Shared/{Feature}/I{Feature}AppService.cs
public interface IProductAppService : IApplicationService
{
    Task<PagedResultDto<ProductListDto>> GetProducts(GetProductsInput input);
    Task<GetProductForEditOutput> GetProductForEdit(NullableIdDto input);
    Task CreateProduct(CreateProductDto input);
    Task UpdateProduct(ProductEditDto input);
    Task DeleteProduct(EntityDto input);
}
```

> **`[HttpPost]` rule**: `Get{Feature}s` list methods that take a **custom input DTO** must be decorated with `[HttpPost]` in the app service implementation. ABP routes `Get*` as HTTP GET by default, making NSwag emit parameters as individual query-string args — the input DTO class (e.g. `GetProductsInput`) never appears in `service-proxies.ts` and client code breaks. Do NOT apply to `GetForEdit` or other `Get*` methods taking `EntityDto`/`NullableIdDto`.
>
> ```csharp
> [AbpAuthorize(AppPermissions.Pages_Products)]
> [HttpPost] // Forces NSwag to generate GetProductsInput as a body DTO in service-proxies.ts
> public async Task<PagedResultDto<ProductListDto>> GetProducts(GetProductsInput input) { ... }
> ```

## Return Types

| Return Type | When |
|-------------|------|
| `PagedResultDto<T>` | Paginated list (has `TotalCount` + `Items`) |
| `ListResultDto<T>` | Full list, no pagination (has `Items` only) |
| `EntityDto` | Single entity reference (has `Id`) |
| `EntityDto<TPrimaryKey>` | Entity with specific PK type |
| `NullableIdDto` | Input that may or may not have an Id |
| `Task` (void) | Create, Update, Delete operations |
| Custom output DTO | Complex return data (e.g., `GetProductForEditOutput`) |

## Method Naming

| Method | Signature | Permission |
|--------|-----------|------------|
| List (paged) | `Get{Feature}s(Get{Feature}sInput)` → `PagedResultDto<{Feature}ListDto>` | View |
| List (all) | `Get{Feature}s()` → `ListResultDto<{Feature}ListDto>` | View |
| Get for edit | `Get{Feature}ForEdit(NullableIdDto)` → `Get{Feature}ForEditOutput` | Create or Edit |
| Create | `Create{Feature}(Create{Feature}Dto)` → `Task` | Create |
| Update | `Update{Feature}({Feature}EditDto)` → `Task` | Edit |
| Delete | `Delete{Feature}(EntityDto)` → `Task` | Delete |
| Combo/dropdown | `Get{Feature}ComboboxItems()` → `List<ComboboxItemDto>` | View |

## Input DTO Patterns

| Pattern | Base Class | Use Case |
|---------|-----------|----------|
| Paged + sorted + filtered | `PagedAndSortedResultRequestDto` | Standard list with search |
| Just filtering | Custom DTO with `Filter` property | Simple lists |
| ID reference | `EntityDto` | Delete, single-entity operations |
| Nullable ID | `NullableIdDto` | GetForEdit (null = create, has value = edit) |

## Reference Files

- **Interface**: `aspnet-core/src/HCMv3.Application.Shared/Editions/IEditionAppService.cs`
