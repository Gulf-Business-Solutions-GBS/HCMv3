using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="SysUserOrgUnitData"/> -- Row-Level-Security scoping DATA ONLY (see
/// entity remarks). No runtime query-filtering enforcement is implemented here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysUserOrgUnitData)]
public class SysUserOrgUnitDataAppService : HCMv3AppServiceBase, ISysUserOrgUnitDataAppService
{
    private readonly IRepository<SysUserOrgUnitData> _repository;

    public SysUserOrgUnitDataAppService(IRepository<SysUserOrgUnitData> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SysUserOrgUnitDataDto>> GetAll(GetAllSysUserOrgUnitDataInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.Role)
            .Include(e => e.OrgLevel)
            .Include(e => e.OrganizationUnit)
            .WhereIf(input.RoleId.HasValue, e => e.RoleId == input.RoleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SysUserOrgUnitDataDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSysUserOrgUnitDataForEditOutput> GetSysUserOrgUnitDataForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSysUserOrgUnitDataForEditOutput
        {
            SysUserOrgUnitData = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSysUserOrgUnitDataDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysUserOrgUnitData_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysUserOrgUnitData_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSysUserOrgUnitDataDto input)
    {
        var entity = MapToEntity(input, new SysUserOrgUnitData());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysUserOrgUnitData_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSysUserOrgUnitDataDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static SysUserOrgUnitData MapToEntity(CreateOrEditSysUserOrgUnitDataDto input, SysUserOrgUnitData entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.RoleId = input.RoleId;
        entity.OrgLevelId = input.OrgLevelId;
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.ShowUnlocked = input.ShowUnlocked;

        return entity;
    }

    private static SysUserOrgUnitDataDto ToDto(SysUserOrgUnitData entity)
    {
        return new SysUserOrgUnitDataDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeFullName = entity.Employee != null ? entity.Employee.FirstNameEn : null,
            RoleId = entity.RoleId,
            RoleDisplayName = entity.Role?.DisplayName,
            OrgLevelId = entity.OrgLevelId,
            OrgLevelNameEn = entity.OrgLevel?.NameEn,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitDisplayName = entity.OrganizationUnit?.DisplayName,
            ShowUnlocked = entity.ShowUnlocked
        };
    }

    private static CreateOrEditSysUserOrgUnitDataDto ToCreateOrEditDto(SysUserOrgUnitData entity)
    {
        return new CreateOrEditSysUserOrgUnitDataDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            RoleId = entity.RoleId,
            OrgLevelId = entity.OrgLevelId,
            OrganizationUnitId = entity.OrganizationUnitId,
            ShowUnlocked = entity.ShowUnlocked
        };
    }
}
