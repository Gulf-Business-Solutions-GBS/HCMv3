using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeFnFPaymentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int? EmployeeActionId { get; set; }
    public int? EmployeeBasicPayDetailId { get; set; }
    public DateTime CompensationDate { get; set; }
    public double LeaveCompensateDays { get; set; }
    public double GratuityCompensateDays { get; set; }
    public double IndemnityCompensateAmount { get; set; }
    public double IndemnityPayAdjustment { get; set; }
    public double TicketCompensateCount { get; set; }
    public double Dependant1TicketCompensateCount { get; set; }
    public double Dependant2TicketCompensateCount { get; set; }
    public double Dependant3TicketCompensateCount { get; set; }
    public double CompensateLoanAmount { get; set; }
    public double CompensateBonusPay { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeFnFPaymentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int? EmployeeActionId { get; set; }

    public int? EmployeeBasicPayDetailId { get; set; }

    [Required]
    public DateTime CompensationDate { get; set; }

    public double LeaveCompensateDays { get; set; }

    public double GratuityCompensateDays { get; set; }

    public double IndemnityCompensateAmount { get; set; }

    public double IndemnityPayAdjustment { get; set; }

    public double TicketCompensateCount { get; set; }

    public double Dependant1TicketCompensateCount { get; set; }

    public double Dependant2TicketCompensateCount { get; set; }

    public double Dependant3TicketCompensateCount { get; set; }

    public double CompensateLoanAmount { get; set; }

    public double CompensateBonusPay { get; set; }

    /// <summary>
    /// Recover every open loan in full on the compensation date. Not stored -- as in legacy, it only decides
    /// what the save does. Without it, an employee with an open loan cannot be settled.
    /// </summary>
    public bool SettleLoanInEndOfService { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }
}

public class GetEmployeeFnFPaymentForEditOutput
{
    public CreateOrEditEmployeeFnFPaymentDto EmployeeFnFPayment { get; set; }
}

public class GetAllEmployeeFnFPaymentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "CompensationDate DESC";
        }
    }
}

/// <summary>
/// What a Full &amp; Final entry may compensate for one employee, the suggested indemnity days (reduced for a
/// resignation by the indemnity pay rule), and the values a NEW entry is pre-filled with.
/// </summary>
public class FullAndFinalBalancesDto
{
    public double LeaveAccrualDays { get; set; }

    public double LeaveRemainingDays { get; set; }

    /// <summary>The full indemnity accrued (the provision) -- the same for resignation and termination.</summary>
    public double GratuityAccrualDays { get; set; }

    public double GratuityRemainingDays { get; set; }

    /// <summary>The indemnity days to pay: accrued days x the rule's percentage for the separation type and service band.</summary>
    public double SuggestedIndemnityDays { get; set; }

    public double TicketAccrualCount { get; set; }

    public double TicketRemainingCount { get; set; }

    public double Dependant1TicketCount { get; set; }

    public double Dependant1TicketRemainingCount { get; set; }

    public double Dependant2TicketCount { get; set; }

    public double Dependant2TicketRemainingCount { get; set; }

    public double Dependant3TicketCount { get; set; }

    public double Dependant3TicketRemainingCount { get; set; }

    public double AutoFillLeaveCompensateDays { get; set; }

    public double AutoFillGratuityCompensateDays { get; set; }

    /// <summary>Total amount due across all of the employee's loans.</summary>
    public double LoanDueAmount { get; set; }

    /// <summary>What is still owed on open loans. Above zero, the entry must settle them.</summary>
    public double LoanBalanceDueAmount { get; set; }

    /// <summary>The most that may be settled as bonus: the average of the employee's past bonus payments.</summary>
    public double SuggestedBonusPay { get; set; }
}
