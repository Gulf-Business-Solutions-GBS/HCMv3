using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the escalation step log entry of an <see cref="AbsenceViolationActionRecord"/>
/// (ported from legacy XAF AbsenceViolationActionLog business object), listed separately and linked back
/// via <see cref="AbsenceViolationActionLog.AbsenceViolationActionRecordId"/>. No business validation in the
/// legacy source (plain CRUD). The legacy <c>TaskEvent</c> FK is dropped — see
/// <see cref="AbsenceViolationActionLog"/> remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionLogs)]
public class AbsenceViolationActionLogsAppService : HCMv3AppServiceBase, IAbsenceViolationActionLogsAppService
{
    private readonly IRepository<AbsenceViolationActionLog> _absenceViolationActionLogRepository;

    public AbsenceViolationActionLogsAppService(IRepository<AbsenceViolationActionLog> absenceViolationActionLogRepository)
    {
        _absenceViolationActionLogRepository = absenceViolationActionLogRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AbsenceViolationActionLogDto>> GetAll(GetAllAbsenceViolationActionLogsInput input)
    {
        input.Normalize();

        var query = _absenceViolationActionLogRepository.GetAll()
            .WhereIf(input.AbsenceViolationActionRecordId.HasValue, l => l.AbsenceViolationActionRecordId == input.AbsenceViolationActionRecordId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsenceViolationActionLogDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAbsenceViolationActionLogForEditOutput> GetAbsenceViolationActionLogForEdit(EntityDto input)
    {
        var entity = await _absenceViolationActionLogRepository.GetAsync(input.Id);

        return new GetAbsenceViolationActionLogForEditOutput
        {
            AbsenceViolationActionLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAbsenceViolationActionLogDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _absenceViolationActionLogRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAbsenceViolationActionLogDto input)
    {
        var entity = MapToEntity(input, new AbsenceViolationActionLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _absenceViolationActionLogRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAbsenceViolationActionLogDto input)
    {
        var entity = await _absenceViolationActionLogRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _absenceViolationActionLogRepository.UpdateAsync(entity);
    }

    private static AbsenceViolationActionLog MapToEntity(CreateOrEditAbsenceViolationActionLogDto input, AbsenceViolationActionLog entity)
    {
        entity.AbsenceViolationActionRecordId = input.AbsenceViolationActionRecordId;
        entity.Step = input.Step;
        entity.Action = input.Action;
        entity.ActionDate = input.ActionDate;
        entity.ObjectOId = input.ObjectOId;

        return entity;
    }

    private static AbsenceViolationActionLogDto ToDto(AbsenceViolationActionLog entity)
    {
        return new AbsenceViolationActionLogDto
        {
            Id = entity.Id,
            AbsenceViolationActionRecordId = entity.AbsenceViolationActionRecordId,
            Step = entity.Step,
            Action = entity.Action,
            ActionDate = entity.ActionDate,
            ObjectOId = entity.ObjectOId
        };
    }

    private static CreateOrEditAbsenceViolationActionLogDto ToCreateOrEditDto(AbsenceViolationActionLog entity)
    {
        return new CreateOrEditAbsenceViolationActionLogDto
        {
            Id = entity.Id,
            AbsenceViolationActionRecordId = entity.AbsenceViolationActionRecordId,
            Step = entity.Step,
            Action = entity.Action,
            ActionDate = entity.ActionDate,
            ObjectOId = entity.ObjectOId
        };
    }
}
