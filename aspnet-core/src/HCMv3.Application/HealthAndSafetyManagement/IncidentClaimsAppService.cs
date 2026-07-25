using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.HealthAndSafetyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Application service for the insurance/compensation claim raised against an
/// <see cref="IncidentRegister"/> (ported from legacy XAF IncidentClaim business object). Legacy
/// <c>PerformValidation</c> required ClaimDate, Incident, PaymentDate and Currency -- ported as-is via
/// DataAnnotations on <see cref="CreateOrEditIncidentClaimDto"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaims)]
public class IncidentClaimsAppService : HCMv3AppServiceBase, IIncidentClaimsAppService
{
    private readonly IRepository<IncidentClaim> _incidentClaimRepository;

    public IncidentClaimsAppService(IRepository<IncidentClaim> incidentClaimRepository)
    {
        _incidentClaimRepository = incidentClaimRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<IncidentClaimDto>> GetAll(GetAllIncidentClaimsInput input)
    {
        input.Normalize();

        var query = _incidentClaimRepository.GetAll()
            .Include(c => c.Currency)
            .Include(c => c.ClaimStatus)
            .WhereIf(input.IncidentId.HasValue, c => c.IncidentId == input.IncidentId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<IncidentClaimDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetIncidentClaimForEditOutput> GetIncidentClaimForEdit(EntityDto input)
    {
        var entity = await _incidentClaimRepository.GetAsync(input.Id);

        return new GetIncidentClaimForEditOutput
        {
            IncidentClaim = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditIncidentClaimDto input)
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

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaims_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _incidentClaimRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaims_Create)]
    protected virtual async Task CreateAsync(CreateOrEditIncidentClaimDto input)
    {
        var entity = MapToEntity(input, new IncidentClaim());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _incidentClaimRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaims_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditIncidentClaimDto input)
    {
        var entity = await _incidentClaimRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _incidentClaimRepository.UpdateAsync(entity);
    }

    private static IncidentClaim MapToEntity(CreateOrEditIncidentClaimDto input, IncidentClaim entity)
    {
        entity.ClaimDate = input.ClaimDate;
        entity.IncidentId = input.IncidentId;
        entity.Amount = input.Amount;
        entity.CurrencyId = input.CurrencyId;
        entity.PaymentDate = input.PaymentDate;
        entity.ClaimStatusId = input.ClaimStatusId;
        entity.Comments = input.Comments;

        return entity;
    }

    private static IncidentClaimDto ToDto(IncidentClaim entity)
    {
        return new IncidentClaimDto
        {
            Id = entity.Id,
            ClaimDate = entity.ClaimDate,
            IncidentId = entity.IncidentId,
            Amount = entity.Amount,
            CurrencyId = entity.CurrencyId,
            CurrencyNameEn = entity.Currency?.NameEn,
            PaymentDate = entity.PaymentDate,
            ClaimStatusId = entity.ClaimStatusId,
            ClaimStatusNameEn = entity.ClaimStatus?.NameEn,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditIncidentClaimDto ToCreateOrEditDto(IncidentClaim entity)
    {
        return new CreateOrEditIncidentClaimDto
        {
            Id = entity.Id,
            ClaimDate = entity.ClaimDate,
            IncidentId = entity.IncidentId,
            Amount = entity.Amount,
            CurrencyId = entity.CurrencyId,
            PaymentDate = entity.PaymentDate,
            ClaimStatusId = entity.ClaimStatusId,
            Comments = entity.Comments
        };
    }
}
