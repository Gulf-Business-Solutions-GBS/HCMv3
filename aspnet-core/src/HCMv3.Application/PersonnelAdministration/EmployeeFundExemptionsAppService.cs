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
/// Application service for <see cref="EmployeeFundExemption"/> (ported from legacy XAF
/// EmployeeFundExemption business object, Wave 2 deferred bucket). Ports the essential active validation
/// only (EndDate not earlier than StartDate). The legacy no-overlap validation for the same EmployeeFund is
/// commented-out/dead code in the legacy source and is therefore NOT ported -- see class remarks on
/// <see cref="EmployeeFundExemption"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundExemptions)]
public class EmployeeFundExemptionsAppService : HCMv3AppServiceBase, IEmployeeFundExemptionsAppService
{
    private readonly IRepository<EmployeeFundExemption> _repository;

    public EmployeeFundExemptionsAppService(IRepository<EmployeeFundExemption> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeFundExemptionDto>> GetAll(GetAllEmployeeFundExemptionsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.EmployeeFundId.HasValue, e => e.EmployeeFundId == input.EmployeeFundId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeFundExemptionDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeFundExemptionForEditOutput> GetEmployeeFundExemptionForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeFundExemptionForEditOutput
        {
            EmployeeFundExemption = new CreateOrEditEmployeeFundExemptionDto
            {
                Id = entity.Id,
                EmployeeFundId = entity.EmployeeFundId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeFundExemptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundExemptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeFundExemptionDto input)
    {
        var entity = MapToEntity(input, new EmployeeFundExemption());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateEntity(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundExemptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeFundExemptionDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateEntity(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFundExemptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateEntity(EmployeeFundExemption entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeFundExemption MapToEntity(CreateOrEditEmployeeFundExemptionDto input, EmployeeFundExemption entity)
    {
        entity.EmployeeFundId = input.EmployeeFundId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeFundExemptionDto ToDto(EmployeeFundExemption entity)
    {
        return new EmployeeFundExemptionDto
        {
            Id = entity.Id,
            EmployeeFundId = entity.EmployeeFundId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }
}
