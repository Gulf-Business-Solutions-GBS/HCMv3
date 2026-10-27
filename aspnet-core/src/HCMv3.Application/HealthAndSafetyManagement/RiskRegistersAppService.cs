using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.HealthAndSafetyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Application service for the health and safety risk register (ported from legacy XAF RiskRegister
/// business object). Legacy <c>PerformValidation</c> required RiskType, Impact, Possibility, ReportedBy
/// and Location -- ported as-is via DataAnnotations on <see cref="CreateOrEditRiskRegisterDto"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_RiskRegisters)]
public class RiskRegistersAppService : HCMv3AppServiceBase, IRiskRegistersAppService
{
    private readonly IRepository<RiskRegister> _riskRegisterRepository;

    public RiskRegistersAppService(IRepository<RiskRegister> riskRegisterRepository)
    {
        _riskRegisterRepository = riskRegisterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RiskRegisterDto>> GetAll(GetAllRiskRegistersInput input)
    {
        input.Normalize();

        var query = _riskRegisterRepository.GetAll()
            .Include(r => r.RiskType)
            .Include(r => r.Impact)
            .Include(r => r.Possibility)
            .Include(r => r.ReportedBy)
            .Include(r => r.Location)
            .Include(r => r.AssignedTo)
            .Include(r => r.Action)
            .Include(r => r.Status)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r => r.Description.Contains(input.Filter))
            .WhereIf(input.RiskTypeId.HasValue, r => r.RiskTypeId == input.RiskTypeId)
            .WhereIf(input.StatusId.HasValue, r => r.StatusId == input.StatusId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RiskRegisterDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRiskRegisterForEditOutput> GetRiskRegisterForEdit(EntityDto input)
    {
        var entity = await _riskRegisterRepository.GetAsync(input.Id);

        return new GetRiskRegisterForEditOutput
        {
            RiskRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRiskRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_RiskRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _riskRegisterRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_RiskRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRiskRegisterDto input)
    {
        var entity = MapToEntity(input, new RiskRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _riskRegisterRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_RiskRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRiskRegisterDto input)
    {
        var entity = await _riskRegisterRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _riskRegisterRepository.UpdateAsync(entity);
    }

    private static RiskRegister MapToEntity(CreateOrEditRiskRegisterDto input, RiskRegister entity)
    {
        entity.ReportingDate = input.ReportingDate;
        entity.ReportingTime = input.ReportingTime;
        entity.RiskTypeId = input.RiskTypeId;
        entity.Description = input.Description;
        entity.ImpactId = input.ImpactId;
        entity.PossibilityId = input.PossibilityId;
        entity.ReportedById = input.ReportedById;
        entity.LocationId = input.LocationId;
        entity.AssignedToId = input.AssignedToId;
        entity.RiskMitigation = input.RiskMitigation;
        entity.ActionId = input.ActionId;
        entity.StatusId = input.StatusId;
        entity.ReassessmentDate = input.ReassessmentDate;

        return entity;
    }

    private static RiskRegisterDto ToDto(RiskRegister entity)
    {
        return new RiskRegisterDto
        {
            Id = entity.Id,
            ReportingDate = entity.ReportingDate,
            ReportingTime = entity.ReportingTime,
            RiskTypeId = entity.RiskTypeId,
            RiskTypeNameEn = entity.RiskType?.NameEn,
            Description = entity.Description,
            ImpactId = entity.ImpactId,
            ImpactNameEn = entity.Impact?.NameEn,
            PossibilityId = entity.PossibilityId,
            PossibilityNameEn = entity.Possibility?.NameEn,
            ReportedById = entity.ReportedById,
            ReportedByCode = entity.ReportedBy?.Code,
            LocationId = entity.LocationId,
            LocationName = entity.Location?.NameEn,
            AssignedToId = entity.AssignedToId,
            AssignedToCode = entity.AssignedTo?.Code,
            RiskMitigation = entity.RiskMitigation,
            ActionId = entity.ActionId,
            ActionNameEn = entity.Action?.NameEn,
            StatusId = entity.StatusId,
            StatusNameEn = entity.Status?.NameEn,
            ReassessmentDate = entity.ReassessmentDate
        };
    }

    private static CreateOrEditRiskRegisterDto ToCreateOrEditDto(RiskRegister entity)
    {
        return new CreateOrEditRiskRegisterDto
        {
            Id = entity.Id,
            ReportingDate = entity.ReportingDate,
            ReportingTime = entity.ReportingTime,
            RiskTypeId = entity.RiskTypeId,
            Description = entity.Description,
            ImpactId = entity.ImpactId,
            PossibilityId = entity.PossibilityId,
            ReportedById = entity.ReportedById,
            LocationId = entity.LocationId,
            AssignedToId = entity.AssignedToId,
            RiskMitigation = entity.RiskMitigation,
            ActionId = entity.ActionId,
            StatusId = entity.StatusId,
            ReassessmentDate = entity.ReassessmentDate
        };
    }
}
