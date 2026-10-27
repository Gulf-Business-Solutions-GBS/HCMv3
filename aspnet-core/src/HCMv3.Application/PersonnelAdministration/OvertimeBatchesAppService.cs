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
using Abp.Organizations;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.TimeManagement;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The department overtime batch screen (ported from legacy <c>Workflow.Payroll.OvertimeRequest</c> and the
/// <c>HCM10.ESS</c> <c>OverTimeController</c> / <c>OvertimeRequestRepository</c>).
///
/// <para>
/// <b>A manager/HR screen, not self service.</b> Legacy raised one request per department and date range,
/// recorded against the logged-in employee, with one line per employee per day. The same here: creates go
/// through <see cref="IAdminTransactionSubmitter.CreateAndSubmitForSubjectAsync{TEntity}"/> with the requester
/// as the subject, the catalog row has no ESS route, and approval -- not this service -- writes the
/// <see cref="EmployeeOvertime"/> rows (<see cref="OvertimeBatchWorkflowTransactionHandler"/>).
/// </para>
///
/// <para>
/// <b>Which legacy loader was ported.</b> Legacy had two: OT-In/OT-Out punches (time-event codes 3/4) and the
/// shift report (<c>GetOvertimeFromShiftReport</c>). The submit path hard-coded <c>var byShift = true;</c>, so
/// the shift report is the one that wrote lines, and that is what this ports -- from the time register, which
/// is what the shift report itself read. Shift start/end, sign in/out, planned hours, overtime hours, late in,
/// early out and the overtime type are snapshotted onto each line.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_OvertimeBatches)]
public class OvertimeBatchesAppService : HCMv3AppServiceBase, IOvertimeBatchesAppService
{
    private const string RequestNumberPrefix = "OB";

    private readonly IRepository<OvertimeBatch> _batchRepository;
    private readonly IRepository<OvertimeBatchDetail> _detailRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IOvertimeBatchService _overtimeBatchService;
    private readonly IEmployeeOvertimeService _employeeOvertimeService;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowApprovalService _workflowApprovalService;

    public OvertimeBatchesAppService(
        IRepository<OvertimeBatch> batchRepository,
        IRepository<OvertimeBatchDetail> detailRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<WorkflowRequest> requestRepository,
        IOvertimeBatchService overtimeBatchService,
        IEmployeeOvertimeService employeeOvertimeService,
        IEmployeeSessionService employeeSessionService,
        IAdminTransactionSubmitter transactionSubmitter,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
    {
        _batchRepository = batchRepository;
        _detailRepository = detailRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _overtimeTypeRepository = overtimeTypeRepository;
        _requestRepository = requestRepository;
        _overtimeBatchService = overtimeBatchService;
        _employeeOvertimeService = employeeOvertimeService;
        _employeeSessionService = employeeSessionService;
        _transactionSubmitter = transactionSubmitter;
        _requestMasterResolver = requestMasterResolver;
        _workflowApprovalService = workflowApprovalService;
    }

    /// <summary>
    /// The batch list, with each batch's status. <c>[HttpPost]</c> because the input is a custom DTO. The
    /// <c>ApprovedOnly</c> filter is off for this query only: this is where a batch is watched through approval.
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<OvertimeBatchListDto>> GetAll(GetAllOvertimeBatchesInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var lines = _detailRepository.GetAll();
            var requests = _requestRepository.GetAll();

            var query = _batchRepository.GetAll()
                .WhereIf(input.OrganizationUnitId.HasValue, b => b.OrganizationUnitId == input.OrganizationUnitId.Value)
                .WhereIf(input.WorkflowStatus.HasValue, b => b.WorkflowStatus == input.WorkflowStatus.Value)
                .WhereIf(input.FromDate.HasValue, b => b.EndDate >= input.FromDate.Value)
                .WhereIf(input.ToDate.HasValue, b => b.StartDate <= input.ToDate.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .Select(b => new OvertimeBatchListDto
                {
                    Id = b.Id,
                    OrganizationUnitId = b.OrganizationUnitId,
                    OrganizationUnitName = b.OrganizationUnit.DisplayName,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Comments = b.Comments,
                    WorkflowStatus = b.WorkflowStatus,
                    WorkflowRequestId = b.WorkflowRequestId,
                    RequestNumber = requests
                        .Where(r => r.Id == b.WorkflowRequestId)
                        .Select(r => r.RequestNumber)
                        .FirstOrDefault(),
                    LineCount = lines.Count(d => d.OvertimeBatchId == b.Id),
                    TotalApprovedHours = lines
                        .Where(d => d.OvertimeBatchId == b.Id)
                        .Sum(d => (double?)d.ApprovedHours) ?? 0,
                    IsPosted = lines.Any(d => d.OvertimeBatchId == b.Id && d.EmployeeOvertimeId != null)
                })
                .ToListAsync();

            return new PagedResultDto<OvertimeBatchListDto>(totalCount, items);
        }
    }

    /// <summary>One batch with its lines. Requires an id; never returns an empty shell for "new".</summary>
    public async Task<GetOvertimeBatchForEditOutput> GetOvertimeBatchForEdit(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var batch = await LoadBatchAsync(input.Id);

            var details = await _detailRepository.GetAll()
                .Where(d => d.OvertimeBatchId == batch.Id)
                .OrderBy(d => d.Employee.Code)
                .ThenBy(d => d.Date)
                .Select(d => new OvertimeBatchLineDto
                {
                    Id = d.Id,
                    TimeRegisterId = d.TimeRegisterId,
                    EmployeeId = d.EmployeeId,
                    EmployeeCode = d.Employee.Code,
                    EmployeeNameEn = d.Employee.FullNameEn,
                    EmployeeNameAr = d.Employee.FullNameAr,
                    Date = d.Date,
                    ShiftStart = d.ShiftStart,
                    ShiftEnd = d.ShiftEnd,
                    SignIn = d.SignIn,
                    SignOut = d.SignOut,
                    PlannedHours = d.PlannedHours,
                    TotalHours = d.TotalHours,
                    LateIn = d.LateIn,
                    EarlyOut = d.EarlyOut,
                    ApprovedHours = d.ApprovedHours,
                    OvertimeTypeId = d.OvertimeTypeId,
                    OvertimeTypeNameEn = d.OvertimeType.NameEn,
                    OvertimeTypeNameAr = d.OvertimeType.NameAr,
                    Comments = d.Comments,
                    EmployeeOvertimeId = d.EmployeeOvertimeId
                })
                .ToListAsync();

            var isPosted = details.Any(d => d.EmployeeOvertimeId.HasValue);

            var unitName = await _organizationUnitRepository.GetAll()
                .Where(u => u.Id == batch.OrganizationUnitId)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync();

            var requestNumber = batch.WorkflowRequestId.HasValue
                ? await _requestRepository.GetAll()
                    .Where(r => r.Id == batch.WorkflowRequestId.Value)
                    .Select(r => r.RequestNumber)
                    .FirstOrDefaultAsync()
                : null;

            return new GetOvertimeBatchForEditOutput
            {
                OvertimeBatch = new CreateOrEditOvertimeBatchDto
                {
                    Id = batch.Id,
                    OrganizationUnitId = batch.OrganizationUnitId,
                    StartDate = batch.StartDate,
                    EndDate = batch.EndDate,
                    Comments = batch.Comments,
                    Details = details
                        .Where(d => d.TimeRegisterId.HasValue)
                        .Select(d => new CreateOrEditOvertimeBatchLineDto
                        {
                            TimeRegisterId = d.TimeRegisterId.Value,
                            ApprovedHours = d.ApprovedHours,
                            OvertimeTypeId = d.OvertimeTypeId,
                            Comments = d.Comments
                        })
                        .ToList()
                },
                Details = details,
                OrganizationUnitName = unitName,
                WorkflowStatus = batch.WorkflowStatus,
                RequestNumber = requestNumber,
                IsEditable = IsEditable(batch, isPosted),
                CanResubmit = CanResubmit(batch)
            };
        }
    }

    /// <inheritdoc/>
    public async Task<List<OvertimeBatchOrganizationUnitDto>> GetOrganizationUnits()
    {
        // Ordered by Code, which is the tree order, so a flat list still reads top-down.
        return await _organizationUnitRepository.GetAll()
            .OrderBy(u => u.Code)
            .Select(u => new OvertimeBatchOrganizationUnitDto
            {
                Id = u.Id,
                Code = u.Code,
                DisplayName = u.DisplayName
            })
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<OvertimeBatchOvertimeTypeDto>> GetOvertimeTypes()
    {
        return await _overtimeTypeRepository.GetAll()
            .OrderBy(t => t.Code)
            .Select(t => new OvertimeBatchOvertimeTypeDto
            {
                Id = t.Id,
                Code = t.Code,
                NameEn = t.NameEn,
                NameAr = t.NameAr
            })
            .ToListAsync();
    }

    /// <summary>
    /// The grid (legacy <c>GetTimeEventGrid</c> -> <c>GetOvertimeFromShiftReport</c>).
    ///
    /// <para>
    /// Who: every ACTIVE employee assigned to the unit or to any unit below it. Legacy matched the chosen unit
    /// against all seven of the assignment's <c>OrgLevel1..7</c> columns, which is "this unit or anything
    /// under it"; this port's single <c>OrganizationUnitId</c> plus the tree code says the same. No date filter
    /// on the assignment, as in legacy.
    /// </para>
    ///
    /// <para>
    /// Which days: with <see cref="GetOvertimeBatchCandidatesInput.OnlyOvertimeDays"/> (legacy
    /// <c>AllowOvertimeHoursOnly</c>) only days the time register calculated overtime on. A day already on a
    /// batch that still stands is left out, exactly as legacy left out days already requested. A day that
    /// already has an overtime row is shown but flagged, because putting it forward would fail.
    /// </para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(
        AppPermissions.Pages_PersonnelAdministration_OvertimeBatches_Create,
        AppPermissions.Pages_PersonnelAdministration_OvertimeBatches_Edit)]
    public async Task<List<OvertimeBatchLineDto>> GetOvertimeBatchCandidates(GetOvertimeBatchCandidatesInput input)
    {
        ValidateDates(input.StartDate, input.EndDate);

        var employeeIds = await GetEmployeeIdsInUnitAsync(input.OrganizationUnitId);
        if (employeeIds.Count == 0)
        {
            return new List<OvertimeBatchLineDto>();
        }

        var start = input.StartDate.Date;
        var end = input.EndDate.Date;

        var rows = await _timeRegisterRepository.GetAll()
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.DayDate >= start && r.DayDate <= end)
            .WhereIf(input.OnlyOvertimeDays, r => r.OvertimeMinutes > 0)
            .OrderBy(r => r.Employee.Code)
            .ThenBy(r => r.DayDate)
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                EmployeeCode = r.Employee.Code,
                EmployeeNameEn = r.Employee.FullNameEn,
                EmployeeNameAr = r.Employee.FullNameAr,
                r.DayDate,
                r.ShiftStartTime1,
                r.ShiftEndTime1,
                r.ActualIn,
                r.ActualOut,
                r.PlannedWorkingMins,
                r.OvertimeMinutes,
                r.LateIn,
                r.EarlyOut,
                r.OvertimeTypeId,
                OvertimeTypeNameEn = r.OvertimeType.NameEn,
                OvertimeTypeNameAr = r.OvertimeType.NameAr
            })
            .ToListAsync();

        var claimed = await _overtimeBatchService.GetClaimedDaysAsync(
            employeeIds, start, end, input.ExcludeOvertimeBatchId ?? 0);

        var recorded = await _employeeOvertimeService.GetDaysWithOvertimeAsync(employeeIds, start, end);

        return rows
            .Where(r => !claimed.Contains((r.EmployeeId, r.DayDate.Date)))
            .Select(r => new OvertimeBatchLineDto
            {
                TimeRegisterId = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeCode = r.EmployeeCode,
                EmployeeNameEn = r.EmployeeNameEn,
                EmployeeNameAr = r.EmployeeNameAr,
                Date = r.DayDate,
                ShiftStart = TimeOrNull(r.ShiftStartTime1),
                ShiftEnd = TimeOrNull(r.ShiftEndTime1),
                SignIn = TimeOrNull(r.ActualIn),
                SignOut = TimeOrNull(r.ActualOut),
                PlannedHours = r.PlannedWorkingMins / 60,
                TotalHours = r.OvertimeMinutes / 60,
                LateIn = r.LateIn,
                EarlyOut = r.EarlyOut,

                // Legacy shift mode started every row at zero (ApprovedHours = TimeSpan.Zero): approved hours
                // are what the requester decides, and an untouched grid must not look like a decision.
                ApprovedHours = 0,

                OvertimeTypeId = r.OvertimeTypeId,
                OvertimeTypeNameEn = r.OvertimeTypeNameEn,
                OvertimeTypeNameAr = r.OvertimeTypeNameAr,
                HasExistingOvertime = recorded.Contains((r.EmployeeId, r.DayDate.Date))
            })
            .ToList();
    }

    /// <summary>
    /// Saves the batch and submits it (legacy <c>OvertimeRequestSubmit</c>, new-request branch).
    ///
    /// <para>
    /// When the type is not workflow-enabled the batch is saved as effective and its overtime rows are written
    /// at once -- otherwise a batch raised with workflow off would never produce any overtime at all.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_OvertimeBatches_Create)]
    public async Task<int> CreateOvertimeBatch(CreateOrEditOvertimeBatchDto input)
    {
        var tenantId = AbpSession.TenantId ?? 0;

        ValidateDates(input.StartDate, input.EndDate);

        var batch = new OvertimeBatch
        {
            TenantId = tenantId,
            OrganizationUnitId = input.OrganizationUnitId,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            Comments = input.Comments
        };

        var lines = await BuildLinesAsync(input, tenantId);
        await _overtimeBatchService.EnsureLinesAreValidAsync(batch, lines);

        batch.Details = lines;

        var subjectEmployeeId = await GetRequesterEmployeeIdAsync();

        var batchId = await _transactionSubmitter.CreateAndSubmitForSubjectAsync(
            _batchRepository,
            batch,
            OvertimeBatchWorkflowTransactionHandler.OvertimeBatchRequestType,
            RequestNumberPrefix,
            subjectEmployeeId);

        if (batch.WorkflowStatus == null || batch.WorkflowStatus == RequestStatus.Closed)
        {
            await _overtimeBatchService.PostOvertimesAsync(batch);
        }

        return batchId;
    }

    /// <summary>
    /// Rewrites a batch nobody is being asked about (sent back, or never routed and not yet posted). Lines are
    /// replaced wholesale; they carry no state of their own until approval posts them.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_OvertimeBatches_Edit)]
    public async Task UpdateOvertimeBatch(CreateOrEditOvertimeBatchDto input)
    {
        if (!input.Id.HasValue)
        {
            throw new UserFriendlyException(L("OvertimeBatchNotFound"));
        }

        var tenantId = AbpSession.TenantId ?? 0;

        ValidateDates(input.StartDate, input.EndDate);

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var batch = await LoadBatchAsync(input.Id.Value);

            if (!IsEditable(batch, await IsPostedAsync(batch.Id)))
            {
                throw new UserFriendlyException(L("OvertimeBatchCannotBeChangedOnceSubmitted"));
            }

            batch.OrganizationUnitId = input.OrganizationUnitId;
            batch.StartDate = input.StartDate.Date;
            batch.EndDate = input.EndDate.Date;
            batch.Comments = input.Comments;

            var lines = await BuildLinesAsync(input, tenantId);
            await _overtimeBatchService.EnsureLinesAreValidAsync(batch, lines);

            await _batchRepository.UpdateAsync(batch);

            foreach (var existing in await _detailRepository.GetAllListAsync(d => d.OvertimeBatchId == batch.Id))
            {
                await _detailRepository.DeleteAsync(existing);
            }

            foreach (var line in lines)
            {
                line.OvertimeBatchId = batch.Id;
                await _detailRepository.InsertAsync(line);
            }

            if (batch.WorkflowStatus == null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
                await _overtimeBatchService.PostOvertimesAsync(batch);
            }
        }
    }

    /// <summary>
    /// Submits a sent-back batch again (legacy resubmit of a Draft <c>OvertimeRequest</c>). The handler re-checks
    /// the lines at submit, so a day recorded in the meantime is refused here, not at approval.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_OvertimeBatches_Edit)]
    public async Task ResubmitOvertimeBatch(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var batch = await LoadBatchAsync(input.Id);

            var request = batch.WorkflowRequestId.HasValue
                ? await _requestRepository.FirstOrDefaultAsync(r => r.Id == batch.WorkflowRequestId.Value)
                : null;

            if (!CanResubmit(batch)
                || request == null
                || request.IsAmendment
                || request.RequestStatus != RequestStatus.Draft)
            {
                throw new UserFriendlyException(L("OnlyARequestSentBackToYouCanBeResubmitted"));
            }

            // Admin accounts are deliberately unlinked; the requester the batch was raised against stands in.
            var actingEmployeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync()
                                   ?? request.EmployeeId
                                   ?? 0;

            await _workflowApprovalService.SubmitAsync(request.Id, actingEmployeeId);
        }
    }

    /// <summary>
    /// Deletes a batch nobody is acting on and whose overtime has not been written. A sent-back batch's
    /// request is closed with it, so it does not linger in anybody's list.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_OvertimeBatches_Delete)]
    public async Task Delete(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var batch = await LoadBatchAsync(input.Id);

            if (!IsEditable(batch, await IsPostedAsync(batch.Id)))
            {
                throw new UserFriendlyException(L("OvertimeBatchCannotBeChangedOnceSubmitted"));
            }

            if (batch.WorkflowRequestId.HasValue)
            {
                var request = await _requestRepository.FirstOrDefaultAsync(r => r.Id == batch.WorkflowRequestId.Value);
                if (request != null && request.RequestStatus == RequestStatus.Draft)
                {
                    request.RequestStatus = RequestStatus.Closed;
                    await _requestRepository.UpdateAsync(request);
                }
            }

            foreach (var existing in await _detailRepository.GetAllListAsync(d => d.OvertimeBatchId == batch.Id))
            {
                await _detailRepository.DeleteAsync(existing);
            }

            await _batchRepository.DeleteAsync(batch);
        }
    }

    #region Helpers

    /// <summary>Draft = sent back (or never submitted); null = not routed. Either way, nobody is being asked.</summary>
    private static bool IsEditable(OvertimeBatch batch, bool isPosted) =>
        !isPosted && (batch.WorkflowStatus == null || batch.WorkflowStatus == RequestStatus.Draft);

    private static bool CanResubmit(OvertimeBatch batch) =>
        batch.WorkflowStatus == RequestStatus.Draft && batch.WorkflowRequestId.HasValue;

    private async Task<OvertimeBatch> LoadBatchAsync(int id)
    {
        var batch = await _batchRepository.FirstOrDefaultAsync(id);
        if (batch == null)
        {
            throw new UserFriendlyException(L("OvertimeBatchNotFound"));
        }

        return batch;
    }

    private Task<bool> IsPostedAsync(int batchId) =>
        _detailRepository.GetAll().AnyAsync(d => d.OvertimeBatchId == batchId && d.EmployeeOvertimeId != null);

    /// <summary>Legacy PerformValidations: both dates required and EndDate not before StartDate.</summary>
    private void ValidateDates(DateTime startDate, DateTime endDate)
    {
        if (startDate == default || endDate == default)
        {
            throw new UserFriendlyException(L("OvertimeBatchDatesAreRequired"));
        }

        if (endDate.Date < startDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    /// <summary>The unit and every unit below it (ABP tree codes nest by prefix), then their active people.</summary>
    private async Task<List<int>> GetEmployeeIdsInUnitAsync(long organizationUnitId)
    {
        var unitCode = await _organizationUnitRepository.GetAll()
            .Where(u => u.Id == organizationUnitId)
            .Select(u => u.Code)
            .FirstOrDefaultAsync();

        if (unitCode == null)
        {
            throw new UserFriendlyException(L("OrganizationUnitNotFound"));
        }

        var unitIds = await _organizationUnitRepository.GetAll()
            .Where(u => u.Code.StartsWith(unitCode))
            .Select(u => u.Id)
            .ToListAsync();

        return await _orgAssignmentRepository.GetAll()
            .Where(a => a.OrganizationUnitId.HasValue
                        && unitIds.Contains(a.OrganizationUnitId.Value)
                        && a.Employee.Status == EmployeeStatus.Active)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Turns what the screen sent into lines. Legacy kept only rows with approved hours above zero or a comment
    /// (<c>timeEventGridData.Where(x =&gt; x.ApprovedHours &gt; TimeSpan.Zero || !string.IsNullOrEmpty(x.Comment))</c>),
    /// and so does this. The time figures are copied from the time register here, never from the browser.
    /// </summary>
    private async Task<List<OvertimeBatchDetail>> BuildLinesAsync(CreateOrEditOvertimeBatchDto input, int tenantId)
    {
        var kept = (input.Details ?? new List<CreateOrEditOvertimeBatchLineDto>())
            .Where(d => d.ApprovedHours > 0 || !d.Comments.IsNullOrWhiteSpace())
            .ToList();

        if (kept.Count == 0)
        {
            return new List<OvertimeBatchDetail>();
        }

        var timeRegisterIds = kept.Select(d => d.TimeRegisterId).Distinct().ToList();

        var registers = await _timeRegisterRepository.GetAll()
            .Where(r => timeRegisterIds.Contains(r.Id))
            .ToListAsync();

        if (registers.Count != timeRegisterIds.Count)
        {
            throw new UserFriendlyException(L("TimeRegisterNotFound"));
        }

        // A day that does not belong to somebody in the chosen unit cannot be smuggled into its batch.
        var unitEmployeeIds = await GetEmployeeIdsInUnitAsync(input.OrganizationUnitId);
        if (registers.Any(r => !unitEmployeeIds.Contains(r.EmployeeId)))
        {
            throw new UserFriendlyException(L("OvertimeBatchEmployeeNotInOrganizationUnit"));
        }

        var requestedTypeIds = kept
            .Where(d => d.OvertimeTypeId.HasValue)
            .Select(d => d.OvertimeTypeId.Value)
            .Distinct()
            .ToList();

        if (requestedTypeIds.Count > 0)
        {
            var found = await _overtimeTypeRepository.GetAll().CountAsync(t => requestedTypeIds.Contains(t.Id));
            if (found != requestedTypeIds.Count)
            {
                throw new UserFriendlyException(L("OvertimeTypeNotFound"));
            }
        }

        var registerById = registers.ToDictionary(r => r.Id);

        return kept
            .Select(d =>
            {
                var register = registerById[d.TimeRegisterId];

                return new OvertimeBatchDetail
                {
                    TenantId = tenantId,
                    EmployeeId = register.EmployeeId,
                    Date = register.DayDate.Date,
                    TimeRegisterId = register.Id,
                    ShiftStart = TimeOrNull(register.ShiftStartTime1),
                    ShiftEnd = TimeOrNull(register.ShiftEndTime1),
                    SignIn = TimeOrNull(register.ActualIn),
                    SignOut = TimeOrNull(register.ActualOut),
                    PlannedHours = register.PlannedWorkingMins / 60,
                    TotalHours = register.OvertimeMinutes / 60,
                    LateIn = register.LateIn,
                    EarlyOut = register.EarlyOut,
                    ApprovedHours = d.ApprovedHours,

                    // Legacy: the line's own choice, else a default; the default here is the type the time
                    // register resolved for the day.
                    OvertimeTypeId = d.OvertimeTypeId ?? register.OvertimeTypeId,

                    Comments = d.Comments
                };
            })
            .ToList();
    }

    /// <summary>
    /// The requester is the subject the request routes by, as in legacy. With workflow on, an unlinked account
    /// is refused loudly -- there would be nobody to route by. With workflow off nothing routes, so an HR admin
    /// (deliberately never linked to an employee) can still record a batch.
    /// </summary>
    private async Task<int> GetRequesterEmployeeIdAsync()
    {
        var employeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();
        if (employeeId.HasValue)
        {
            return employeeId.Value;
        }

        if (await _requestMasterResolver.IsWorkflowEnabledAsync(OvertimeBatchWorkflowTransactionHandler.OvertimeBatchRequestType))
        {
            throw new UserFriendlyException(L("CurrentUserIsNotLinkedToAnEmployee"));
        }

        return 0;
    }

    /// <summary>The time register leaves unset times at their default (or legacy's 1900-01-01); show those as blank.</summary>
    private static DateTime? TimeOrNull(DateTime value) => value.Year <= 1900 ? null : value;

    #endregion
}
