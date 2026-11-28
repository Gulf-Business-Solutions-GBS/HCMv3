using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeTktAdjustmentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int TicketTypeId { get; set; }
    public string TicketTypeName { get; set; }
    public AdjustmentType AdjustmentType { get; set; }
    public DateTime EffectiveDate { get; set; }
    public double QuotaAccrued { get; set; }
    public double QuotaAdjusted { get; set; }
    public TicketPaymentType TicketPaymentType { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeTktAdjustmentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int TicketTypeId { get; set; }

    public AdjustmentType AdjustmentType { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    public double QuotaAccrued { get; set; }

    public double QuotaAdjusted { get; set; }

    public TicketPaymentType TicketPaymentType { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeTktAdjustmentForEditOutput
{
    public CreateOrEditEmployeeTktAdjustmentDto EmployeeTktAdjustment { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }
}

/// <summary>Every employee for the form's dropdown on the Employee Ticket Adjustments list.</summary>
public class GetEmployeeTktAdjustmentEmployeesInput
{
    /// <summary>Optional text matched against the employee's code and English and Arabic names.</summary>
    public string Filter { get; set; }
}

/// <summary>Which ticket quota an adjustment on this employee / ticket type / date draws on.</summary>
public class GetEmployeeTktAdjustmentQuotaInput
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Range(1, int.MaxValue)]
    public int TicketTypeId { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    /// <summary>The adjustment being edited, so its own saved value is counted back into the balance shown.</summary>
    public int? Id { get; set; }
}

/// <summary>
/// The ticket quota whose entitlement period covers the effective date, with the balance the adjustment may
/// draw on (legacy QuotaBalance = Accrued - (Used + Adjusted)).
/// </summary>
public class EmployeeTktAdjustmentQuotaDto
{
    public bool QuotaFound { get; set; }
    public DateTime? EntitlementFrom { get; set; }
    public DateTime? EntitlementTo { get; set; }
    public double QuotaAccrued { get; set; }
    public double QuotaUsed { get; set; }
    public double QuotaAdjusted { get; set; }
    public double QuotaBalance { get; set; }

    /// <summary>
    /// What this adjustment may take: the quota balance, plus this record's own saved value when it is being
    /// edited against the same quota (legacy gives the old value back before checking).
    /// </summary>
    public double AvailableBalance { get; set; }
}

public class GetAllEmployeeTktAdjustmentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    /// <summary>Optional text matched against employee code / names and ticket type names.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "EffectiveDate DESC";
        }
    }
}
