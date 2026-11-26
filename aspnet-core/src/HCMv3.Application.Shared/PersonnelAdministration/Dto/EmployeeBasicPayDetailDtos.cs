using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeBasicPayDetailDto : EntityDto<int>
{
    public int EmployeeBasicPayId { get; set; }
    public int WageTypeId { get; set; }
    public string WageTypeName { get; set; }
    public double Amount { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencyName { get; set; }
    public double DependantNumber { get; set; }
    public DependantUnit DependantUnit { get; set; }
    public int? DependantWageTypeId { get; set; }
    public string DependantWageTypeName { get; set; }
}

public class CreateOrEditEmployeeBasicPayDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeBasicPayId { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    public double Amount { get; set; }

    [Required]
    public int CurrencyId { get; set; }

    public double DependantNumber { get; set; }

    public DependantUnit DependantUnit { get; set; }

    public int? DependantWageTypeId { get; set; }
}

public class GetEmployeeBasicPayDetailForEditOutput
{
    public CreateOrEditEmployeeBasicPayDetailDto EmployeeBasicPayDetail { get; set; }
}

public class GetAllEmployeeBasicPayDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeBasicPayId { get; set; }

    /// <summary>Free-text filter matched against the wage type code and names.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id DESC";
        }
    }
}

/// <summary>
/// A wage type that may be chosen on a Basic Pay Detail line (legacy <c>EmployeeBasicPayDetail.WageType</c> lookup).
/// </summary>
public class BasicPayWageTypeLookupDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}

public class GetSelectableBasicPayWageTypesInput
{
    /// <summary>The wage type already on the line being edited; kept in the list even if it no longer qualifies.</summary>
    public int? CurrentWageTypeId { get; set; }
}
