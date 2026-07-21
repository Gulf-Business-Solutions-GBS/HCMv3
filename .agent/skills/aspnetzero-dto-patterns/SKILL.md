---
name: aspnetzero-dto-patterns
description: "DTO creation patterns with Data Annotations for ASP.NET Zero"
---

# ASP.NET Zero DTO Patterns

## Quick Start

DTOs go in **Application.Shared** under a feature `Dto` folder:

```
aspnet-core/src/HCMv3.Application.Shared/
  {Feature}/
    Dto/
      {Feature}ListDto.cs           # For list/table display
      {Feature}EditDto.cs           # For edit form data
      Create{Feature}Dto.cs         # For create input
      Get{Feature}ForEditOutput.cs  # Wrapper for edit form
      Get{Feature}Input.cs          # Paged/filtered list input
    I{Feature}AppService.cs         # Service interface
```

## DTO Types

### List DTO (for tables)

```csharp
namespace HCMv3.{Feature}.Dto;

public class ProductListDto : EntityDto
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreationTime { get; set; }
}
```

### Edit DTO (for forms)

```csharp
public class ProductEditDto : EntityDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    public string Description { get; set; }

    [Range(0, 999999)]
    public decimal Price { get; set; }
}
```

### Create DTO (input)

```csharp
public class CreateProductDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    public string Description { get; set; }

    [Range(0, 999999)]
    public decimal Price { get; set; }
}
```

### GetForEdit Output (wrapper)

```csharp
public class GetProductForEditOutput
{
    public ProductEditDto Product { get; set; }
    // Add lookup lists here if needed
}
```

### GetForEdit Input (required Id)

```csharp
public class GetProductForEditInput : EntityDto
{
}
```

### Paged Input

```csharp
public class GetProductsInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}
```

## Validation Rules

- Use `System.ComponentModel.DataAnnotations` (NOT FluentValidation)
- **Never** use `double.MaxValue` as a max attribute
- Common attributes: `[Required]`, `[StringLength(max)]`, `[Range(min, max)]`, `[EmailAddress]`
- DTOs must be serializable

## Key Conventions

| Rule | Description |
|------|-------------|
| Separate DTOs per operation | Don't use one DTO for both create and list |
| `EntityDto` for edit | Id is required for edit operations and GetForEdit |
| `EntityDto` for list | Non-nullable Id since it always comes from DB |
| Inherit `PagedAndSortedResultRequestDto` | For any list input that supports paging |
| `Filter` property on list inputs | Standard name for text search |

## Reference Files

- **Edit DTO**: `aspnet-core/src/HCMv3.Application.Shared/Editions/Dto/EditionEditDto.cs`
- **Create DTO**: `aspnet-core/src/HCMv3.Application.Shared/Editions/Dto/CreateEditionDto.cs`
- **GetForEdit output**: `aspnet-core/src/HCMv3.Application.Shared/Editions/Dto/GetEditionEditOutput.cs`

## Checklist

- [ ] DTOs in Application.Shared/{Feature}/Dto/
- [ ] Validation with Data Annotations
- [ ] Separate DTOs per operation (List, Edit, Create, GetForEditOutput, Input)
- [ ] PagedAndSortedResultRequestDto for paged inputs
- [ ] No business logic in DTOs
- [ ] Namespace: `HCMv3.{Feature}.Dto`
