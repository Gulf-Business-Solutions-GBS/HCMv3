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
/// Application service for the date-range service step of an <see cref="EmployeeAccommodation"/> (ported
/// from legacy XAF AccommodationServices business object), listed separately and linked back via
/// <see cref="AccommodationServices.EmployeeAccommodationId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationServices)]
public class AccommodationServicesAppService : HCMv3AppServiceBase, IAccommodationServicesAppService
{
    private readonly IRepository<AccommodationServices> _accommodationServicesRepository;

    public AccommodationServicesAppService(IRepository<AccommodationServices> accommodationServicesRepository)
    {
        _accommodationServicesRepository = accommodationServicesRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AccommodationServicesDto>> GetAll(GetAllAccommodationServicesInput input)
    {
        input.Normalize();

        var query = _accommodationServicesRepository.GetAll()
            .Include(s => s.AccommodationServiceType)
            .WhereIf(input.EmployeeAccommodationId.HasValue, s => s.EmployeeAccommodationId == input.EmployeeAccommodationId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AccommodationServicesDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAccommodationServicesForEditOutput> GetAccommodationServicesForEdit(EntityDto input)
    {
        var entity = await _accommodationServicesRepository.GetAsync(input.Id);

        return new GetAccommodationServicesForEditOutput
        {
            AccommodationServices = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAccommodationServicesDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationServices_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _accommodationServicesRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationServices_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAccommodationServicesDto input)
    {
        var entity = MapToEntity(input, new AccommodationServices());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _accommodationServicesRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationServices_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAccommodationServicesDto input)
    {
        var entity = await _accommodationServicesRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _accommodationServicesRepository.UpdateAsync(entity);
    }

    private static AccommodationServices MapToEntity(CreateOrEditAccommodationServicesDto input, AccommodationServices entity)
    {
        entity.EmployeeAccommodationId = input.EmployeeAccommodationId;
        entity.AccommodationServiceTypeId = input.AccommodationServiceTypeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate.
    /// </summary>
    private void ValidateAsync(AccommodationServices entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static AccommodationServicesDto ToDto(AccommodationServices entity)
    {
        return new AccommodationServicesDto
        {
            Id = entity.Id,
            EmployeeAccommodationId = entity.EmployeeAccommodationId,
            AccommodationServiceTypeId = entity.AccommodationServiceTypeId,
            AccommodationServiceTypeName = entity.AccommodationServiceType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditAccommodationServicesDto ToCreateOrEditDto(AccommodationServices entity)
    {
        return new CreateOrEditAccommodationServicesDto
        {
            Id = entity.Id,
            EmployeeAccommodationId = entity.EmployeeAccommodationId,
            AccommodationServiceTypeId = entity.AccommodationServiceTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }
}
