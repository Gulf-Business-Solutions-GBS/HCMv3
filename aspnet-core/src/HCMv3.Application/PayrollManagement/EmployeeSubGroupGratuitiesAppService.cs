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
/// Application service for the date-range, years-of-service-tiered gratuity accrual rule of an
/// <see cref="EmployeeSubGroup"/> (ported from legacy XAF EmployeeSubGroupGratuity business object), listed
/// separately and linked back via <see cref="EmployeeSubGroupGratuity.EmployeeSubGroupId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupGratuities)]
public class EmployeeSubGroupGratuitiesAppService : HCMv3AppServiceBase, IEmployeeSubGroupGratuitiesAppService
{
    private readonly IRepository<EmployeeSubGroupGratuity> _employeeSubGroupGratuityRepository;

    public EmployeeSubGroupGratuitiesAppService(IRepository<EmployeeSubGroupGratuity> employeeSubGroupGratuityRepository)
    {
        _employeeSubGroupGratuityRepository = employeeSubGroupGratuityRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupGratuityDto>> GetAll(GetAllEmployeeSubGroupGratuitiesInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupGratuityRepository.GetAll()
            .Include(g => g.EmployeeSubGroup)
            .WhereIf(input.EmployeeSubGroupId.HasValue, g => g.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupGratuityDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupGratuityForEditOutput> GetEmployeeSubGroupGratuityForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupGratuityRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupGratuityForEditOutput
        {
            EmployeeSubGroupGratuity = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupGratuityDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupGratuities_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupGratuityRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupGratuities_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupGratuityDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupGratuity());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupGratuityRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupGratuities_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupGratuityDto input)
    {
        var entity = await _employeeSubGroupGratuityRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupGratuityRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupGratuity MapToEntity(CreateOrEditEmployeeSubGroupGratuityDto input, EmployeeSubGroupGratuity entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.ChangeYear = input.ChangeYear;
        entity.GratuityDays = input.GratuityDays;
        entity.BasisDays = input.BasisDays;
        entity.ImpactFromDateOfJoining = input.ImpactFromDateOfJoining;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlapping date range with a
    /// sibling row for the same (EmployeeSubGroupId, ChangeYear) pair (the legacy source ran this overlap
    /// check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupGratuity entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _employeeSubGroupGratuityRepository.GetAll()
            .AnyAsync(g => g.Id != entity.Id &&
                           g.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           g.ChangeYear == entity.ChangeYear &&
                           g.StartDate.Date <= entity.EndDate.Date &&
                           g.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static EmployeeSubGroupGratuityDto ToDto(EmployeeSubGroupGratuity entity)
    {
        return new EmployeeSubGroupGratuityDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ChangeYear = entity.ChangeYear,
            GratuityDays = entity.GratuityDays,
            BasisDays = entity.BasisDays,
            ImpactFromDateOfJoining = entity.ImpactFromDateOfJoining
        };
    }

    private static CreateOrEditEmployeeSubGroupGratuityDto ToCreateOrEditDto(EmployeeSubGroupGratuity entity)
    {
        return new CreateOrEditEmployeeSubGroupGratuityDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ChangeYear = entity.ChangeYear,
            GratuityDays = entity.GratuityDays,
            BasisDays = entity.BasisDays,
            ImpactFromDateOfJoining = entity.ImpactFromDateOfJoining
        };
    }
}
