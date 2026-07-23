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
/// Application service for the date-bound Employee-DottedLineManager assignment (ported from legacy
/// XAF EmployeeDottedLineManager business object). Unlike <see cref="EmployeeManager"/>, there is no
/// EndDate auto-chaining -- only an overlap/duplicate check (legacy <c>PerformValidation</c> parity).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDottedLineManagers)]
public class EmployeeDottedLineManagersAppService : HCMv3AppServiceBase, IEmployeeDottedLineManagersAppService
{
    private readonly IRepository<EmployeeDottedLineManager> _repository;

    public EmployeeDottedLineManagersAppService(IRepository<EmployeeDottedLineManager> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDottedLineManagerDto>> GetAll(GetAllEmployeeDottedLineManagersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(m => m.Employee)
            .Include(m => m.Manager)
            .WhereIf(input.EmployeeId.HasValue, m => m.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDottedLineManagerDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeDottedLineManagerForEditOutput> GetEmployeeDottedLineManagerForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeDottedLineManagerForEditOutput
        {
            EmployeeDottedLineManager = new CreateOrEditEmployeeDottedLineManagerDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                ManagerId = entity.ManagerId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDottedLineManagerDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDottedLineManagers_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDottedLineManagerDto input)
    {
        var entity = new EmployeeDottedLineManager
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            ManagerId = input.ManagerId,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date
        };

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDottedLineManagers_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDottedLineManagerDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.ManagerId = input.ManagerId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDottedLineManagers_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeDottedLineManager entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlapping = await _repository.GetAll()
            .AnyAsync(m => m.Id != entity.Id
                           && m.EmployeeId == entity.EmployeeId
                           && m.StartDate <= entity.EndDate
                           && m.EndDate >= entity.StartDate);
        if (overlapping)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeDottedLineManagerDto ToDto(EmployeeDottedLineManager entity)
    {
        return new EmployeeDottedLineManagerDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            ManagerId = entity.ManagerId,
            ManagerCode = entity.Manager?.Code,
            ManagerFullNameEn = entity.Manager?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
