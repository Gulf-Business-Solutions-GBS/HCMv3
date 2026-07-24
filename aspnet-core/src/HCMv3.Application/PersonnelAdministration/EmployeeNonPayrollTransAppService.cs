using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeNonPayrollTrans"/> (ported from legacy XAF
/// EmployeeNonPayrollTrans business object, Wave 4 deferred bucket). Ports the essential required-field
/// validation only. The legacy <c>NonPayrollWageType</c> data-source filter (restricting selectable
/// WageTypes) is NOT enforced at this layer -- see class remarks on <see cref="EmployeeNonPayrollTrans"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans)]
public class EmployeeNonPayrollTransAppService : HCMv3AppServiceBase, IEmployeeNonPayrollTransAppService
{
    private readonly IRepository<EmployeeNonPayrollTrans> _repository;

    public EmployeeNonPayrollTransAppService(IRepository<EmployeeNonPayrollTrans> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeNonPayrollTransDto>> GetAll(GetAllEmployeeNonPayrollTransInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WageType)
            .Include(e => e.Currency)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeNonPayrollTransDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeNonPayrollTransForEditOutput> GetEmployeeNonPayrollTransForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeNonPayrollTransForEditOutput
        {
            EmployeeNonPayrollTrans = new CreateOrEditEmployeeNonPayrollTransDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                WageTypeId = entity.WageTypeId,
                Amount = entity.Amount,
                CurrencyId = entity.CurrencyId,
                PaymentDate = entity.PaymentDate,
                IsLocked = entity.IsLocked,
                Comments = entity.Comments,
                NotificationSent = entity.NotificationSent,
                PayableBy = entity.PayableBy,
                PaymentMethod = entity.PaymentMethod
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeNonPayrollTransDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeNonPayrollTransDto input)
    {
        var entity = MapToEntity(input, new EmployeeNonPayrollTrans());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeNonPayrollTransDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeNonPayrollTrans MapToEntity(CreateOrEditEmployeeNonPayrollTransDto input, EmployeeNonPayrollTrans entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.WageTypeId = input.WageTypeId;
        entity.Amount = input.Amount;
        entity.CurrencyId = input.CurrencyId;
        entity.PaymentDate = input.PaymentDate.Date;
        entity.IsLocked = input.IsLocked;
        entity.Comments = input.Comments;
        entity.NotificationSent = input.NotificationSent;
        entity.PayableBy = input.PayableBy;
        entity.PaymentMethod = input.PaymentMethod;
        return entity;
    }

    private static EmployeeNonPayrollTransDto ToDto(EmployeeNonPayrollTrans entity)
    {
        return new EmployeeNonPayrollTransDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeNameEn = entity.WageType?.NameEn,
            Amount = entity.Amount,
            CurrencyId = entity.CurrencyId,
            CurrencyNameEn = entity.Currency?.NameEn,
            PaymentDate = entity.PaymentDate,
            IsLocked = entity.IsLocked,
            Comments = entity.Comments,
            NotificationSent = entity.NotificationSent,
            PayableBy = entity.PayableBy,
            PaymentMethod = entity.PaymentMethod
        };
    }
}
