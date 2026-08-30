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
    private readonly ConfigApplyStage _config;
    private readonly WageTypeApplyStage _wageTypes;
    private readonly EmployeeApplyStage _employees;
    private readonly OrgAssignmentApplyStage _assignments;
    private readonly SalaryApplyStage _salaries;
    private readonly BankApplyStage _banks;
    private readonly OpeningBalanceApplyStage _balances;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public GoLiveApplyAppService(
        ConfigApplyStage config,
        WageTypeApplyStage wageTypes,
        EmployeeApplyStage employees,
        OrgAssignmentApplyStage assignments,
        SalaryApplyStage salaries,
        BankApplyStage banks,
        OpeningBalanceApplyStage balances,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _config = config;
        _wageTypes = wageTypes;
        _employees = employees;
        _assignments = assignments;
        _salaries = salaries;
        _banks = banks;
        _balances = balances;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public Task<GoLiveApplyResultDto> Preview() => RunAsync(apply: false);

    public Task<GoLiveApplyResultDto> Apply() => RunAsync(apply: true);

    /// <summary>
    /// Stages run in dependency order, which is not the order the customer fills the steps in: wage
    /// types before salaries because a salary line needs one, employees before everything in S3 because
    /// each of those sheets keys off an employee.
    /// </summary>
    private async Task<GoLiveApplyResultDto> RunAsync(bool apply)
    {
        var result = new GoLiveApplyResultDto { Applied = apply };

        // Configuration first: the org assignment below points at the payroll area it creates.
        result.Stages.Add(await _config.RunAsync(apply));
        await FlushAsync(apply);

        // Wage types next: a salary detail row cannot exist without one.
        result.Stages.Add(await _wageTypes.RunAsync(apply));
        await FlushAsync(apply);

        result.Stages.Add(await _employees.RunAsync(apply));
        await FlushAsync(apply);

        // An employee with no assignment has no payroll area, so the engine never sees them.
        result.Stages.Add(await _assignments.RunAsync(apply));
        result.Stages.Add(await _salaries.RunAsync(apply));
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
