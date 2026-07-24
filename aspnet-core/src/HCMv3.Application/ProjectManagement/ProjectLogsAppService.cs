using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.ProjectManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Application service for <see cref="ProjectLog"/> (ported from legacy XAF ProjectLog business object).
/// No lifecycle overrides/business validation in the legacy source -- plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectLogs)]
public class ProjectLogsAppService : HCMv3AppServiceBase, IProjectLogsAppService
{
    private readonly IRepository<ProjectLog> _repository;

    public ProjectLogsAppService(IRepository<ProjectLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ProjectLogDto>> GetAll(GetAllProjectLogsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(l => l.Employee)
            .WhereIf(input.EmployeeId.HasValue, l => l.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ProjectLogDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetProjectLogForEditOutput> GetProjectLogForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetProjectLogForEditOutput
        {
            ProjectLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditProjectLogDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditProjectLogDto input)
    {
        var entity = MapToEntity(input, new ProjectLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditProjectLogDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ProjectLog MapToEntity(CreateOrEditProjectLogDto input, ProjectLog entity)
    {
        entity.Message = input.Message;
        entity.RunTimeStamp = input.RunTimeStamp;
        entity.EmployeeId = input.EmployeeId;
        entity.Status = input.Status;
        entity.InPeriod = input.InPeriod;
        entity.StopFlag = input.StopFlag;
        entity.NoOfEmployee = input.NoOfEmployee;

        return entity;
    }

    private static ProjectLogDto ToDto(ProjectLog entity)
    {
        return new ProjectLogDto
        {
            Id = entity.Id,
            Message = entity.Message,
            RunTimeStamp = entity.RunTimeStamp,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FullNameEn,
            Status = entity.Status,
            InPeriod = entity.InPeriod,
            StopFlag = entity.StopFlag,
            NoOfEmployee = entity.NoOfEmployee
        };
    }

    private static CreateOrEditProjectLogDto ToCreateOrEditDto(ProjectLog entity)
    {
        return new CreateOrEditProjectLogDto
        {
            Id = entity.Id,
            Message = entity.Message,
            RunTimeStamp = entity.RunTimeStamp,
            EmployeeId = entity.EmployeeId,
            Status = entity.Status,
            InPeriod = entity.InPeriod,
            StopFlag = entity.StopFlag,
            NoOfEmployee = entity.NoOfEmployee
        };
    }
}
