using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TravelManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TravelManagement;

/// <summary>
/// Application service for <see cref="TravelPerDiem"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelPerDiem</c>). Legacy <c>PerformValidation</c>
/// parity: the combination of FromDestination/ToDestination/EmployeeGroup/EmployeeSubGroup must be unique.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelPerDiems)]
public class TravelPerDiemsAppService : HCMv3AppServiceBase, ITravelPerDiemsAppService
{
    private readonly IRepository<TravelPerDiem> _repository;

    public TravelPerDiemsAppService(IRepository<TravelPerDiem> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TravelPerDiemDto>> GetAll(GetAllTravelPerDiemsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(p => p.FromDestination)
            .Include(p => p.ToDestination)
            .Include(p => p.EmployeeGroup)
            .Include(p => p.EmployeeSubGroup)
            .WhereIf(input.FromDestinationId.HasValue, p => p.FromDestinationId == input.FromDestinationId)
            .WhereIf(input.ToDestinationId.HasValue, p => p.ToDestinationId == input.ToDestinationId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TravelPerDiemDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTravelPerDiemForEditOutput> GetTravelPerDiemForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTravelPerDiemForEditOutput
        {
            TravelPerDiem = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTravelPerDiemDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelPerDiems_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTravelPerDiemDto input)
    {
        var entity = MapToEntity(input, new TravelPerDiem());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelPerDiems_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTravelPerDiemDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelPerDiems_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: uniqueness across FromDestination/ToDestination/EmployeeGroup/
    /// EmployeeSubGroup.
    /// </summary>
    private async Task ValidateUniquenessAsync(TravelPerDiem entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id &&
                           p.FromDestinationId == entity.FromDestinationId &&
                           p.ToDestinationId == entity.ToDestinationId &&
                           p.EmployeeGroupId == entity.EmployeeGroupId &&
                           p.EmployeeSubGroupId == entity.EmployeeSubGroupId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("TravelPerDiemAlreadyExists"));
        }
    }

    private static TravelPerDiem MapToEntity(CreateOrEditTravelPerDiemDto input, TravelPerDiem entity)
    {
        entity.FromDestinationId = input.FromDestinationId;
        entity.ToDestinationId = input.ToDestinationId;
        entity.EmployeeGroupId = input.EmployeeGroupId;
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.Amount = input.Amount;

        return entity;
    }

    private static TravelPerDiemDto ToDto(TravelPerDiem entity)
    {
        return new TravelPerDiemDto
        {
            Id = entity.Id,
            FromDestinationId = entity.FromDestinationId,
            FromDestinationNameEn = entity.FromDestination?.NameEn,
            ToDestinationId = entity.ToDestinationId,
            ToDestinationNameEn = entity.ToDestination?.NameEn,
            EmployeeGroupId = entity.EmployeeGroupId,
            EmployeeGroupNameEn = entity.EmployeeGroup?.NameEn,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupNameEn = entity.EmployeeSubGroup?.NameEn,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditTravelPerDiemDto ToCreateOrEditDto(TravelPerDiem entity)
    {
        return new CreateOrEditTravelPerDiemDto
        {
            Id = entity.Id,
            FromDestinationId = entity.FromDestinationId,
            ToDestinationId = entity.ToDestinationId,
            EmployeeGroupId = entity.EmployeeGroupId,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            Amount = entity.Amount
        };
    }
}
