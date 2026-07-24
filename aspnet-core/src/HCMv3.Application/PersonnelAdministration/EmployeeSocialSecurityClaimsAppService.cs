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
/// Application service for <see cref="EmployeeSocialSecurityClaim"/> (ported from legacy XAF
/// EmployeeSocialSecurityClaim business object, Wave 3 deferred bucket). Ports the legacy validation:
/// employee and claim contribution amounts cannot both be zero.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurityClaims)]
public class EmployeeSocialSecurityClaimsAppService : HCMv3AppServiceBase, IEmployeeSocialSecurityClaimsAppService
{
    private readonly IRepository<EmployeeSocialSecurityClaim> _repository;

    public EmployeeSocialSecurityClaimsAppService(IRepository<EmployeeSocialSecurityClaim> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSocialSecurityClaimDto>> GetAll(GetAllEmployeeSocialSecurityClaimsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(c => c.Employee)
            .WhereIf(input.EmployeeId.HasValue, c => c.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSocialSecurityClaimDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSocialSecurityClaimForEditOutput> GetEmployeeSocialSecurityClaimForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeSocialSecurityClaimForEditOutput
        {
            EmployeeSocialSecurityClaim = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSocialSecurityClaimDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurityClaims_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurityClaims_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSocialSecurityClaimDto input)
    {
        var entity = MapToEntity(input, new EmployeeSocialSecurityClaim());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurityClaims_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSocialSecurityClaimDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        Validate(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>Legacy <c>PerformValidations</c> parity: both contribution amounts cannot be zero.</summary>
    private void Validate(EmployeeSocialSecurityClaim entity)
    {
        if (entity.EmployeeContributionAmount == 0 && entity.EmployerContributionAmount == 0)
        {
            throw new UserFriendlyException(L("EmployeeAndEmployerContributionAmountCannotBothBeBlank"));
        }
    }

    private static EmployeeSocialSecurityClaim MapToEntity(CreateOrEditEmployeeSocialSecurityClaimDto input, EmployeeSocialSecurityClaim entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.ClaimDate = input.ClaimDate.Date;
        entity.EmployeeContributionAmount = input.EmployeeContributionAmount;
        entity.EmployerContributionAmount = input.EmployerContributionAmount;
        return entity;
    }

    private static EmployeeSocialSecurityClaimDto ToDto(EmployeeSocialSecurityClaim entity)
    {
        return new EmployeeSocialSecurityClaimDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            ClaimDate = entity.ClaimDate,
            EmployeeContributionAmount = entity.EmployeeContributionAmount,
            EmployerContributionAmount = entity.EmployerContributionAmount
        };
    }

    private static CreateOrEditEmployeeSocialSecurityClaimDto ToCreateOrEditDto(EmployeeSocialSecurityClaim entity)
    {
        return new CreateOrEditEmployeeSocialSecurityClaimDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            ClaimDate = entity.ClaimDate,
            EmployeeContributionAmount = entity.EmployeeContributionAmount,
            EmployerContributionAmount = entity.EmployerContributionAmount
        };
    }
}
