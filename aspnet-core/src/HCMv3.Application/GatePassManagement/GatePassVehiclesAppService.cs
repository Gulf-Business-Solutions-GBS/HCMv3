using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GatePassManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Application service for <see cref="GatePassVehicle"/>, the aggregated child of <see cref="GatePass"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GatePassManagement.GatePassVehicle</c>). Legacy
/// <c>PerformValidation</c> parity: VehicleType/VehicleBrand/VehicleModel/VehiclePlateNumber/
/// VehicleDocExpiryDate required, and no duplicate VehiclePlateNumber within the same gate pass.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassVehicles)]
public class GatePassVehiclesAppService : HCMv3AppServiceBase, IGatePassVehiclesAppService
{
    private readonly IRepository<GatePassVehicle> _repository;

    public GatePassVehiclesAppService(IRepository<GatePassVehicle> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<GatePassVehicleDto>> GetAll(GetAllGatePassVehiclesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(v => v.VehicleType)
            .Include(v => v.VehicleBrand)
            .Include(v => v.VehicleModel)
            .WhereIf(input.GatePassId.HasValue, v => v.GatePassId == input.GatePassId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<GatePassVehicleDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetGatePassVehicleForEditOutput> GetGatePassVehicleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetGatePassVehicleForEditOutput
        {
            GatePassVehicle = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditGatePassVehicleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassVehicles_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassVehicles_Create)]
    protected virtual async Task CreateAsync(CreateOrEditGatePassVehicleDto input)
    {
        var entity = MapToEntity(input, new GatePassVehicle());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateNoDuplicatePlateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassVehicles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditGatePassVehicleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateNoDuplicatePlateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>Legacy parity: within the same gate pass, VehiclePlateNumber cannot repeat.</summary>
    private async Task ValidateNoDuplicatePlateAsync(GatePassVehicle entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(v => v.Id != entity.Id &&
                           v.GatePassId == entity.GatePassId &&
                           v.VehiclePlateNumber == entity.VehiclePlateNumber);

        if (duplicate)
        {
            throw new UserFriendlyException(L("GatePassVehiclePlateNumberAlreadyExists"));
        }
    }

    private static GatePassVehicle MapToEntity(CreateOrEditGatePassVehicleDto input, GatePassVehicle entity)
    {
        entity.GatePassId = input.GatePassId;
        entity.VehicleTypeId = input.VehicleTypeId;
        entity.VehiclePlateNumber = input.VehiclePlateNumber;
        entity.VehicleDocExpiryDate = input.VehicleDocExpiryDate;
        entity.VehicleBrandId = input.VehicleBrandId;
        entity.VehicleModelId = input.VehicleModelId;

        return entity;
    }

    private static GatePassVehicleDto ToDto(GatePassVehicle entity)
    {
        return new GatePassVehicleDto
        {
            Id = entity.Id,
            GatePassId = entity.GatePassId,
            VehicleTypeId = entity.VehicleTypeId,
            VehicleTypeNameEn = entity.VehicleType?.NameEn,
            VehiclePlateNumber = entity.VehiclePlateNumber,
            VehicleDocExpiryDate = entity.VehicleDocExpiryDate,
            VehicleBrandId = entity.VehicleBrandId,
            VehicleBrandNameEn = entity.VehicleBrand?.NameEn,
            VehicleModelId = entity.VehicleModelId,
            VehicleModelNameEn = entity.VehicleModel?.NameEn
        };
    }

    private static CreateOrEditGatePassVehicleDto ToCreateOrEditDto(GatePassVehicle entity)
    {
        return new CreateOrEditGatePassVehicleDto
        {
            Id = entity.Id,
            GatePassId = entity.GatePassId,
            VehicleTypeId = entity.VehicleTypeId,
            VehiclePlateNumber = entity.VehiclePlateNumber,
            VehicleDocExpiryDate = entity.VehicleDocExpiryDate,
            VehicleBrandId = entity.VehicleBrandId,
            VehicleModelId = entity.VehicleModelId
        };
    }
}
