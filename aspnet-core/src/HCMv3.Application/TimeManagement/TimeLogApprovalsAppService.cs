using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement.Dto;
using HCMv3.TimeManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// The time log approval screen (ported from legacy <c>HCM10.ESS/Repository/TimeLogRepository.cs</c>).
///
/// <para>
/// <b>This is a manager/HR grid, not self service.</b> Somebody reviews what the time engine calculated for
/// their people and proposes corrections -- forgive this absence, accept fifteen deductible minutes instead
/// of forty. Creates therefore go through <see cref="IAdminTransactionSubmitter"/>, and the catalog row has no
/// ESS route and no ESS permission, exactly as for <c>EmployeeAction</c>.
/// </para>
///
/// <para>
/// <b>Nothing here writes to <see cref="TimeRegister"/>.</b> That sentence is the port. Legacy's submit path
/// (lines 301-340) pushed the corrected figures onto the <c>Approved*</c> columns before the request was even
/// routed, so a corrected timesheet was live before anybody approved it and a rejection did not undo it. The
/// proposed figures go onto <see cref="TimeLogApprovalDetail"/> instead, and
/// <see cref="TimeLogApprovalWorkflowTransactionHandler.OnApprovedAsync"/> is the only code that touches a
/// calculated day.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeLogApprovals)]
public class TimeLogApprovalsAppService : HCMv3AppServiceBase, ITimeLogApprovalsAppService
{
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<TimeLogApproval> _timeLogApprovalRepository;
    private readonly IRepository<TimeLogApprovalDetail> _timeLogApprovalDetailRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public TimeLogApprovalsAppService(
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<TimeLogApproval> timeLogApprovalRepository,
        IRepository<TimeLogApprovalDetail> timeLogApprovalDetailRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _timeRegisterRepository = timeRegisterRepository;
        _timeLogApprovalRepository = timeLogApprovalRepository;
        _timeLogApprovalDetailRepository = timeLogApprovalDetailRepository;
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _transactionSubmitter = transactionSubmitter;
    }

