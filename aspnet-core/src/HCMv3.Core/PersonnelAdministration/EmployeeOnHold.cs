using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee payroll-hold record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeOnHold</c>, Wave 2 deferred bucket).
///
/// DELIBERATE SIMPLIFICATIONS / DROPPED FIELDS:
/// <list type="bullet">
/// <item><description>The legacy <c>PayrollControlRecord.CalendarMonthStart</c> lock-date validation (Payroll
/// module concern) is not ported as an entity concern; the no-overlap validation for the same employee IS
/// ported in <c>EmployeeOnHoldsAppService</c>.</description></item>
/// <item><description>Legacy auto-normalizes StartDate/EndDate to first/last day of month on save; ported as
/// an AppService-level concern.</description></item>
/// </list>
/// </summary>
public class EmployeeOnHold : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    /// <summary>Used to hold all record types for the employee.</summary>
    public virtual bool OnHold { get; set; }

    public virtual bool OnHoldBasicPay { get; set; }

    public virtual bool OnHoldLeavePay { get; set; }

    public virtual bool OnHoldIndemnityPay { get; set; }

    public virtual bool OnHoldAirTicketPay { get; set; }

    public virtual bool OnHoldLoanDeduction { get; set; }

    public virtual bool OnHoldAllEarnings { get; set; }

    public virtual bool OnHoldAllDeductions { get; set; }

    public virtual OnHoldStatus BankTransfer { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    public virtual int? EmployeeActionId { get; set; }

    public virtual EmployeeAction EmployeeAction { get; set; }
}
