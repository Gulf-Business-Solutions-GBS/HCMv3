using System;
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
using HCMv3.OrganizationManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Application service for the Organization Unit Type master (ported from legacy XAF
/// OrganizationUnitType business object). Legacy <c>PerformValidation</c> parity: Code must be one of
/// the 10 fixed <see cref="OrganizationUnitType.AllowedCodes"/>, at most one record may have
/// <see cref="OrganizationUnitType.AllowLeaveConflict"/> set, at most one (non Level0) record may have
/// <see cref="OrganizationUnitType.AssignPayrollArea"/> set, plus the standard duplicate-Code check
/// (Code is user-selected from a dropdown here, not auto-generated).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrganizationUnitTypes)]
public class OrganizationUnitTypesAppService : HCMv3AppServiceBase, IOrganizationUnitTypesAppService
{
    private readonly IRepository<OrganizationUnitType> _organizationUnitTypeRepository;

    public OrganizationUnitTypesAppService(IRepository<OrganizationUnitType> organizationUnitTypeRepository)
    {
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OrganizationUnitTypeDto>> GetAll(GetAllOrganizationUnitTypesInput input)
    {
        input.Normalize();

        var query = _organizationUnitTypeRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OrganizationUnitTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOrganizationUnitTypeForEditOutput> GetOrganizationUnitTypeForEdit(EntityDto input)
    {
        var organizationUnitType = await _organizationUnitTypeRepository.GetAsync(input.Id);

        return new GetOrganizationUnitTypeForEditOutput
        {
            OrganizationUnitType = ToCreateOrEditDto(organizationUnitType)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOrganizationUnitTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrganizationUnitTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOrganizationUnitTypeDto input)
    {
        var organizationUnitType = new OrganizationUnitType
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            AllowLeaveConflict = input.AllowLeaveConflict,
            AssignPayrollArea = input.AssignPayrollArea,
            TenantId = AbpSession.TenantId ?? 0
        };

        await ValidateAsync(organizationUnitType);

        await _organizationUnitTypeRepository.InsertAsync(organizationUnitType);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrganizationUnitTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOrganizationUnitTypeDto input)
    {
        var organizationUnitType = await _organizationUnitTypeRepository.GetAsync(input.Id.Value);

        organizationUnitType.Code = input.Code;
        organizationUnitType.NameEn = input.NameEn;
        organizationUnitType.NameAr = input.NameAr;
        organizationUnitType.AllowLeaveConflict = input.AllowLeaveConflict;
        organizationUnitType.AssignPayrollArea = input.AssignPayrollArea;

        await ValidateAsync(organizationUnitType);

        await _organizationUnitTypeRepository.UpdateAsync(organizationUnitType);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrganizationUnitTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _organizationUnitTypeRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity for OrganizationUnitType (fixed Code list, single
    /// AllowLeaveConflict/AssignPayrollArea, duplicate-Code check).
    /// </summary>
    private async Task ValidateAsync(OrganizationUnitType organizationUnitType)
    {
        if (!OrganizationUnitType.AllowedCodes.Contains(organizationUnitType.Code, StringComparer.Ordinal))
        {
            throw new UserFriendlyException(L("OrganizationUnitTypeCodeMustBeFromList"));
        }

        var duplicateCode = await _organizationUnitTypeRepository.GetAll()
            .AnyAsync(t => t.Id != organizationUnitType.Id && t.Code == organizationUnitType.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        if (organizationUnitType.AllowLeaveConflict)
        {
            var conflicting = await _organizationUnitTypeRepository.GetAll()
                .FirstOrDefaultAsync(t => t.Id != organizationUnitType.Id && t.AllowLeaveConflict);
            if (conflicting != null)
            {
                throw new UserFriendlyException(L("AllowLeaveConflictAlreadySetOnAnotherType", conflicting.NameEn));
            }
        }

        if (organizationUnitType.AssignPayrollArea)
        {
            if (organizationUnitType.Code == OrganizationUnitType.Level0Code)
            {
                throw new UserFriendlyException(L("AssignPayrollAreaNotAllowedOnLevel0"));
            }

            var conflicting = await _organizationUnitTypeRepository.GetAll()
                .FirstOrDefaultAsync(t => t.Id != organizationUnitType.Id && t.AssignPayrollArea);
            if (conflicting != null)
            {
                throw new UserFriendlyException(L("AssignPayrollAreaAlreadySetOnAnotherType", conflicting.NameEn));
            }
        }
    }

    private static OrganizationUnitTypeDto ToDto(OrganizationUnitType organizationUnitType)
    {
        return new OrganizationUnitTypeDto
        {
            Id = organizationUnitType.Id,
            Code = organizationUnitType.Code,
            NameEn = organizationUnitType.NameEn,
            NameAr = organizationUnitType.NameAr,
            AllowLeaveConflict = organizationUnitType.AllowLeaveConflict,
            AssignPayrollArea = organizationUnitType.AssignPayrollArea
        };
    }

    private static CreateOrEditOrganizationUnitTypeDto ToCreateOrEditDto(OrganizationUnitType organizationUnitType)
    {
        return new CreateOrEditOrganizationUnitTypeDto
        {
            Id = organizationUnitType.Id,
            Code = organizationUnitType.Code,
            NameEn = organizationUnitType.NameEn,
            NameAr = organizationUnitType.NameAr,
            AllowLeaveConflict = organizationUnitType.AllowLeaveConflict,
            AssignPayrollArea = organizationUnitType.AssignPayrollArea
        };
    }
}
