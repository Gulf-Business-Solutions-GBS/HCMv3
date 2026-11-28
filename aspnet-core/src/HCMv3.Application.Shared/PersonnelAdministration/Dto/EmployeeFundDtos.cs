using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeFundDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double Amount { get; set; }
    public double PaidAmount { get; set; }
    public double BalanceAmount { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeFundDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeFundForEditOutput
{
    public CreateOrEditEmployeeFundDto EmployeeFund { get; set; }

    /// <summary>Display only: so the employee shows in the dropdown even when the lookup did not return them.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Display only.</summary>
    public string EmployeeFullNameEn { get; set; }

    /// <summary>Read-only (legacy): Opening + Deduction + External Deduction rows, worked out from the fund's details.</summary>
    public double Amount { get; set; }

    /// <summary>Read-only (legacy): Payment rows.</summary>
    public double PaidAmount { get; set; }

    /// <summary>Read-only (legacy): Amount - Paid Amount.</summary>
    public double BalanceAmount { get; set; }
}

/// <summary>Input for the employee dropdown on the Fund list's create dialog.</summary>
public class GetEmployeeFundEmployeesInput
{
    /// <summary>Optional text matched against the employee's code and English and Arabic names.</summary>
    public string Filter { get; set; }
}

public class GetAllEmployeeFundsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    /// <summary>Optional text matched against the employee's code and names, and the fund's comments.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
