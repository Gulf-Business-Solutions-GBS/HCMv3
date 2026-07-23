using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AttendanceType"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditAttendanceTypeDto
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

    public bool IsPaid { get; set; }

    public bool HaveQuotaAccrual { get; set; }

    public bool IncludeWeekend { get; set; }

    public bool IncludeRestDay { get; set; }

    public bool IncludePublicHoliday { get; set; }

    public bool CalcOnCalendarDays { get; set; }

    public bool IsFullDay { get; set; }

    public int? WageTypeId { get; set; }

    public AccrualPeriod AccrualPeriod { get; set; }

    public int? LinkLeaveQuotaId { get; set; }
}
