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
using HCMv3.OrganizationManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Application service for the Work Location master (ported from legacy XAF WorkLocation business
/// object). The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks) lives
/// here, out of the persistence lifecycle. No other special business rules.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_WorkLocations)]
public class WorkLocationsAppService : HCMv3AppServiceBase, IWorkLocationsAppService
{
    private readonly IRepository<WorkLocation> _workLocationRepository;

    public WorkLocationsAppService(IRepository<WorkLocation> workLocationRepository)
    {
        _workLocationRepository = workLocationRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkLocationDto>> GetAll(GetAllWorkLocationsInput input)
    {
        input.Normalize();

        var query = _workLocationRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), w =>
                w.Code.Contains(input.Filter) ||
                w.NameEn.Contains(input.Filter) ||
                w.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkLocationDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetWorkLocationForEditOutput> GetWorkLocationForEdit(EntityDto input)
    {
        var workLocation = await _workLocationRepository.GetAsync(input.Id);

        return new GetWorkLocationForEditOutput
        {
            WorkLocation = ToCreateOrEditDto(workLocation)
        };
    }

    public async Task CreateOrEdit(CreateOrEditWorkLocationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_WorkLocations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkLocationDto input)
    {
        var workLocation = new WorkLocation
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            Latitude = input.Latitude,
            Longitude = input.Longitude,
            GeoRange = input.GeoRange,
            Address = input.Address,
            GeoFenceCoordinates = input.GeoFenceCoordinates,
            IsActive = input.IsActive,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(workLocation);
        await ValidateUniquenessAsync(workLocation);

        await _workLocationRepository.InsertAsync(workLocation);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_WorkLocations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkLocationDto input)
    {
        var workLocation = await _workLocationRepository.GetAsync(input.Id.Value);

        workLocation.Code = input.Code;
        workLocation.NameEn = input.NameEn;
        workLocation.NameAr = input.NameAr;
        workLocation.Latitude = input.Latitude;
        workLocation.Longitude = input.Longitude;
        workLocation.GeoRange = input.GeoRange;
        workLocation.Address = input.Address;
        workLocation.GeoFenceCoordinates = input.GeoFenceCoordinates;
        workLocation.IsActive = input.IsActive;

        await AssignCodeIfEmptyAsync(workLocation);
        await ValidateUniquenessAsync(workLocation);

        await _workLocationRepository.UpdateAsync(workLocation);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_WorkLocations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _workLocationRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(WorkLocation workLocation)
    {
        if (!workLocation.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _workLocationRepository.GetAll()
            .Where(w => w.Code != null && w.Code != "")
            .Select(w => w.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        workLocation.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(WorkLocation workLocation)
    {
        var duplicateCode = await _workLocationRepository.GetAll()
            .AnyAsync(w => w.Id != workLocation.Id && w.Code == workLocation.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _workLocationRepository.GetAll()
            .AnyAsync(w => w.Id != workLocation.Id &&
                           (w.NameEn == workLocation.NameEn || w.NameAr == workLocation.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static WorkLocationDto ToDto(WorkLocation workLocation)
    {
        return new WorkLocationDto
        {
            Id = workLocation.Id,
            Code = workLocation.Code,
            NameEn = workLocation.NameEn,
            NameAr = workLocation.NameAr,
            Latitude = workLocation.Latitude,
            Longitude = workLocation.Longitude,
            GeoRange = workLocation.GeoRange,
            Address = workLocation.Address,
            GeoFenceCoordinates = workLocation.GeoFenceCoordinates,
            IsActive = workLocation.IsActive
        };
    }

    private static CreateOrEditWorkLocationDto ToCreateOrEditDto(WorkLocation workLocation)
    {
        return new CreateOrEditWorkLocationDto
        {
            Id = workLocation.Id,
            Code = workLocation.Code,
            NameEn = workLocation.NameEn,
            NameAr = workLocation.NameAr,
            Latitude = workLocation.Latitude,
            Longitude = workLocation.Longitude,
            GeoRange = workLocation.GeoRange,
            Address = workLocation.Address,
            GeoFenceCoordinates = workLocation.GeoFenceCoordinates,
            IsActive = workLocation.IsActive
        };
    }
}
