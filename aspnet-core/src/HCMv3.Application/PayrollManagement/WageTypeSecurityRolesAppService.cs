using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the role-based access-restriction list consumed when
/// <see cref="WageType.IsSecured"/> is true (ported from legacy XAF WageTypeSecurityRole business object),
/// listed separately and linked back via <see cref="WageTypeSecurityRole.WageTypeId"/>. FKs ASP.NET Zero's
/// own built-in Role entity — see <see cref="WageTypeSecurityRole"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeSecurityRoles)]
public class WageTypeSecurityRolesAppService : HCMv3AppServiceBase, IWageTypeSecurityRolesAppService
{
    private readonly IRepository<WageTypeSecurityRole> _wageTypeSecurityRoleRepository;

    public WageTypeSecurityRolesAppService(IRepository<WageTypeSecurityRole> wageTypeSecurityRoleRepository)
    {
        _wageTypeSecurityRoleRepository = wageTypeSecurityRoleRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WageTypeSecurityRoleDto>> GetAll(GetAllWageTypeSecurityRolesInput input)
    {
        input.Normalize();

        var query = _wageTypeSecurityRoleRepository.GetAll()
            .Include(r => r.WageType)
            .Include(r => r.Role)
            .WhereIf(input.WageTypeId.HasValue, r => r.WageTypeId == input.WageTypeId)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Role.DisplayName.Contains(input.Filter) ||
                r.Role.Name.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WageTypeSecurityRoleDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetWageTypeSecurityRoleForEditOutput> GetWageTypeSecurityRoleForEdit(EntityDto input)
    {
        var entity = await _wageTypeSecurityRoleRepository.GetAsync(input.Id);

        return new GetWageTypeSecurityRoleForEditOutput
        {
            WageTypeSecurityRole = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditWageTypeSecurityRoleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeSecurityRoles_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _wageTypeSecurityRoleRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeSecurityRoles_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWageTypeSecurityRoleDto input)
    {
        var entity = MapToEntity(input, new WageTypeSecurityRole());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _wageTypeSecurityRoleRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeSecurityRoles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWageTypeSecurityRoleDto input)
    {
        var entity = await _wageTypeSecurityRoleRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _wageTypeSecurityRoleRepository.UpdateAsync(entity);
    }

    private static WageTypeSecurityRole MapToEntity(CreateOrEditWageTypeSecurityRoleDto input, WageTypeSecurityRole entity)
    {
        entity.WageTypeId = input.WageTypeId;
        entity.RoleId = input.RoleId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate RoleId per WageType.
    /// </summary>
    private async Task ValidateAsync(WageTypeSecurityRole entity)
    {
        var duplicate = await _wageTypeSecurityRoleRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           r.WageTypeId == entity.WageTypeId &&
                           r.RoleId == entity.RoleId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("RoleAlreadyAssigned"));
        }
    }

    private static WageTypeSecurityRoleDto ToDto(WageTypeSecurityRole entity)
    {
        return new WageTypeSecurityRoleDto
        {
            Id = entity.Id,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            RoleId = entity.RoleId,
            RoleName = entity.Role?.DisplayName
        };
    }

    private static CreateOrEditWageTypeSecurityRoleDto ToCreateOrEditDto(WageTypeSecurityRole entity)
    {
        return new CreateOrEditWageTypeSecurityRoleDto
        {
            Id = entity.Id,
            WageTypeId = entity.WageTypeId,
            RoleId = entity.RoleId
        };
    }
}
