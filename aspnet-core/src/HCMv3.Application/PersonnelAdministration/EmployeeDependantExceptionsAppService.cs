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
/// Application service for <see cref="EmployeeDependantException"/> (ported from legacy XAF
/// EmployeeDependantException business object, Wave 3 deferred bucket). Ports the legacy validation: the
/// exception's date range must fall within its parent <see cref="EmployeeDependant"/>'s date range, and no
/// two exception rows for the same dependant+ExceptionBenefitType may overlap.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDependantExceptions)]
public class EmployeeDependantExceptionsAppService : HCMv3AppServiceBase, IEmployeeDependantExceptionsAppService
{
    private readonly IRepository<EmployeeDependantException> _repository;
    private readonly IRepository<EmployeeDependant> _dependantRepository;

    public EmployeeDependantExceptionsAppService(
        IRepository<EmployeeDependantException> repository,
        IRepository<EmployeeDependant> dependantRepository)
    {
        _repository = repository;
        _dependantRepository = dependantRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDependantExceptionDto>> GetAll(GetAllEmployeeDependantExceptionsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.EmployeeDependant)
            .WhereIf(input.EmployeeDependantId.HasValue, e => e.EmployeeDependantId == input.EmployeeDependantId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDependantExceptionDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeDependantExceptionForEditOutput> GetEmployeeDependantExceptionForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeDependantExceptionForEditOutput
        {
            EmployeeDependantException = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDependantExceptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDependantExceptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDependantExceptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDependantExceptionDto input)
    {
        var entity = MapToEntity(input, new EmployeeDependantException());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDependantExceptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDependantExceptionDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(EmployeeDependantException entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var parentDependant = await _dependantRepository.GetAsync(entity.EmployeeDependantId);
        if (entity.StartDate < parentDependant.StartDate || entity.EndDate > parentDependant.EndDate)
        {
            throw new UserFriendlyException(L("ExceptionDateRangeMustFallWithinDependantDateRange"));
        }

        var overlap = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id &&
                           e.EmployeeDependantId == entity.EmployeeDependantId &&
                           e.ExceptionBenefitType == entity.ExceptionBenefitType &&
                           e.StartDate <= entity.EndDate &&
                           e.EndDate >= entity.StartDate);
        if (overlap)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeDependantException MapToEntity(CreateOrEditEmployeeDependantExceptionDto input, EmployeeDependantException entity)
    {
        entity.EmployeeDependantId = input.EmployeeDependantId;
        entity.ExceptionBenefitType = input.ExceptionBenefitType;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.Amount = input.Amount;
        return entity;
    }

    private static EmployeeDependantExceptionDto ToDto(EmployeeDependantException entity)
    {
        return new EmployeeDependantExceptionDto
        {
            Id = entity.Id,
            EmployeeDependantId = entity.EmployeeDependantId,
            EmployeeDependantNameEn = entity.EmployeeDependant?.NameEn,
            ExceptionBenefitType = entity.ExceptionBenefitType,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditEmployeeDependantExceptionDto ToCreateOrEditDto(EmployeeDependantException entity)
    {
        return new CreateOrEditEmployeeDependantExceptionDto
        {
            Id = entity.Id,
            EmployeeDependantId = entity.EmployeeDependantId,
            ExceptionBenefitType = entity.ExceptionBenefitType,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Amount = entity.Amount
        };
    }
}
