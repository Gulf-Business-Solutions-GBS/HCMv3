using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Time Policy master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.TimePolicy</c>).
/// Large flat configuration entity with no FK relations — every scalar field is ported faithfully since
/// this is pure configuration (no business-logic simplification needed). The legacy <c>IExcelImport</c>
/// marker and the <c>TimePolicyCopy</c> "copy wizard" DTO are XAF UI concepts and are not ported.
/// </summary>
public class TimePolicy : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual bool ShouldSignIn { get; set; }

    public virtual bool ShouldSignOut { get; set; }

    public virtual bool ShouldBreakSignIn { get; set; }

    public virtual bool ShouldBreakSignOut { get; set; }

    public virtual bool CalcLateInAfterTolerance { get; set; }

    public virtual bool CalcEarlyOutAfterTolerance { get; set; }

    public virtual double LateInTolerance { get; set; }

    public virtual double EarlyOutTolerance { get; set; }

    public virtual bool CalcBreakLateInAfterTolerance { get; set; }

    public virtual bool CalcBreakEarlyOutAfterTolerance { get; set; }

    public virtual double BreakLateInTolerance { get; set; }

    public virtual double BreakEarlyOutTolerance { get; set; }

    /// <summary>
    /// Time limit to fetch all In Transactions, otherwise it's impossible to know which IN transaction is
    /// for the actual duty start or the start after break finishes.
    /// </summary>
    public virtual double ClockInLimit { get; set; }

    /// <summary>
    /// Time limit to fetch all Out Transactions.
    /// </summary>
    public virtual double ClockOutLimit { get; set; }

    /// <summary>
    /// Time limit to fetch all Break In Transactions.
    /// </summary>
    public virtual double BreakInLimit { get; set; }

    /// <summary>
    /// Time limit to fetch all Break Out Transactions.
    /// </summary>
    public virtual double BreakOutLimit { get; set; }

    public virtual MonthlyToleranceType MonthlyToleranceType { get; set; }

    public virtual double MonthlyAfterTolerance { get; set; }

    public virtual TimeEventRule TimeEventRule { get; set; }

    public virtual TimeMgmtStatus TimeMgmtStatus { get; set; }

    /// <summary>
    /// Time limit to tolerate overtime.
    /// </summary>
    public virtual double OTMinLimit { get; set; }

    /// <summary>
    /// Whether to consider week-off over holiday (legacy: "To Consider Weekoff over Holiday for (LINADINA)").
    /// </summary>
    public virtual bool MarkWeekoffOnHoliday { get; set; }

    public virtual bool AllowPunchInFromWorkLocationOnly { get; set; }

    public virtual bool AllowPunchOutFromWorkLocationOnly { get; set; }

    /// <summary>
    /// Whether face recognition is required.
    /// </summary>
    public virtual bool IsFaceRequired { get; set; }

    public virtual bool IsMissedPunchInNotiEnabled { get; set; }

    public virtual double MissedPunchInNotiTimeLimit { get; set; }

    public virtual bool IsLatePunchInNotiEnabled { get; set; }

    public virtual double LatePunchInNotiTimeLimit { get; set; }

    public virtual bool IsMissedPunchOutNotiEnabled { get; set; }

    public virtual double MissedPunchOutNotiTimeLimit { get; set; }

    public virtual bool IsEarlyPunchOutNotiEnabled { get; set; }

    public virtual double EarlyPunchOutNotiTimeLimit { get; set; }

    public virtual bool AllowPunchInFromExtWorklocation { get; set; }

    public virtual bool AllowPunchOutFromExtWorklocation { get; set; }

    public virtual double MonthlyLateInTolerance { get; set; }

    public virtual double MonthlyEarlyOutTolerance { get; set; }
}
