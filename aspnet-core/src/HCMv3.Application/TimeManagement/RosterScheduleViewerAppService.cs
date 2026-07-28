using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement.Dto;
using HCMv3.TimeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Read-only viewer service ported from the legacy XAF/WebForms roster scheduling screens.
///
/// <para><b>What the legacy screens actually did (investigation result).</b></para>
///
/// <para><c>uscRosterDutySchedule.ascx.cs</c> (791 lines, of which ~430 are commented-out or DevExpress
/// scheduler plumbing) is a three-pane READ-ONLY viewer:</para>
/// <list type="number">
/// <item><description><c>dgvxRosterDutyPeriod</c> — grid of <see cref="DutyPeriod"/> (Oid/Code/NameEn), bound
/// by <c>BindRosterDuty()</c>. Its "NewRow"/"EditRow" context-menu items call <c>OpenDutyPeriod()</c>, which
/// merely pops the standard XAF DutyPeriod detail view — i.e. the existing
/// <see cref="DutyPeriodsAppService"/> CRUD screen. Nothing is persisted here.</description></item>
/// <item><description><c>dgvxEmployee</c> — grid of employees assigned to the selected duty period, bound by
/// <c>FillEmployeeGrid()</c>. Read only; <c>dgvxEmployee_ContextMenuItemClick</c> has empty
/// <c>NewRow</c>/<c>EditRow</c> bodies.</description></item>
/// <item><description><c>ASPxScheduler1</c> — a DevExpress scheduler bound by <c>BindSchedular()</c> to an
/// in-memory <see cref="System.Data.DataTable"/> built by walking the visible interval day by day and calling
/// <c>RosterService.GetRosterDuty(session, emp.Oid, date, null)</c>.</description></item>
/// </list>
///
/// <para><b>It persists nothing.</b> The scheduler's write path is
/// <c>CustomEventDataSource.InsertMethodHandler/UpdateMethodHandler/DeleteMethodHandler</c>, which mutate a
/// <c>BindingList&lt;CustomEvent&gt;</c> held in <c>Session["CustomEventListData"]</c> — never a database.
/// The custom popup-menu items ("Add Duty Period", "Show Duty Period", "Add Duty Schedule", "Show Duty
/// Schedule") are added in <c>ASPxScheduler1_PopupMenuShowing</c> but no <c>ItemClick</c> handler is ever
/// wired (<c>menu.ItemClick += Menu_ItemClick;</c> is commented out and <c>OnMenuItemClick</c> is an empty
/// method), so those menu items are inert.</para>
///
/// <para><c>uscRosterSchedule.ascx.cs</c> (245 lines) is a 3-input form (Roster Duty Schedule code, Month
/// Start MMyyyy, Month End MMyyyy) plus a "Generate" button. <b>Its entire <c>btnGenerate_Click</c> body —
/// lines 73-243 — is commented out</b>, so the screen is inert dead code and persists nothing. The commented
/// intent was to generate <see cref="RosterDailyDuty"/> rows from
/// <c>RosterDutySchedule.RosterDutyPeriod.RosterDutyPeriodDetail</c> Day1Duty..Day7Duty. That generator is
/// deliberately NOT reconstructed here — see the gap note at the bottom of this file.</para>
///
/// <para><b>Consequently this service exposes only the two read projections that the existing per-entity
/// CRUD app services do not already cover.</b> DutyPeriod listing (pane 1) is already served by
/// <see cref="DutyPeriodsAppService"/> and is not duplicated.</para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterScheduleViewer)]
public class RosterScheduleViewerAppService : HCMv3AppServiceBase, IRosterScheduleViewerAppService
{
    private const int MaxSchedulerDays = 366;
    private const int MaxSchedulerEmployees = 200;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<RosterGroupDetail> _rosterGroupDetailRepository;
    private readonly IRepository<RosterGroupSchedule> _rosterGroupScheduleRepository;
    private readonly IRosterService _rosterService;

