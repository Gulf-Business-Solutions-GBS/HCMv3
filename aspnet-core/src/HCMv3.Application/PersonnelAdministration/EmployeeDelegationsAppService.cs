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
/// Application service for <see cref="EmployeeDelegation"/> (ported from legacy XAF
/// EmployeeDelegation business object). Legacy <c>PerformValidation</c> parity: blank checks,
/// EndDate &gt;= StartDate, and an overlap/duplicate check per employee.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations)]
public class EmployeeDelegationsAppService : HCMv3AppServiceBase, IEmployeeDelegationsAppService
{
    private readonly IRepository<EmployeeDelegation> _repository;

    public EmployeeDelegationsAppService(IRepository<EmployeeDelegation> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDelegationDto>> GetAll(GetAllEmployeeDelegationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.Employee)
            .Include(d => d.DelegateTo)
            .WhereIf(input.EmployeeId.HasValue, d => d.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDelegationDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeDelegationForEditOutput> GetEmployeeDelegationForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeDelegationForEditOutput
        {
            EmployeeDelegation = new CreateOrEditEmployeeDelegationDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                DelegateToId = entity.DelegateToId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDelegationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDelegationDto input)
    {
        var entity = new EmployeeDelegation
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            DelegateToId = input.DelegateToId,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            Comments = input.Comments
        };

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDelegationDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.DelegateToId = input.DelegateToId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.Comments = input.Comments;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeDelegation entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlapping = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id
                           && d.EmployeeId == entity.EmployeeId
                           && d.StartDate <= entity.EndDate
                           && d.EndDate >= entity.StartDate);
        if (overlapping)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeDelegationDto ToDto(EmployeeDelegation entity)
    {
        return new EmployeeDelegationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DelegateToId = entity.DelegateToId,
            DelegateToCode = entity.DelegateTo?.Code,
            DelegateToFullNameEn = entity.DelegateTo?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }
}
