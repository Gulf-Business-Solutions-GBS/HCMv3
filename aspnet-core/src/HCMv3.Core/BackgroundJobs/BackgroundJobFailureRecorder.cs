using System;
using System.Text.Json;
using System.Transactions;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Events.Bus.Exceptions;
using Abp.Events.Bus.Handlers;
using Abp.Timing;
using Castle.Core.Logging;

namespace HCMv3.BackgroundJobs;

/// <summary>
/// Keeps the reason a queued background job failed, from the event ABP raises on every failed attempt.
///
/// <para>
/// ABP's job manager catches the job's exception, raises its retry count, logs it, and raises
/// <see cref="AbpHandledExceptionData"/> wrapping a <see cref="BackgroundJobException"/>. Nothing else in the app listens,
/// so without this the error exists only in the log file on the server. See <see cref="BackgroundJobFailure"/>.
/// </para>
///
/// <para>
/// Like the worker recorder, it never throws: a job that already failed must not have its failure handling fail too.
/// </para>
/// </summary>
public class BackgroundJobFailureRecorder : IEventHandler<AbpHandledExceptionData>, ITransientDependency
{
    private readonly IRepository<BackgroundJobFailure, long> _failureRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public BackgroundJobFailureRecorder(IRepository<BackgroundJobFailure, long> failureRepository, IUnitOfWorkManager unitOfWorkManager)
    {
        _failureRepository = failureRepository;
        _unitOfWorkManager = unitOfWorkManager;
        Logger = NullLogger.Instance;
    }

    public ILogger Logger { get; set; }

    public void HandleEvent(AbpHandledExceptionData eventData)
    {
        // Every handled exception in the app arrives here, not just job failures. Anything else is not ours.
        if (eventData?.Exception is not BackgroundJobException jobException || jobException.BackgroundJob == null)
        {
            return;
        }

        var job = jobException.BackgroundJob;

        try
        {
            using var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);

            var tenantId = TenantIdOf(job.JobArgs);

            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                _failureRepository.Insert(new BackgroundJobFailure
                {
                    TenantId = tenantId,
                    JobId = job.Id,
                    JobName = JobNameOf(job.JobType),
                    TryCount = job.TryCount,
                    FailedTime = Clock.Now,
                    Error = BackgroundWorkerRecorder.Truncate(
                        BackgroundWorkerRecorder.Describe(jobException.InnerException ?? jobException))
                });

                _unitOfWorkManager.Current.SaveChanges();
            }

            // Pruned across every company, which is why the tenant filter is off: one company's save must not leave
            // another's month-old rows behind.
            using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var cutoff = Clock.Now.AddDays(-BackgroundJobsConsts.FailureRetentionDays);
                _failureRepository.HardDelete(f => f.FailedTime < cutoff);
            }

            uow.Complete();
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not record the failure of background job " + job.Id + ".", ex);
        }
    }

    /// <summary>
    /// The company a job belongs to, from the TenantId every job argument in this solution carries. Null -- a host job --
    /// when the arguments cannot be read or have none, which is the safe side: a host row is visible only to the host.
    /// </summary>
    public static int? TenantIdOf(string jobArgs)
    {
        if (string.IsNullOrWhiteSpace(jobArgs))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(jobArgs);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, "TenantId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var id)
                    ? id
                    : null;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    /// <summary>"HCMv3.PayrollManagement.PayrollEngineBackgroundJob, HCMv3.Application, ..." to "PayrollEngineBackgroundJob".</summary>
    public static string JobNameOf(string jobType)
    {
        if (string.IsNullOrWhiteSpace(jobType))
        {
            return "(unknown)";
        }

        var typeName = jobType.Split(',')[0].Trim();

        // A generic job type carries its arguments in brackets; the name is the part before them.
        var bracket = typeName.IndexOf('`');
        if (bracket > 0)
        {
            typeName = typeName.Substring(0, bracket);
        }

        var dot = typeName.LastIndexOf('.');
        var name = dot >= 0 ? typeName.Substring(dot + 1) : typeName;

        return name.Length > BackgroundJobsConsts.MaxNameLength ? name.Substring(0, BackgroundJobsConsts.MaxNameLength) : name;
    }
}
