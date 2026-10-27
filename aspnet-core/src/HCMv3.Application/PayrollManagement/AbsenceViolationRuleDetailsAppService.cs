using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the escalation step of an <see cref="AbsenceViolationRule"/> (ported from legacy
/// XAF AbsenceViolationRuleDetail business object), listed separately and linked back via
/// <see cref="AbsenceViolationRuleDetail.AbsenceViolationRuleId"/>. See
/// <see cref="PayrollManagement.AbsenceViolationRuleDetail"/> class remarks for how the polymorphic
/// <c>ActionType</c> resolution is simplified.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleDetails)]
public class AbsenceViolationRuleDetailsAppService : HCMv3AppServiceBase, IAbsenceViolationRuleDetailsAppService
{
    private readonly IRepository<AbsenceViolationRuleDetail> _absenceViolationRuleDetailRepository;

    public AbsenceViolationRuleDetailsAppService(IRepository<AbsenceViolationRuleDetail> absenceViolationRuleDetailRepository)
    {
        _absenceViolationRuleDetailRepository = absenceViolationRuleDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AbsenceViolationRuleDetailDto>> GetAll(GetAllAbsenceViolationRuleDetailsInput input)
    {
        input.Normalize();

        var query = _absenceViolationRuleDetailRepository.GetAll()
            .Include(d => d.AbsenceViolationRule)
            .Include(d => d.NotificationConfig)
            .WhereIf(input.AbsenceViolationRuleId.HasValue, d => d.AbsenceViolationRuleId == input.AbsenceViolationRuleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsenceViolationRuleDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAbsenceViolationRuleDetailForEditOutput> GetAbsenceViolationRuleDetailForEdit(EntityDto input)
    {
        var entity = await _absenceViolationRuleDetailRepository.GetAsync(input.Id);

        return new GetAbsenceViolationRuleDetailForEditOutput
        {
            AbsenceViolationRuleDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAbsenceViolationRuleDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _absenceViolationRuleDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAbsenceViolationRuleDetailDto input)
    {
        var entity = MapToEntity(input, new AbsenceViolationRuleDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _absenceViolationRuleDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAbsenceViolationRuleDetailDto input)
    {
        var entity = await _absenceViolationRuleDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _absenceViolationRuleDetailRepository.UpdateAsync(entity);
    }

    private static AbsenceViolationRuleDetail MapToEntity(CreateOrEditAbsenceViolationRuleDetailDto input, AbsenceViolationRuleDetail entity)
    {
        entity.AbsenceViolationRuleId = input.AbsenceViolationRuleId;
        entity.Step = input.Step;
        entity.DayInterval = input.DayInterval;
        entity.Action = input.Action;
        entity.ActionTypeId = input.ActionTypeId;
        entity.HoldTillPunchFound = input.HoldTillPunchFound;
        entity.NotificationConfigId = input.NotificationConfigId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Step &gt; 0 and no other (non-deleted) detail row for the
    /// same parent rule uses the same Step.
    /// </summary>
    private async Task ValidateAsync(AbsenceViolationRuleDetail entity)
    {
        if (entity.Step <= 0)
        {
            throw new UserFriendlyException(L("StepMustBeGreaterThanZero"));
        }

        var duplicateStep = await _absenceViolationRuleDetailRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.AbsenceViolationRuleId == entity.AbsenceViolationRuleId &&
                           d.Step == entity.Step);
        if (duplicateStep)
        {
            throw new UserFriendlyException(L("StepAlreadyExists", entity.Step));
        }
    }

    private static AbsenceViolationRuleDetailDto ToDto(AbsenceViolationRuleDetail entity)
    {
        return new AbsenceViolationRuleDetailDto
        {
            Id = entity.Id,
            AbsenceViolationRuleId = entity.AbsenceViolationRuleId,
            AbsenceViolationRuleName = entity.AbsenceViolationRule?.NameEn,
            Step = entity.Step,
            DayInterval = entity.DayInterval,
            Action = entity.Action,
            ActionTypeId = entity.ActionTypeId,
            HoldTillPunchFound = entity.HoldTillPunchFound,
            NotificationConfigId = entity.NotificationConfigId,
            NotificationConfigName = entity.NotificationConfig?.Name
        };
    }

    private static CreateOrEditAbsenceViolationRuleDetailDto ToCreateOrEditDto(AbsenceViolationRuleDetail entity)
    {
        return new CreateOrEditAbsenceViolationRuleDetailDto
        {
            Id = entity.Id,
            AbsenceViolationRuleId = entity.AbsenceViolationRuleId,
            Step = entity.Step,
            DayInterval = entity.DayInterval,
            Action = entity.Action,
            ActionTypeId = entity.ActionTypeId,
            HoldTillPunchFound = entity.HoldTillPunchFound,
            NotificationConfigId = entity.NotificationConfigId
        };
    }
}
