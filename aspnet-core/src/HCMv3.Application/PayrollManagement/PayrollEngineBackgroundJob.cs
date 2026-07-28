using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.PayrollManagement;

/// <summary>
/// The ONLY place a Payroll Engine run is actually executed. <see cref="PayrollEngineAppService"/> enqueues
/// this job and never calls <see cref="IPayrollEngineService"/> directly -- the same single-execution-path
/// arrangement as <c>TimeEngineBackgroundJob</c>.
///
/// <para>Replaces legacy's <c>Program.Main(args)</c> process launch
/// (<c>HCM10.PayrollEngine/Program.cs</c> lines 154-215).</para>
///
/// <para><b>Deviations from legacy's Main:</b>
/// (1) the <c>dd/MM/yyyy</c> string parsing of <c>args[]</c> is gone -- the arguments arrive typed;
/// (2) the connection-string / XAF plumbing is gone -- ABP owns it;
/// (3) legacy's outermost <c>catch</c> (lines 352-369) wrote an employee-less failed <c>PayrollLog</c> row
/// and swallowed the exception. This job lets a whole-run failure surface so ABP's background job manager
/// records it, because the engine already writes a <c>PayrollLog</c> row for every per-employee failure
/// itself and a silently-swallowed row would hide a run that never started.</para>
/// </summary>
public class PayrollEngineBackgroundJob : AsyncBackgroundJob<PayrollEngineJobArgs>, ITransientDependency
{
    private readonly IPayrollEngineService _payrollEngineService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PayrollEngineBackgroundJob(
        IPayrollEngineService payrollEngineService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _payrollEngineService = payrollEngineService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override async Task ExecuteAsync(PayrollEngineJobArgs args)
    {
        // Background jobs start with no tenant scope; every payroll entity is IMustHaveTenant, so the scope
        // has to be re-established explicitly or the run would see (and write) nothing.
        using (var uow = _unitOfWorkManager.Begin())
        {
            using (_unitOfWorkManager.Current.SetTenantId(args.TenantId))
            {
                var result = await _payrollEngineService.RunAsync(new PayrollEngineRunInput
                {
                    PayrollRunType = args.PayrollRunType,
                    EmployeeCodes = args.EmployeeCodes,
                    PayInDate = args.PayInDate,
                    PayrollAreaName = args.PayrollAreaName,
                    OffCyclePaymentDate = args.OffCyclePaymentDate,
                    OffCycleReason = args.OffCycleReason,
                    RegisterTarget = args.RegisterTarget
                });

                Logger.Info(string.Format(
                    "PayrollEngine run finished. PayInDate={0:dd/MM/yyyy} Employees={1} Succeeded={2} Failed={3} Aborted={4}",
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
