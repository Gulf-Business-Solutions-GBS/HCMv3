using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeLoanDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int LoanTypeId { get; set; }
    public string LoanTypeName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime RecoveryStartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double ActualAmount { get; set; }
    public double DueAmount { get; set; }
    public double BalanceDueAmount { get; set; }
    public double FixedAmount { get; set; }
    public string Comments { get; set; }
    public bool RecoverFracInLastTrans { get; set; }
    public bool IncludeInLoanPay { get; set; }
}

public class CreateOrEditEmployeeLoanDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int LoanTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime RecoveryStartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public double ActualAmount { get; set; }

    [Required]
    public double DueAmount { get; set; }

    public double FixedAmount { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public bool RecoverFracInLastTrans { get; set; }

    public bool IncludeInLoanPay { get; set; }

    /// <summary>
    /// Record this as final instead of sending it for approval.
    ///
    /// <para>
    /// Offered only to a user holding <c>RecordWithoutApproval</c>, and only on a type whose catalog row
    /// permits a bypass. The server checks both and refuses otherwise, so this being true is a request
    /// rather than a decision.
    /// </para>
    /// </summary>
    public bool RecordDirectly { get; set; }
}

public class GetEmployeeLoanForEditOutput
{
    public CreateOrEditEmployeeLoanDto EmployeeLoan { get; set; }

    /// <summary>Display only: so the employee shows in the dropdown even when the lookup did not return them.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Display only.</summary>
    public string EmployeeFullNameEn { get; set; }

    /// <summary>Display only: kept by payroll and recoveries, never edited on the form (legacy read-only).</summary>
    public double BalanceDueAmount { get; set; }
}

/// <summary>Input for the employee dropdown on the Employee Loans list's create dialog.</summary>
public class GetEmployeeLoanEmployeesInput
{
    /// <summary>Optional text matched against the employee's code and English and Arabic names.</summary>
    public string Filter { get; set; }
}

public class GetAllEmployeeLoansInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    /// <summary>Optional text matched against the employee's code and names and the loan type's names.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
