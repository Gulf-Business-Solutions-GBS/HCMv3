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
/// Application service for the date-range dependant-tier health-reimbursement entitlement attached directly
/// to an <see cref="EmployeeSubGroup"/> (ported from legacy XAF EmployeeSubGroupHealthReimburse business
/// object), listed separately and linked back via
/// <see cref="EmployeeSubGroupHealthReimburse.EmployeeSubGroupId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupHealthReimburses)]
public class EmployeeSubGroupHealthReimbursesAppService : HCMv3AppServiceBase, IEmployeeSubGroupHealthReimbursesAppService
{
    private readonly IRepository<EmployeeSubGroupHealthReimburse> _employeeSubGroupHealthReimburseRepository;

    public EmployeeSubGroupHealthReimbursesAppService(IRepository<EmployeeSubGroupHealthReimburse> employeeSubGroupHealthReimburseRepository)
    {
        _employeeSubGroupHealthReimburseRepository = employeeSubGroupHealthReimburseRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupHealthReimburseDto>> GetAll(GetAllEmployeeSubGroupHealthReimbursesInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupHealthReimburseRepository.GetAll()
            .Include(h => h.EmployeeSubGroup)
            .Include(h => h.DependantType)
            .WhereIf(input.EmployeeSubGroupId.HasValue, h => h.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupHealthReimburseDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupHealthReimburseForEditOutput> GetEmployeeSubGroupHealthReimburseForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupHealthReimburseRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupHealthReimburseForEditOutput
        {
            EmployeeSubGroupHealthReimburse = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupHealthReimburseDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupHealthReimburses_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupHealthReimburseRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupHealthReimburses_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupHealthReimburseDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupHealthReimburse());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupHealthReimburseRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupHealthReimburses_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupHealthReimburseDto input)
    {
        var entity = await _employeeSubGroupHealthReimburseRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupHealthReimburseRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupHealthReimburse MapToEntity(CreateOrEditEmployeeSubGroupHealthReimburseDto input, EmployeeSubGroupHealthReimburse entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.DependantTypeId = input.DependantTypeId;
        entity.Amount = input.Amount;
        entity.AgeLimit = input.AgeLimit;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlapping date range with a
    /// sibling row for the same (EmployeeSubGroupId, DependantTypeId) pair (the legacy source ran this
    /// overlap check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupHealthReimburse entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _employeeSubGroupHealthReimburseRepository.GetAll()
            .AnyAsync(h => h.Id != entity.Id &&
                           h.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           h.DependantTypeId == entity.DependantTypeId &&
                           h.StartDate.Date <= entity.EndDate.Date &&
                           h.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static EmployeeSubGroupHealthReimburseDto ToDto(EmployeeSubGroupHealthReimburse entity)
    {
        return new EmployeeSubGroupHealthReimburseDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            DependantTypeId = entity.DependantTypeId,
            DependantTypeName = entity.DependantType?.NameEn,
            Amount = entity.Amount,
            AgeLimit = entity.AgeLimit
        };
    }

    private static CreateOrEditEmployeeSubGroupHealthReimburseDto ToCreateOrEditDto(EmployeeSubGroupHealthReimburse entity)
    {
        return new CreateOrEditEmployeeSubGroupHealthReimburseDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            DependantTypeId = entity.DependantTypeId,
            Amount = entity.Amount,
            AgeLimit = entity.AgeLimit
        };
    }
}
