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
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeAbsAdjustment"/> -- the legacy "Leave Abs Adjustment" screen
/// (XAF <c>EmployeeAbsAdjustment</c>). Records the adjustment AND, as legacy <c>OnSaving</c>/<c>OnDeleting</c>
/// did, moves <c>QuotaAdjusted</c> on the linked annual-leave <see cref="EmployeeAbsenceQuota"/> through
/// <see cref="IEmployeeAbsAdjustmentQuotaService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments)]
public class EmployeeAbsAdjustmentsAppService : HCMv3AppServiceBase, IEmployeeAbsAdjustmentsAppService
{
    private readonly IRepository<EmployeeAbsAdjustment> _repository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeAbsAdjustmentQuotaService _quotaService;

    public EmployeeAbsAdjustmentsAppService(
        IRepository<EmployeeAbsAdjustment> repository,
        IRepository<Employee> employeeRepository,
        IEmployeeAbsAdjustmentQuotaService quotaService)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _quotaService = quotaService;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAbsAdjustmentDto>> GetAll(GetAllEmployeeAbsAdjustmentsInput input)
    {
        input.Normalize();

        var filter = input.Filter?.Trim();

        var query = _repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.AbsenceType)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value)
            .WhereIf(!filter.IsNullOrWhiteSpace(), a =>
                a.Employee.Code.Contains(filter)
                || a.Employee.FullNameEn.Contains(filter)
                || a.AbsenceType.NameEn.Contains(filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAbsAdjustmentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAbsAdjustmentForEditOutput> GetEmployeeAbsAdjustmentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var employee = await _employeeRepository.FirstOrDefaultAsync(entity.EmployeeId);

        return new GetEmployeeAbsAdjustmentForEditOutput
        {
            EmployeeAbsAdjustment = new CreateOrEditEmployeeAbsAdjustmentDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AbsenceTypeId = entity.AbsenceTypeId,
                AdjustmentType = entity.AdjustmentType,
                EffectiveDate = entity.EffectiveDate,
                QuotaAdjusted = entity.QuotaAdjusted,
                Comments = entity.Comments,
                AdjustmentPaymentType = entity.AdjustmentPaymentType
            },
            EmployeeCodeWithFullNameEn = employee == null
                ? null
                : employee.FullNameEn.IsNullOrWhiteSpace() ? employee.Code : employee.Code + " " + employee.FullNameEn
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAbsAdjustmentDto input)
    {
        // Legacy PerformValidations: "EffectiveDate cannot be null."
        if (input.EffectiveDate == default)
        {
            throw new UserFriendlyException(L("EffectiveDateCannotBeEmpty"));
        }

        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAbsAdjustmentDto input)
    {
        var entity = MapToEntity(input, new EmployeeAbsAdjustment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // A new row has no previous values (legacy Oid == -1): only the new value is charged.
        await _quotaService.ApplyAsync(entity, previous: null);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAbsAdjustmentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        // Legacy OnLoaded: the values as stored, captured before the row is overwritten.
        var previous = EmployeeAbsAdjustmentSnapshot.Of(entity);

        MapToEntity(input, entity);

        await _quotaService.ApplyAsync(entity, previous);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        // Legacy OnDeleting: the stored adjustment goes back to the quota.
        await _quotaService.ReverseAsync(entity);

        await _repository.DeleteAsync(entity);
    }

    private static EmployeeAbsAdjustment MapToEntity(CreateOrEditEmployeeAbsAdjustmentDto input, EmployeeAbsAdjustment entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.AdjustmentType = input.AdjustmentType;
        entity.EffectiveDate = input.EffectiveDate.Date;
        entity.QuotaAdjusted = System.Math.Round(input.QuotaAdjusted, 6);
        entity.Comments = input.Comments;
        entity.AdjustmentPaymentType = input.AdjustmentPaymentType;
        return entity;
    }

    private static EmployeeAbsAdjustmentDto ToDto(EmployeeAbsAdjustment entity)
    {
        return new EmployeeAbsAdjustmentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            AdjustmentType = entity.AdjustmentType,
            EffectiveDate = entity.EffectiveDate,
            QuotaAdjusted = entity.QuotaAdjusted,
            Comments = entity.Comments,
            AdjustmentPaymentType = entity.AdjustmentPaymentType
        };
    }
}
