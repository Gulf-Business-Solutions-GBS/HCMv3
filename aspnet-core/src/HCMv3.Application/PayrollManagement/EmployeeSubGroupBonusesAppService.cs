using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the date-range, years-of-service-tiered bonus percentage of an
/// <see cref="EmployeeSubGroup"/> (ported from legacy XAF EmployeeSubGroupBonus business object), listed
/// separately and linked back via <see cref="EmployeeSubGroupBonus.EmployeeSubGroupId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBonuses)]
public class EmployeeSubGroupBonusesAppService : HCMv3AppServiceBase, IEmployeeSubGroupBonusesAppService
{
    private readonly IRepository<EmployeeSubGroupBonus> _employeeSubGroupBonusRepository;

    public EmployeeSubGroupBonusesAppService(IRepository<EmployeeSubGroupBonus> employeeSubGroupBonusRepository)
    {
        _employeeSubGroupBonusRepository = employeeSubGroupBonusRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupBonusDto>> GetAll(GetAllEmployeeSubGroupBonusesInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupBonusRepository.GetAll()
            .Include(b => b.EmployeeSubGroup)
            .WhereIf(input.EmployeeSubGroupId.HasValue, b => b.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupBonusDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupBonusForEditOutput> GetEmployeeSubGroupBonusForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupBonusRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupBonusForEditOutput
        {
            EmployeeSubGroupBonus = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupBonusDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBonuses_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupBonusRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBonuses_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupBonusDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupBonus());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupBonusRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBonuses_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupBonusDto input)
    {
        var entity = await _employeeSubGroupBonusRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupBonusRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupBonus MapToEntity(CreateOrEditEmployeeSubGroupBonusDto input, EmployeeSubGroupBonus entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.ChangeYear = input.ChangeYear;
        entity.Percentage = input.Percentage;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlapping date range with a
    /// sibling row for the same (EmployeeSubGroupId, ChangeYear) pair (the legacy source ran this overlap
    /// check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupBonus entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _employeeSubGroupBonusRepository.GetAll()
            .AnyAsync(b => b.Id != entity.Id &&
                           b.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           b.ChangeYear == entity.ChangeYear &&
                           b.StartDate.Date <= entity.EndDate.Date &&
                           b.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static EmployeeSubGroupBonusDto ToDto(EmployeeSubGroupBonus entity)
    {
        return new EmployeeSubGroupBonusDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ChangeYear = entity.ChangeYear,
            Percentage = entity.Percentage
        };
    }

    private static CreateOrEditEmployeeSubGroupBonusDto ToCreateOrEditDto(EmployeeSubGroupBonus entity)
    {
        return new CreateOrEditEmployeeSubGroupBonusDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ChangeYear = entity.ChangeYear,
            Percentage = entity.Percentage
        };
    }
}
