using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IOvertimeBatchService"/>
public class OvertimeBatchService : HCMv3DomainServiceBase, IOvertimeBatchService, ITransientDependency
{
    /// <summary>
    /// The overtime type every row created from an approved batch gets -- legacy
    /// <c>OvertimeRequest.OnSaving</c> line 168: <c>new XPQuery&lt;OvertimeType&gt;(Session).FirstOrDefault(x =&gt; x.Code == "4")</c>.
    ///
    /// <para>
    /// <b>Kept as legacy has it, on purpose.</b> Legacy added an <c>OvertimeType</c> to each line later (2025-09)
    /// and never changed the approval to use it, so whatever type a line shows, the overtime is paid at the
    /// code-4 type's rate. Changing that would change pay, and payroll must match legacy (parity rule); it is
    /// pinned by a test instead. If the business wants the line's type used, that is a decision, not a fix.
    /// </para>
    /// </summary>
    public const string LegacyApprovalOvertimeTypeCode = "4";

    private readonly IRepository<OvertimeBatch> _batchRepository;
    private readonly IRepository<OvertimeBatchDetail> _detailRepository;
    private readonly IRepository<EmployeeOvertime> _overtimeRepository;
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeOvertimeService _employeeOvertimeService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public OvertimeBatchService(
        IRepository<OvertimeBatch> batchRepository,
        IRepository<OvertimeBatchDetail> detailRepository,
        IRepository<EmployeeOvertime> overtimeRepository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<Employee> employeeRepository,
        IEmployeeOvertimeService employeeOvertimeService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _batchRepository = batchRepository;
        _detailRepository = detailRepository;
        _overtimeRepository = overtimeRepository;
        _overtimeTypeRepository = overtimeTypeRepository;
        _employeeRepository = employeeRepository;
        _employeeOvertimeService = employeeOvertimeService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <inheritdoc/>
    public async Task EnsureLinesAreValidAsync(OvertimeBatch batch, IReadOnlyCollection<OvertimeBatchDetail> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            throw new UserFriendlyException(L("OvertimeBatchHasNoLines"));
        }

        var start = batch.StartDate.Date;
        var end = batch.EndDate.Date;

        if (lines.Any(l => l.Date.Date < start || l.Date.Date > end))
        {
            throw new UserFriendlyException(L("OvertimeBatchLineOutsideDateRange"));
        }

        if (lines.Any(l => l.ApprovedHours < 0))
        {
            throw new UserFriendlyException(L("ApprovedHoursCannotBeNegative"));
        }

        var duplicate = lines
            .GroupBy(l => new { l.EmployeeId, Day = l.Date.Date })
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
        {
            throw new UserFriendlyException(L(
                "OvertimeBatchHasDuplicateEmployeeDay",
                EmployeeCode(duplicate.Key.EmployeeId),
                duplicate.Key.Day.ToString("dd/MM/yyyy")));
        }

        var employeeIds = lines.Select(l => l.EmployeeId).Distinct().ToList();

        // Another batch that still stands already claims the day. Legacy's loader hid such days but nothing
        // stopped a hand-built second request; this is that check, made real.
        var claimed = await GetClaimedDaysAsync(employeeIds, start, end, batch.Id);

        var clash = lines.FirstOrDefault(l => claimed.Contains((l.EmployeeId, l.Date.Date)));
        if (clash != null)
        {
            throw new UserFriendlyException(L(
                "OvertimeBatchDayAlreadyInAnotherBatch",
                EmployeeCode(clash.EmployeeId),
                clash.Date.ToString("dd/MM/yyyy")));
        }

        // The overtime rule itself (one row per employee per day), from the shared service -- not re-written here.
        var recorded = await _employeeOvertimeService.GetDaysWithOvertimeAsync(employeeIds, start, end);
        var alreadyRecorded = lines.FirstOrDefault(l => recorded.Contains((l.EmployeeId, l.Date.Date)));
        if (alreadyRecorded != null)
        {
            throw new UserFriendlyException(L(
                "OvertimeAlreadyRecordedForEmployeeOnDate",
                EmployeeCode(alreadyRecorded.EmployeeId),
                alreadyRecorded.Date.ToString("dd/MM/yyyy")));
        }
    }

