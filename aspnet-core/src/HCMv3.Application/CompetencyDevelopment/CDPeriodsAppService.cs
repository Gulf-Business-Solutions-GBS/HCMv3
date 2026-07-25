using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.CompetencyDevelopment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Application service for competency-development review periods (ported from legacy XAF CDPeriod business
/// object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDPeriods)]
public class CDPeriodsAppService : HCMv3AppServiceBase, ICDPeriodsAppService
{
    private readonly IRepository<CDPeriod> _repository;

    public CDPeriodsAppService(IRepository<CDPeriod> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CDPeriodDto>> GetAll(GetAllCDPeriodsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p => p.Name.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CDPeriodDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCDPeriodForEditOutput> GetCDPeriodForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCDPeriodForEditOutput
        {
            CDPeriod = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCDPeriodDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDPeriods_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDPeriods_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCDPeriodDto input)
    {
        var entity = MapToEntity(input, new CDPeriod());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDPeriods_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCDPeriodDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static CDPeriod MapToEntity(CreateOrEditCDPeriodDto input, CDPeriod entity)
    {
        entity.Name = input.Name;
        entity.Closed = input.Closed;

        return entity;
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: unique Name.</summary>
    private async Task ValidateAsync(CDPeriod entity)
    {
        var duplicateName = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id && p.Name == entity.Name);
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static CDPeriodDto ToDto(CDPeriod entity)
    {
        return new CDPeriodDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Closed = entity.Closed
        };
    }

    private static CreateOrEditCDPeriodDto ToCreateOrEditDto(CDPeriod entity)
    {
        return new CreateOrEditCDPeriodDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Closed = entity.Closed
        };
    }
}
