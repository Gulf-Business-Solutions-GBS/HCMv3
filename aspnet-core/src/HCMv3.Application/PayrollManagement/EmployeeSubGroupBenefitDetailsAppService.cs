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
/// Application service for the date-range dependant-tier detail of an <see cref="EmployeeSubGroupBenefit"/>
/// (ported from legacy XAF EmployeeSubGroupBenefitDetail business object), listed separately and linked back
/// via <see cref="EmployeeSubGroupBenefitDetail.EmployeeSubGroupBenefitId"/> -- nested one level deeper than
/// its 12 siblings.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefitDetails)]
public class EmployeeSubGroupBenefitDetailsAppService : HCMv3AppServiceBase, IEmployeeSubGroupBenefitDetailsAppService
{
    private readonly IRepository<EmployeeSubGroupBenefitDetail> _employeeSubGroupBenefitDetailRepository;

    public EmployeeSubGroupBenefitDetailsAppService(IRepository<EmployeeSubGroupBenefitDetail> employeeSubGroupBenefitDetailRepository)
    {
        _employeeSubGroupBenefitDetailRepository = employeeSubGroupBenefitDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupBenefitDetailDto>> GetAll(GetAllEmployeeSubGroupBenefitDetailsInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupBenefitDetailRepository.GetAll()
            .Include(d => d.DependantType)
            .WhereIf(input.EmployeeSubGroupBenefitId.HasValue, d => d.EmployeeSubGroupBenefitId == input.EmployeeSubGroupBenefitId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupBenefitDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupBenefitDetailForEditOutput> GetEmployeeSubGroupBenefitDetailForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupBenefitDetailRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupBenefitDetailForEditOutput
        {
            EmployeeSubGroupBenefitDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupBenefitDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefitDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupBenefitDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefitDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupBenefitDetailDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupBenefitDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupBenefitDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefitDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupBenefitDetailDto input)
    {
        var entity = await _employeeSubGroupBenefitDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupBenefitDetailRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupBenefitDetail MapToEntity(CreateOrEditEmployeeSubGroupBenefitDetailDto input, EmployeeSubGroupBenefitDetail entity)
    {
        entity.EmployeeSubGroupBenefitId = input.EmployeeSubGroupBenefitId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.DependantTypeId = input.DependantTypeId;
        entity.Amount = input.Amount;
        entity.AgeLimit = input.AgeLimit;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlapping date range with a
    /// sibling detail for the same (EmployeeSubGroupBenefitId, DependantTypeId) pair (the legacy source ran
    /// this overlap check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupBenefitDetail entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _employeeSubGroupBenefitDetailRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.EmployeeSubGroupBenefitId == entity.EmployeeSubGroupBenefitId &&
                           d.DependantTypeId == entity.DependantTypeId &&
                           d.StartDate.Date <= entity.EndDate.Date &&
                           d.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static EmployeeSubGroupBenefitDetailDto ToDto(EmployeeSubGroupBenefitDetail entity)
    {
        return new EmployeeSubGroupBenefitDetailDto
        {
            Id = entity.Id,
            EmployeeSubGroupBenefitId = entity.EmployeeSubGroupBenefitId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            DependantTypeId = entity.DependantTypeId,
            DependantTypeName = entity.DependantType?.NameEn,
            Amount = entity.Amount,
            AgeLimit = entity.AgeLimit
        };
    }

    private static CreateOrEditEmployeeSubGroupBenefitDetailDto ToCreateOrEditDto(EmployeeSubGroupBenefitDetail entity)
    {
        return new CreateOrEditEmployeeSubGroupBenefitDetailDto
        {
            Id = entity.Id,
            EmployeeSubGroupBenefitId = entity.EmployeeSubGroupBenefitId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            DependantTypeId = entity.DependantTypeId,
            Amount = entity.Amount,
            AgeLimit = entity.AgeLimit
        };
    }
}
