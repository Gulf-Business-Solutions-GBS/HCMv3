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

    [StringLength(4000)]
    public string Comments { get; set; }
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
