using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeSeparationDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime SubmissionDate { get; set; }
    public DateTime LastDayInService { get; set; }
    public int NoticePeriodDays { get; set; }
    public DateTime PlannedLastDayInService { get; set; }
    public SeparationType SeparationType { get; set; }
    public int SeparationReasonId { get; set; }
    public string SeparationReasonName { get; set; }
    public DateTime? RevokeDate { get; set; }
    public DateTime? OnHoldFromDate { get; set; }
    public DateTime? OnHoldTillDate { get; set; }
    public string RevokeComments { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeSeparationDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime SubmissionDate { get; set; }

    [Required]
    public DateTime LastDayInService { get; set; }

    public int NoticePeriodDays { get; set; }

    public DateTime PlannedLastDayInService { get; set; }

    [Required]
    public SeparationType SeparationType { get; set; }

    [Required]
    public int SeparationReasonId { get; set; }

    public DateTime? RevokeDate { get; set; }

    /// <summary>
    /// Legacy "OnHold Information". When a from-date is given and at least one flag is ticked, saving the
    /// separation creates the matching On Hold record that payroll reads; clearing them removes it.
    /// </summary>
    public DateTime? OnHoldFromDate { get; set; }

    public DateTime? OnHoldTillDate { get; set; }

    public bool OnHoldAll { get; set; }

    public bool OnHoldBasicPay { get; set; }

    public bool OnHoldLeavePay { get; set; }

    public bool OnHoldIndemnityPay { get; set; }

    public bool OnHoldAirTicketPay { get; set; }

    public bool OnHoldLoanDeduction { get; set; }

    public bool OnHoldAllEarnings { get; set; }

    public bool OnHoldAllDeductions { get; set; }

    public OnHoldStatus BankTransfer { get; set; }

    [StringLength(500)]
    public string RevokeComments { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeSeparationForEditOutput
{
    public CreateOrEditEmployeeSeparationDto EmployeeSeparation { get; set; }
}

public class GetAllEmployeeSeparationsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "SubmissionDate DESC";
        }
    }
}
