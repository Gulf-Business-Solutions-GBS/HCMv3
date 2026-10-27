using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;

namespace HCMv3.PayrollManagement;

/// <summary>
/// PayrollArea master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollArea</c>).
///
/// DELIBERATE SIMPLIFICATIONS:
/// <list type="bullet">
/// <item><description>Unlike most Code/Name lookups in this port, the legacy <c>Name</c> is a single field
/// (no NameAr) — ported faithfully as a single <see cref="Name"/> property.</description></item>
/// <item><description>The legacy <c>AfterConstruction</c> default-currency assignment
/// (<c>CurrencyService.GetDefaultCurrency</c>) has no equivalent "default currency" concept anywhere else
/// in this port, so <see cref="CurrencyId"/> is left with no default — the caller must supply one (it is
/// still a required field, matching legacy validation).</description></item>
/// <item><description>The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not
/// ported.</description></item>
/// </list>
/// </summary>
public class PayrollArea : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string Name { get; set; }

    public virtual int PayrollPeriodId { get; set; }

    public virtual PayrollPeriod PayrollPeriod { get; set; }

    public virtual int CurrencyId { get; set; }

    public virtual Currency Currency { get; set; }

    public virtual int CutOffDay { get; set; }

    /// <summary>
    /// FK to <see cref="OrganizationManagement.CostElement"/> (legacy "FICompany").
    /// </summary>
    public virtual int? FICompanyId { get; set; }

    public virtual CostElement FICompany { get; set; }

    /// <summary>
    /// FK to <see cref="PayrollManagement.AccountInfo"/> (legacy "FIBank").
    /// </summary>
    public virtual int? FIBankId { get; set; }

    public virtual AccountInfo FIBank { get; set; }

    public virtual int? CountryId { get; set; }

    public virtual Country Country { get; set; }

    /// <summary>
    /// Legacy <c>PayrollArea.PAReleaseNotification</c>: sent when a period of this area is locked for payroll
    /// (<c>PayrollControlRecordsAppService.SetState</c> to ReleasedForPayroll). Optional.
    /// </summary>
    public virtual int? PAReleaseNotificationId { get; set; }

    public virtual NotificationConfig PAReleaseNotification { get; set; }

    /// <summary>Legacy <c>PayrollArea.PAExitNotification</c>: sent when a period is exited (Closed). Optional.</summary>
    public virtual int? PAExitNotificationId { get; set; }

    public virtual NotificationConfig PAExitNotification { get; set; }

    /// <summary>
    /// Legacy <c>PayrollArea.PACorrectionNotification</c>: sent when a period is released for correction. Optional.
    /// </summary>
    public virtual int? PACorrectionNotificationId { get; set; }

    public virtual NotificationConfig PACorrectionNotification { get; set; }
}
