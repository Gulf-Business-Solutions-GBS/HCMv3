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
/// Application service for the pay-band deduction step of an <see cref="EmployeeSubGroup"/> fund (ported
/// from legacy XAF EmployeeSubGroupFund business object), listed separately and linked back via
/// <see cref="EmployeeSubGroupFund.EmployeeSubGroupId"/>. Note: the legacy overlap check explicitly filters
/// out soft-deleted sibling rows (<c>!IsDeleted</c>); this port relies on ABP's standard soft-delete query
/// filter, which already excludes deleted rows from <c>IRepository.GetAll()</c>, so no explicit filter is
/// needed here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupFunds)]
public class EmployeeSubGroupFundsAppService : HCMv3AppServiceBase, IEmployeeSubGroupFundsAppService
{
    private readonly IRepository<EmployeeSubGroupFund> _employeeSubGroupFundRepository;

    public EmployeeSubGroupFundsAppService(IRepository<EmployeeSubGroupFund> employeeSubGroupFundRepository)
    {
        _employeeSubGroupFundRepository = employeeSubGroupFundRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupFundDto>> GetAll(GetAllEmployeeSubGroupFundsInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupFundRepository.GetAll()
            .Include(f => f.EmployeeSubGroup)
            .WhereIf(input.EmployeeSubGroupId.HasValue, f => f.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupFundDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupFundForEditOutput> GetEmployeeSubGroupFundForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupFundRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupFundForEditOutput
        {
            EmployeeSubGroupFund = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupFundDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupFunds_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupFundRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupFunds_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupFundDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupFund());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupFundRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupFunds_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupFundDto input)
    {
        var entity = await _employeeSubGroupFundRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupFundRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupFund MapToEntity(CreateOrEditEmployeeSubGroupFundDto input, EmployeeSubGroupFund entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.FromPay = input.FromPay;
        entity.ToPay = input.ToPay;
        entity.FundDedType = input.FundDedType;
        entity.Percentage = input.Percentage;
        entity.MinAmount = input.MinAmount;
        entity.MaxAmount = input.MaxAmount;
        entity.FixAmount = input.FixAmount;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: FromPay &lt;= ToPay and no overlapping pay-band with a
    /// sibling row under the same EmployeeSubGroup (the legacy source ran this overlap check twice with
    /// equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupFund entity)
    {
        if (entity.FromPay > entity.ToPay)
        {
            throw new UserFriendlyException(L("FromPayCannotBeGreaterThanToPay"));
        }

        var overlap = await _employeeSubGroupFundRepository.GetAll()
            .AnyAsync(f => f.Id != entity.Id &&
                           f.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           f.FromPay <= entity.ToPay &&
                           f.ToPay >= entity.FromPay);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static EmployeeSubGroupFundDto ToDto(EmployeeSubGroupFund entity)
    {
        return new EmployeeSubGroupFundDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            FromPay = entity.FromPay,
            ToPay = entity.ToPay,
            FundDedType = entity.FundDedType,
            Percentage = entity.Percentage,
            MinAmount = entity.MinAmount,
            MaxAmount = entity.MaxAmount,
            FixAmount = entity.FixAmount
        };
    }

    private static CreateOrEditEmployeeSubGroupFundDto ToCreateOrEditDto(EmployeeSubGroupFund entity)
    {
        return new CreateOrEditEmployeeSubGroupFundDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            FromPay = entity.FromPay,
            ToPay = entity.ToPay,
            FundDedType = entity.FundDedType,
            Percentage = entity.Percentage,
            MinAmount = entity.MinAmount,
            MaxAmount = entity.MaxAmount,
            FixAmount = entity.FixAmount
        };
    }
}
