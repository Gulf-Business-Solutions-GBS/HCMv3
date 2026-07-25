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
/// Application service for <see cref="EmployeePoolCar"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GatePassManagement.EmployeePoolCar</c>). Legacy <c>PerformValidation</c>
/// parity: Driver/PoolCar/StartDate/EndDate required, EndDate must not be earlier than StartDate.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GatePassManagement_EmployeePoolCars)]
public class EmployeePoolCarsAppService : HCMv3AppServiceBase, IEmployeePoolCarsAppService
{
    private readonly IRepository<EmployeePoolCar> _repository;

    public EmployeePoolCarsAppService(IRepository<EmployeePoolCar> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeePoolCarDto>> GetAll(GetAllEmployeePoolCarsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Driver)
            .Include(e => e.PoolCar)
            .WhereIf(input.DriverEmployeeId.HasValue, e => e.DriverEmployeeId == input.DriverEmployeeId)
            .WhereIf(input.PoolCarId.HasValue, e => e.PoolCarId == input.PoolCarId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeePoolCarDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeePoolCarForEditOutput> GetEmployeePoolCarForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeePoolCarForEditOutput
        {
            EmployeePoolCar = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeePoolCarDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_EmployeePoolCars_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_EmployeePoolCars_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeePoolCarDto input)
    {
        var entity = MapToEntity(input, new EmployeePoolCar());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDates(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_EmployeePoolCars_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeePoolCarDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateDates(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>Legacy parity: EndDate cannot be earlier than StartDate.</summary>
    private void ValidateDates(EmployeePoolCar entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeePoolCar MapToEntity(CreateOrEditEmployeePoolCarDto input, EmployeePoolCar entity)
    {
        entity.DriverEmployeeId = input.DriverEmployeeId;
        entity.PoolCarId = input.PoolCarId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.StartTime = input.StartTime;
        entity.EndTime = input.EndTime;
        entity.IsReturned = input.IsReturned;

        return entity;
    }

    private static EmployeePoolCarDto ToDto(EmployeePoolCar entity)
    {
        return new EmployeePoolCarDto
        {
            Id = entity.Id,
            DriverEmployeeId = entity.DriverEmployeeId,
            DriverFullName = entity.Driver?.FirstNameEn,
            PoolCarId = entity.PoolCarId,
            PoolCarPlateNumber = entity.PoolCar?.VehiclePlateNumber,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            IsReturned = entity.IsReturned
        };
    }

    private static CreateOrEditEmployeePoolCarDto ToCreateOrEditDto(EmployeePoolCar entity)
    {
        return new CreateOrEditEmployeePoolCarDto
        {
            Id = entity.Id,
            DriverEmployeeId = entity.DriverEmployeeId,
            PoolCarId = entity.PoolCarId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            IsReturned = entity.IsReturned
        };
    }
}
