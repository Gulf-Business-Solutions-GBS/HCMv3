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
using HCMv3.TravelManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TravelManagement;

/// <summary>
/// Application service for <see cref="Travel"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.Travel</c>). Legacy <c>PerformValidation</c> parity
/// ported here: auto Code, duplicate Code check, EndDate must not be earlier than StartDate. See
/// <see cref="Travel"/> class remarks for the Applicant-drop and TotalTravelCost-rollup simplifications.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TravelManagement_Travels)]
public class TravelsAppService : HCMv3AppServiceBase, ITravelsAppService
{
    private readonly IRepository<Travel> _repository;

    public TravelsAppService(IRepository<Travel> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TravelDto>> GetAll(GetAllTravelsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.Employee)
            .Include(t => t.TravelType)
            .WhereIf(input.EmployeeId.HasValue, t => t.EmployeeId == input.EmployeeId)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t => t.Code.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TravelDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTravelForEditOutput> GetTravelForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTravelForEditOutput
        {
            Travel = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTravelDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_Travels_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTravelDto input)
    {
        var entity = MapToEntity(input, new Travel());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);
        await AssignCodeIfEmptyAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_Travels_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTravelDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);
        await AssignCodeIfEmptyAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_Travels_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate must not be earlier than StartDate.
    /// </summary>
    private Task ValidateAsync(Travel entity)
    {
        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Travel entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            var duplicateCode = await _repository.GetAll()
                .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
            if (duplicateCode)
            {
                throw new UserFriendlyException(L("DuplicateCodeFound"));
            }

            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(t => t.Code != null && t.Code != "")
            .Select(t => t.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private static Travel MapToEntity(CreateOrEditTravelDto input, Travel entity)
    {
        entity.Code = input.Code;
        entity.EmployeeId = input.EmployeeId;
        entity.TravelTypeId = input.TravelTypeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.Comments = input.Comments;

        return entity;
    }

    private static TravelDto ToDto(Travel entity)
    {
        return new TravelDto
        {
            Id = entity.Id,
            Code = entity.Code,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee != null
                ? $"{entity.Employee.FirstNameEn} {entity.Employee.LastNameEn}"
                : null,
            TravelTypeId = entity.TravelTypeId,
            TravelTypeNameEn = entity.TravelType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TotalTravelCost = entity.TotalTravelCost,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditTravelDto ToCreateOrEditDto(Travel entity)
    {
        return new CreateOrEditTravelDto
        {
            Id = entity.Id,
            Code = entity.Code,
            EmployeeId = entity.EmployeeId,
            TravelTypeId = entity.TravelTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }
}
