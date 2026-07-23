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
/// Application service for the EmployeeGroup master (ported from legacy XAF EmployeeGroup business object).
/// See <see cref="EmployeeGroup"/> class remarks for the dead-field / EmployeeSubGroup simplifications.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeGroups)]
public class EmployeeGroupsAppService : HCMv3AppServiceBase, IEmployeeGroupsAppService
{
    private readonly IRepository<EmployeeGroup> _employeeGroupRepository;

    public EmployeeGroupsAppService(IRepository<EmployeeGroup> employeeGroupRepository)
    {
        _employeeGroupRepository = employeeGroupRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeGroupDto>> GetAll(GetAllEmployeeGroupsInput input)
    {
        input.Normalize();

        var query = _employeeGroupRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), g =>
                g.Code.Contains(input.Filter) ||
                g.NameEn.Contains(input.Filter) ||
                g.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeGroupDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeGroupForEditOutput> GetEmployeeGroupForEdit(EntityDto input)
    {
        var entity = await _employeeGroupRepository.GetAsync(input.Id);

        return new GetEmployeeGroupForEditOutput
        {
            EmployeeGroup = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeGroupDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeGroups_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeGroupRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeGroups_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeGroupDto input)
    {
        var entity = MapToEntity(input, new EmployeeGroup());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _employeeGroupRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeGroups_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeGroupDto input)
    {
        var entity = await _employeeGroupRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _employeeGroupRepository.UpdateAsync(entity);
    }

    private static EmployeeGroup MapToEntity(CreateOrEditEmployeeGroupDto input, EmployeeGroup entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.IndemnityMaxNumber = input.IndemnityMaxNumber;
        entity.IndemnityMaxUnit = input.IndemnityMaxUnit;
        entity.IndemnityMaxAccruePercentage = input.IndemnityMaxAccruePercentage;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(EmployeeGroup entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _employeeGroupRepository.GetAll()
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
    private async Task ValidateAsync(EmployeeGroup entity)
    {
        var duplicateCode = await _employeeGroupRepository.GetAll()
            .AnyAsync(g => g.Id != entity.Id && g.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _employeeGroupRepository.GetAll()
            .AnyAsync(g => g.Id != entity.Id &&
                           (g.NameEn == entity.NameEn || g.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static EmployeeGroupDto ToDto(EmployeeGroup entity)
    {
        return new EmployeeGroupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IndemnityMaxNumber = entity.IndemnityMaxNumber,
            IndemnityMaxUnit = entity.IndemnityMaxUnit,
            IndemnityMaxAccruePercentage = entity.IndemnityMaxAccruePercentage
        };
    }

    private static CreateOrEditEmployeeGroupDto ToCreateOrEditDto(EmployeeGroup entity)
    {
        return new CreateOrEditEmployeeGroupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IndemnityMaxNumber = entity.IndemnityMaxNumber,
            IndemnityMaxUnit = entity.IndemnityMaxUnit,
            IndemnityMaxAccruePercentage = entity.IndemnityMaxAccruePercentage
        };
    }
}
