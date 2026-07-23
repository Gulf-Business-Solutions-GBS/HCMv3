using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// PayrollControlRecord (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollControlRecord</c>). Exactly one control record
/// is allowed per <see cref="PayrollArea"/> (enforced in <c>PayrollControlRecordsAppService</c>, legacy
/// "Control record is unique for a PayrollArea" parity).
///
/// DELIBERATE SIMPLIFICATIONS:
/// <list type="bullet">
/// <item><description>The legacy <c>PeriodLocked</c>/<c>PeriodClosed</c> enums (No=0/Yes=1) carry no extra
/// semantics beyond a boolean and are simplified to plain <see cref="bool"/> fields, matching this
/// codebase's convention elsewhere (e.g. <c>WageType.Status</c>).</description></item>
/// <item><description>The legacy SalaryRegister bulk-lock cascade (on PeriodLocked/PeriodClosed
/// transitions) is NOT ported — <c>SalaryRegister</c> doesn't exist yet in this port (a later
/// wave).</description></item>
/// <item><description>The legacy commented-out/dead retro-log block (<c>EarliestMasterDataChange</c> reset)
/// is NOT ported (already dead code in the legacy source).</description></item>
/// <item><description>The legacy computed, non-persistent <c>PayrollStatus</c> display string is a pure UI
/// convenience and is NOT ported — it can be derived client-side from
/// <see cref="PeriodLocked"/>/<see cref="PeriodClosed"/>/<see cref="CalendarMonthStart"/> if
/// needed.</description></item>
/// </list>
///
/// The 12-branch fiscal-period-shift calculator (legacy <c>GetStartEndDateFromPayrollPeriod</c>) is ported
/// faithfully as the self-contained, pure static <see cref="PayrollPeriodShiftCalculator"/> helper (no
/// cross-module dependency), consumed by <c>PayrollControlRecordsAppService</c> when computing
/// <see cref="CalendarMonthStart"/>/<see cref="CalendarMonthEnd"/> on create.
/// </summary>
public class PayrollControlRecord : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PayrollAreaId { get; set; }

    public virtual PayrollArea PayrollArea { get; set; }

    public virtual int PayrollMonth { get; set; }

    public virtual int PayrollYear { get; set; }

    public virtual DateTime CalendarMonthStart { get; set; }

    public virtual DateTime CalendarMonthEnd { get; set; }

    /// <summary>Simplified from the legacy PeriodLocked enum (No/Yes) — see class remarks.</summary>
    public virtual bool PeriodLocked { get; set; }

    /// <summary>Simplified from the legacy PeriodClosed enum (No/Yes) — see class remarks.</summary>
    public virtual bool PeriodClosed { get; set; }
}
