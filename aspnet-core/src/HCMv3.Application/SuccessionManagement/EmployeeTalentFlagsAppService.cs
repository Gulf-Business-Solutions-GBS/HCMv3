using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.SuccessionManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// Application service for <see cref="EmployeeTalentFlag"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.EmployeeTalentFlag</c>). The legacy
/// StartDate/EndDate overlap check was already commented out (dead code) in the source XAF business object
/// -- not reproduced here, only the required-field validation (via DTO Data Annotations) is.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmployeeTalentFlags)]
public class EmployeeTalentFlagsAppService : HCMv3AppServiceBase, IEmployeeTalentFlagsAppService
{
    private readonly IRepository<EmployeeTalentFlag> _repository;

    public EmployeeTalentFlagsAppService(IRepository<EmployeeTalentFlag> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTalentFlagDto>> GetAll(GetAllEmployeeTalentFlagsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.RiskOfLoss)
            .Include(e => e.ImpactOfLoss)
            .Include(e => e.ReasonForLeaving)
            .Include(e => e.TalentPool)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTalentFlagDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTalentFlagForEditOutput> GetEmployeeTalentFlagForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeTalentFlagForEditOutput
        {
            EmployeeTalentFlag = new CreateOrEditEmployeeTalentFlagDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                RiskOfLossId = entity.RiskOfLossId,
                ImpactOfLossId = entity.ImpactOfLossId,
                ReasonForLeavingId = entity.ReasonForLeavingId,
                TalentPoolId = entity.TalentPoolId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeTalentFlagDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmployeeTalentFlags_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTalentFlagDto input)
    {
        var entity = MapToEntity(input, new EmployeeTalentFlag());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmployeeTalentFlags_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTalentFlagDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmployeeTalentFlags_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeTalentFlag MapToEntity(CreateOrEditEmployeeTalentFlagDto input, EmployeeTalentFlag entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.RiskOfLossId = input.RiskOfLossId;
        entity.ImpactOfLossId = input.ImpactOfLossId;
        entity.ReasonForLeavingId = input.ReasonForLeavingId;
        entity.TalentPoolId = input.TalentPoolId;
        return entity;
    }

    private static EmployeeTalentFlagDto ToDto(EmployeeTalentFlag entity)
    {
        return new EmployeeTalentFlagDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            RiskOfLossId = entity.RiskOfLossId,
            RiskOfLossNameEn = entity.RiskOfLoss?.NameEn,
            ImpactOfLossId = entity.ImpactOfLossId,
            ImpactOfLossNameEn = entity.ImpactOfLoss?.NameEn,
            ReasonForLeavingId = entity.ReasonForLeavingId,
            ReasonForLeavingNameEn = entity.ReasonForLeaving?.NameEn,
            TalentPoolId = entity.TalentPoolId,
            TalentPoolNameEn = entity.TalentPool?.NameEn
        };
    }
}
