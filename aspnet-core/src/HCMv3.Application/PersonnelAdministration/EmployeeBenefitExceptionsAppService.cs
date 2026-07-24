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
/// Application service for <see cref="EmployeeBenefitException"/> (ported from legacy XAF
/// EmployeeBenefitException business object, Wave 4 deferred bucket). Ports the essential active
/// validation only: required fields, EndDate &gt;= StartDate, and no overlapping StartDate/EndDate window
/// for the same Employee+ExceptionBenefitType.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBenefitExceptions)]
public class EmployeeBenefitExceptionsAppService : HCMv3AppServiceBase, IEmployeeBenefitExceptionsAppService
{
    private readonly IRepository<EmployeeBenefitException> _repository;

    public EmployeeBenefitExceptionsAppService(IRepository<EmployeeBenefitException> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeBenefitExceptionDto>> GetAll(GetAllEmployeeBenefitExceptionsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeBenefitExceptionDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeBenefitExceptionForEditOutput> GetEmployeeBenefitExceptionForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeBenefitExceptionForEditOutput
        {
            EmployeeBenefitException = new CreateOrEditEmployeeBenefitExceptionDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                ExceptionBenefitType = entity.ExceptionBenefitType,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Amount = entity.Amount,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeBenefitExceptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBenefitExceptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeBenefitExceptionDto input)
    {
        var entity = MapToEntity(input, new EmployeeBenefitException());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBenefitExceptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeBenefitExceptionDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBenefitExceptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmployeeBenefitException entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var hasOverlap = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId && e.ExceptionBenefitType == entity.ExceptionBenefitType
                && e.StartDate.Date <= entity.EndDate.Date && e.EndDate.Date >= entity.StartDate.Date);
        if (hasOverlap)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static EmployeeBenefitException MapToEntity(CreateOrEditEmployeeBenefitExceptionDto input, EmployeeBenefitException entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.ExceptionBenefitType = input.ExceptionBenefitType;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.Amount = input.Amount;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeBenefitExceptionDto ToDto(EmployeeBenefitException entity)
    {
        return new EmployeeBenefitExceptionDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            ExceptionBenefitType = entity.ExceptionBenefitType,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Amount = entity.Amount,
            Comments = entity.Comments
        };
    }
}
