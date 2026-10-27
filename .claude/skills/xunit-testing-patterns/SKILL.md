---
name: xunit-testing-patterns
description: "xUnit testing patterns with Shouldly and NSubstitute for ASP.NET Zero"
---

# ASP.NET Zero Testing Patterns

## Quick Start

```csharp
// aspnet-core/test/HCMv3.Tests/{Feature}/ProductAppService_Tests.cs
namespace HCMv3.Tests.{Feature};

public class ProductAppService_Tests : AppTestBase
{
    private readonly IProductAppService _productAppService;

    public ProductAppService_Tests()
    {
        LoginAsHostAdmin();
        _productAppService = Resolve<IProductAppService>();
    }

    [Fact]
    public async Task Should_Get_Products()
    {
        // Act
        var result = await _productAppService.GetProducts(new GetProductsInput());

        // Assert
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Should_Create_Product()
    {
        // Act
        await _productAppService.CreateProduct(new CreateProductDto
        {
            Name = "Test Product",
            Price = 99.99m
        });

        // Assert
        await UsingDbContextAsync(async context =>
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Name == "Test Product");
            product.ShouldNotBeNull();
            product.Price.ShouldBe(99.99m);
        });
    }
}
```

## Test Infrastructure

| Component | Purpose |
|-----------|---------|
| `AppTestBase` | Base class — provides DI, session, DB access |
| `LoginAsHostAdmin()` | Sets session to host admin user |
| `Resolve<T>()` | Gets service from DI container |
| `UsingDbContext(ctx => ...)` | Synchronous DB access for assertions |
| `UsingDbContextAsync(async ctx => ...)` | Async DB access for assertions |
| `[Fact]` | Standard xUnit test attribute |
| `[MultiTenantFact]` | Runs only when multi-tenancy is enabled |
| `[MultiTenantTheory]` | Theory that runs with multi-tenancy |

## File & Method Naming

| Convention | Pattern | Example |
|------------|---------|---------|
| File name | `{ClassName}_Tests.cs` | `ProductAppService_Tests.cs` |
| Class name | `{ClassName}_Tests` | `ProductAppService_Tests` |
| Method name | `Should_{ExpectedBehavior}` | `Should_Create_Product` |
| Test folder | `Tests/{Feature}/` | `Tests/Products/` |

## Shouldly Assertions

```csharp
// Equality
result.Name.ShouldBe("Expected");
result.Count.ShouldBe(5);

// Null checks
result.ShouldNotBeNull();
result.ShouldBeNull();

// Comparisons
result.Count.ShouldBeGreaterThan(0);
result.Count.ShouldBeGreaterThanOrEqualTo(1);
result.Price.ShouldBeLessThan(1000);

// Collections
result.Items.ShouldNotBeEmpty();
result.Items.ShouldContain(x => x.Name == "Test");
result.Items.Count.ShouldBe(3);

// Boolean
result.IsActive.ShouldBeTrue();
result.IsDeleted.ShouldBeFalse();
```

## Testing CRUD Operations

### Get/List

```csharp
[Fact]
public async Task Should_Get_Products()
{
    var result = await _productAppService.GetProducts(
        new GetProductsInput { MaxResultCount = 10 });

    result.TotalCount.ShouldBeGreaterThanOrEqualTo(0);
    result.Items.ShouldNotBeNull();
}
```

### GetForEdit

```csharp
[Fact]
public async Task Should_Get_Product_For_Edit()
{
    var existingProduct = UsingDbContext(ctx =>
        ctx.Products.First());

    var result = await _productAppService.GetProductForEdit(
        new NullableIdDto(existingProduct.Id));

    result.Product.ShouldNotBeNull();
    result.Product.Name.ShouldBe(existingProduct.Name);
}
```

### Create

```csharp
[Fact]
public async Task Should_Create_Product()
{
    await _productAppService.CreateProduct(new CreateProductDto
    {
        Name = "New Product",
        Price = 50
    });

    await UsingDbContextAsync(async ctx =>
    {
        var product = await ctx.Products
            .FirstOrDefaultAsync(p => p.Name == "New Product");
        product.ShouldNotBeNull();
    });
}
```

### Update

```csharp
[Fact]
public async Task Should_Update_Product()
{
    var product = UsingDbContext(ctx => ctx.Products.First());

    await _productAppService.UpdateProduct(new ProductEditDto
    {
        Id = product.Id,
        Name = "Updated Name",
        Price = product.Price
    });

    UsingDbContext(ctx =>
    {
        var updated = ctx.Products.First(p => p.Id == product.Id);
        updated.Name.ShouldBe("Updated Name");
    });
}
```

### Delete

```csharp
[Fact]
public async Task Should_Delete_Product()
{
    var product = UsingDbContext(ctx => ctx.Products.First());

    await _productAppService.DeleteProduct(new EntityDto(product.Id));

    UsingDbContext(ctx =>
    {
        ctx.Products.FirstOrDefault(p => p.Id == product.Id)
            .ShouldBeNull(); // Soft-deleted, filtered out
    });
}
```

### Exception Testing

```csharp
[Fact]
public async Task Should_Not_Delete_Product_With_Orders()
{
    var product = UsingDbContext(ctx =>
        ctx.Products.First(p => p.Orders.Any()));

    var exception = await Assert.ThrowsAsync<UserFriendlyException>(
        async () => await _productAppService.DeleteProduct(
            new EntityDto(product.Id)));

    exception.Message.ShouldContain("has orders");
}
```

## Multi-Tenancy Tests

```csharp
[MultiTenantFact]
public async Task Should_Create_Product_For_Tenant()
{
    // LoginAsHostAdmin is called in constructor
    await _productAppService.CreateProduct(new CreateProductDto
    {
        Name = "Tenant Product"
    });

    await UsingDbContextAsync(async ctx =>
    {
        var product = await ctx.Products
            .FirstOrDefaultAsync(p => p.Name == "Tenant Product");
        product.ShouldNotBeNull();
        product.TenantId.ShouldBe(AbpSession.TenantId.Value);
    });
}
```

## Reference Files

- **Full test example**: `aspnet-core/test/HCMv3.Tests/Editions/EditionAppService_Tests.cs`
- **Test base**: `aspnet-core/test/HCMv3.Test.Base/`

## Checklist

- [ ] Test class in `Tests/{Feature}/`
- [ ] Inherits from `AppTestBase`
- [ ] `LoginAsHostAdmin()` in constructor
- [ ] Service resolved via `Resolve<I{Feature}AppService>()`
- [ ] Tests for: Get, Create, Update, Delete
- [ ] Shouldly assertions
- [ ] `[MultiTenantFact]` for tenant-scoped tests
- [ ] Exception scenarios tested
- [ ] Tests pass: `dotnet test`
