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
/// Application service for <see cref="EmployeeCostDistribution"/> (ported from legacy XAF
/// EmployeeCostDistribution business object, Wave 4 deferred bucket). The legacy <c>PerformValidation</c>
/// call is commented out/dead code in the legacy source, so no additional cross-record validation is
/// ported here (matches legacy active behavior).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeCostDistributions)]
public class EmployeeCostDistributionsAppService : HCMv3AppServiceBase, IEmployeeCostDistributionsAppService
{
    private readonly IRepository<EmployeeCostDistribution> _repository;

    public EmployeeCostDistributionsAppService(IRepository<EmployeeCostDistribution> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeCostDistributionDto>> GetAll(GetAllEmployeeCostDistributionsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeCostDistributionDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeCostDistributionForEditOutput> GetEmployeeCostDistributionForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeCostDistributionForEditOutput
        {
            EmployeeCostDistribution = new CreateOrEditEmployeeCostDistributionDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                OrganizationUnitId = entity.OrganizationUnitId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                CostCount = entity.CostCount,
                UsedDays = entity.UsedDays
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeCostDistributionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeCostDistributions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeCostDistributionDto input)
    {
        var entity = MapToEntity(input, new EmployeeCostDistribution());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeCostDistributions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeCostDistributionDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeCostDistributions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeCostDistribution MapToEntity(CreateOrEditEmployeeCostDistributionDto input, EmployeeCostDistribution entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.CostCount = input.CostCount;
        entity.UsedDays = input.UsedDays;
        return entity;
    }

    private static EmployeeCostDistributionDto ToDto(EmployeeCostDistribution entity)
    {
        return new EmployeeCostDistributionDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            OrganizationUnitId = entity.OrganizationUnitId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            CostCount = entity.CostCount,
            UsedDays = entity.UsedDays
        };
    }
}
