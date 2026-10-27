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
/// Application service for the per-employee date-range exception of an <see cref="AbsenceViolationRule"/>
/// (ported from legacy XAF AbsenceViolationRuleException business object), listed separately and linked
/// back via <see cref="AbsenceViolationRuleException.AbsenceViolationRuleId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleExceptions)]
public class AbsenceViolationRuleExceptionsAppService : HCMv3AppServiceBase, IAbsenceViolationRuleExceptionsAppService
{
    private readonly IRepository<AbsenceViolationRuleException> _absenceViolationRuleExceptionRepository;

    public AbsenceViolationRuleExceptionsAppService(IRepository<AbsenceViolationRuleException> absenceViolationRuleExceptionRepository)
    {
        _absenceViolationRuleExceptionRepository = absenceViolationRuleExceptionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AbsenceViolationRuleExceptionDto>> GetAll(GetAllAbsenceViolationRuleExceptionsInput input)
    {
        input.Normalize();

        var query = _absenceViolationRuleExceptionRepository.GetAll()
            .Include(e => e.AbsenceViolationRule)
            .Include(e => e.Employee)
            .WhereIf(input.AbsenceViolationRuleId.HasValue, e => e.AbsenceViolationRuleId == input.AbsenceViolationRuleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsenceViolationRuleExceptionDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAbsenceViolationRuleExceptionForEditOutput> GetAbsenceViolationRuleExceptionForEdit(EntityDto input)
    {
        var entity = await _absenceViolationRuleExceptionRepository.GetAsync(input.Id);

        return new GetAbsenceViolationRuleExceptionForEditOutput
        {
            AbsenceViolationRuleException = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAbsenceViolationRuleExceptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleExceptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _absenceViolationRuleExceptionRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleExceptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAbsenceViolationRuleExceptionDto input)
    {
        var entity = MapToEntity(input, new AbsenceViolationRuleException());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _absenceViolationRuleExceptionRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleExceptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAbsenceViolationRuleExceptionDto input)
    {
        var entity = await _absenceViolationRuleExceptionRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _absenceViolationRuleExceptionRepository.UpdateAsync(entity);
    }

    private static AbsenceViolationRuleException MapToEntity(CreateOrEditAbsenceViolationRuleExceptionDto input, AbsenceViolationRuleException entity)
    {
        entity.AbsenceViolationRuleId = input.AbsenceViolationRuleId;
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlapping date range for the
    /// same employee under the same parent rule.
    /// </summary>
    private async Task ValidateAsync(AbsenceViolationRuleException entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _absenceViolationRuleExceptionRepository.GetAll()
            .AnyAsync(e => e.Id != entity.Id &&
                           e.AbsenceViolationRuleId == entity.AbsenceViolationRuleId &&
                           e.EmployeeId == entity.EmployeeId &&
                           e.StartDate.Date <= entity.EndDate.Date &&
                           e.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static AbsenceViolationRuleExceptionDto ToDto(AbsenceViolationRuleException entity)
    {
        return new AbsenceViolationRuleExceptionDto
        {
            Id = entity.Id,
            AbsenceViolationRuleId = entity.AbsenceViolationRuleId,
            AbsenceViolationRuleName = entity.AbsenceViolationRule?.NameEn,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FirstNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditAbsenceViolationRuleExceptionDto ToCreateOrEditDto(AbsenceViolationRuleException entity)
    {
        return new CreateOrEditAbsenceViolationRuleExceptionDto
        {
            Id = entity.Id,
            AbsenceViolationRuleId = entity.AbsenceViolationRuleId,
            EmployeeId = entity.EmployeeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
