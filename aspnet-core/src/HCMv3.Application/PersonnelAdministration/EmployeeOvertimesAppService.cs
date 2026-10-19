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
using HCMv3.PersonnelAdministration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.Workflow.Services;
using HCMv3.PersonnelAdministration.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeOvertime"/> (ported from legacy XAF EmployeeOvertime business
/// object, Wave 4 deferred bucket). Ports the essential active validation only: required fields and no
/// duplicate Employee+EffectiveDate. The legacy read-only <c>ExpectedOvertimeAmount</c> is calculated on read by
/// <see cref="IExpectedOvertimeAmountService"/>. The EmployeeSubGroup.PayOvertime cross-check is NOT ported -- see
/// <see cref="EmployeeOvertime"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes)]
public class EmployeeOvertimesAppService : HCMv3AppServiceBase, IEmployeeOvertimesAppService
{
    private readonly IRepository<EmployeeOvertime> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly IExpectedOvertimeAmountService _expectedOvertimeAmountService;
    private readonly IEmployeeOvertimeService _employeeOvertimeService;

    public EmployeeOvertimesAppService(IRepository<EmployeeOvertime> repository,
        IAdminTransactionSubmitter transactionSubmitter,
        IExpectedOvertimeAmountService expectedOvertimeAmountService,
        IEmployeeOvertimeService employeeOvertimeService)
    {
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
        _expectedOvertimeAmountService = expectedOvertimeAmountService;
        _employeeOvertimeService = employeeOvertimeService;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeOvertimeDto>> GetAll(GetAllEmployeeOvertimesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.OvertimeType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var dtos = items.Select(ToDto).ToList();

        // Legacy non-persistent ExpectedOvertimeAmount, worked out for the page only.
        var amounts = await _expectedOvertimeAmountService.GetExpectedOvertimeAmountsAsync(items);
        foreach (var dto in dtos)
        {
            dto.ExpectedOvertimeAmount = amounts.TryGetValue(dto.Id, out var amount) ? amount : 0;
        }

        return new PagedResultDto<EmployeeOvertimeDto>(totalCount, dtos);
    }

    public async Task<GetEmployeeOvertimeForEditOutput> GetEmployeeOvertimeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeOvertimeForEditOutput
        {
            EmployeeOvertime = new CreateOrEditEmployeeOvertimeDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EffectiveDate = entity.EffectiveDate,
                OvertimeTypeId = entity.OvertimeTypeId,
                OvertimeHours = entity.OvertimeHours,
                ActualOvertimeHours = entity.ActualOvertimeHours,
                ApprovedOvertimeHours = entity.ApprovedOvertimeHours,
                Comments = entity.Comments
            },
            ExpectedOvertimeAmount = await _expectedOvertimeAmountService.GetExpectedOvertimeAmountAsync(
                entity.EmployeeId, entity.OvertimeTypeId, entity.EffectiveDate, entity.ApprovedOvertimeHours)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeOvertimeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeOvertimeDto input)
    {
        var entity = MapToEntity(input, new EmployeeOvertime());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            OvertimeWorkflowTransactionHandler.OvertimeRequestType,
            "OT",
            entity.OvertimeTypeId,
            input.RecordDirectly);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeOvertimeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// One row per employee per day -- shared with the overtime batch, whose approval writes rows too.
    /// See <see cref="IEmployeeOvertimeService"/>.
    /// </summary>
    private Task ValidateEntityAsync(EmployeeOvertime entity) =>
        _employeeOvertimeService.EnsureNoOvertimeOnDateAsync(entity.EmployeeId, entity.EffectiveDate, entity.Id);

    private static EmployeeOvertime MapToEntity(CreateOrEditEmployeeOvertimeDto input, EmployeeOvertime entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.EffectiveDate = input.EffectiveDate.Date;
        entity.OvertimeTypeId = input.OvertimeTypeId;
        entity.OvertimeHours = input.OvertimeHours;
        entity.ActualOvertimeHours = input.ActualOvertimeHours;
        entity.ApprovedOvertimeHours = input.ApprovedOvertimeHours;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeOvertimeDto ToDto(EmployeeOvertime entity)
    {
        return new EmployeeOvertimeDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            EffectiveDate = entity.EffectiveDate,
            OvertimeTypeId = entity.OvertimeTypeId,
            OvertimeTypeNameEn = entity.OvertimeType?.NameEn,
            OvertimeHours = entity.OvertimeHours,
            ActualOvertimeHours = entity.ActualOvertimeHours,
            ApprovedOvertimeHours = entity.ApprovedOvertimeHours,
            Comments = entity.Comments
        };
    }
}
