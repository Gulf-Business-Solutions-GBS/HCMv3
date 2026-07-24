using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Organizations;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.OrganizationManagement.Dto;
using HCMv3.PayrollManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Combined app service for the HR org-unit screen: wraps ASP.NET Zero's own <c>OrganizationUnitManager</c>
/// (tree structure, hierarchical Code, depth cap) together with the <see cref="OrgUnitProfile"/>
/// 1:1 extension row (business Code, Arabic name, <see cref="OrganizationUnitType"/> classification,
/// Address, GeoLocation). See <see cref="OrganizationUnitDepthHelper"/> for the shared depth-cap logic
/// also enforced by Zero's own built-in org-unit admin screen.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrgUnits)]
public class OrgUnitsAppService : HCMv3AppServiceBase, IOrgUnitsAppService
{
    private readonly OrganizationUnitManager _organizationUnitManager;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<OrgUnitProfile> _profileRepository;
    private readonly IRepository<OrganizationUnitType> _organizationUnitTypeRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;

    public OrgUnitsAppService(
        OrganizationUnitManager organizationUnitManager,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<OrgUnitProfile> profileRepository,
        IRepository<OrganizationUnitType> organizationUnitTypeRepository,
        IRepository<PayrollArea> payrollAreaRepository)
    {
        _organizationUnitManager = organizationUnitManager;
        _organizationUnitRepository = organizationUnitRepository;
        _profileRepository = profileRepository;
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
        _payrollAreaRepository = payrollAreaRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OrgUnitDto>> GetAll(GetAllOrgUnitsInput input)
    {
        input.Normalize();

        var query = (from ou in _organizationUnitRepository.GetAll()
                     join p in _profileRepository.GetAll() on ou.Id equals p.OrganizationUnitId into profiles
                     from p in profiles.DefaultIfEmpty()
                     select new { ou, p })
                     .WhereIf(!input.Filter.IsNullOrWhiteSpace(), x =>
                         x.ou.DisplayName.Contains(input.Filter) ||
                         (x.p != null && x.p.NameAr.Contains(input.Filter)) ||
                         (x.p != null && x.p.Code.Contains(input.Filter)));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy("ou." + input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var organizationUnitTypes = await _organizationUnitTypeRepository.GetAllListAsync();
        var payrollAreas = await _payrollAreaRepository.GetAllListAsync();
        var parentIds = items.Where(i => i.ou.ParentId.HasValue).Select(i => i.ou.ParentId.Value).Distinct().ToList();
        var parents = parentIds.Count > 0
            ? await _organizationUnitRepository.GetAll().Where(x => parentIds.Contains(x.Id)).ToListAsync()
            : new System.Collections.Generic.List<OrganizationUnit>();

        return new PagedResultDto<OrgUnitDto>(
            totalCount,
            items.Select(item => ToDto(item.ou, item.p, organizationUnitTypes, parents, payrollAreas)).ToList());
    }

    public async Task<GetOrgUnitForEditOutput> GetOrgUnitForEdit(EntityDto<long> input)
    {
        var ou = await _organizationUnitRepository.GetAsync(input.Id);
        var profile = await _profileRepository.FirstOrDefaultAsync(p => p.OrganizationUnitId == input.Id);

        return new GetOrgUnitForEditOutput
        {
            OrgUnit = new CreateOrEditOrgUnitDto
            {
                Id = ou.Id,
                NameEn = ou.DisplayName,
                NameAr = profile?.NameAr,
                Code = profile?.Code,
                ParentId = ou.ParentId,
                OrganizationUnitTypeId = profile?.OrganizationUnitTypeId ?? 0,
                Address = profile?.Address,
                OrgGeoLocation = profile?.OrgGeoLocation,
                PayrollAreaId = profile?.PayrollAreaId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditOrgUnitDto input)
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

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrgUnits_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOrgUnitDto input)
    {
        var resultingDepth = await OrganizationUnitDepthHelper.ComputeResultingDepthAsync(
            _organizationUnitRepository, input.ParentId);
        if (resultingDepth > OrganizationUnitDepthHelper.MaxDepth)
        {
            throw new UserFriendlyException(L("OrganizationUnitMaxDepthExceeded", OrganizationUnitDepthHelper.MaxDepth));
        }

        var organizationUnit = new OrganizationUnit(AbpSession.TenantId, input.NameEn, input.ParentId);
        await _organizationUnitManager.CreateAsync(organizationUnit);
        await CurrentUnitOfWork.SaveChangesAsync();

        var profile = new OrgUnitProfile
        {
            OrganizationUnitId = organizationUnit.Id,
            Code = input.Code,
            NameAr = input.NameAr,
            OrganizationUnitTypeId = input.OrganizationUnitTypeId,
            Address = input.Address,
            OrgGeoLocation = input.OrgGeoLocation,
            PayrollAreaId = input.PayrollAreaId,
            TenantId = AbpSession.TenantId ?? 0
        };
        await _profileRepository.InsertAsync(profile);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrgUnits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOrgUnitDto input)
    {
        var organizationUnit = await _organizationUnitRepository.GetAsync(input.Id.Value);

        if (organizationUnit.ParentId != input.ParentId)
        {
            var resultingDepth = await OrganizationUnitDepthHelper.ComputeResultingDepthAsync(
                _organizationUnitRepository, input.ParentId, organizationUnit.Id);
            if (resultingDepth > OrganizationUnitDepthHelper.MaxDepth)
            {
                throw new UserFriendlyException(L("OrganizationUnitMaxDepthExceeded", OrganizationUnitDepthHelper.MaxDepth));
            }

            await _organizationUnitManager.MoveAsync(organizationUnit.Id, input.ParentId);
        }

        organizationUnit.DisplayName = input.NameEn;
        await _organizationUnitManager.UpdateAsync(organizationUnit);

        var profile = await _profileRepository.FirstOrDefaultAsync(p => p.OrganizationUnitId == organizationUnit.Id);
        if (profile == null)
        {
            profile = new OrgUnitProfile
            {
                OrganizationUnitId = organizationUnit.Id,
                TenantId = AbpSession.TenantId ?? 0
            };
            await _profileRepository.InsertAsync(profile);
        }

        profile.Code = input.Code;
        profile.NameAr = input.NameAr;
        profile.OrganizationUnitTypeId = input.OrganizationUnitTypeId;
        profile.Address = input.Address;
        profile.OrgGeoLocation = input.OrgGeoLocation;
        profile.PayrollAreaId = input.PayrollAreaId;
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_OrgUnits_Delete)]
    public async Task Delete(EntityDto<long> input)
    {
        await _profileRepository.DeleteAsync(p => p.OrganizationUnitId == input.Id);
        await _organizationUnitManager.DeleteAsync(input.Id);
    }

    private static OrgUnitDto ToDto(
        OrganizationUnit ou,
        OrgUnitProfile profile,
        System.Collections.Generic.List<OrganizationUnitType> organizationUnitTypes,
        System.Collections.Generic.List<OrganizationUnit> parents,
        System.Collections.Generic.List<PayrollArea> payrollAreas)
    {
        var organizationUnitType = profile != null
            ? organizationUnitTypes.FirstOrDefault(t => t.Id == profile.OrganizationUnitTypeId)
            : null;
        var parent = ou.ParentId.HasValue
            ? parents.FirstOrDefault(p => p.Id == ou.ParentId.Value)
            : null;
        var payrollArea = profile?.PayrollAreaId != null
            ? payrollAreas.FirstOrDefault(a => a.Id == profile.PayrollAreaId.Value)
            : null;

        return new OrgUnitDto
        {
            Id = ou.Id,
            TreeCode = ou.Code,
            NameEn = ou.DisplayName,
            NameAr = profile?.NameAr,
            Code = profile?.Code,
            ParentId = ou.ParentId,
            ParentNameEn = parent?.DisplayName,
            OrganizationUnitTypeId = profile?.OrganizationUnitTypeId ?? 0,
            OrganizationUnitTypeName = organizationUnitType?.NameEn,
            Address = profile?.Address,
            OrgGeoLocation = profile?.OrgGeoLocation,
            PayrollAreaId = profile?.PayrollAreaId,
            PayrollAreaName = payrollArea?.Name
        };
    }
}
