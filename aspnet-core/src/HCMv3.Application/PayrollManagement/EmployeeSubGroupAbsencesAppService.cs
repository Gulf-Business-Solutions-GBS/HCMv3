using System;
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
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the date-range leave quota row of an <see cref="EmployeeSubGroup"/> /
/// <see cref="AbsenceType"/> pair (ported from legacy XAF EmployeeSubGroupAbsence business object), listed
/// separately and linked back via <see cref="EmployeeSubGroupAbsence.EmployeeSubGroupId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsences)]
public class EmployeeSubGroupAbsencesAppService : HCMv3AppServiceBase, IEmployeeSubGroupAbsencesAppService
{
    private readonly IRepository<EmployeeSubGroupAbsence> _employeeSubGroupAbsenceRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public EmployeeSubGroupAbsencesAppService(
        IRepository<EmployeeSubGroupAbsence> employeeSubGroupAbsenceRepository,
        IRepository<Employee> employeeRepository)
    {
        _employeeSubGroupAbsenceRepository = employeeSubGroupAbsenceRepository;
        _employeeRepository = employeeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupAbsenceDto>> GetAll(GetAllEmployeeSubGroupAbsencesInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupAbsenceRepository.GetAll()
            .Include(a => a.EmployeeSubGroup)
            .Include(a => a.AbsenceType)
            .WhereIf(input.EmployeeSubGroupId.HasValue, a => a.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupAbsenceDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupAbsenceForEditOutput> GetEmployeeSubGroupAbsenceForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupAbsenceRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupAbsenceForEditOutput
        {
            EmployeeSubGroupAbsence = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupAbsenceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsences_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupAbsenceRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>EmployeeSubGroupAbsence.GetEmployeeSubGroupLeave</c> parity: resolves the applicable leave
    /// quota row for an employee on a given date. Prefers the tightest years-of-service tier whose
    /// <c>ChangeYear</c> is greater than the employee's actual tenure (in years, computed from
    /// <see cref="Employee.JoiningDate"/>) and falls back to any date-matching row when no tier matches.
    /// </summary>
    public async Task<EmployeeSubGroupAbsenceDto> GetEmployeeSubGroupLeave(int employeeSubGroupId, int employeeId, int absenceTypeId, DateTime dayDate)
    {
        var employee = await _employeeRepository.GetAsync(employeeId);
        var totalYears = Math.Round((dayDate.Subtract(employee.JoiningDate).TotalDays + 1) / 365, 2);

        var entity = await _employeeSubGroupAbsenceRepository.GetAll()
            .Where(r => r.EmployeeSubGroupId == employeeSubGroupId &&
                        r.AbsenceTypeId == absenceTypeId &&
                        r.ChangeYear > totalYears &&
                        r.StartDate <= dayDate &&
                        r.EndDate >= dayDate)
            .OrderBy(r => r.ChangeYear)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            entity = await _employeeSubGroupAbsenceRepository.GetAll()
                .Where(r => r.EmployeeSubGroupId == employeeSubGroupId &&
                            r.AbsenceTypeId == absenceTypeId &&
                            r.StartDate <= dayDate &&
                            r.EndDate >= dayDate)
                .FirstOrDefaultAsync();
        }

        return entity == null ? null : ToDto(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsences_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupAbsenceDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupAbsence());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupAbsenceRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsences_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupAbsenceDto input)
    {
        var entity = await _employeeSubGroupAbsenceRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupAbsenceRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupAbsence MapToEntity(CreateOrEditEmployeeSubGroupAbsenceDto input, EmployeeSubGroupAbsence entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.ChangeYear = input.ChangeYear;
        entity.TotalDays = input.TotalDays;
        entity.MaxLimit = input.MaxLimit;
        entity.AbsenceDeductionRuleId = input.AbsenceDeductionRuleId;
        entity.MaxLimitReachNotificationInDays = input.MaxLimitReachNotificationInDays;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlapping date range with a
    /// sibling row for the same (EmployeeSubGroupId, AbsenceTypeId) pair (the legacy source ran this overlap
    /// check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupAbsence entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _employeeSubGroupAbsenceRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           r.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           r.AbsenceTypeId == entity.AbsenceTypeId &&
                           r.StartDate.Date <= entity.EndDate.Date &&
                           r.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static EmployeeSubGroupAbsenceDto ToDto(EmployeeSubGroupAbsence entity)
    {
        return new EmployeeSubGroupAbsenceDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ChangeYear = entity.ChangeYear,
            TotalDays = entity.TotalDays,
            MaxLimit = entity.MaxLimit,
            AbsenceDeductionRuleId = entity.AbsenceDeductionRuleId,
            MaxLimitReachNotificationInDays = entity.MaxLimitReachNotificationInDays
        };
    }

    private static CreateOrEditEmployeeSubGroupAbsenceDto ToCreateOrEditDto(EmployeeSubGroupAbsence entity)
    {
        return new CreateOrEditEmployeeSubGroupAbsenceDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            AbsenceTypeId = entity.AbsenceTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ChangeYear = entity.ChangeYear,
            TotalDays = entity.TotalDays,
            MaxLimit = entity.MaxLimit,
            AbsenceDeductionRuleId = entity.AbsenceDeductionRuleId,
            MaxLimitReachNotificationInDays = entity.MaxLimitReachNotificationInDays
        };
    }
}
