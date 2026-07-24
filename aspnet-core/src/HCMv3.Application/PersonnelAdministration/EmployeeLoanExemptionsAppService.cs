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
/// Application service for <see cref="EmployeeLoanExemption"/> (ported from legacy XAF
/// EmployeeLoanExemption business object, Wave 2 deferred bucket). Ports the essential active validation
/// only (no duplicate exemption for the same EmployeeLoan and calendar month). The legacy cascade that
/// recomputes the parent <see cref="EmployeeLoan.EndDate"/> is NOT ported -- see class remarks on
/// <see cref="EmployeeLoanExemption"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions)]
public class EmployeeLoanExemptionsAppService : HCMv3AppServiceBase, IEmployeeLoanExemptionsAppService
{
    private readonly IRepository<EmployeeLoanExemption> _repository;

    public EmployeeLoanExemptionsAppService(IRepository<EmployeeLoanExemption> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeLoanExemptionDto>> GetAll(GetAllEmployeeLoanExemptionsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.EmployeeLoanId.HasValue, e => e.EmployeeLoanId == input.EmployeeLoanId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeLoanExemptionDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeLoanExemptionForEditOutput> GetEmployeeLoanExemptionForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeLoanExemptionForEditOutput
        {
            EmployeeLoanExemption = new CreateOrEditEmployeeLoanExemptionDto
            {
                Id = entity.Id,
                EmployeeLoanId = entity.EmployeeLoanId,
                ExemptionDate = entity.ExemptionDate,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeLoanExemptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeLoanExemptionDto input)
    {
        var entity = MapToEntity(input, new EmployeeLoanExemption());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeLoanExemptionDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeLoanExemption entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id
                           && e.EmployeeLoanId == entity.EmployeeLoanId
                           && e.ExemptionDate.Year == entity.ExemptionDate.Year
                           && e.ExemptionDate.Month == entity.ExemptionDate.Month);
        if (duplicate)
        {
            throw new UserFriendlyException(L("ExemptionAlreadyExistsForMonth"));
        }
    }

    private static EmployeeLoanExemption MapToEntity(CreateOrEditEmployeeLoanExemptionDto input, EmployeeLoanExemption entity)
    {
        entity.EmployeeLoanId = input.EmployeeLoanId;
        entity.ExemptionDate = input.ExemptionDate.Date;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeLoanExemptionDto ToDto(EmployeeLoanExemption entity)
    {
        return new EmployeeLoanExemptionDto
        {
            Id = entity.Id,
            EmployeeLoanId = entity.EmployeeLoanId,
            ExemptionDate = entity.ExemptionDate,
            Comments = entity.Comments
        };
    }
}