    /// <summary>
    /// The grid (legacy <c>GetTimeRegisterData</c>, lines 30-100). One employee, or everybody in a
    /// department; legacy's seven-way <c>OrgLevel1..7</c> branch collapses to a single
    /// <c>EmployeeOrgAssignment.OrganizationUnitId</c> filter here, because this port has one org unit FK
    /// rather than seven columns.
    ///
    /// <para>
    /// Legacy returned "Records not found!" as a message on a success-shaped payload; an empty list says the
    /// same thing without asking the caller to read prose. The date-order check is kept as a real refusal
    /// because a reversed range silently returns nothing, which looks like "this employee has no timesheet".
    /// </para>
    ///
    /// <para>
    /// <see cref="TimeLogApprovalGridRowDto.IsLocked"/> is computed here rather than left to the handler to
    /// discover at submit: telling somebody a day cannot go forward before they build a batch around it is
    /// worth a second query.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<List<TimeLogApprovalGridRowDto>> GetTimeLogApprovalGrid(GetTimeLogApprovalGridInput input)
    {
        if (input.StartDate.Date > input.EndDate.Date)
        {
            throw new UserFriendlyException(L("StartDateCannotBeGreaterThanEndDate"));
        }

        var employeeIds = await ResolveEmployeeIdsAsync(input);
        if (employeeIds.Count == 0)
        {
            return new List<TimeLogApprovalGridRowDto>();
        }

        var startDate = input.StartDate.Date;
        var endDate = input.EndDate.Date;

        var rows = await _timeRegisterRepository.GetAll()
            .Where(r => employeeIds.Contains(r.EmployeeId)
                        && r.DayDate >= startDate
                        && r.DayDate <= endDate)
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.DayDate)
            .Select(r => new TimeLogApprovalGridRowDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeNameEn = r.Employee.FullNameEn,
                EmployeeNameAr = r.Employee.FullNameAr,
                DayDate = r.DayDate,
                DedLateInEarlyOutMins = r.DedLateInEarlyOutMins,
                ApprovedDedLateInEarlyOutMins = r.ApprovedDedLateInEarlyOutMins,
                AbsentDay = r.AbsentDay != 0,
                ApprovedAbsentDay = r.ApprovedAbsentDay != 0,
                ActualWorkingDay = r.ActualWorkingDay,
                ApprovedActualWorkingDay = r.ApprovedActualWorkingDay,
                Status = r.Status,
                Remarks = r.Remarks
            })
            .ToListAsync();

        if (rows.Count == 0)
        {
            return rows;
        }

        var rowIds = rows.Select(r => r.Id).ToList();

        List<int> inFlightIds;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            inFlightIds = await _timeLogApprovalDetailRepository.GetAll()
                .Where(d => rowIds.Contains(d.TimeRegisterId)
                            && (d.TimeLogApproval.WorkflowStatus == RequestStatus.Draft
                                || d.TimeLogApproval.WorkflowStatus == RequestStatus.Open))
                .Select(d => d.TimeRegisterId)
                .Distinct()
                .ToListAsync();
        }

        foreach (var row in rows)
        {
            row.IsLocked = row.Status == TimeRegisterStatus.Approved || inFlightIds.Contains(row.Id);
        }

        return rows;
    }

    /// <summary>
    /// Puts the selected days forward (legacy <c>SaveTimeLogRequest</c>), MINUS the write to
    /// <see cref="TimeRegister"/> that made legacy's approval step decorative.
    ///
    /// <para>
    /// <b>One header per employee.</b> The selection can span a department, but a workflow request is routed
    /// by the subject employee's own workflow group, so a single header covering two employees in two
    /// departments would have to pick one of their approvers and ignore the other. The days are grouped by
    /// employee and each group becomes its own <see cref="TimeLogApproval"/> and its own request. The
    /// department the grid was filtered by is recorded on each header so the batch is still identifiable as
    /// one action.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeLogApprovals_Create)]
    public async Task<SubmitTimeLogApprovalResultDto> SubmitTimeLogApproval(SubmitTimeLogApprovalDto input)
    {
        if (input.Days == null || input.Days.Count == 0)
        {
            throw new UserFriendlyException(L("TimeLogApprovalHasNoDays"));
        }

        if (input.StartDate.Date > input.EndDate.Date)
        {
            throw new UserFriendlyException(L("StartDateCannotBeGreaterThanEndDate"));
        }

        var tenantId = AbpSession.TenantId ?? 0;

        var requestedIds = input.Days.Select(d => d.TimeRegisterId).Distinct().ToList();

        var timeRegisters = await _timeRegisterRepository.GetAll()
            .Where(r => requestedIds.Contains(r.Id))
            .Select(r => new { r.Id, r.EmployeeId })
            .ToListAsync();

        if (timeRegisters.Count != requestedIds.Count)
        {
            throw new UserFriendlyException(L("TimeRegisterNotFound"));
        }

        var employeeIdByTimeRegisterId = timeRegisters.ToDictionary(r => r.Id, r => r.EmployeeId);

        var result = new SubmitTimeLogApprovalResultDto();

        foreach (var group in input.Days
                     .GroupBy(d => employeeIdByTimeRegisterId[d.TimeRegisterId])
                     .OrderBy(g => g.Key))
        {
            var approval = new TimeLogApproval
            {
                TenantId = tenantId,
                EmployeeId = group.Key,
                OrganizationUnitId = input.OrganizationUnitId,
                StartDate = input.StartDate.Date,
                EndDate = input.EndDate.Date,
                Comments = input.Comments,
                Details = group.Select(d => new TimeLogApprovalDetail
                {
                    TenantId = tenantId,
                    TimeRegisterId = d.TimeRegisterId,
                    ProposedAbsentDay = d.ProposedAbsentDay ? 1 : 0,
                    ProposedDedLateInEarlyOutMins = d.ProposedDedLateInEarlyOutMins,

                    // Shown-to-the-requester figure only. The authoritative number is recomputed at approval
                    // time by the handler, because the engine may recalculate the day in between.
                    ProposedActualWorkingDay = 0
                }).ToList()
            };

            // The details travel with the header so the handler's validation sees them: EF inserts the graph
            // and the submitter saves before it submits.
            var approvalId = await _transactionSubmitter.CreateAndSubmitAsync(
                _timeLogApprovalRepository,
                approval,
                TimeLogApprovalWorkflowTransactionHandler.TimeLogApprovalRequestType,
                "TL");

            result.TimeLogApprovalIds.Add(approvalId);
            result.DayCount += approval.Details.Count;
        }

        return result;
    }

    /// <summary>
    /// Who the grid is about. An explicit employee wins, as in legacy; otherwise everybody assigned to the
    /// department. Legacy applied no date filter to the assignment and neither does this -- somebody who
    /// moved out of the department mid-period still had their days calculated under it.
    /// </summary>
    private async Task<List<int>> ResolveEmployeeIdsAsync(GetTimeLogApprovalGridInput input)
    {
        if (input.EmployeeId.HasValue)
        {
            var exists = await _employeeRepository.GetAll().AnyAsync(e => e.Id == input.EmployeeId.Value);
            if (!exists)
            {
                throw new UserFriendlyException(L("EmployeeNotFound"));
            }

            return new List<int> { input.EmployeeId.Value };
        }

        if (!input.OrganizationUnitId.HasValue)
        {
            throw new UserFriendlyException(L("SelectAnEmployeeOrAnOrganizationUnit"));
        }

        return await _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.OrganizationUnitId == input.OrganizationUnitId.Value)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync();
    }
}
