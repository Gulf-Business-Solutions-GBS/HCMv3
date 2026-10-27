using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms)]
public class PAGoalsAppService : HCMv3AppServiceBase, IPAGoalsAppService
{
    private readonly IRepository<PAGoal> _repository;

    public PAGoalsAppService(IRepository<PAGoal> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAGoalDto>> GetAll(GetAllPAGoalsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.PAFormId.HasValue, e => e.PAFormId == input.PAFormId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PAGoalDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAGoalForEditOutput> GetPAGoalForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAGoalForEditOutput { PAGoal = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAGoalDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditPAGoalDto input)
    {
        var entity = new PAGoal
        {
            TenantId = AbpSession.TenantId ?? 0,
            PAFormId = input.PAFormId,
            Goal = input.Goal
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAGoalDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.PAFormId = input.PAFormId;
        entity.Goal = input.Goal;

        await _repository.UpdateAsync(entity);
    }

    private static PAGoalDto ToDto(PAGoal entity)
    {
        return new PAGoalDto
        {
            Id = entity.Id,
            PAFormId = entity.PAFormId,
            Goal = entity.Goal
        };
    }

    private static CreateOrEditPAGoalDto ToCreateOrEditDto(PAGoal entity)
    {
        return new CreateOrEditPAGoalDto
        {
            Id = entity.Id,
            PAFormId = entity.PAFormId,
            Goal = entity.Goal
        };
    }
}
