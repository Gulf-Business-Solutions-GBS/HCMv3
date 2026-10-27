using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeAttendanceDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int AttendanceTypeId { get; set; }
    public string AttendanceTypeName { get; set; }
    public AttendanceUnit AttendanceUnit { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double AttendanceHours { get; set; }
    public double TotalAttendDays { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeAttendanceDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int AttendanceTypeId { get; set; }

    public AttendanceUnit AttendanceUnit { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public double AttendanceHours { get; set; }

    /// <summary>
    /// Read-only, filled by <c>GetEmployeeAttendanceForEdit</c> for display. Ignored on save: the server always
    /// recalculates it (legacy <c>EmployeeAttendance.UpdateAttendanceQuota</c>, lines 348-381).
    /// </summary>
    public double TotalAttendDays { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }

    /// <summary>
    /// Record this as final instead of sending it for approval.
    ///
    /// <para>
    /// Offered only to a user holding <c>RecordWithoutApproval</c>, and only on a type whose catalog row
    /// permits a bypass. The server checks both and refuses otherwise, so this being true is a request
    /// rather than a decision.
    /// </para>
    /// </summary>
    public bool RecordDirectly { get; set; }
}

public class GetEmployeeAttendanceForEditOutput
{
    public CreateOrEditEmployeeAttendanceDto EmployeeAttendance { get; set; }
}

public class GetAllEmployeeAttendancesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
