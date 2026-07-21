# Generate Tests

Generate xUnit test class for an existing application service.

Provide the app service name (e.g., `Product` or `ProductAppService`).

## Prerequisites

- App service and interface must exist
- Entity and DTOs must exist
- Solution must build successfully

## What It Does

1. **Reads** the app service class to discover public methods
2. **Creates** test class in `aspnet-core/test/HCMv3.Tests/{Feature}/{Service}_Tests.cs`
3. **Generates tests** for all CRUD operations
4. **Runs tests** to verify they pass

## Generated Tests

| Test | Purpose |
|------|---------|
| `Should_Get_{Feature}s` | List/paging works correctly |
| `Should_Get_{Feature}_For_Edit` | Edit form data loads |
| `Should_Create_{Feature}` | Create saves entity to DB |
| `Should_Update_{Feature}` | Update modifies entity in DB |
| `Should_Delete_{Feature}` | Delete soft-deletes entity |
| `Should_Set_TenantId_On_Create` | Multi-tenancy TenantId is set |

## Test Conventions

- File: `{ClassName}_Tests.cs`
- Class: inherits `AppTestBase`
- Constructor: `LoginAsHostAdmin()` + `Resolve<I{Feature}AppService>()`
- Methods: `Should_{ExpectedBehavior}`
- Assertions: Shouldly (`ShouldBe`, `ShouldNotBeNull`, etc.)
- DB verification: `UsingDbContext` / `UsingDbContextAsync`
- Multi-tenancy: `[MultiTenantFact]`

## Verification

After writing tests, run:
```bash
dotnet test aspnet-core/test/HCMv3.Tests/
```

## Example

```
/generate-tests Product
```
