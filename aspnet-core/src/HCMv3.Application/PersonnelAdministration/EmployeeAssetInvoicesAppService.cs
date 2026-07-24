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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeAssetInvoice"/> (ported from legacy XAF EmployeeAssetInvoice
/// business object, Wave 4 deferred bucket). Ports the essential active validation only: required fields
/// and no duplicate invoice for the same Employee+Asset+Month+Year. The legacy
/// <c>AddMobileDeduction</c>/<c>DeleteMobileDeduction</c> cascade against <c>EmployeePayment</c> is NOT
/// ported -- see class remarks on <see cref="EmployeeAssetInvoice"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices)]
public class EmployeeAssetInvoicesAppService : HCMv3AppServiceBase, IEmployeeAssetInvoicesAppService
{
    private readonly IRepository<EmployeeAssetInvoice> _repository;

    public EmployeeAssetInvoicesAppService(IRepository<EmployeeAssetInvoice> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAssetInvoiceDto>> GetAll(GetAllEmployeeAssetInvoicesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.Asset)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAssetInvoiceDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAssetInvoiceForEditOutput> GetEmployeeAssetInvoiceForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAssetInvoiceForEditOutput
        {
            EmployeeAssetInvoice = new CreateOrEditEmployeeAssetInvoiceDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AssetId = entity.AssetId,
                InvoiceDate = entity.InvoiceDate,
                InvoiceAmount = entity.InvoiceAmount,
                EmployeePlanAmount = entity.EmployeePlanAmount,
                DeductionAmount = entity.DeductionAmount,
                DeductFromEmployee = entity.DeductFromEmployee,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAssetInvoiceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAssetInvoiceDto input)
    {
        var entity = MapToEntity(input, new EmployeeAssetInvoice());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAssetInvoiceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmployeeAssetInvoice entity)
    {
        var hasDuplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId && e.AssetId == entity.AssetId
                && e.InvoiceDate.Month == entity.InvoiceDate.Month && e.InvoiceDate.Year == entity.InvoiceDate.Year);
        if (hasDuplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static EmployeeAssetInvoice MapToEntity(CreateOrEditEmployeeAssetInvoiceDto input, EmployeeAssetInvoice entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AssetId = input.AssetId;
        entity.InvoiceDate = input.InvoiceDate.Date;
        entity.InvoiceAmount = input.InvoiceAmount;
        entity.EmployeePlanAmount = input.EmployeePlanAmount;
        entity.DeductionAmount = input.DeductionAmount;
        entity.DeductFromEmployee = input.DeductFromEmployee;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeAssetInvoiceDto ToDto(EmployeeAssetInvoice entity)
    {
        return new EmployeeAssetInvoiceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AssetId = entity.AssetId,
            AssetNameEn = entity.Asset?.NameEn,
            InvoiceDate = entity.InvoiceDate,
            InvoiceAmount = entity.InvoiceAmount,
            EmployeePlanAmount = entity.EmployeePlanAmount,
            DeductionAmount = entity.DeductionAmount,
            DeductFromEmployee = entity.DeductFromEmployee,
            Comments = entity.Comments
        };
    }
}
