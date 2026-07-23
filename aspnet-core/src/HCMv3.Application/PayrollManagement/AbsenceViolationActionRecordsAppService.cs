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
/// Application service for the standalone runtime absence/time-event violation-escalation log header
/// (ported from legacy XAF AbsenceViolationAction business object, renamed to
/// <see cref="AbsenceViolationActionRecord"/> in this port — see its class remarks for why). No business
/// validation in the legacy source (plain CRUD).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionRecords)]
public class AbsenceViolationActionRecordsAppService : HCMv3AppServiceBase, IAbsenceViolationActionRecordsAppService
{
    private readonly IRepository<AbsenceViolationActionRecord> _absenceViolationActionRecordRepository;

    public AbsenceViolationActionRecordsAppService(IRepository<AbsenceViolationActionRecord> absenceViolationActionRecordRepository)
    {
        _absenceViolationActionRecordRepository = absenceViolationActionRecordRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AbsenceViolationActionRecordDto>> GetAll(GetAllAbsenceViolationActionRecordsInput input)
    {
        input.Normalize();

        var query = _absenceViolationActionRecordRepository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsenceViolationActionRecordDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAbsenceViolationActionRecordForEditOutput> GetAbsenceViolationActionRecordForEdit(EntityDto input)
    {
        var entity = await _absenceViolationActionRecordRepository.GetAsync(input.Id);

        return new GetAbsenceViolationActionRecordForEditOutput
        {
            AbsenceViolationActionRecord = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAbsenceViolationActionRecordDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionRecords_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _absenceViolationActionRecordRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionRecords_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAbsenceViolationActionRecordDto input)
    {
        var entity = MapToEntity(input, new AbsenceViolationActionRecord());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _absenceViolationActionRecordRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionRecords_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAbsenceViolationActionRecordDto input)
    {
        var entity = await _absenceViolationActionRecordRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _absenceViolationActionRecordRepository.UpdateAsync(entity);
    }

    private static AbsenceViolationActionRecord MapToEntity(CreateOrEditAbsenceViolationActionRecordDto input, AbsenceViolationActionRecord entity)
    {
        entity.ActionLogType = input.ActionLogType;
        entity.LogStatus = input.LogStatus;
        entity.EntityObjectId = input.EntityObjectId;

        return entity;
    }

    private static AbsenceViolationActionRecordDto ToDto(AbsenceViolationActionRecord entity)
    {
        return new AbsenceViolationActionRecordDto
        {
            Id = entity.Id,
            ActionLogType = entity.ActionLogType,
            LogStatus = entity.LogStatus,
            EntityObjectId = entity.EntityObjectId
        };
    }

    private static CreateOrEditAbsenceViolationActionRecordDto ToCreateOrEditDto(AbsenceViolationActionRecord entity)
    {
        return new CreateOrEditAbsenceViolationActionRecordDto
        {
            Id = entity.Id,
            ActionLogType = entity.ActionLogType,
            LogStatus = entity.LogStatus,
            EntityObjectId = entity.EntityObjectId
        };
    }
}
