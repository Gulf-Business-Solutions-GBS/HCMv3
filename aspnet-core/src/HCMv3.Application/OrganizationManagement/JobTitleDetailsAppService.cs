using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.OrganizationManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Application service for <see cref="JobTitleDetail"/>: the default workflow roles of a job title. Ports
/// legacy <c>JobTitleDetail.PerformValidation</c> (a role is required) and refuses the same role twice on one
/// job title, which legacy left possible but which only ever doubled the same approvers.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_JobTitleDetails)]
public class JobTitleDetailsAppService : HCMv3AppServiceBase, IJobTitleDetailsAppService
{
    private readonly IRepository<JobTitleDetail> _repository;

    public JobTitleDetailsAppService(IRepository<JobTitleDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<JobTitleDetailDto>> GetAll(GetAllJobTitleDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.JobTitle)
            .Include(d => d.WorkflowRole)
            .WhereIf(input.JobTitleId.HasValue, d => d.JobTitleId == input.JobTitleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<JobTitleDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetJobTitleDetailForEditOutput> GetJobTitleDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetJobTitleDetailForEditOutput
        {
            JobTitleDetail = new CreateOrEditJobTitleDetailDto
            {
                Id = entity.Id,
                JobTitleId = entity.JobTitleId,
                WorkflowRoleId = entity.WorkflowRoleId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditJobTitleDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_JobTitleDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditJobTitleDetailDto input)
    {
        var entity = new JobTitleDetail
        {
            TenantId = AbpSession.TenantId ?? 0,
            JobTitleId = input.JobTitleId,
            WorkflowRoleId = input.WorkflowRoleId
        };

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_JobTitleDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditJobTitleDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.JobTitleId = input.JobTitleId;
        entity.WorkflowRoleId = input.WorkflowRoleId;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_JobTitleDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(JobTitleDetail entity)
    {
        if (entity.WorkflowRoleId <= 0)
        {
            throw new UserFriendlyException(L("WorkflowRoleCannotBeBlank"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id
                           && d.JobTitleId == entity.JobTitleId
                           && d.WorkflowRoleId == entity.WorkflowRoleId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("JobTitleDetailRoleAlreadyListed"));
        }
    }

    private static JobTitleDetailDto ToDto(JobTitleDetail entity)
    {
        return new JobTitleDetailDto
        {
            Id = entity.Id,
            JobTitleId = entity.JobTitleId,
            JobTitleName = entity.JobTitle?.NameEn,
            WorkflowRoleId = entity.WorkflowRoleId,
            WorkflowRoleName = entity.WorkflowRole?.NameEn
        };
    }
}
