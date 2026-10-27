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
/// Application service for <see cref="EmployeeGrievance"/> (ported from legacy XAF EmployeeGrievance
/// business object, Wave 4 deferred bucket). Ports the essential active validation only: required fields
/// and no duplicate Employee+GrievanceType+GrievanceDate.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGrievances)]
public class EmployeeGrievancesAppService : HCMv3AppServiceBase, IEmployeeGrievancesAppService
{
    private readonly IRepository<EmployeeGrievance> _repository;

    public EmployeeGrievancesAppService(IRepository<EmployeeGrievance> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeGrievanceDto>> GetAll(GetAllEmployeeGrievancesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.GrievanceType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeGrievanceDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeGrievanceForEditOutput> GetEmployeeGrievanceForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeGrievanceForEditOutput
        {
            EmployeeGrievance = new CreateOrEditEmployeeGrievanceDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                GrievanceTypeId = entity.GrievanceTypeId,
                GrievanceDate = entity.GrievanceDate,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeGrievanceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGrievances_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeGrievanceDto input)
    {
        var entity = MapToEntity(input, new EmployeeGrievance());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGrievances_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeGrievanceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGrievances_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmployeeGrievance entity)
    {
        var hasDuplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId
                && e.GrievanceTypeId == entity.GrievanceTypeId && e.GrievanceDate == entity.GrievanceDate);
        if (hasDuplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static EmployeeGrievance MapToEntity(CreateOrEditEmployeeGrievanceDto input, EmployeeGrievance entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.GrievanceTypeId = input.GrievanceTypeId;
        entity.GrievanceDate = input.GrievanceDate.Date;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeGrievanceDto ToDto(EmployeeGrievance entity)
    {
        return new EmployeeGrievanceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            GrievanceTypeId = entity.GrievanceTypeId,
            GrievanceTypeNameEn = entity.GrievanceType?.NameEn,
            GrievanceDate = entity.GrievanceDate,
            Comments = entity.Comments
        };
    }
}
