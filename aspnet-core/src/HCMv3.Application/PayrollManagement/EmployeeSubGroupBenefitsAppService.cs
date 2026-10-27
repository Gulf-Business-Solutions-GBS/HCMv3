using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the benefit-type entitlement header of an <see cref="EmployeeSubGroup"/> (ported
/// from legacy XAF EmployeeSubGroupBenefit business object), listed separately and linked back via
/// <see cref="EmployeeSubGroupBenefit.EmployeeSubGroupId"/>. Its own child is
/// <see cref="EmployeeSubGroupBenefitDetail"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefits)]
public class EmployeeSubGroupBenefitsAppService : HCMv3AppServiceBase, IEmployeeSubGroupBenefitsAppService
{
    private readonly IRepository<EmployeeSubGroupBenefit> _employeeSubGroupBenefitRepository;

    public EmployeeSubGroupBenefitsAppService(IRepository<EmployeeSubGroupBenefit> employeeSubGroupBenefitRepository)
    {
        _employeeSubGroupBenefitRepository = employeeSubGroupBenefitRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupBenefitDto>> GetAll(GetAllEmployeeSubGroupBenefitsInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupBenefitRepository.GetAll()
            .Include(b => b.EmployeeSubGroup)
            .Include(b => b.BenefitType)
            .WhereIf(input.EmployeeSubGroupId.HasValue, b => b.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupBenefitDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupBenefitForEditOutput> GetEmployeeSubGroupBenefitForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupBenefitRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupBenefitForEditOutput
        {
            EmployeeSubGroupBenefit = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupBenefitDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefits_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupBenefitRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefits_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupBenefitDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupBenefit());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _employeeSubGroupBenefitRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupBenefitDto input)
    {
        var entity = await _employeeSubGroupBenefitRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _employeeSubGroupBenefitRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupBenefit MapToEntity(CreateOrEditEmployeeSubGroupBenefitDto input, EmployeeSubGroupBenefit entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.BenefitTypeId = input.BenefitTypeId;
        entity.MaxAmount = input.MaxAmount;

        return entity;
    }

    private static EmployeeSubGroupBenefitDto ToDto(EmployeeSubGroupBenefit entity)
    {
        return new EmployeeSubGroupBenefitDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            BenefitTypeId = entity.BenefitTypeId,
            BenefitTypeName = entity.BenefitType?.NameEn,
            MaxAmount = entity.MaxAmount
        };
    }

    private static CreateOrEditEmployeeSubGroupBenefitDto ToCreateOrEditDto(EmployeeSubGroupBenefit entity)
    {
        return new CreateOrEditEmployeeSubGroupBenefitDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            BenefitTypeId = entity.BenefitTypeId,
            MaxAmount = entity.MaxAmount
        };
    }
}
