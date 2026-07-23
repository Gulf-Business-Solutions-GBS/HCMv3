using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="DutyPeriod"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditDutyPeriodDto
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
    public DateTime EndDate { get; set; }

    public int? HolidayCalendarId { get; set; }

    public int? SundayDutyId { get; set; }

    public int? MondayDutyId { get; set; }

    public int? TuesdayDutyId { get; set; }

    public int? WednesdayDutyId { get; set; }

    public int? ThursdayDutyId { get; set; }

    public int? FridayDutyId { get; set; }

    public int? SaturdayDutyId { get; set; }
}
