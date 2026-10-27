using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee Full &amp; Final (end-of-service) settlement record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeFnFPayment</c>, Wave 3 deferred bucket).
///
/// <see cref="EmployeeActionId"/> FKs <see cref="EmployeeAction"/> (already ported), nullable per Wave 2's
/// established pattern. <see cref="EmployeeBasicPayDetailId"/> FKs <see cref="EmployeeBasicPayDetail"/>
/// (Wave 2), nullable for the same reason.
///
/// DELIBERATE SIMPLIFICATIONS / DROPPED FIELDS -- this legacy entity is, by far, the largest single
/// business object in the deferred bucket (2000+ lines): the vast majority of its legacy members are
/// <c>[NonPersistent]</c> computed getters (leave/indemnity/ticket accrual &amp; remaining-balance
/// read-outs sourced live from <c>EmployeeAbsenceQuota</c>/<c>EmployeeGratuityQuota</c>/
/// <c>EmployeeTicketQuota</c>/<c>EmployeeLoan</c>/etc.), or orchestration methods
/// (<c>UpdateQuotaBalance</c>/<c>AddIndemnityPayAdjust</c>/<c>AddBonusPay</c>/<c>SettleAllLoans</c>/
/// <c>UpdatedPositionStatus</c>/the indemnity-days calculation engine) that cascade writes into half a
/// dozen other modules (Payroll salary registers, position relationships, loan recoveries, user
/// activation). The entity keeps only the plain persisted ledger/compensation columns.
///
/// <para><b>Since the end-of-service port, the indemnity orchestration lives in
/// <see cref="Services.IFullAndFinalSettlementService"/></b>: the balances and the suggested indemnity days
/// (the resignation / termination split), auto-fill, the validations, and the save side effects -- the
/// <c>EmployeeGratuity</c> row the end-of-service off-cycle pays from, leave encashment, tickets, the
/// indemnity pay adjustment, and the employee going inactive. Loans, bonus pay, position status, login
/// deactivation and the asset check remain unported; see that service's remarks. The dropped-getter list
/// below still describes the ENTITY:</para>
/// <list type="bullet">
/// <item><description>All computed/<c>[NonPersistent]</c> getters (LeaveAccrualDays, LeaveRemainingDays,
/// ExpectedLeaveAmount, GratuityAccrualDays, GratuityRemDays, SuggestedCompensateDays/Amount,
/// IndemnityCompensateAmount's auto-calc fallback, TicketAccrualCount/RemainingCount and the
/// Dependant1/2/3 equivalents, LoanAmount/LoanDueAmount, SuggestedBonusPay, JoiningDate,
/// LastDayInService, BasicPayStart/End/Total/BasicPay collection, TotalUnpaidDays) are
/// dropped.</description></item>
/// <item><description>The legacy <c>EmpSponsor</c> collection is a derived/computed navigation (current
/// sponsor assignments as of today), not a stored FK column -- NOT ported as a field; a future UI can
/// query it live from <see cref="EmployeeSponsor"/> by EmployeeId.</description></item>
/// <item><description>The legacy <c>SettleLoanInEndOfService</c> flag is itself <c>[NonPersistent]</c> in
/// the legacy source (used only transiently during save to decide whether to auto-generate loan recovery
/// records) -- not ported as a column.</description></item>
/// <item><description><c>IndemnityCompensateAmount</c> here keeps the legacy property name
/// ("Indemnity Compensate Amount" in the legacy UI, backed by <c>GratuityCompensateDays</c> in the legacy
/// source) as a plain persisted field, accepted as client input rather than server-recomputed from the
/// indemnity-days engine.</description></item>
/// </list>
/// </summary>
public class EmployeeFnFPayment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int? EmployeeActionId { get; set; }

    public virtual EmployeeAction EmployeeAction { get; set; }

    public virtual int? EmployeeBasicPayDetailId { get; set; }

    public virtual EmployeeBasicPayDetail EmployeeBasicPayDetail { get; set; }

    public virtual DateTime CompensationDate { get; set; }

    public virtual double LeaveCompensateDays { get; set; }

    /// <summary>Legacy display name "Indemnity Compensate Days".</summary>
    public virtual double GratuityCompensateDays { get; set; }

    /// <summary>Legacy display name "Indemnity Compensate Amount".</summary>
    public virtual double IndemnityCompensateAmount { get; set; }

    public virtual double IndemnityPayAdjustment { get; set; }

    public virtual double TicketCompensateCount { get; set; }

    public virtual double Dependant1TicketCompensateCount { get; set; }

    public virtual double Dependant2TicketCompensateCount { get; set; }

    public virtual double Dependant3TicketCompensateCount { get; set; }

    public virtual double CompensateLoanAmount { get; set; }

    public virtual double CompensateBonusPay { get; set; }

    [StringLength(4000)]
    public virtual string Comments { get; set; }
}
