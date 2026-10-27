using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.AccommodationManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// Application service for the employee accommodation assignment header (ported from legacy XAF
/// EmployeeAccommodation business object). Its <c>Services</c> aggregated child collection is exposed
/// separately via <see cref="AccommodationServicesAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_EmployeeAccommodations)]
public class EmployeeAccommodationsAppService : HCMv3AppServiceBase, IEmployeeAccommodationsAppService
{
    private readonly IRepository<EmployeeAccommodation> _employeeAccommodationRepository;

    public EmployeeAccommodationsAppService(IRepository<EmployeeAccommodation> employeeAccommodationRepository)
    {
        _employeeAccommodationRepository = employeeAccommodationRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAccommodationDto>> GetAll(GetAllEmployeeAccommodationsInput input)
    {
        input.Normalize();

        var query = _employeeAccommodationRepository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.AccommodationType)
            .Include(a => a.Location)
            .Include(a => a.Building)
            .Include(a => a.Floor)
            .Include(a => a.Flat)
            .Include(a => a.Room)
            .Include(a => a.Bed)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAccommodationDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAccommodationForEditOutput> GetEmployeeAccommodationForEdit(EntityDto input)
    {
        var entity = await _employeeAccommodationRepository.GetAsync(input.Id);

        return new GetEmployeeAccommodationForEditOutput
        {
            EmployeeAccommodation = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAccommodationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_EmployeeAccommodations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeAccommodationRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_EmployeeAccommodations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAccommodationDto input)
    {
        var entity = MapToEntity(input, new EmployeeAccommodation());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeAccommodationRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_EmployeeAccommodations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAccommodationDto input)
    {
        var entity = await _employeeAccommodationRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeAccommodationRepository.UpdateAsync(entity);
    }

    private static EmployeeAccommodation MapToEntity(CreateOrEditEmployeeAccommodationDto input, EmployeeAccommodation entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.AccommodationTypeId = input.AccommodationTypeId;
        entity.DeductionAmount = input.DeductionAmount;
        entity.LocationId = input.LocationId;
        entity.BuildingId = input.BuildingId;
        entity.FloorId = input.FloorId;
        entity.FlatId = input.FlatId;
        entity.RoomId = input.RoomId;
        entity.BedId = input.BedId;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlapping date range with
    /// another (non-deleted) accommodation assignment for the same employee.
    /// </summary>
    private async Task ValidateAsync(EmployeeAccommodation entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlaps = await _employeeAccommodationRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id
                           && a.EmployeeId == entity.EmployeeId
                           && a.StartDate.Date <= entity.EndDate.Date
                           && a.EndDate.Date >= entity.StartDate.Date);
        if (overlaps)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeAccommodationDto ToDto(EmployeeAccommodation entity)
    {
        return new EmployeeAccommodationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            AccommodationTypeId = entity.AccommodationTypeId,
            AccommodationTypeName = entity.AccommodationType?.NameEn,
            DeductionAmount = entity.DeductionAmount,
            LocationId = entity.LocationId,
            LocationName = entity.Location?.NameEn,
            BuildingId = entity.BuildingId,
            BuildingName = entity.Building?.NameEn,
            FloorId = entity.FloorId,
            FloorName = entity.Floor?.NameEn,
            FlatId = entity.FlatId,
            FlatName = entity.Flat?.NameEn,
            RoomId = entity.RoomId,
            RoomName = entity.Room?.NameEn,
            BedId = entity.BedId,
            BedName = entity.Bed?.NameEn,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditEmployeeAccommodationDto ToCreateOrEditDto(EmployeeAccommodation entity)
    {
        return new CreateOrEditEmployeeAccommodationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            AccommodationTypeId = entity.AccommodationTypeId,
            DeductionAmount = entity.DeductionAmount,
            LocationId = entity.LocationId,
            BuildingId = entity.BuildingId,
            FloorId = entity.FloorId,
            FlatId = entity.FlatId,
            RoomId = entity.RoomId,
            BedId = entity.BedId,
            Comments = entity.Comments
        };
    }
}
