using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Uow;
using HCMv3.Authorization;
using HCMv3.GoLiveSetup.Apply;
using HCMv3.GoLiveSetup.Dto;

namespace HCMv3.GoLiveSetup;

/// <inheritdoc cref="IGoLiveApplyAppService"/>
[AbpAuthorize(AppPermissions.Pages_GoLiveSetup)]
public class GoLiveApplyAppService : HCMv3AppServiceBase, IGoLiveApplyAppService
{
    private readonly EmployeeApplyStage _employees;
    private readonly BankApplyStage _banks;
    private readonly OpeningBalanceApplyStage _balances;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public GoLiveApplyAppService(
        EmployeeApplyStage employees,
        BankApplyStage banks,
        OpeningBalanceApplyStage balances,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _employees = employees;
        _banks = banks;
        _balances = balances;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public Task<GoLiveApplyResultDto> Preview() => RunAsync(apply: false);

    public Task<GoLiveApplyResultDto> Apply() => RunAsync(apply: true);

    /// <summary>
    /// Stages run in dependency order: employees first, because bank details and opening balances both
    /// key off an employee that must already exist. Salaries are still missing -- they need a wage type,
    /// and the registry has no pay-elements step to source one from yet.
    /// </summary>
    private async Task<GoLiveApplyResultDto> RunAsync(bool apply)
    {
        var result = new GoLiveApplyResultDto { Applied = apply };

        result.Stages.Add(await _employees.RunAsync(apply));
        await FlushAsync(apply);

        result.Stages.Add(await _banks.RunAsync(apply));
        result.Stages.Add(await _balances.RunAsync(apply));

        result.TotalCreated = result.Stages.Sum(s => s.Created);
        result.TotalUpdated = result.Stages.Sum(s => s.Updated);
        result.TotalSkipped = result.Stages.Sum(s => s.Skipped);
        result.TotalFailed = result.Stages.Sum(s => s.Failed);

        if (apply)
        {
            Logger.Info(
                $"Go-Live Setup applied for tenant {AbpSession.TenantId}: " +
                $"{result.TotalCreated} created, {result.TotalUpdated} updated, {result.TotalFailed} failed.");
        }

        return result;
    }

    /// <summary>
    /// Pushes the previous stage's inserts to the database. Without this the next stage cannot see
    /// them: the rows are tracked but unsaved, and a LINQ query does not read the change tracker. It
    /// is what makes "employees first, then everything that keys off an employee" actually work.
    /// </summary>
    private async Task FlushAsync(bool apply)
    {
        if (apply)
        {
            await _unitOfWorkManager.Current.SaveChangesAsync();
        }
    }
}
