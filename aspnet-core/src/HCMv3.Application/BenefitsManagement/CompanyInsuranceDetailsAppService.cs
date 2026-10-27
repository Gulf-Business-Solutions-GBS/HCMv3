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
/// Application service for <see cref="CompanyInsuranceDetail"/>, the company-side installment/recovery
/// ledger of an <see cref="EmployeeInsurance"/> (ported from legacy XAF CompanyInsuranceDetail business
/// object). Legacy's <c>PerformValidation</c> and <c>UpdateDueBalance</c> cascade (recomputing the
/// parent's <see cref="EmployeeInsurance.BalanceCompanyContribution"/> after every create/update/delete)
/// are ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_BenefitsManagement_CompanyInsuranceDetails)]
public class CompanyInsuranceDetailsAppService : HCMv3AppServiceBase, ICompanyInsuranceDetailsAppService
{
    private readonly IRepository<CompanyInsuranceDetail> _companyInsuranceDetailRepository;
    private readonly IRepository<EmployeeInsurance> _employeeInsuranceRepository;

    public CompanyInsuranceDetailsAppService(
        IRepository<CompanyInsuranceDetail> companyInsuranceDetailRepository,
        IRepository<EmployeeInsurance> employeeInsuranceRepository)
    {
        _companyInsuranceDetailRepository = companyInsuranceDetailRepository;
        _employeeInsuranceRepository = employeeInsuranceRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CompanyInsuranceDetailDto>> GetAll(GetAllCompanyInsuranceDetailsInput input)
    {
        input.Normalize();

        var query = _companyInsuranceDetailRepository.GetAll()
            .WhereIf(input.EmployeeInsuranceId.HasValue, d => d.EmployeeInsuranceId == input.EmployeeInsuranceId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CompanyInsuranceDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCompanyInsuranceDetailForEditOutput> GetCompanyInsuranceDetailForEdit(EntityDto input)
    {
        var entity = await _companyInsuranceDetailRepository.GetAsync(input.Id);

        return new GetCompanyInsuranceDetailForEditOutput
        {
            CompanyInsuranceDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCompanyInsuranceDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_BenefitsManagement_CompanyInsuranceDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _companyInsuranceDetailRepository.GetAsync(input.Id);
        var employeeInsuranceId = entity.EmployeeInsuranceId;

        await _companyInsuranceDetailRepository.DeleteAsync(input.Id);
        await RecalculateBalanceAsync(employeeInsuranceId);
    }

    [AbpAuthorize(AppPermissions.Pages_BenefitsManagement_CompanyInsuranceDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCompanyInsuranceDetailDto input)
    {
        var employeeInsurance = await _employeeInsuranceRepository.GetAsync(input.EmployeeInsuranceId);
        await ValidateAsync(input, employeeInsurance, excludeId: null);

        var entity = MapToEntity(input, new CompanyInsuranceDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _companyInsuranceDetailRepository.InsertAsync(entity);
        await RecalculateBalanceAsync(input.EmployeeInsuranceId);
    }

    [AbpAuthorize(AppPermissions.Pages_BenefitsManagement_CompanyInsuranceDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCompanyInsuranceDetailDto input)
    {
        var employeeInsurance = await _employeeInsuranceRepository.GetAsync(input.EmployeeInsuranceId);
        await ValidateAsync(input, employeeInsurance, excludeId: input.Id);

        var entity = await _companyInsuranceDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _companyInsuranceDetailRepository.UpdateAsync(entity);
        await RecalculateBalanceAsync(input.EmployeeInsuranceId);
    }

    /// <summary>
    /// Legacy <c>CompanyInsuranceDetail.PerformValidation</c> parity.
    /// </summary>
    private async Task ValidateAsync(CreateOrEditCompanyInsuranceDetailDto input, EmployeeInsurance employeeInsurance, int? excludeId)
    {
        var recoveredAmount = await _companyInsuranceDetailRepository.GetAll()
            .Where(d => d.EmployeeInsuranceId == input.EmployeeInsuranceId && d.Id != (excludeId ?? 0))
            .SumAsync(d => (double?)d.InstallmentAmount) ?? 0;

        if (recoveredAmount + input.InstallmentAmount > employeeInsurance.CompanyContribution)
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
    /// <see cref="EmployeeInsurance.BalanceCompanyContribution"/>.
    /// </summary>
    private async Task RecalculateBalanceAsync(int employeeInsuranceId)
    {
        var employeeInsurance = await _employeeInsuranceRepository.GetAsync(employeeInsuranceId);

        var totalPaid = await _companyInsuranceDetailRepository.GetAll()
            .Where(d => d.EmployeeInsuranceId == employeeInsuranceId)
            .SumAsync(d => (double?)d.InstallmentAmount) ?? 0;

        employeeInsurance.BalanceCompanyContribution = EmployeeInsurancesAppService.Round(employeeInsurance.CompanyContribution - totalPaid);

        await _employeeInsuranceRepository.UpdateAsync(employeeInsurance);
    }

    private static CompanyInsuranceDetail MapToEntity(CreateOrEditCompanyInsuranceDetailDto input, CompanyInsuranceDetail entity)
    {
        entity.EmployeeInsuranceId = input.EmployeeInsuranceId;
        entity.InstallmentDate = input.InstallmentDate;
        entity.InstallmentAmount = input.InstallmentAmount;
        entity.InstallmentType = input.InstallmentType;

        return entity;
    }

    private static CompanyInsuranceDetailDto ToDto(CompanyInsuranceDetail entity)
    {
        return new CompanyInsuranceDetailDto
        {
            Id = entity.Id,
            EmployeeInsuranceId = entity.EmployeeInsuranceId,
            InstallmentDate = entity.InstallmentDate,
            InstallmentAmount = entity.InstallmentAmount,
            InstallmentType = entity.InstallmentType
        };
    }

    private static CreateOrEditCompanyInsuranceDetailDto ToCreateOrEditDto(CompanyInsuranceDetail entity)
    {
        return new CreateOrEditCompanyInsuranceDetailDto
        {
            Id = entity.Id,
            EmployeeInsuranceId = entity.EmployeeInsuranceId,
            InstallmentDate = entity.InstallmentDate,
            InstallmentAmount = entity.InstallmentAmount,
            InstallmentType = entity.InstallmentType
        };
    }
}
