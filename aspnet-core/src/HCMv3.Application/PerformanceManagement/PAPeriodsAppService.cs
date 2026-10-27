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
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAPeriods)]
public class PAPeriodsAppService : HCMv3AppServiceBase, IPAPeriodsAppService
{
    private readonly IRepository<PAPeriod> _repository;

    public PAPeriodsAppService(IRepository<PAPeriod> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAPeriodDto>> GetAll(GetAllPAPeriodsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), e => e.Name.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PAPeriodDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAPeriodForEditOutput> GetPAPeriodForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAPeriodForEditOutput { PAPeriod = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAPeriodDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAPeriods_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAPeriods_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPAPeriodDto input)
    {
        var entity = new PAPeriod
        {
            TenantId = AbpSession.TenantId ?? 0,
            Name = input.Name,
            Closed = input.Closed
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAPeriods_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAPeriodDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.Name = input.Name;
        entity.Closed = input.Closed;

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: Name required and unique.</summary>
    private async Task ValidateAsync(PAPeriod entity)
    {
        var duplicateName = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.Name == entity.Name);
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static PAPeriodDto ToDto(PAPeriod entity)
    {
        return new PAPeriodDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Closed = entity.Closed
        };
    }

    private static CreateOrEditPAPeriodDto ToCreateOrEditDto(PAPeriod entity)
    {
        return new CreateOrEditPAPeriodDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Closed = entity.Closed
        };
    }
}
