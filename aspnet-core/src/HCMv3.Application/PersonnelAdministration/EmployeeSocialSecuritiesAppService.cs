using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeSocialSecurity"/> (ported from legacy XAF
/// EmployeeSocialSecurity business object, Wave 3 deferred bucket). The legacy auto-end-dating cascade
/// (each new record automatically closes the previous open-ended record's EndDate) is NOT ported here --
/// same simplification approach as Wave 2's <c>EmployeeOnHold</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurities)]
public class EmployeeSocialSecuritiesAppService : HCMv3AppServiceBase, IEmployeeSocialSecuritiesAppService
{
    private readonly IRepository<EmployeeSocialSecurity> _repository;

    public EmployeeSocialSecuritiesAppService(IRepository<EmployeeSocialSecurity> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSocialSecurityDto>> GetAll(GetAllEmployeeSocialSecuritiesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(s => s.Employee)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSocialSecurityDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSocialSecurityForEditOutput> GetEmployeeSocialSecurityForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeSocialSecurityForEditOutput
        {
            EmployeeSocialSecurity = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSocialSecurityDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurities_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurities_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSocialSecurityDto input)
    {
        var entity = MapToEntity(input, new EmployeeSocialSecurity());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurities_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSocialSecurityDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static EmployeeSocialSecurity MapToEntity(CreateOrEditEmployeeSocialSecurityDto input, EmployeeSocialSecurity entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.SocialSecurityNum = input.SocialSecurityNum;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.SocialSecurityContrib = input.SocialSecurityContrib;
        return entity;
    }

    private static EmployeeSocialSecurityDto ToDto(EmployeeSocialSecurity entity)
    {
        return new EmployeeSocialSecurityDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            SocialSecurityNum = entity.SocialSecurityNum,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            SocialSecurityContrib = entity.SocialSecurityContrib
        };
    }

    private static CreateOrEditEmployeeSocialSecurityDto ToCreateOrEditDto(EmployeeSocialSecurity entity)
    {
        return new CreateOrEditEmployeeSocialSecurityDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            SocialSecurityNum = entity.SocialSecurityNum,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            SocialSecurityContrib = entity.SocialSecurityContrib
        };
    }
}
