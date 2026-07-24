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
}

public class GetEmployeeLoanForEditOutput
{
    public CreateOrEditEmployeeLoanDto EmployeeLoan { get; set; }
}

public class GetAllEmployeeLoansInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
