using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeDependant"/> (ported from legacy XAF EmployeeDependant
/// business object, Wave 3 deferred bucket). Ports the legacy overlap/duplicate validation: no two
/// EmployeeDependant rows for the same employee+TicketType with overlapping date ranges. The legacy
/// auto-end-dating cascade and the dependant-exception validation cross-checks performed from
/// <c>EmployeeTicket</c> are NOT ported here (same simplification approach as elsewhere in this wave).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDependants)]
public class EmployeeDependantsAppService : HCMv3AppServiceBase, IEmployeeDependantsAppService
{
    private readonly IRepository<EmployeeDependant> _repository;

    public EmployeeDependantsAppService(IRepository<EmployeeDependant> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDependantDto>> GetAll(GetAllEmployeeDependantsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.Employee)
            .Include(d => d.DependantType)
            .Include(d => d.TicketType)
            .WhereIf(input.EmployeeId.HasValue, d => d.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDependantDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeDependantForEditOutput> GetEmployeeDependantForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeDependantForEditOutput
        {
            EmployeeDependant = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDependantDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDependants_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDependants_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDependantDto input)
    {
        var entity = MapToEntity(input, new EmployeeDependant());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDependants_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDependantDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no overlapping date ranges for the same employee+TicketType.
    /// </summary>
    private async Task ValidateAsync(EmployeeDependant entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.EmployeeId == entity.EmployeeId &&
                           d.TicketTypeId == entity.TicketTypeId &&
                           d.StartDate <= entity.EndDate &&
                           d.EndDate >= entity.StartDate);
        if (overlap)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeDependant MapToEntity(CreateOrEditEmployeeDependantDto input, EmployeeDependant entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.DependantTypeId = input.DependantTypeId;
        entity.BirthDate = input.BirthDate.Date;
        entity.TicketTypeId = input.TicketTypeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.SpecialNeed = input.SpecialNeed;
        entity.IncludeInSocialSecurity = input.IncludeInSocialSecurity;
        entity.IncludeInHealthInsurance = input.IncludeInHealthInsurance;
        return entity;
    }

    private static EmployeeDependantDto ToDto(EmployeeDependant entity)
    {
        return new EmployeeDependantDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            DependantTypeId = entity.DependantTypeId,
            DependantTypeName = entity.DependantType?.NameEn,
            BirthDate = entity.BirthDate,
            TicketTypeId = entity.TicketTypeId,
            TicketTypeName = entity.TicketType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            SpecialNeed = entity.SpecialNeed,
            IncludeInSocialSecurity = entity.IncludeInSocialSecurity,
            IncludeInHealthInsurance = entity.IncludeInHealthInsurance
        };
    }

    private static CreateOrEditEmployeeDependantDto ToCreateOrEditDto(EmployeeDependant entity)
    {
        return new CreateOrEditEmployeeDependantDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            DependantTypeId = entity.DependantTypeId,
            BirthDate = entity.BirthDate,
            TicketTypeId = entity.TicketTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            SpecialNeed = entity.SpecialNeed,
            IncludeInSocialSecurity = entity.IncludeInSocialSecurity,
            IncludeInHealthInsurance = entity.IncludeInHealthInsurance
        };
    }
}
