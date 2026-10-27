using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The four things the Employee Workbench needs that no per-employee service already answers: a search
/// that can leave ex-employees out, the header strip, leaves not returned from, and the last week's
/// attendance with missing punches marked.
///
/// READ ONLY, ON PURPOSE. Every change made from the workbench goes through the existing form and service
/// of the record being changed, so a rule enforced on the Employee record screen cannot be bypassed here.
///
/// PERMISSIONS: the class permission opens the screen, search and header. The leave and attendance feeds
/// also require the permission of the records they read, exactly as the tiles that show them do.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkbench)]
public class EmployeeWorkbenchAppService : HCMv3AppServiceBase, IEmployeeWorkbenchAppService
{
    /// <summary>Today and the six days before it.</summary>
    public const int RecentAttendanceDays = 7;

    /// <summary>
    /// The time engine writes <c>default(DateTime)</c> for "no punch" and migrated legacy rows carry
    /// 1800-01-01, so anything at or before this year is treated as no punch at all.
    /// </summary>
    private const int NoPunchYearCeiling = 1900;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeManager> _managerRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;

    public EmployeeWorkbenchAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeManager> managerRepository,
        IRepository<EmployeeSeparation> separationRepository,
        IRepository<EmployeeAbsence> absenceRepository,
        IRepository<TimeRegister> timeRegisterRepository)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _managerRepository = managerRepository;
        _separationRepository = separationRepository;
        _absenceRepository = absenceRepository;
        _timeRegisterRepository = timeRegisterRepository;
    }

    /// <summary>
    /// <c>[HttpPost]</c> because ABP maps <c>Get*</c>/<c>Search*</c> inputs to query strings otherwise and
    /// NSwag would then never generate the input class for Angular.
    /// </summary>
    [HttpPost]
    public async Task<ListResultDto<WorkbenchEmployeeSearchItemDto>> SearchEmployees(SearchWorkbenchEmployeesInput input)
    {
        var filter = input.Filter.Trim();

        // A civil ID is a number, so it can only ever be matched exactly -- a "contains" on a number
        // column is neither possible nor wanted.
        var hasCivilId = long.TryParse(filter, out var civilId);

        var items = await _employeeRepository.GetAll()
            .Where(e => input.IncludeExEmployees || e.Status == EmployeeStatus.Active)
            .Where(e => e.Code.Contains(filter)
                        || e.FullNameEn.Contains(filter)
                        || e.FullNameAr.Contains(filter)
                        || (hasCivilId && e.CivilID == civilId))
            // Current staff first, so an ex-employee never pushes a current one out of the list.
            .OrderByDescending(e => e.Status)
            .ThenBy(e => e.Code)
            .Take(input.MaxResultCount)
            .Select(e => new WorkbenchEmployeeSearchItemDto
            {
                Id = e.Id,
                Code = e.Code,
                FullNameEn = e.FullNameEn,
                FullNameAr = e.FullNameAr,
                Status = e.Status
            })
            .ToListAsync();

        return new ListResultDto<WorkbenchEmployeeSearchItemDto>(items);
    }

    public async Task<EmployeeWorkbenchHeaderDto> GetHeader(EntityDto input)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(input.Id);
        if (employee == null)
        {
            throw new UserFriendlyException(L("EmployeeNotFound"));
        }

        var today = Clock.Now.Date;

        // "Current" is the latest row already started. A leaver's rows have all ended, and a new joiner's
        // may all start tomorrow, so when nothing has started yet the latest row is still the best answer.
        var assignment = await _orgAssignmentRepository.GetAll()
            .Include(a => a.JobTitle)
            .Include(a => a.Position)
            .Include(a => a.OrganizationUnit)
            .Include(a => a.PayrollArea)
            .Where(a => a.EmployeeId == employee.Id)
            .OrderByDescending(a => a.StartDate <= today)
            .ThenByDescending(a => a.StartDate)
            .FirstOrDefaultAsync();

        var manager = await _managerRepository.GetAll()
            .Include(m => m.Manager)
            .Where(m => m.EmployeeId == employee.Id)
            .OrderByDescending(m => m.StartDate <= today)
            .ThenByDescending(m => m.StartDate)
            .Select(m => m.Manager)
            .FirstOrDefaultAsync();

        var lastDayInService = await _separationRepository.GetAll()
            .Where(s => s.EmployeeId == employee.Id && s.RevokeDate == null)
            .OrderByDescending(s => s.LastDayInService)
            .Select(s => (DateTime?)s.LastDayInService)
            .FirstOrDefaultAsync();

        return new EmployeeWorkbenchHeaderDto
        {
            Id = employee.Id,
            Code = employee.Code,
            FullNameEn = employee.FullNameEn,
            FullNameAr = employee.FullNameAr,
            Status = employee.Status,
            JoiningDate = employee.JoiningDate,
            JobTitleNameEn = assignment?.JobTitle?.NameEn,
            JobTitleNameAr = assignment?.JobTitle?.NameAr,
            PositionNameEn = assignment?.Position?.NameEn,
            PositionNameAr = assignment?.Position?.NameAr,
            OrganizationUnitName = assignment?.OrganizationUnit?.DisplayName,
            PayrollAreaName = assignment?.PayrollArea?.Name,
            ManagerFullNameEn = manager?.FullNameEn,
            ManagerFullNameAr = manager?.FullNameAr,
            LastDayInService = lastDayInService
        };
    }

    /// <summary>
    /// The payroll engine's own not-returned rule (<c>PayrollEngineService.ResolveNotReturnFromLeaveAbsence</c>,
    /// legacy line 906): a whole-day leave of a type that requires a return date, ended, with no return
    /// recorded. Using the same rule means the workbench lists exactly the leaves payroll will treat as
    /// not returned. Only effective leave is read (the default approved-only filter stays on): a leave
    /// still waiting for approval has not started, so nobody can be late back from it.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences)]
    public async Task<ListResultDto<WorkbenchNotReturnedLeaveDto>> GetNotReturnedLeaves(EntityDto input)
    {
        var today = Clock.Now.Date;

        var leaves = await _absenceRepository.GetAll()
            .Where(a => a.EmployeeId == input.Id
                        && a.AbsenceType != null
                        && a.AbsenceType.IsReturnDateRequired
                        && a.ReturnDate == default(DateTime)
                        && !a.IsReturned
                        && a.EndDate < today
                        && a.AbsenceHours == 0)
            .OrderByDescending(a => a.StartDate)
            .Select(a => new
            {
                a.Id,
                a.AbsenceType.NameEn,
                a.AbsenceType.NameAr,
                a.StartDate,
                a.EndDate
            })
            .ToListAsync();

        var items = leaves
            .Select(a => new WorkbenchNotReturnedLeaveDto
            {
                Id = a.Id,
                AbsenceTypeNameEn = a.NameEn,
                AbsenceTypeNameAr = a.NameAr,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                DaysOverdue = (today - a.EndDate.Date).Days
            })
            .ToList();

        return new ListResultDto<WorkbenchNotReturnedLeaveDto>(items);
    }

    /// <summary>
    /// The last seven days as the time engine evaluated them. A day the engine has not run for yet simply
    /// is not listed; this reads results, it never evaluates.
    ///
    /// A missing punch is only ever flagged on a working day, and never "missing out" for today: someone
    /// still at their desk has not missed anything yet.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeRegisters)]
    public async Task<ListResultDto<WorkbenchAttendanceDayDto>> GetRecentAttendance(EntityDto input)
    {
        var today = Clock.Now.Date;
        var from = today.AddDays(-(RecentAttendanceDays - 1));
        var to = today.AddDays(1);

        var days = await _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == input.Id && r.DayDate >= from && r.DayDate < to)
            .OrderByDescending(r => r.DayDate)
            .Select(r => new
            {
                r.DayDate,
                r.ActualIn,
                r.ActualOut,
                r.PlannedWorkingDay,
                r.LeaveDay,
                r.PublicHoliday,
                r.WeeklyOff,
                r.RestDay,
                r.AbsentDay
            })
            .ToListAsync();

        var items = new List<WorkbenchAttendanceDayDto>(days.Count);
        foreach (var day in days)
        {
            var actualIn = AsPunch(day.ActualIn);
            var actualOut = AsPunch(day.ActualOut);
            var isOnLeave = day.LeaveDay > 0;
            var isOff = day.PublicHoliday > 0 || day.WeeklyOff > 0 || day.RestDay > 0;
            var isWorkingDay = day.PlannedWorkingDay > 0 && !isOnLeave && !isOff;

            items.Add(new WorkbenchAttendanceDayDto
            {
                DayDate = day.DayDate,
                ActualIn = actualIn,
                ActualOut = actualOut,
                IsWorkingDay = isWorkingDay,
                IsOnLeave = isOnLeave,
                IsAbsent = day.AbsentDay > 0,
                IsMissingIn = isWorkingDay && actualIn == null && actualOut != null,
                IsMissingOut = isWorkingDay && actualIn != null && actualOut == null && day.DayDate.Date < today
            });
        }

        return new ListResultDto<WorkbenchAttendanceDayDto>(items);
    }

    private static DateTime? AsPunch(DateTime value)
    {
        return value.Year <= NoPunchYearCeiling ? null : value;
    }
}
