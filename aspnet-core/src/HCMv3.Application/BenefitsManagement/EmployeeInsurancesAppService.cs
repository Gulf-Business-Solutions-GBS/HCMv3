using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.BenefitsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.BenefitsManagement;

/// <summary>
/// Application service for <see cref="EmployeeInsurance"/> (ported from legacy XAF EmployeeInsurance
/// business object). Legacy's <c>PerformValidations</c> is ported here (out of the persistence
/// lifecycle); legacy's lazily-computed installment/balance getters are ported as explicit
/// calculations performed on create/update -- see <see cref="EmployeeInsurance"/> remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_BenefitsManagement_EmployeeInsurances)]
public class EmployeeInsurancesAppService : HCMv3AppServiceBase, IEmployeeInsurancesAppService
{
    private readonly IRepository<EmployeeInsurance> _employeeInsuranceRepository;

    public EmployeeInsurancesAppService(IRepository<EmployeeInsurance> employeeInsuranceRepository)
    {
        _employeeInsuranceRepository = employeeInsuranceRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeInsuranceDto>> GetAll(GetAllEmployeeInsurancesInput input)
    {
        input.Normalize();

        var query = _employeeInsuranceRepository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.InsurancePlan)
            .Include(e => e.InsuranceProvider)
            .Include(e => e.Dependant)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeInsuranceDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeInsuranceForEditOutput> GetEmployeeInsuranceForEdit(EntityDto input)
    {
        var entity = await _employeeInsuranceRepository.GetAsync(input.Id);

        return new GetEmployeeInsuranceForEditOutput
        {
            EmployeeInsurance = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeInsuranceDto input)
    {
        PerformValidations(input);

        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_BenefitsManagement_EmployeeInsurances_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeInsuranceRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_BenefitsManagement_EmployeeInsurances_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeInsuranceDto input)
    {
        var entity = MapToEntity(input, new EmployeeInsurance());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Legacy UpdateDueBalance parity: with no installments recorded yet, the balance equals the
        // full contribution amount.
        entity.BalanceCompanyContribution = Round(entity.CompanyContribution);
        entity.BalanceEmployeeContribution = Round(entity.EmployeeContribution);

        await _employeeInsuranceRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_BenefitsManagement_EmployeeInsurances_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeInsuranceDto input)
    {
        var entity = await _employeeInsuranceRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _employeeInsuranceRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidations</c> parity (<c>EmployeeInsurance.cs</c>). The
    /// <c>!IsSystemGenerated</c> guard (skipping validation for system-generated balance-recompute
    /// saves) does not apply here since balance recomputation lives in the detail app services and
    /// updates only the balance columns directly via repository, not through this method.
    /// </summary>
    private void PerformValidations(CreateOrEditEmployeeInsuranceDto input)
    {
        if (input.EndDate < input.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var installmentStartDate = input.InstallmentStartDate == default
            ? new DateTime(input.StartDate.Year, input.StartDate.Month, 1).AddMonths(1)
            : input.InstallmentStartDate;

        if (input.StartDate > installmentStartDate)
        {
            throw new UserFriendlyException(L("InstallmentStartDateShouldBeAfterStartDate"));
        }

        if (input.InsuranceAmount == 0)
        {
            throw new UserFriendlyException(L("InsuranceAmountShouldNotBeZero"));
        }

        if (Math.Abs(input.EmployeeContribution + input.CompanyContribution - input.InsuranceAmount) > 0.0001)
        {
            throw new UserFriendlyException(L("InsuranceAmountShouldEqualContributions"));
        }

        if (input.CompanyContribution > 0)
        {
            var companyDedAmount = input.InstallmentCompanyDedAmount == 0
                ? ComputeInstallmentAmount(input.CompanyContribution, input.StartDate, input.EndDate)
                : input.InstallmentCompanyDedAmount;

            if (companyDedAmount == 0)
            {
                throw new UserFriendlyException(L("InstallmentDedAmountShouldNotBeZero"));
            }

            if (companyDedAmount > input.CompanyContribution)
            {
                throw new UserFriendlyException(L("InstallmentDedAmountExceedsContribution"));
            }
        }

        if (input.EmployeeContribution > 0)
        {
            var employeeDedAmount = input.InstallmentEmployeeDedAmount == 0
                ? ComputeInstallmentAmount(input.EmployeeContribution, input.StartDate, input.EndDate)
                : input.InstallmentEmployeeDedAmount;

            if (employeeDedAmount == 0)
            {
                throw new UserFriendlyException(L("InstallmentDedAmountShouldNotBeZero"));
            }

            if (employeeDedAmount > input.EmployeeContribution)
            {
                throw new UserFriendlyException(L("InstallmentDedAmountExceedsContribution"));
            }
        }
    }

    private static EmployeeInsurance MapToEntity(CreateOrEditEmployeeInsuranceDto input, EmployeeInsurance entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.InsurancePlanId = input.InsurancePlanId;
        entity.InsuranceProviderId = input.InsuranceProviderId;
        entity.DependantId = input.DependantId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.InstallmentStartDate = input.InstallmentStartDate == default
            ? new DateTime(input.StartDate.Year, input.StartDate.Month, 1).AddMonths(1)
            : input.InstallmentStartDate;
        entity.InsuranceAmount = input.InsuranceAmount;
        entity.CompanyInterval = input.CompanyInterval;
        entity.CompanyContribution = input.CompanyContribution;
        entity.InstallmentCompanyDedAmount = input.InstallmentCompanyDedAmount == 0
            ? ComputeInstallmentAmount(input.CompanyContribution, input.StartDate, input.EndDate)
            : input.InstallmentCompanyDedAmount;
        entity.EmployeeInterval = input.EmployeeInterval;
        entity.EmployeeContribution = input.EmployeeContribution;
        entity.InstallmentEmployeeDedAmount = input.InstallmentEmployeeDedAmount == 0
            ? ComputeInstallmentAmount(input.EmployeeContribution, input.StartDate, input.EndDate)
            : input.InstallmentEmployeeDedAmount;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>InstallmentCompanyDedAmount</c>/<c>InstallmentEmployeeDedAmount</c> getter parity:
    /// contribution / total months between StartDate and EndDate (legacy <c>GetTotalMonths</c>).
    /// </summary>
    private static double ComputeInstallmentAmount(double contribution, DateTime startDate, DateTime endDate)
    {
        var totalMonths = GetTotalMonths(startDate, endDate);
        return totalMonths == 0 ? 0 : Round(contribution / totalMonths);
    }

    /// <summary>
    /// Ported verbatim from legacy <c>EmployeeInsurance.GetTotalMonths</c>.
    /// </summary>
    internal static int GetTotalMonths(DateTime dt1, DateTime dt2)
    {
        var t = dt1;
        double months = 0;
        while (t <= dt2)
        {
            var daysInMonth = DateTime.DaysInMonth(t.Year, t.Month);
            var endOfMonth = new DateTime(t.Year, t.Month, daysInMonth);
            var cutDay = endOfMonth <= dt2 ? daysInMonth : dt2.Day;
            months += (cutDay - t.Day + 1) / (double)daysInMonth;
            t = new DateTime(t.Year, t.Month, 1).AddMonths(1);
        }

        return Convert.ToInt32(months);
    }

    /// <summary>
    /// DELIBERATE SIMPLIFICATION: legacy rounds via <c>CurrencyService.Rounding(Employee, value)</c>
    /// (employee's payroll-area currency decimal count); that dependency chain is not ported yet (see
    /// <see cref="EmployeeInsurance"/> remarks), so a fixed 2-decimal rounding is used instead.
    /// </summary>
    internal static double Round(double value) => Math.Round(value, 2);

    private static EmployeeInsuranceDto ToDto(EmployeeInsurance entity)
    {
        return new EmployeeInsuranceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            InsurancePlanId = entity.InsurancePlanId,
            InsurancePlanNameEn = entity.InsurancePlan?.NameEn,
            InsuranceProviderId = entity.InsuranceProviderId,
            InsuranceProviderNameEn = entity.InsuranceProvider?.NameEn,
            DependantId = entity.DependantId,
            DependantNameEn = entity.Dependant?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            InstallmentStartDate = entity.InstallmentStartDate,
            InsuranceAmount = entity.InsuranceAmount,
            CompanyInterval = entity.CompanyInterval,
            CompanyContribution = entity.CompanyContribution,
            InstallmentCompanyDedAmount = entity.InstallmentCompanyDedAmount,
            BalanceCompanyContribution = entity.BalanceCompanyContribution,
            EmployeeInterval = entity.EmployeeInterval,
            EmployeeContribution = entity.EmployeeContribution,
            InstallmentEmployeeDedAmount = entity.InstallmentEmployeeDedAmount,
            BalanceEmployeeContribution = entity.BalanceEmployeeContribution,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditEmployeeInsuranceDto ToCreateOrEditDto(EmployeeInsurance entity)
    {
        return new CreateOrEditEmployeeInsuranceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            InsurancePlanId = entity.InsurancePlanId,
            InsuranceProviderId = entity.InsuranceProviderId,
            DependantId = entity.DependantId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            InstallmentStartDate = entity.InstallmentStartDate,
            InsuranceAmount = entity.InsuranceAmount,
            CompanyInterval = entity.CompanyInterval,
            CompanyContribution = entity.CompanyContribution,
            InstallmentCompanyDedAmount = entity.InstallmentCompanyDedAmount,
            EmployeeInterval = entity.EmployeeInterval,
            EmployeeContribution = entity.EmployeeContribution,
            InstallmentEmployeeDedAmount = entity.InstallmentEmployeeDedAmount,
            Comments = entity.Comments
        };
    }
}
