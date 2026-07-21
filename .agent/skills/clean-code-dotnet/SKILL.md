---
name: clean-code-dotnet
description: "Clean code principles applied to .NET and ABP development"
---

# Clean Code for .NET / ABP

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Entity | PascalCase | `Product`, `DoctorSchedule` |
| DTO | `{Entity}Dto`, `Create{Entity}Dto` | `ProductListDto` |
| AppService | `{Entity}AppService` | `ProductAppService` |
| Interface | `I{Name}` | `IProductAppService` |
| Private field | `_camelCase` | `_productRepository` |
| Constant | PascalCase | `MaxProductNameLength` |
| Permission | `Pages_{Area}_{Feature}_{Action}` | `Pages_Products_Create` |

## SOLID in ABP Context

| Principle | ABP Application |
|-----------|----------------|
| **SRP** | Each app service handles one aggregate; don't mix concerns |
| **OCP** | Use virtual methods for extension; inherit `HCMv3AppServiceBase` |
| **LSP** | DTOs must be valid substitutes for their base types |
| **ISP** | Separate interfaces per feature (`IProductAppService`, not `IGenericService`) |
| **DIP** | Inject `IRepository<T>` not `DbContext`; inject `IService` not implementation |

## Async/Await Rules

- All DB operations must be async
- Suffix async methods with `Async` only for interface methods
- Never `.Result` or `.Wait()` on async code
- Use `ConfigureAwait(false)` only in library code, not in app services

## Method Size

- Keep methods under 30 lines
- Extract complex LINQ queries to private methods
- Extract validation logic to separate methods

## Repository Pattern

- Use `IRepository<T>` for standard CRUD
- Only create custom repos for complex queries
- Never access `DbContext` from Application layer

## Common Anti-Patterns

| Anti-Pattern | Fix |
|-------------|-----|
| Business logic in controller | Move to app service |
| Returning entities from app services | Map to DTOs |
| Direct DbContext in app services | Use `IRepository<T>` |
| Hardcoded error messages | Use `L("Key")` |
| Missing `[AbpAuthorize]` | Add to all public methods |
| Not setting TenantId | Set from `AbpSession` after mapping |