    public RosterScheduleViewerAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<RosterGroupDetail> rosterGroupDetailRepository,
        IRepository<RosterGroupSchedule> rosterGroupScheduleRepository,
        IRosterService rosterService)
    {
        _employeeRepository = employeeRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _rosterGroupDetailRepository = rosterGroupDetailRepository;
        _rosterGroupScheduleRepository = rosterGroupScheduleRepository;
        _rosterService = rosterService;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Port of legacy <c>FillEmployeeGrid(int dutyPeriod_Oid)</c>. See
    /// <see cref="RosterDutyScheduleEmployeeDto"/> for the two legacy predicate bugs corrected here and for
    /// the dropped hard-coded <c>AbsentPer = 10</c> column.
    /// </remarks>
    [HttpPost]
    public async Task<PagedResultDto<RosterDutyScheduleEmployeeDto>> GetRosterDutyScheduleEmployees(
        GetRosterDutyScheduleEmployeesInput input)
    {
        input.Normalize();

        var asOf = input.AsOfDate.Date;

        // Branch 1 (legacy "1. Check first in EmployeeDutyPeriod"): employees directly assigned.
        var directEmployeeIds = await _employeeDutyPeriodRepository.GetAll()
            .Where(r => r.DutyPeriodId == input.DutyPeriodId && r.StartDate <= asOf && r.EndDate >= asOf)
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync();

        // Branch 2 (legacy "2. Check in RosterGroupSchedule"): employees reached via roster group.
        var rosterGroupIds = await _rosterGroupScheduleRepository.GetAll()
            .Where(r => r.DutyPeriodId == input.DutyPeriodId && r.StartDate <= asOf && r.EndDate >= asOf)
            .Select(r => r.RosterGroupId)
            .Distinct()
            .ToListAsync();

        var groupMembers = await _rosterGroupDetailRepository.GetAll()
            .Include(r => r.RosterGroup)
            .Where(r => rosterGroupIds.Contains(r.RosterGroupId) && r.StartDate <= asOf && r.EndDate >= asOf)
            .Select(r => new { r.EmployeeId, RosterGroupName = r.RosterGroup.NameEn })
            .ToListAsync();

        // Legacy wrote `ds1.Union(ds2);` and threw the result away, binding only ds1. Intent honoured here.
        var groupMemberById = groupMembers
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().RosterGroupName);

        var allEmployeeIds = directEmployeeIds.Union(groupMemberById.Keys).ToList();
        if (allEmployeeIds.Count == 0)
        {
            return new PagedResultDto<RosterDutyScheduleEmployeeDto>(0, new List<RosterDutyScheduleEmployeeDto>());
        }

        var query = _employeeRepository.GetAll()
            .Where(e => allEmployeeIds.Contains(e.Id))
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                e => e.Code.Contains(input.Filter) || e.FullNameEn.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var employees = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Code" : input.Sorting)
            .PageBy(input)
            .Select(e => new { e.Id, e.Code, e.FullNameEn })
            .ToListAsync();

        var pageEmployeeIds = employees.Select(e => e.Id).ToList();

        // Legacy read Employee.CurrentDepartment / Employee.CurrentWorkLocation (XPO calculated properties
        // that do not exist on the ported Employee entity). Equivalent here: the date-effective org assignment.
        var orgAssignments = await _employeeOrgAssignmentRepository.GetAll()
            .Include(a => a.OrganizationUnit)
            .Include(a => a.WorkLocation)
            .Where(a => pageEmployeeIds.Contains(a.EmployeeId) && a.StartDate <= asOf && a.EndDate >= asOf)
            .Select(a => new
            {
                a.EmployeeId,
                a.StartDate,
                Department = a.OrganizationUnit == null ? null : a.OrganizationUnit.DisplayName,
                Location = a.WorkLocation == null ? null : a.WorkLocation.NameEn
            })
            .ToListAsync();

        var orgByEmployee = orgAssignments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartDate).First());

        var items = employees.Select(e =>
        {
            orgByEmployee.TryGetValue(e.Id, out var org);
            var isGroupMember = groupMemberById.TryGetValue(e.Id, out var rosterGroupName);

            return new RosterDutyScheduleEmployeeDto
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.FullNameEn,
                Department = org?.Department ?? string.Empty,
                Location = org?.Location ?? string.Empty,
                AssignmentSource = directEmployeeIds.Contains(e.Id) ? "EmployeeDutyPeriod" : "RosterGroup",
                RosterGroupName = isGroupMember ? rosterGroupName : null
            };
        }).ToList();

        return new PagedResultDto<RosterDutyScheduleEmployeeDto>(totalCount, items);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Port of legacy <c>BindSchedular()</c>. Legacy derived the date range from
    /// <c>ASPxScheduler1.ActiveView.GetVisibleIntervals().Interval</c>; the client now supplies it explicitly.
    ///
    /// Legacy also contained a genuine infinite-loop bug: its <c>while (Act_startDate &lt;= Act_endDate)</c>
    /// advanced <c>Act_startDate</c> only INSIDE <c>if (duty != null)</c>, so the first day with no resolvable
    /// duty hung the request forever. The day cursor here advances unconditionally.
    ///
    /// Legacy built its resource list from an <c>ArrayList arrayList</c> that was never populated (the
    /// <c>lstEmployee</c> loop that filled it is commented out), so the scheduler always rendered empty in
    /// practice. This port takes the selected employee ids from the caller.
    ///
    /// Duty resolution is delegated wholly to <see cref="IRosterService.GetRosterDutyAsync"/> — the same
    /// legacy <c>RosterService.GetRosterDuty</c> entry point, already ported.
    /// </remarks>
    [HttpPost]
    public async Task<GetRosterDutyScheduleOutput> GetRosterDutySchedule(GetRosterDutyScheduleInput input)
    {
        input.Normalize();

        if (input.EmployeeIds.Count == 0)
        {
            return new GetRosterDutyScheduleOutput();
        }

        if (input.EndDate < input.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        if (input.EmployeeIds.Count > MaxSchedulerEmployees)
        {
            throw new UserFriendlyException(L("RosterScheduleTooManyEmployees", MaxSchedulerEmployees));
        }

        var dayCount = (int)(input.EndDate - input.StartDate).TotalDays + 1;
        if (dayCount > MaxSchedulerDays)
        {
            throw new UserFriendlyException(L("RosterScheduleDateRangeTooLarge", MaxSchedulerDays));
        }

        var employeeIds = input.EmployeeIds.Distinct().ToList();

        var resources = await _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .OrderBy(e => e.Code)
            .Select(e => new RosterDutyScheduleResourceDto
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.FullNameEn
            })
            .ToListAsync();

        var days = new List<RosterDutyScheduleDayDto>(resources.Count * dayCount);

        foreach (var resource in resources)
        {
            for (var date = input.StartDate; date <= input.EndDate; date = date.AddDays(1))
            {
                days.Add(await ResolveDayAsync(resource.Id, date));
            }
        }

        return new GetRosterDutyScheduleOutput
        {
            Resources = resources,
            Days = days
        };
    }

    private async Task<RosterDutyScheduleDayDto> ResolveDayAsync(int employeeId, DateTime date)
    {
        var cell = new RosterDutyScheduleDayDto
        {
            EmployeeId = employeeId,
            DayDate = date
        };

        try
        {
            // throwException: false — one misconfigured duty period must not blank the whole grid.
            var duty = await _rosterService.GetRosterDutyAsync(employeeId, date, null, false);
            if (duty == null)
            {
                return cell;
            }

            cell.DutyId = duty.Id;
            cell.DutyCode = duty.Code;
            cell.DutyName = duty.NameEn;
            cell.StartTime = duty.StartTime;
            cell.EndTime = duty.EndTime;
            cell.IsWeeklyOff = duty.IsWeeklyOff;
            cell.IsRestDay = duty.IsRestDay;
            cell.IsNightShift = duty.IsNightShift;
            cell.IsHalfDay = duty.IsHalfDay;
        }
        catch (Exception ex)
        {
            // RosterService still throws unconditionally for "no week information filled" even when
            // throwException is false (see RosterService.ResolveRotationDuty).
            cell.ErrorMessage = ex.Message;
        }

        return cell;
    }
}

