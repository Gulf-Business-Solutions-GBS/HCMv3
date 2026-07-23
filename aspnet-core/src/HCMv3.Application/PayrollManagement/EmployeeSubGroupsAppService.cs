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
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the EmployeeSubGroup master, child of <see cref="EmployeeGroup"/> (ported from
/// legacy XAF EmployeeSubGroup business object). See <see cref="EmployeeSubGroup"/> class remarks for the
/// flat parent+child simplification of the 13 aggregated child collections.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroups)]
public class EmployeeSubGroupsAppService : HCMv3AppServiceBase, IEmployeeSubGroupsAppService
{
    private readonly IRepository<EmployeeSubGroup> _employeeSubGroupRepository;

    public EmployeeSubGroupsAppService(IRepository<EmployeeSubGroup> employeeSubGroupRepository)
    {
        _employeeSubGroupRepository = employeeSubGroupRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupDto>> GetAll(GetAllEmployeeSubGroupsInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupRepository.GetAll()
            .Include(g => g.EmployeeGroup)
            .WhereIf(input.EmployeeGroupId.HasValue, g => g.EmployeeGroupId == input.EmployeeGroupId)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), g =>
                g.Code.Contains(input.Filter) ||
                g.NameEn.Contains(input.Filter) ||
                g.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupForEditOutput> GetEmployeeSubGroupForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupForEditOutput
        {
            EmployeeSubGroup = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroups_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroups_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroup());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _employeeSubGroupRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroups_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupDto input)
    {
        var entity = await _employeeSubGroupRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _employeeSubGroupRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroup MapToEntity(CreateOrEditEmployeeSubGroupDto input, EmployeeSubGroup entity)
    {
        entity.EmployeeGroupId = input.EmployeeGroupId;
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.TicketClassId = input.TicketClassId;
        entity.PayOvertime = input.PayOvertime;
        entity.AirTicketPayType = input.AirTicketPayType;
        entity.MaxHealthReimburseAmount = input.MaxHealthReimburseAmount;
        entity.HealthInsurancePeriod = input.HealthInsurancePeriod;
        entity.AutoPayAirTicket = input.AutoPayAirTicket;
        entity.MaxDependantAirTicketCount = input.MaxDependantAirTicketCount;
        entity.MaxDependantHealthReimburseCount = input.MaxDependantHealthReimburseCount;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(EmployeeSubGroup entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _employeeSubGroupRepository.GetAll()
            .Where(g => g.Code != null && g.Code != "")
            .Select(g => g.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroup entity)
    {
        var duplicateCode = await _employeeSubGroupRepository.GetAll()
            .AnyAsync(g => g.Id != entity.Id && g.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _employeeSubGroupRepository.GetAll()
            .AnyAsync(g => g.Id != entity.Id &&
                           (g.NameEn == entity.NameEn || g.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static EmployeeSubGroupDto ToDto(EmployeeSubGroup entity)
    {
        return new EmployeeSubGroupDto
        {
            Id = entity.Id,
            EmployeeGroupId = entity.EmployeeGroupId,
            EmployeeGroupName = entity.EmployeeGroup?.NameEn,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            TicketClassId = entity.TicketClassId,
            PayOvertime = entity.PayOvertime,
            AirTicketPayType = entity.AirTicketPayType,
            MaxHealthReimburseAmount = entity.MaxHealthReimburseAmount,
            HealthInsurancePeriod = entity.HealthInsurancePeriod,
            AutoPayAirTicket = entity.AutoPayAirTicket,
            MaxDependantAirTicketCount = entity.MaxDependantAirTicketCount,
            MaxDependantHealthReimburseCount = entity.MaxDependantHealthReimburseCount
        };
    }

    private static CreateOrEditEmployeeSubGroupDto ToCreateOrEditDto(EmployeeSubGroup entity)
    {
        return new CreateOrEditEmployeeSubGroupDto
        {
            Id = entity.Id,
            EmployeeGroupId = entity.EmployeeGroupId,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            TicketClassId = entity.TicketClassId,
            PayOvertime = entity.PayOvertime,
            AirTicketPayType = entity.AirTicketPayType,
            MaxHealthReimburseAmount = entity.MaxHealthReimburseAmount,
            HealthInsurancePeriod = entity.HealthInsurancePeriod,
            AutoPayAirTicket = entity.AutoPayAirTicket,
            MaxDependantAirTicketCount = entity.MaxDependantAirTicketCount,
            MaxDependantHealthReimburseCount = entity.MaxDependantHealthReimburseCount
        };
    }
}
