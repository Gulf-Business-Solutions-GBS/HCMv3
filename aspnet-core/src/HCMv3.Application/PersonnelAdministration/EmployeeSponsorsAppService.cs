using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GovtRelationsManagement;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeSponsor"/> (ported from legacy XAF EmployeeSponsor business
/// object, Wave 3 deferred bucket).
///
/// CROSS-MODULE QUOTA ENFORCEMENT: unlike every other ledger entity in Waves 1-3, this legacy entity's
/// quota-balance cascade genuinely crosses module boundaries (PersonnelAdministration -&gt;
/// GovtRelationsManagement), so it IS ported here (simplified vs. the legacy's full "only when the
/// assignment's Start/EndDate window covers today, tracked via prev-value fields on Update" logic):
/// <list type="bullet">
/// <item><description>On <see cref="CreateAsync"/>: after inserting the assignment, the matching
/// <see cref="SponsorJobTitle"/> row (by SponsorId+GovtJobTitleId) has its
/// <see cref="SponsorJobTitle.CitizenQuotaUsed"/> or <see cref="SponsorJobTitle.NonCitizenQuotaUsed"/>
/// incremented by 1, based on whether the assigned employee's Nationality's NationalityGroup is "Citizen"
/// (case-sensitive NameEn match, same as legacy) or not.</description></item>
/// <item><description>On <see cref="Delete"/>: the same counter is decremented by 1, floored at 0 (the
/// legacy has no floor -- this port adds one defensively, per instruction, to avoid an inconsistent
/// negative Used count if delete is called out of order).</description></item>
/// <item><description>Update does NOT re-run the increment/decrement cascade (simplified vs. legacy, which
/// re-evaluates prev-vs-new Sponsor/GovtJobTitle/date-window on every save) -- only Create/Delete adjust the
/// quota counters, per instruction.</description></item>
/// </list>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSponsors)]
public class EmployeeSponsorsAppService : HCMv3AppServiceBase, IEmployeeSponsorsAppService
{
    private readonly IRepository<EmployeeSponsor> _repository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<SponsorJobTitle> _sponsorJobTitleRepository;

    public EmployeeSponsorsAppService(
        IRepository<EmployeeSponsor> repository,
        IRepository<Employee> employeeRepository,
        IRepository<SponsorJobTitle> sponsorJobTitleRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _sponsorJobTitleRepository = sponsorJobTitleRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSponsorDto>> GetAll(GetAllEmployeeSponsorsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(s => s.Employee)
            .Include(s => s.Sponsor)
            .Include(s => s.GovtJobTitle)
            .Include(s => s.SalaryCurrency)
            .Include(s => s.WorkVisaType)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSponsorDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSponsorForEditOutput> GetEmployeeSponsorForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeSponsorForEditOutput
        {
            EmployeeSponsor = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSponsorDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSponsors_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        await AdjustSponsorJobTitleQuotaAsync(entity, increment: false);

        await _repository.DeleteAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSponsors_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSponsorDto input)
    {
        var entity = MapToEntity(input, new EmployeeSponsor());
        entity.TenantId = AbpSession.TenantId ?? 0;

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        await _repository.InsertAsync(entity);

        await AdjustSponsorJobTitleQuotaAsync(entity, increment: true);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSponsors_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSponsorDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Increments (Create) or decrements (Delete) the matching <see cref="SponsorJobTitle"/> row's
    /// Citizen/NonCitizen Used counter by 1, based on the assigned employee's Nationality's
    /// NationalityGroup. Decrements are floored at 0.
    /// </summary>
    private async Task AdjustSponsorJobTitleQuotaAsync(EmployeeSponsor entity, bool increment)
    {
        var sponsorJobTitle = await _sponsorJobTitleRepository.GetAll()
            .FirstOrDefaultAsync(t => t.SponsorId == entity.SponsorId && t.GovtJobTitleId == entity.GovtJobTitleId);
        if (sponsorJobTitle == null)
        {
            throw new UserFriendlyException(L("NoSponsorJobTitleQuotaFound", entity.SponsorId, entity.GovtJobTitleId));
        }

        var employee = await _employeeRepository.GetAll()
            .Include(e => e.Nationality)
            .ThenInclude(n => n.NationalityGroup)
            .FirstOrDefaultAsync(e => e.Id == entity.EmployeeId);

        var isCitizen = employee?.Nationality?.NationalityGroup?.NameEn == "Citizen";
        var delta = increment ? 1 : -1;

        if (isCitizen)
        {
            sponsorJobTitle.CitizenQuotaUsed = System.Math.Max(0, sponsorJobTitle.CitizenQuotaUsed + delta);
        }
        else
        {
            sponsorJobTitle.NonCitizenQuotaUsed = System.Math.Max(0, sponsorJobTitle.NonCitizenQuotaUsed + delta);
        }

        await _sponsorJobTitleRepository.UpdateAsync(sponsorJobTitle);
    }

    private static EmployeeSponsor MapToEntity(CreateOrEditEmployeeSponsorDto input, EmployeeSponsor entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.SponsorId = input.SponsorId;
        entity.GovtJobTitleId = input.GovtJobTitleId;
        entity.EmpSponsorRelation = input.EmpSponsorRelation;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.ContractSalary = input.ContractSalary;
        entity.SalaryCurrencyId = input.SalaryCurrencyId;
        entity.ResidencyType = input.ResidencyType;
        entity.WorkVisaTypeId = input.WorkVisaTypeId;
        return entity;
    }

    private static EmployeeSponsorDto ToDto(EmployeeSponsor entity)
    {
        return new EmployeeSponsorDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            SponsorId = entity.SponsorId,
            SponsorNameEn = entity.Sponsor?.NameEn,
            GovtJobTitleId = entity.GovtJobTitleId,
            GovtJobTitleNameEn = entity.GovtJobTitle?.NameEn,
            EmpSponsorRelation = entity.EmpSponsorRelation,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ContractSalary = entity.ContractSalary,
            SalaryCurrencyId = entity.SalaryCurrencyId,
            SalaryCurrencyNameEn = entity.SalaryCurrency?.NameEn,
            ResidencyType = entity.ResidencyType,
            WorkVisaTypeId = entity.WorkVisaTypeId,
            WorkVisaTypeNameEn = entity.WorkVisaType?.NameEn
        };
    }

    private static CreateOrEditEmployeeSponsorDto ToCreateOrEditDto(EmployeeSponsor entity)
    {
        return new CreateOrEditEmployeeSponsorDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            SponsorId = entity.SponsorId,
            GovtJobTitleId = entity.GovtJobTitleId,
            EmpSponsorRelation = entity.EmpSponsorRelation,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ContractSalary = entity.ContractSalary,
            SalaryCurrencyId = entity.SalaryCurrencyId,
            ResidencyType = entity.ResidencyType,
            WorkVisaTypeId = entity.WorkVisaTypeId
        };
    }
}
