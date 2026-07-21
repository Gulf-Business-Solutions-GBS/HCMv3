---
name: error-handling-patterns
description: "Error handling and exception patterns for ASP.NET Zero"
---

# Error Handling Patterns

## Exception Types

| Exception | When to Use | Shown to User |
|-----------|-------------|---------------|
| `UserFriendlyException` | Business rule violations | Yes (message displayed) |
| `AbpAuthorizationException` | Permission failures | Yes (auto-handled by ABP) |
| `AbpValidationException` | Input validation failures | Yes (auto-handled by ABP) |
| Standard exceptions | Unexpected errors | No (logged, generic message) |

## UserFriendlyException

```csharp
// With localized message
throw new UserFriendlyException(L("ProductNotFound"));

// With placeholder
throw new UserFriendlyException(L("ThereAreTenantsSubscribedToThisEdition"));

// With details
throw new UserFriendlyException(
    L("CannotDeleteProduct"),
    L("ProductHasActiveOrders")
);
```

## Business Rule Validation

```csharp
[AbpAuthorize(AppPermissions.Pages_Products_Delete)]
public async Task DeleteProduct(EntityDto input)
{
    var orderCount = await _orderRepository.CountAsync(o => o.ProductId == input.Id);
    if (orderCount > 0)
    {
        throw new UserFriendlyException(L("CannotDeleteProductWithOrders"));
    }

    await _productRepository.DeleteAsync(input.Id);
}
```

## Key Rules

- Always localize error messages: `L("Key")`, never hardcode strings
- Use `UserFriendlyException` for expected business errors
- Let ABP handle authorization and validation exceptions automatically
- Never expose internal exception details to clients
- Use `Check.NotNull()` for argument validation in domain services

## Reference Files

- **Exception usage**: `aspnet-core/src/HCMv3.Application/Editions/EditionAppService.cs` (lines 113-116)
