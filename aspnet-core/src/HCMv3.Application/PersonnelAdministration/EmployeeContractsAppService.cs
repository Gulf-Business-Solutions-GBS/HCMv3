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
/// Application service for <see cref="EmployeeContract"/> (ported from legacy XAF EmployeeContract
/// business object, Wave B). The legacy EndDate auto-chaining (<c>SetEndDate</c>) is entirely
/// commented-out dead code in the legacy source and is NOT ported. Ports the active
/// <c>PerformValidation</c>: required-field checks, no-duplicate-StartDate, and
/// no-overlapping-date-range-with-other-contracts-for-the-same-employee.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeContracts)]
public class EmployeeContractsAppService : HCMv3AppServiceBase, IEmployeeContractsAppService
{
    private readonly IRepository<EmployeeContract> _repository;

    public EmployeeContractsAppService(IRepository<EmployeeContract> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeContractDto>> GetAll(GetAllEmployeeContractsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(c => c.Employee)
            .Include(c => c.ContractType)
            .WhereIf(input.EmployeeId.HasValue, c => c.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeContractDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeContractForEditOutput> GetEmployeeContractForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeContractForEditOutput
        {
            EmployeeContract = new CreateOrEditEmployeeContractDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                ContractTypeId = entity.ContractTypeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeContractDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeContracts_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeContractDto input)
    {
        var entity = MapToEntity(input, new EmployeeContract());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeContracts_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeContractDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeContracts_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeContract entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var duplicateStartDate = await _repository.GetAll()
            .AnyAsync(c => c.Id != entity.Id
                           && c.EmployeeId == entity.EmployeeId
                           && c.StartDate == entity.StartDate);
        if (duplicateStartDate)
        {
            throw new UserFriendlyException(L("StartDateAlreadyExists", entity.StartDate.ToString("dd/MM/yyyy")));
        }

        var overlaps = await _repository.GetAll()
            .AnyAsync(c => c.Id != entity.Id
                           && c.EmployeeId == entity.EmployeeId
                           && c.StartDate.Date <= entity.EndDate.Date
                           && c.EndDate.Date >= entity.StartDate.Date);
        if (overlaps)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeContract MapToEntity(CreateOrEditEmployeeContractDto input, EmployeeContract entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.ContractTypeId = input.ContractTypeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        return entity;
    }

    private static EmployeeContractDto ToDto(EmployeeContract entity)
    {
        return new EmployeeContractDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            ContractTypeId = entity.ContractTypeId,
            ContractTypeName = entity.ContractType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
