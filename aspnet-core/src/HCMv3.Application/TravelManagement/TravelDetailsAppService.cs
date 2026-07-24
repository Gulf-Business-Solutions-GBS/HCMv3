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
/// Application service for <see cref="TravelDetail"/>, the aggregated child of <see cref="Travel"/> (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.TravelManagement.TravelDetail</c>). Legacy
/// <c>PerformValidation</c> parity ported here: ArrivalDate must not be earlier than DepartureDate. Delete is
/// blocked when child <see cref="TravelDetailCost"/> rows exist (legacy <c>OnDeleting</c> parity). See
/// <see cref="TravelDetail"/> class remarks for the dropped legacy <c>PerDiemRate</c>/<c>PerDiemAmount</c>
/// non-persistent properties, the dropped <c>SavePerDiemAmount()</c> auto-creation side effect, and the
/// TotalAmount-rollup simplification.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetails)]
public class TravelDetailsAppService : HCMv3AppServiceBase, ITravelDetailsAppService
{
    private readonly IRepository<TravelDetail> _repository;
    private readonly IRepository<TravelDetailCost> _travelDetailCostRepository;
    private readonly IRepository<TravelPerDiem> _travelPerDiemRepository;

    public TravelDetailsAppService(
        IRepository<TravelDetail> repository,
        IRepository<TravelDetailCost> travelDetailCostRepository,
        IRepository<TravelPerDiem> travelPerDiemRepository)
    {
        _repository = repository;
        _travelDetailCostRepository = travelDetailCostRepository;
        _travelPerDiemRepository = travelPerDiemRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TravelDetailDto>> GetAll(GetAllTravelDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.Departure)
            .Include(d => d.Arrival)
            .Include(d => d.Hotel)
            .Include(d => d.TravelAirline)
            .WhereIf(input.TravelId.HasValue, d => d.TravelId == input.TravelId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TravelDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTravelDetailForEditOutput> GetTravelDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTravelDetailForEditOutput
        {
            TravelDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTravelDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTravelDetailDto input)
    {
        var entity = MapToEntity(input, new TravelDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDates(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTravelDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateDates(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>OnDeleting</c> parity: block delete while child <see cref="TravelDetailCost"/> rows exist.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var hasCosts = await _travelDetailCostRepository.GetAll()
            .AnyAsync(c => c.TravelDetailId == input.Id);
        if (hasCosts)
        {
            throw new UserFriendlyException(L("DeleteTravelDetailCostsFirst"));
        }

        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// On-demand replacement for the legacy XAF non-persistent <c>PerDiemRate</c>/<c>PerDiemAmount</c>
    /// properties -- see <see cref="TravelDetail"/> class remarks and
    /// <see cref="Dto.CalculateExpectedPerDiemInput"/> remarks (EmployeeGroup/EmployeeSubGroup must be
    /// supplied by the caller in this codebase, since <c>EmployeeOrgAssignment</c> doesn't carry those FKs).
    /// </summary>
    public async Task<CalculateExpectedPerDiemOutput> CalculateExpectedPerDiemAsync(CalculateExpectedPerDiemInput input)
    {
        var detail = await _repository.GetAsync(input.TravelDetailId);

        var rate = await _travelPerDiemRepository.GetAll()
            .Where(p => p.FromDestinationId == detail.DepartureId &&
                        p.ToDestinationId == detail.ArrivalId &&
                        p.EmployeeGroupId == input.EmployeeGroupId &&
                        p.EmployeeSubGroupId == input.EmployeeSubGroupId)
            .Select(p => (double?)p.Amount)
            .FirstOrDefaultAsync() ?? 0;

        var amount = System.Math.Round(detail.TotalNights * rate, 3);

        return new CalculateExpectedPerDiemOutput
        {
            PerDiemRate = rate,
            PerDiemAmount = amount
        };
    }

    private void ValidateDates(TravelDetail entity)
    {
        if (entity.ArrivalDate.Date < entity.DepartureDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static TravelDetail MapToEntity(CreateOrEditTravelDetailDto input, TravelDetail entity)
    {
        entity.TravelId = input.TravelId;
        entity.DepartureId = input.DepartureId;
        entity.ArrivalId = input.ArrivalId;
        entity.DepartureDate = input.DepartureDate;
        entity.DepartureTime = input.DepartureTime;
        entity.ArrivalDate = input.ArrivalDate;
        entity.ArrivalTime = input.ArrivalTime;
        entity.HotelId = input.HotelId;
        entity.TravelAirlineId = input.TravelAirlineId;
        entity.NightStay = input.NightStay;
        entity.TotalNights = input.TotalNights;
        entity.Comments = input.Comments;

        return entity;
    }

    private static TravelDetailDto ToDto(TravelDetail entity)
    {
        return new TravelDetailDto
        {
            Id = entity.Id,
            TravelId = entity.TravelId,
            DepartureId = entity.DepartureId,
            DepartureNameEn = entity.Departure?.NameEn,
            ArrivalId = entity.ArrivalId,
            ArrivalNameEn = entity.Arrival?.NameEn,
            DepartureDate = entity.DepartureDate,
            DepartureTime = entity.DepartureTime,
            ArrivalDate = entity.ArrivalDate,
            ArrivalTime = entity.ArrivalTime,
            HotelId = entity.HotelId,
            HotelNameEn = entity.Hotel?.NameEn,
            TravelAirlineId = entity.TravelAirlineId,
            TravelAirlineNameEn = entity.TravelAirline?.NameEn,
            NightStay = entity.NightStay,
            TotalNights = entity.TotalNights,
            TotalAmount = entity.TotalAmount,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditTravelDetailDto ToCreateOrEditDto(TravelDetail entity)
    {
        return new CreateOrEditTravelDetailDto
        {
            Id = entity.Id,
            TravelId = entity.TravelId,
            DepartureId = entity.DepartureId,
            ArrivalId = entity.ArrivalId,
            DepartureDate = entity.DepartureDate,
            DepartureTime = entity.DepartureTime,
            ArrivalDate = entity.ArrivalDate,
            ArrivalTime = entity.ArrivalTime,
            HotelId = entity.HotelId,
            TravelAirlineId = entity.TravelAirlineId,
            NightStay = entity.NightStay,
            TotalNights = entity.TotalNights,
            Comments = entity.Comments
        };
    }
}
