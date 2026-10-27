using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.Dto;

public class SearchWorkbenchEmployeesInput
{
    public const int DefaultMaxResultCount = 20;
    public const int MaxMaxResultCount = 50;

    /// <summary>Employee code, English or Arabic name, or civil ID.</summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Filter { get; set; }

    /// <summary>
    /// Off by default: the workbench is mostly used on current staff, and a leaver who shares a name with a
    /// current employee is exactly the wrong person to pick by accident.
    /// </summary>
    public bool IncludeExEmployees { get; set; }

    [Range(1, MaxMaxResultCount)]
    public int MaxResultCount { get; set; } = DefaultMaxResultCount;
}

public class WorkbenchEmployeeSearchItemDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    public EmployeeStatus Status { get; set; }
}

/// <summary>The strip above the tiles: enough to be sure this is the right person before changing anything.</summary>
public class EmployeeWorkbenchHeaderDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    public EmployeeStatus Status { get; set; }

    public DateTime JoiningDate { get; set; }

    public string JobTitleNameEn { get; set; }

    public string JobTitleNameAr { get; set; }

    public string PositionNameEn { get; set; }

    public string PositionNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public string PayrollAreaName { get; set; }

    public string ManagerFullNameEn { get; set; }

    public string ManagerFullNameAr { get; set; }

    /// <summary>The last day of the latest separation that was not revoked; null when there is none.</summary>
    public DateTime? LastDayInService { get; set; }
}

public class WorkbenchNotReturnedLeaveDto
{
    /// <summary>The <c>EmployeeAbsence</c> id, so the screen can open the leave itself to record the return.</summary>
    public int Id { get; set; }

    public string AbsenceTypeNameEn { get; set; }

    public string AbsenceTypeNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Days since the leave ended, counting from the day after <see cref="EndDate"/>.</summary>
    public int DaysOverdue { get; set; }
}

public class WorkbenchAttendanceDayDto
{
    public DateTime DayDate { get; set; }

    /// <summary>Null when there was no in punch that day.</summary>
    public DateTime? ActualIn { get; set; }

    /// <summary>Null when there was no out punch that day.</summary>
    public DateTime? ActualOut { get; set; }

    /// <summary>A planned working day that is not leave, a public holiday, a weekly off or a rest day.</summary>
    public bool IsWorkingDay { get; set; }

    public bool IsOnLeave { get; set; }

    public bool IsAbsent { get; set; }

    /// <summary>A working day with an out punch and no in punch.</summary>
    public bool IsMissingIn { get; set; }

    /// <summary>A working day with an in punch and no out punch.</summary>
    public bool IsMissingOut { get; set; }
}

public class GetWorkbenchPayslipPdfInput
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [Range(1900, 2999)]
    public int Year { get; set; }

    public PayrollRunType PayrollRunType { get; set; } = PayrollRunType.Regular;
}
