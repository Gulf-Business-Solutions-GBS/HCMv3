using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GatePassManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Application service for the PoolCar master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GatePassManagement.PoolCar</c>). Legacy <c>PerformValidation</c> parity:
/// VehicleType/VehicleBrand/VehicleModel/VehiclePlateNumber/VehicleDocExpiryDate required.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GatePassManagement_PoolCars)]
public class PoolCarsAppService : HCMv3AppServiceBase, IPoolCarsAppService
{
    private readonly IRepository<PoolCar> _repository;

    public PoolCarsAppService(IRepository<PoolCar> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PoolCarDto>> GetAll(GetAllPoolCarsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(p => p.VehicleType)
            .Include(p => p.VehicleBrand)
            .Include(p => p.VehicleModel)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p => p.VehiclePlateNumber.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PoolCarDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPoolCarForEditOutput> GetPoolCarForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetPoolCarForEditOutput
        {
            PoolCar = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPoolCarDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_PoolCars_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_PoolCars_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPoolCarDto input)
    {
        var entity = MapToEntity(input, new PoolCar());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_PoolCars_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPoolCarDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static PoolCar MapToEntity(CreateOrEditPoolCarDto input, PoolCar entity)
    {
        entity.VehicleTypeId = input.VehicleTypeId;
        entity.VehiclePlateNumber = input.VehiclePlateNumber;
        entity.VehicleDocExpiryDate = input.VehicleDocExpiryDate;
        entity.VehicleBrandId = input.VehicleBrandId;
        entity.VehicleModelId = input.VehicleModelId;

        return entity;
    }

    private static PoolCarDto ToDto(PoolCar entity)
    {
        return new PoolCarDto
        {
            Id = entity.Id,
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

    private static CreateOrEditPoolCarDto ToCreateOrEditDto(PoolCar entity)
    {
        return new CreateOrEditPoolCarDto
        {
            Id = entity.Id,
            VehicleTypeId = entity.VehicleTypeId,
            VehiclePlateNumber = entity.VehiclePlateNumber,
            VehicleDocExpiryDate = entity.VehicleDocExpiryDate,
            VehicleBrandId = entity.VehicleBrandId,
            VehicleModelId = entity.VehicleModelId
        };
    }
}
