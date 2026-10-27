using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Castle.Core.Logging;

namespace HCMv3.BackgroundJobs;

/// <summary>Writes a scheduled task's latest run to <see cref="BackgroundWorkerStatus"/>.</summary>
public interface IBackgroundWorkerRecorder
{
    void RecordRun(string workerName, int intervalMilliseconds, DateTime startTime, Exception failure);

    Task RecordRunAsync(string workerName, int intervalMilliseconds, DateTime startTime, Exception failure);
}

/// <inheritdoc cref="IBackgroundWorkerRecorder"/>
/// <remarks>
/// <para>
/// <b>It never throws.</b> Recording is a spectator: a database hiccup while writing a task's status must not stop the
/// task, and must never replace the task's own error with one about the recorder. A failure to record is logged and
/// dropped.
/// </para>
///
/// <para>
/// <b>Its own transaction.</b> A task's work and its status are unrelated, so the status is written in a new unit of
/// work -- a task that rolled its work back still gets its failure recorded.
/// </para>
/// </remarks>
public class BackgroundWorkerRecorder : IBackgroundWorkerRecorder, ITransientDependency
{
    private readonly IRepository<BackgroundWorkerStatus> _statusRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public BackgroundWorkerRecorder(IRepository<BackgroundWorkerStatus> statusRepository, IUnitOfWorkManager unitOfWorkManager)
    {
        _statusRepository = statusRepository;
        _unitOfWorkManager = unitOfWorkManager;
        Logger = NullLogger.Instance;
    }

    public ILogger Logger { get; set; }

    public void RecordRun(string workerName, int intervalMilliseconds, DateTime startTime, Exception failure)
    {
        try
        {
            using var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);

            using (_unitOfWorkManager.Current.SetTenantId(null))
            {
                var status = _statusRepository.GetAll().FirstOrDefault(s => s.WorkerName == workerName);

                if (status == null)
                {
                    status = new BackgroundWorkerStatus { WorkerName = workerName };
                    Apply(status, intervalMilliseconds, startTime, failure);
                    _statusRepository.Insert(status);
                }
                else
                {
                    Apply(status, intervalMilliseconds, startTime, failure);
                }

                uow.Complete();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not record the run of background task " + workerName + ".", ex);
        }
    }

    public async Task RecordRunAsync(string workerName, int intervalMilliseconds, DateTime startTime, Exception failure)
    {
        try
        {
            using var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);

            using (_unitOfWorkManager.Current.SetTenantId(null))
            {
                var status = _statusRepository.GetAll().FirstOrDefault(s => s.WorkerName == workerName);

                if (status == null)
                {
                    status = new BackgroundWorkerStatus { WorkerName = workerName };
                    Apply(status, intervalMilliseconds, startTime, failure);
                    await _statusRepository.InsertAsync(status);
                }
                else
                {
                    Apply(status, intervalMilliseconds, startTime, failure);
                }

                await uow.CompleteAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not record the run of background task " + workerName + ".", ex);
        }
    }

    private static void Apply(BackgroundWorkerStatus status, int intervalMilliseconds, DateTime startTime, Exception failure)
    {
        var now = Clock.Now;

        status.IntervalMilliseconds = intervalMilliseconds;
        status.LastStartTime = startTime;
        status.LastEndTime = now;
        status.LastSucceeded = failure == null;
        status.RunCount++;

        if (failure == null)
        {
            status.LastSuccessTime = now;
            return;
        }

        status.LastFailureTime = now;
        status.FailureCount++;
        status.LastError = Truncate(Describe(failure));
    }

    /// <summary>The outer message and, when it only wraps another, the inner one -- the part that says what went wrong.</summary>
    public static string Describe(Exception exception)
    {
        var inner = exception;

        while (inner.InnerException != null)
        {
            inner = inner.InnerException;
        }

        return ReferenceEquals(inner, exception)
            ? exception.GetType().Name + ": " + exception.Message
            : exception.GetType().Name + ": " + exception.Message + " -> " + inner.GetType().Name + ": " + inner.Message;
    }

    public static string Truncate(string text)
    {
        return text != null && text.Length > BackgroundJobsConsts.MaxErrorLength
            ? text.Substring(0, BackgroundJobsConsts.MaxErrorLength)
            : text;
    }
}

/// <summary>For a worker built without the container. Records nothing.</summary>
public sealed class NullBackgroundWorkerRecorder : IBackgroundWorkerRecorder
{
    public static readonly NullBackgroundWorkerRecorder Instance = new();

    public void RecordRun(string workerName, int intervalMilliseconds, DateTime startTime, Exception failure)
    {
    }

    public Task RecordRunAsync(string workerName, int intervalMilliseconds, DateTime startTime, Exception failure)
    {
        return Task.CompletedTask;
    }
}
