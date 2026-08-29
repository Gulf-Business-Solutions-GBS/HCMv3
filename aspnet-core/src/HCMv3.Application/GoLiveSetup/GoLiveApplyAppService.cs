using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using HCMv3.Authorization;
using HCMv3.GoLiveSetup.Apply;
using HCMv3.GoLiveSetup.Dto;

namespace HCMv3.GoLiveSetup;

/// <inheritdoc cref="IGoLiveApplyAppService"/>
[AbpAuthorize(AppPermissions.Pages_GoLiveSetup)]
public class GoLiveApplyAppService : HCMv3AppServiceBase, IGoLiveApplyAppService
{
    private readonly EmployeeApplyStage _employees;

    public GoLiveApplyAppService(EmployeeApplyStage employees)
    {
        _employees = employees;
    }

    public Task<GoLiveApplyResultDto> Preview() => RunAsync(apply: false);

    public Task<GoLiveApplyResultDto> Apply() => RunAsync(apply: true);

    /// <summary>
    /// Stages run in dependency order. Only employees today; salaries, bank details and opening balances
    /// slot in after it because each keys off an employee that must already exist.
    /// </summary>
    private async Task<GoLiveApplyResultDto> RunAsync(bool apply)
    {
        var result = new GoLiveApplyResultDto { Applied = apply };

        result.Stages.Add(await _employees.RunAsync(apply));

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
}
