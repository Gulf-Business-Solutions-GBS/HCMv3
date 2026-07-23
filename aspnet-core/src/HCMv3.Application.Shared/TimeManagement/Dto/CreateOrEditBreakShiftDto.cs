using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="BreakShift"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditBreakShiftDto
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

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public bool IsPaid { get; set; }

    public bool IsBreakStartNextDay { get; set; }

    public bool IsBreakEndNextDay { get; set; }
}