// ---------------------------------------------------------------------------------------------------------
// EXPLICIT GAP — legacy uscRosterSchedule.ascx.cs "Generate" (RosterDailyDuty generator).
//
// Not ported. There is no behaviour to port: the whole body of btnGenerate_Click is commented out in
// C:\GBS\HCM\HRMS_Production\HCM10.Web\UserControls\TimeManagement\uscRosterSchedule.ascx.cs, lines 73-243.
// It was never live code in the legacy application.
//
// Beyond being dead, the commented draft is not type-compatible with the ported HCMv3 model, so
// reconstructing it would require inventing schema:
//   * It reads rosterDutySchedule.RosterDutyPeriod.RosterDutyPeriodDetail. The ported
//     HCMv3.TimeManagement.RosterDutySchedule has NO RosterDutyPeriod navigation — it carries DutyPeriodId
//     pointing at the CLASSIC DutyPeriod (a legacy naming inconsistency already documented in
//     RosterDutySchedulesAppService).
//   * It assigns rosterDailyDuty.DailyDuty = <...>.Day1Duty..Day7Duty. The ported RosterDailyDuty.DailyDutyId
//     targets DailyDuty, whereas DutyPeriodDetail.Day1DutyId..Day7DutyId target Duty. Different types.
//   * Its rotation arithmetic (the 5x repeated newDutyDay blocks) is a strictly less capable copy of the
//     formula already ported, correctly and generally, in
//     HCMv3.Core/TimeManagement/Services/RosterService.ResolveRotationDuty.
//
// Reviving it needs a product decision on the RosterDutySchedule -> RosterDutyPeriod FK and on the
// Duty/DailyDuty mismatch. Deliberately left as a loud gap rather than a plausible-looking guess.
// ---------------------------------------------------------------------------------------------------------
