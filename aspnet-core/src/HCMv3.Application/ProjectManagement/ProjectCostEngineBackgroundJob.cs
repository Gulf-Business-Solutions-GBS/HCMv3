using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using HCMv3.ProjectManagement.Services;

namespace HCMv3.ProjectManagement;

/// <summary>
/// The ONLY place a Project Cost Engine run is actually executed -- the same single-execution-path
/// arrangement as <c>PayrollEngineBackgroundJob</c>/<c>TimeEngineBackgroundJob</c>.
///
/// <para>Replaces legacy's
/// <c>ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteLongOperation), this)</c>
/// (<c>uscProjectCosting.ascx.cs</c> line 1611), which ran the whole engine on a thread-pool thread of the
/// web process, holding a static <c>LongOperationStatus</c> string that the page polled.</para>
///
/// <para><b>Deviation:</b> legacy's outer <c>catch</c> (lines 278-294) wrote an EMPLOYEE-LESS failed
/// <c>ProjectLog</c> row and swallowed the exception. This port's <c>ProjectLog.EmployeeId</c> is a
/// non-nullable <c>int</c>, so a whole-run failure is allowed to surface here instead and ABP's background
/// job manager records it. Per-employee failures still get their own <c>ProjectLog</c> rows inside the
/// engine, exactly as legacy. <b>Reported gap.</b></para>
/// </summary>
public class ProjectCostEngineBackgroundJob : AsyncBackgroundJob<ProjectCostEngineJobArgs>, ITransientDependency
{
    private readonly IProjectCostEngineService _projectCostEngineService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ProjectCostEngineBackgroundJob(
        IProjectCostEngineService projectCostEngineService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _projectCostEngineService = projectCostEngineService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override async Task ExecuteAsync(ProjectCostEngineJobArgs args)
    {
        // Background jobs start with no tenant scope; every entity involved is IMustHaveTenant, so the
        // scope has to be re-established explicitly or the run would see (and write) nothing.
        using (var uow = _unitOfWorkManager.Begin())
        {
            using (_unitOfWorkManager.Current.SetTenantId(args.TenantId))
            {
                var result = await _projectCostEngineService.RunAsync(new ProjectCostEngineRunInput
                {
                    PayrollRunType = args.PayrollRunType,
                    EmployeeCodes = args.EmployeeCodes,
                    PayInDate = args.PayInDate,
                    PayrollAreaName = args.PayrollAreaName,
                    OffCyclePaymentDate = args.OffCyclePaymentDate,
                    OffCycleReason = args.OffCycleReason
                });

                Logger.Info(string.Format(
                    "ProjectCostEngine run finished. PayInDate={0:dd/MM/yyyy} Employees={1} Succeeded={2} Failed={3} Aborted={4}",
                    result.PayInDate,
                    result.EmployeeResults.Count,
                    result.EmployeeResults.FindAll(r => r.Succeeded).Count,
                    result.EmployeeResults.FindAll(r => !r.Succeeded).Count,
                    result.Aborted));
            }

            await uow.CompleteAsync();
        }
    }
}
