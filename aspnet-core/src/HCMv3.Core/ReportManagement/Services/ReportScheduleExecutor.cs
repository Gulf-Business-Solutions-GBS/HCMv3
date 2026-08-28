using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Castle.Core.Logging;

namespace HCMv3.ReportManagement.Services;

/// <inheritdoc cref="IReportScheduleExecutor"/>
public class ReportScheduleExecutor : HCMv3DomainServiceBase, IReportScheduleExecutor
{
    private readonly IRepository<ReportSchedule> _scheduleRepository;
    private readonly IRepository<ReportScheduleRun, long> _runRepository;
    private readonly IRepository<ReportScheduleRunSlice, long> _sliceRepository;
    private readonly IReportBurstResolver _burstResolver;
    private readonly IReportRecipientScopeResolver _scopeResolver;
    private readonly IEnumerable<IReportSliceRenderer> _renderers;
    private readonly IEnumerable<IReportSliceDeliveryChannel> _channels;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ReportScheduleExecutor(
        IRepository<ReportSchedule> scheduleRepository,
        IRepository<ReportScheduleRun, long> runRepository,
        IRepository<ReportScheduleRunSlice, long> sliceRepository,
        IReportBurstResolver burstResolver,
        IReportRecipientScopeResolver scopeResolver,
        IEnumerable<IReportSliceRenderer> renderers,
        IEnumerable<IReportSliceDeliveryChannel> channels,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _scheduleRepository = scheduleRepository;
        _runRepository = runRepository;
        _sliceRepository = sliceRepository;
        _burstResolver = burstResolver;
        _scopeResolver = scopeResolver;
        _renderers = renderers;
        _channels = channels;
        _unitOfWorkManager = unitOfWorkManager;
        Logger = NullLogger.Instance;
    }

    public ILogger Logger { get; set; }

    public async Task<ReportScheduleExecutionResult> ExecuteAsync(int scheduleId, bool isManual)
    {
        // Synchronous LINQ: Core must not reference EF Core, so the async LINQ operators are not
        // available here. Same constraint RlsService works under.
        var schedule = _scheduleRepository.GetAllIncluding(
                s => s.Recipients,
                s => s.Destinations,
                s => s.Filters)
                .FirstOrDefault(s => s.Id == scheduleId);

        if (schedule == null)
        {
            throw new InvalidOperationException($"Report schedule {scheduleId} not found.");
        }

        var run = new ReportScheduleRun
        {
            TenantId = schedule.TenantId,
            ReportScheduleId = schedule.Id,
            StartedAt = Clock.Now,
            Status = ReportRunStatus.Running,
            IsManual = isManual,
        };

        run.Id = await _runRepository.InsertAndGetIdAsync(run);
        await _unitOfWorkManager.Current.SaveChangesAsync();

        var result = new ReportScheduleExecutionResult { RunId = run.Id };

        try
        {
            var slices = await _burstResolver.ResolveSlicesAsync(schedule);
            result.SliceCount = slices.Count;
            run.SliceCount = slices.Count;

            if (slices.Count == 0)
            {
                // Not an error, but not a success either -- somebody configured a schedule that reaches
                // nobody, and that must be visible rather than looking like a clean run.
                run.Status = ReportRunStatus.SkippedEmpty;
                run.FinishedAt = Clock.Now;
                result.Status = run.Status;
                result.Message = "No slices resolved: the schedule's recipient rules matched nobody.";
                await _unitOfWorkManager.Current.SaveChangesAsync();
                return result;
            }

            var renderer = _renderers.FirstOrDefault(r => r.CanRender(schedule.ReportKind));
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"No renderer is registered for {schedule.ReportKind}.");
            }

            foreach (var slice in slices)
            {
                foreach (var recipient in slice.Recipients)
                {
                    result.RecipientCount++;
                    await ProcessRecipientAsync(schedule, run, slice, recipient, renderer, result);
                }
            }

