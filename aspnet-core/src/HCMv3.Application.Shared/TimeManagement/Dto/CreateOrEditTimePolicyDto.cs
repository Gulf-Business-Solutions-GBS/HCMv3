using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TimePolicy"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditTimePolicyDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code.
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public bool ShouldSignIn { get; set; }

    public bool ShouldSignOut { get; set; }

    public bool ShouldBreakSignIn { get; set; }

    public bool ShouldBreakSignOut { get; set; }

    public bool CalcLateInAfterTolerance { get; set; }

    public bool CalcEarlyOutAfterTolerance { get; set; }

    public double LateInTolerance { get; set; }

    public double EarlyOutTolerance { get; set; }

    public bool CalcBreakLateInAfterTolerance { get; set; }

    public bool CalcBreakEarlyOutAfterTolerance { get; set; }

    public double BreakLateInTolerance { get; set; }

    public double BreakEarlyOutTolerance { get; set; }

    public double ClockInLimit { get; set; }

    public double ClockOutLimit { get; set; }

    public double BreakInLimit { get; set; }

    public double BreakOutLimit { get; set; }

    public MonthlyToleranceType MonthlyToleranceType { get; set; }

    public double MonthlyAfterTolerance { get; set; }

    public TimeEventRule TimeEventRule { get; set; }

    public TimeMgmtStatus TimeMgmtStatus { get; set; }

    public double OTMinLimit { get; set; }

    public bool MarkWeekoffOnHoliday { get; set; }

    public bool AllowPunchInFromWorkLocationOnly { get; set; }

    public bool AllowPunchOutFromWorkLocationOnly { get; set; }

    public bool IsFaceRequired { get; set; }

    public bool IsMissedPunchInNotiEnabled { get; set; }

    public double MissedPunchInNotiTimeLimit { get; set; }

    public bool IsLatePunchInNotiEnabled { get; set; }

    public double LatePunchInNotiTimeLimit { get; set; }

    public bool IsMissedPunchOutNotiEnabled { get; set; }

    public double MissedPunchOutNotiTimeLimit { get; set; }

    public bool IsEarlyPunchOutNotiEnabled { get; set; }

    public double EarlyPunchOutNotiTimeLimit { get; set; }

    public bool AllowPunchInFromExtWorklocation { get; set; }

    public bool AllowPunchOutFromExtWorklocation { get; set; }

    public double MonthlyLateInTolerance { get; set; }

    public double MonthlyEarlyOutTolerance { get; set; }
}
