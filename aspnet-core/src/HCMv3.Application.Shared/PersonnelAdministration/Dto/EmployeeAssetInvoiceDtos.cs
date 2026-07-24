using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeAssetInvoiceDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int AssetId { get; set; }
    public string AssetNameEn { get; set; }
    public DateTime InvoiceDate { get; set; }
    public double InvoiceAmount { get; set; }
    public double EmployeePlanAmount { get; set; }
    public double DeductionAmount { get; set; }
    public bool DeductFromEmployee { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeAssetInvoiceDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int AssetId { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; }

    public double InvoiceAmount { get; set; }

    public double EmployeePlanAmount { get; set; }

    public double DeductionAmount { get; set; }

    public bool DeductFromEmployee { get; set; }

    public string Comments { get; set; }
}

public class GetEmployeeAssetInvoiceForEditOutput
{
    public CreateOrEditEmployeeAssetInvoiceDto EmployeeAssetInvoice { get; set; }
}

public class GetAllEmployeeAssetInvoicesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "InvoiceDate DESC";
        }
    }
}
