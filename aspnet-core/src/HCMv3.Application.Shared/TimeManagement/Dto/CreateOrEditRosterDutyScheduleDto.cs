using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="RosterDutySchedule"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditRosterDutyScheduleDto
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

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public int DutyPeriodId { get; set; }

    [Required]
    public int StartPoint { get; set; }

    public int? HolidayCalendarId { get; set; }
}
