using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeOvertime"/> (ported from legacy XAF EmployeeOvertime business
/// object, Wave 4 deferred bucket). Ports the essential active validation only: required fields and no
/// duplicate Employee+EffectiveDate. The legacy <c>ExpectedOvertimeAmount</c> wage-rate computation and the
/// EmployeeSubGroup.PayOvertime cross-check are NOT ported -- see class remarks on
/// <see cref="EmployeeOvertime"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes)]
public class EmployeeOvertimesAppService : HCMv3AppServiceBase, IEmployeeOvertimesAppService
{
    private readonly IRepository<EmployeeOvertime> _repository;

    public EmployeeOvertimesAppService(IRepository<EmployeeOvertime> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeOvertimeDto>> GetAll(GetAllEmployeeOvertimesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.OvertimeType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeOvertimeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeOvertimeForEditOutput> GetEmployeeOvertimeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeOvertimeForEditOutput
        {
            EmployeeOvertime = new CreateOrEditEmployeeOvertimeDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EffectiveDate = entity.EffectiveDate,
                OvertimeTypeId = entity.OvertimeTypeId,
                OvertimeHours = entity.OvertimeHours,
                ActualOvertimeHours = entity.ActualOvertimeHours,
                ApprovedOvertimeHours = entity.ApprovedOvertimeHours,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeOvertimeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeOvertimeDto input)
    {
        var entity = MapToEntity(input, new EmployeeOvertime());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeOvertimeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmployeeOvertime entity)
    {
        var hasDuplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId && e.EffectiveDate == entity.EffectiveDate);
        if (hasDuplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static EmployeeOvertime MapToEntity(CreateOrEditEmployeeOvertimeDto input, EmployeeOvertime entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.EffectiveDate = input.EffectiveDate.Date;
        entity.OvertimeTypeId = input.OvertimeTypeId;
        entity.OvertimeHours = input.OvertimeHours;
        entity.ActualOvertimeHours = input.ActualOvertimeHours;
        entity.ApprovedOvertimeHours = input.ApprovedOvertimeHours;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeOvertimeDto ToDto(EmployeeOvertime entity)
    {
        return new EmployeeOvertimeDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            EffectiveDate = entity.EffectiveDate,
            OvertimeTypeId = entity.OvertimeTypeId,
            OvertimeTypeNameEn = entity.OvertimeType?.NameEn,
            OvertimeHours = entity.OvertimeHours,
            ActualOvertimeHours = entity.ActualOvertimeHours,
            ApprovedOvertimeHours = entity.ApprovedOvertimeHours,
            Comments = entity.Comments
        };
    }
}
