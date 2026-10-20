using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeLoanInstallmentDto : EntityDto<int>
{
    public int EmployeeLoanId { get; set; }

    /// <summary>Legacy label "Effective Date".</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Maintained by the server: the day before the next row, or 31/12/9999.</summary>
    public DateTime EndDate { get; set; }

    public double Amount { get; set; }

    public string Remark { get; set; }
}

public class CreateOrEditEmployeeLoanInstallmentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeLoanId { get; set; }

    /// <summary>Must be the first day of a month.</summary>
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public double Amount { get; set; }

    [StringLength(500)]
    public string Remark { get; set; }
}

public class GetEmployeeLoanInstallmentForEditOutput
{
    public CreateOrEditEmployeeLoanInstallmentDto EmployeeLoanInstallment { get; set; }
}

public class GetAllEmployeeLoanInstallmentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeLoanId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