    /// <inheritdoc/>
    public Task<HashSet<(int EmployeeId, DateTime Date)>> GetClaimedDaysAsync(
        IReadOnlyCollection<int> employeeIds,
        DateTime startDate,
        DateTime endDate,
        int excludeBatchId = 0)
    {
        var result = new HashSet<(int EmployeeId, DateTime Date)>();

        if (employeeIds == null || employeeIds.Count == 0)
        {
            return Task.FromResult(result);
        }

        var ids = employeeIds.ToList();
        var start = startDate.Date;
        var end = endDate.Date;

        // A batch's rows are hidden by ApprovedOnly while in flight, which is exactly when they must count.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // The same live set the single-day overtime handler counts: not routed (null), in an inbox
            // (Open), approved, or recorded directly (Closed). Rejected and sent-back batches release their days.
            var liveBatchIds = _batchRepository.GetAll()
                .Where(b => b.Id != excludeBatchId
                            && (b.WorkflowStatus == null
                                || b.WorkflowStatus == RequestStatus.Open
                                || b.WorkflowStatus == RequestStatus.Approved
                                || b.WorkflowStatus == RequestStatus.Closed))
                .Select(b => b.Id);

            var rows = _detailRepository.GetAll()
                .Where(d => liveBatchIds.Contains(d.OvertimeBatchId)
                            && ids.Contains(d.EmployeeId)
                            && d.Date >= start
                            && d.Date <= end)
                .Select(d => new { d.EmployeeId, d.Date })
                .ToList();

            foreach (var row in rows)
            {
                result.Add((row.EmployeeId, row.Date.Date));
            }
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public async Task PostOvertimesAsync(OvertimeBatch batch)
    {
        List<OvertimeBatchDetail> lines;
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            lines = _detailRepository.GetAll()
                .Where(d => d.OvertimeBatchId == batch.Id && d.EmployeeOvertimeId == null)
                .OrderBy(d => d.Id)
                .ToList();
        }

        if (lines.Count == 0)
        {
            // Nothing left to post: approval already honoured (a retry, a replay), or a batch with no lines.
            return;
        }

        var overtimeTypeId = _overtimeTypeRepository.GetAll()
            .Where(t => t.Code == LegacyApprovalOvertimeTypeCode)
            .Select(t => (int?)t.Id)
            .FirstOrDefault();

        if (!overtimeTypeId.HasValue)
        {
            // Legacy assigned null here and EmployeeOvertime.PerformValidations then refused it ("OvertimeType
            // Cannot be blank."), failing the approval. Same outcome, with a message that says what to set up.
            throw new UserFriendlyException(L("OvertimeBatchApprovalOvertimeTypeNotFound", LegacyApprovalOvertimeTypeCode));
        }

        // Legacy EmployeeOvertime.PerformValidations "Record already exists": a day recorded since the batch was
        // submitted fails the approval, as it did there. Every line is checked BEFORE any row is written, so a
        // refusal never leaves half a batch posted, whatever the unit of work does.
        foreach (var line in lines)
        {
            await _employeeOvertimeService.EnsureNoOvertimeOnDateAsync(line.EmployeeId, line.Date);
        }

        foreach (var line in lines)
        {
            var overtimeId = await _overtimeRepository.InsertAndGetIdAsync(new EmployeeOvertime
            {
                TenantId = batch.TenantId,
                EmployeeId = line.EmployeeId,
                EffectiveDate = line.Date.Date,
                ActualOvertimeHours = Math.Round(line.TotalHours, 2),
                OvertimeHours = Math.Round(line.TotalHours, 2),
                ApprovedOvertimeHours = Math.Round(line.ApprovedHours, 2),
                OvertimeTypeId = overtimeTypeId.Value

                // WorkflowStatus left null: effective now, like the rows the time engine writes. The decision
                // was taken on the batch; giving each row the batch's request id would let the overtime screens
                // act on a request that is not theirs.
            });

            line.EmployeeOvertimeId = overtimeId;
            await _detailRepository.UpdateAsync(line);
        }
    }

    private string EmployeeCode(int employeeId)
    {
        return _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => e.Code)
            .FirstOrDefault() ?? employeeId.ToString();
    }
}