            run.Status = result.FailedCount == 0
                ? ReportRunStatus.Succeeded
                : result.DeliveredCount > 0
                    ? ReportRunStatus.PartiallySucceeded
                    : ReportRunStatus.Failed;
        }
        catch (Exception ex)
        {
            // Only a failure of the RUN itself lands here -- per-recipient failures are caught below and
            // recorded against their slice so one bad address cannot stop everyone else's report.
            Logger.Error($"Report schedule {scheduleId} failed.", ex);
            run.Status = ReportRunStatus.Failed;
            run.ErrorMessage = Truncate(ex.Message, 4000);
            result.Message = ex.Message;
        }

        run.FinishedAt = Clock.Now;
        schedule.LastRunTime = run.FinishedAt;
        result.Status = run.Status;

        await _unitOfWorkManager.Current.SaveChangesAsync();
        return result;
    }

    private async Task ProcessRecipientAsync(
        ReportSchedule schedule,
        ReportScheduleRun run,
        ReportBurstSlice slice,
        ReportSliceRecipient recipient,
        IReportSliceRenderer renderer,
        ReportScheduleExecutionResult result)
    {
        var logSlice = new ReportScheduleRunSlice
        {
            TenantId = schedule.TenantId,
            ReportScheduleRunId = run.Id,
            BurstKey = slice.Key,
            BurstKeyDisplay = Truncate(slice.Display, 512),
            RecipientUserId = recipient.UserId,
            RecipientEmail = Truncate(recipient.Email, 256),
            WasRecipientScoped = recipient.ApplyRecipientScope,
            Status = ReportRunStatus.Running,
        };

        try
        {
            // SCOPE FIRST, RENDER SECOND. See IReportScheduleExecutor remarks.
            HashSet<int> visibleEmployeeIds = null;

            if (recipient.ApplyRecipientScope)
            {
                if (recipient.UserId.HasValue)
                {
                    visibleEmployeeIds = await _scopeResolver.GetVisibleEmployeeIdsForUserAsync(recipient.UserId.Value);
                }
                else if (recipient.EmployeeId.HasValue)
                {
                    visibleEmployeeIds = await _scopeResolver.GetVisibleEmployeeIdsForEmployeeAsync(recipient.EmployeeId.Value);
                }
                else
                {
                    // A recipient who must be scoped but has no identity to scope BY. Refusing is the
                    // only safe reading: the alternative is sending them everything.
                    throw new InvalidOperationException(
                        "Recipient requires row-level scoping but has no linked user or employee to resolve a scope from.");
                }

                if (visibleEmployeeIds != null && visibleEmployeeIds.Count == 0)
                {
                    logSlice.Status = ReportRunStatus.SkippedEmpty;
                    logSlice.RowCount = 0;
                    result.EmptyCount++;
                    await _sliceRepository.InsertAsync(logSlice);
                    return;
                }
            }

            var rendered = await renderer.RenderAsync(schedule, slice, visibleEmployeeIds);
            logSlice.RowCount = rendered?.RowCount ?? 0;
            logSlice.FileName = Truncate(rendered?.FileName, 512);

            if (rendered == null || rendered.RowCount == 0)
            {
                // Deliberately not delivered. A monthly empty attachment trains recipients to ignore
                // the schedule entirely, which is worse than not sending it.
                logSlice.Status = ReportRunStatus.SkippedEmpty;
                result.EmptyCount++;
                await _sliceRepository.InsertAsync(logSlice);
                return;
            }

            foreach (var destination in schedule.Destinations)
            {
                var channel = _channels.FirstOrDefault(c => c.Method == destination.Method);
                if (channel == null)
                {
                    throw new InvalidOperationException(
                        $"No delivery channel is registered for {destination.Method}.");
                }

                logSlice.Method = destination.Method;
                await channel.DeliverAsync(schedule, destination, slice, recipient, rendered);
            }

            logSlice.Status = ReportRunStatus.Succeeded;
            result.DeliveredCount++;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Report schedule {schedule.Id}: slice '{slice.Key}' to '{recipient.Email}' failed.",
                ex);

            logSlice.Status = ReportRunStatus.Failed;
            logSlice.ErrorMessage = Truncate(ex.Message, 4000);
            result.FailedCount++;
        }

        await _sliceRepository.InsertAsync(logSlice);
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
        {
            return value;
        }

        return value.Substring(0, max);
    }
}
