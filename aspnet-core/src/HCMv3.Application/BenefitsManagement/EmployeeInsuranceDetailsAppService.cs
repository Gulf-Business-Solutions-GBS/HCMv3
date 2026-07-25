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
/// Application service for <see cref="EmployeeInsuranceDetail"/>, the employee-side installment/recovery
/// ledger of an <see cref="EmployeeInsurance"/> (ported from legacy XAF EmployeeInsuranceDetail business
/// object). Legacy's <c>PerformValidation</c> and <c>UpdateDueBalance</c> cascade (recomputing the
/// parent's <see cref="EmployeeInsurance.BalanceEmployeeContribution"/> after every create/update/delete)
/// are ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_BenefitsManagement_EmployeeInsuranceDetails)]
public class EmployeeInsuranceDetailsAppService : HCMv3AppServiceBase, IEmployeeInsuranceDetailsAppService
{
    private readonly IRepository<EmployeeInsuranceDetail> _employeeInsuranceDetailRepository;
    private readonly IRepository<EmployeeInsurance> _employeeInsuranceRepository;

    public EmployeeInsuranceDetailsAppService(
        IRepository<EmployeeInsuranceDetail> employeeInsuranceDetailRepository,
        IRepository<EmployeeInsurance> employeeInsuranceRepository)
    {
        _employeeInsuranceDetailRepository = employeeInsuranceDetailRepository;
        _employeeInsuranceRepository = employeeInsuranceRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeInsuranceDetailDto>> GetAll(GetAllEmployeeInsuranceDetailsInput input)
    {
        input.Normalize();

        var query = _employeeInsuranceDetailRepository.GetAll()
            .WhereIf(input.EmployeeInsuranceId.HasValue, d => d.EmployeeInsuranceId == input.EmployeeInsuranceId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeInsuranceDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeInsuranceDetailForEditOutput> GetEmployeeInsuranceDetailForEdit(EntityDto input)
    {
        var entity = await _employeeInsuranceDetailRepository.GetAsync(input.Id);

        return new GetEmployeeInsuranceDetailForEditOutput
        {
            EmployeeInsuranceDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeInsuranceDetailDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_BenefitsManagement_EmployeeInsuranceDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _employeeInsuranceDetailRepository.GetAsync(input.Id);
        var employeeInsuranceId = entity.EmployeeInsuranceId;

        await _employeeInsuranceDetailRepository.DeleteAsync(input.Id);
        await RecalculateBalanceAsync(employeeInsuranceId);
    }

    [AbpAuthorize(AppPermissions.Pages_BenefitsManagement_EmployeeInsuranceDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeInsuranceDetailDto input)
    {
        var employeeInsurance = await _employeeInsuranceRepository.GetAsync(input.EmployeeInsuranceId);
        await ValidateAsync(input, employeeInsurance, excludeId: null);

        var entity = MapToEntity(input, new EmployeeInsuranceDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _employeeInsuranceDetailRepository.InsertAsync(entity);
        await RecalculateBalanceAsync(input.EmployeeInsuranceId);
    }

    [AbpAuthorize(AppPermissions.Pages_BenefitsManagement_EmployeeInsuranceDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeInsuranceDetailDto input)
    {
        var employeeInsurance = await _employeeInsuranceRepository.GetAsync(input.EmployeeInsuranceId);
        await ValidateAsync(input, employeeInsurance, excludeId: input.Id);

        var entity = await _employeeInsuranceDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _employeeInsuranceDetailRepository.UpdateAsync(entity);
        await RecalculateBalanceAsync(input.EmployeeInsuranceId);
    }

    /// <summary>
    /// Legacy <c>EmployeeInsuranceDetail.PerformValidation</c> parity.
    /// </summary>
    private async Task ValidateAsync(CreateOrEditEmployeeInsuranceDetailDto input, EmployeeInsurance employeeInsurance, int? excludeId)
    {
        var recoveredAmount = await _employeeInsuranceDetailRepository.GetAll()
            .Where(d => d.EmployeeInsuranceId == input.EmployeeInsuranceId && d.Id != (excludeId ?? 0))
            .SumAsync(d => (double?)d.InstallmentAmount) ?? 0;

        if (recoveredAmount + input.InstallmentAmount > employeeInsurance.EmployeeContribution)
        {
            throw new UserFriendlyException(L("InstallmentAmountExceedsBalance"));
        }

        if (input.InstallmentDate < employeeInsurance.InstallmentStartDate)
        {
            throw new UserFriendlyException(L("InstallmentDateBeforeInstallmentStartDate"));
        }
    }

    /// <summary>
    /// Legacy <c>UpdateDueBalance</c> parity: recompute and persist the parent's
    /// <see cref="EmployeeInsurance.BalanceEmployeeContribution"/>.
    /// </summary>
    private async Task RecalculateBalanceAsync(int employeeInsuranceId)
    {
        var employeeInsurance = await _employeeInsuranceRepository.GetAsync(employeeInsuranceId);

        var totalPaid = await _employeeInsuranceDetailRepository.GetAll()
            .Where(d => d.EmployeeInsuranceId == employeeInsuranceId)
            .SumAsync(d => (double?)d.InstallmentAmount) ?? 0;

        employeeInsurance.BalanceEmployeeContribution = EmployeeInsurancesAppService.Round(employeeInsurance.EmployeeContribution - totalPaid);

        await _employeeInsuranceRepository.UpdateAsync(employeeInsurance);
    }

    private static EmployeeInsuranceDetail MapToEntity(CreateOrEditEmployeeInsuranceDetailDto input, EmployeeInsuranceDetail entity)
    {
        entity.EmployeeInsuranceId = input.EmployeeInsuranceId;
        entity.InstallmentDate = input.InstallmentDate;
        entity.InstallmentAmount = input.InstallmentAmount;
        entity.InstallmentType = input.InstallmentType;

        return entity;
    }

    private static EmployeeInsuranceDetailDto ToDto(EmployeeInsuranceDetail entity)
    {
        return new EmployeeInsuranceDetailDto
        {
            Id = entity.Id,
            EmployeeInsuranceId = entity.EmployeeInsuranceId,
            InstallmentDate = entity.InstallmentDate,
            InstallmentAmount = entity.InstallmentAmount,
            InstallmentType = entity.InstallmentType
        };
    }

    private static CreateOrEditEmployeeInsuranceDetailDto ToCreateOrEditDto(EmployeeInsuranceDetail entity)
    {
        return new CreateOrEditEmployeeInsuranceDetailDto
        {
            Id = entity.Id,
            EmployeeInsuranceId = entity.EmployeeInsuranceId,
            InstallmentDate = entity.InstallmentDate,
            InstallmentAmount = entity.InstallmentAmount,
            InstallmentType = entity.InstallmentType
        };
    }
}
