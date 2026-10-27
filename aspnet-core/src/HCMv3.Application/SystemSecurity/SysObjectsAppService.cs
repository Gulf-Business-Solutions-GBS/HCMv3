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
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="SysObject"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.SysObject). No PerformValidation existed in the legacy source
/// beyond required Name -- enforced here via unique Name.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysObjects)]
public class SysObjectsAppService : HCMv3AppServiceBase, ISysObjectsAppService
{
    private readonly IRepository<SysObject> _repository;

    public SysObjectsAppService(IRepository<SysObject> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SysObjectDto>> GetAll(GetAllSysObjectsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), o =>
                o.Name.Contains(input.Filter) || o.FullName.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SysObjectDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSysObjectForEditOutput> GetSysObjectForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSysObjectForEditOutput
        {
            SysObject = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSysObjectDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysObjects_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysObjects_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSysObjectDto input)
    {
        var entity = MapToEntity(input, new SysObject());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysObjects_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSysObjectDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(SysObject entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(o => o.Id != entity.Id && o.Name == entity.Name);
        if (duplicate)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static SysObject MapToEntity(CreateOrEditSysObjectDto input, SysObject entity)
    {
        entity.Name = input.Name;
        entity.FullName = input.FullName;
        entity.IsPayrollSensitive = input.IsPayrollSensitive;
        entity.RespectPermission = input.RespectPermission;
        entity.MustRetroRun = input.MustRetroRun;
        entity.IsTimeSensitive = input.IsTimeSensitive;

        return entity;
    }

    private static SysObjectDto ToDto(SysObject entity)
    {
        return new SysObjectDto
        {
            Id = entity.Id,
            Name = entity.Name,
            FullName = entity.FullName,
            IsPayrollSensitive = entity.IsPayrollSensitive,
            RespectPermission = entity.RespectPermission,
            MustRetroRun = entity.MustRetroRun,
            IsTimeSensitive = entity.IsTimeSensitive
        };
    }

    private static CreateOrEditSysObjectDto ToCreateOrEditDto(SysObject entity)
    {
        return new CreateOrEditSysObjectDto
        {
            Id = entity.Id,
            Name = entity.Name,
            FullName = entity.FullName,
            IsPayrollSensitive = entity.IsPayrollSensitive,
            RespectPermission = entity.RespectPermission,
            MustRetroRun = entity.MustRetroRun,
            IsTimeSensitive = entity.IsTimeSensitive
        };
    }
}
