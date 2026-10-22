using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.MultiTenancy;
using Abp.Organizations;
using HCMv3.Storage;

namespace HCMv3.OrganizationManagement.Services;

/// <summary>
/// Which organization unit's logo a printed report shows, decided the way the legacy report SQL decided it.
/// The logo itself is a file in the tenant file store (<see cref="OrgUnitProfile.LogoPath"/>); nothing here
/// keeps or returns image bytes from the database.
/// </summary>
public interface ICompanyLogoResolver
{
    /// <summary>
    /// The logo of the employee's company: of the employee's own unit and every unit above it, the TOP-MOST one
    /// whose unit type is named <see cref="CompanyLogoResolver.CompanyUnitTypeName"/>. That is legacy
    /// <c>CASE WHEN Company_1.OID IS NOT NULL THEN Company_1.logo WHEN Company_2.OID ... END</c>, where
    /// <c>Company_n</c> is <c>EmployeeOrgAssignment.OrgLevel{n}</c> joined only when its type is 'Company'
    /// and OrgLevel1 is the top of the tree. As in legacy, a company unit with no logo prints no logo: the
    /// walk does not go on to a lower company that has one. Null when there is no such unit or no logo.
    /// </summary>
    Task<string> GetEmployeeCompanyLogoPathAsync(long? organizationUnitId);

    /// <summary>
    /// The logo for the reports whose legacy layout read it from a separate one-row query instead of the
    /// employee's row (<c>SELECT logo FROM OrganizationUnit WHERE &lt;type filter&gt;</c>): the first unit whose
    /// type has the given English name or code, in tree order. Pass one filter; the other may be null.
    /// </summary>
    Task<string> GetFirstUnitLogoPathAsync(string unitTypeNameEn, string unitTypeCode);

    /// <summary><see cref="GetEmployeeCompanyLogoPathAsync"/> for <paramref name="tenantId"/>, read from the file store.</summary>
    Task<byte[]> ReadEmployeeCompanyLogoAsync(int tenantId, long? organizationUnitId);

    /// <summary><see cref="GetFirstUnitLogoPathAsync"/> for <paramref name="tenantId"/>, read from the file store.</summary>
    Task<byte[]> ReadFirstUnitLogoAsync(int tenantId, string unitTypeNameEn, string unitTypeCode);
}

/// <inheritdoc cref="ICompanyLogoResolver"/>
/// <remarks>
/// One resolver for every report, so the 35 report layouts that print a logo cannot drift apart. The report
/// SQL only returns the employee's org unit id; the walk up the tree happens here, using Zero's
/// dot-separated <c>OrganizationUnit.Code</c> (an ancestor's code is a prefix of its descendants' codes).
/// </remarks>
public class CompanyLogoResolver : HCMv3DomainServiceBase, ICompanyLogoResolver
{
    /// <summary>The legacy reports matched the company level by the unit type's English name.</summary>
    public const string CompanyUnitTypeName = "Company";

    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<OrganizationUnitType> _organizationUnitTypeRepository;
    private readonly IEntityImageStore _imageStore;
    private readonly ITenantCache _tenantCache;

    public CompanyLogoResolver(
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<OrganizationUnitType> organizationUnitTypeRepository,
        IEntityImageStore imageStore,
        ITenantCache tenantCache)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
        _imageStore = imageStore;
        _tenantCache = tenantCache;
    }

    [UnitOfWork]
    public virtual Task<string> GetEmployeeCompanyLogoPathAsync(long? organizationUnitId)
    {
        if (organizationUnitId == null)
        {
            return Task.FromResult<string>(null);
        }

        var unit = _organizationUnitRepository.FirstOrDefault(organizationUnitId.Value);
        if (unit == null || string.IsNullOrEmpty(unit.Code))
        {
            return Task.FromResult<string>(null);
        }

        var pathCodes = AncestorOrSelfCodes(unit.Code);

        var companies = (
                from profile in _orgUnitProfileRepository.GetAll()
                join type in _organizationUnitTypeRepository.GetAll() on profile.OrganizationUnitTypeId equals type.Id
                join pathUnit in _organizationUnitRepository.GetAll() on profile.OrganizationUnitId equals pathUnit.Id
                where type.NameEn == CompanyUnitTypeName && pathCodes.Contains(pathUnit.Code)
                select new { pathUnit.Code, profile.LogoPath })
            .ToList();

        // Top-most first: legacy tested OrgLevel1 (the top of the tree) before OrgLevel2 and so on.
        var company = companies.OrderBy(c => c.Code.Length).FirstOrDefault();

        return Task.FromResult(NullIfEmpty(company?.LogoPath));
    }

    [UnitOfWork]
    public virtual Task<string> GetFirstUnitLogoPathAsync(string unitTypeNameEn, string unitTypeCode)
    {
        if (string.IsNullOrEmpty(unitTypeNameEn) && string.IsNullOrEmpty(unitTypeCode))
        {
            return Task.FromResult<string>(null);
        }

        var first = (
                from profile in _orgUnitProfileRepository.GetAll()
                join type in _organizationUnitTypeRepository.GetAll() on profile.OrganizationUnitTypeId equals type.Id
                join unit in _organizationUnitRepository.GetAll() on profile.OrganizationUnitId equals unit.Id
                where (unitTypeNameEn == null || type.NameEn == unitTypeNameEn)
                      && (unitTypeCode == null || type.Code == unitTypeCode)
                select new { unit.Code, unit.Id, profile.LogoPath })
            .ToList()
            // Legacy had no ORDER BY, so "first" was whatever SQL Server returned; tree order makes it stable.
            .OrderBy(u => u.Code, System.StringComparer.Ordinal)
            .ThenBy(u => u.Id)
            .FirstOrDefault();

        return Task.FromResult(NullIfEmpty(first?.LogoPath));
    }

    public virtual Task<byte[]> ReadEmployeeCompanyLogoAsync(int tenantId, long? organizationUnitId) =>
        ReadForTenantAsync(tenantId, () => GetEmployeeCompanyLogoPathAsync(organizationUnitId));

    public virtual Task<byte[]> ReadFirstUnitLogoAsync(int tenantId, string unitTypeNameEn, string unitTypeCode) =>
        ReadForTenantAsync(tenantId, () => GetFirstUnitLogoPathAsync(unitTypeNameEn, unitTypeCode));

    /// <summary>
    /// Resolves the logo path inside <paramref name="tenantId"/>'s data and reads the file. A tenant that does not
    /// exist (removed since the report was opened, or a bad id) has no logo: the report prints without one rather
    /// than failing, because ABP's tenant cache throws for an unknown tenant as soon as a query runs.
    /// </summary>
    private async Task<byte[]> ReadForTenantAsync(int tenantId, System.Func<Task<string>> resolvePath)
    {
        if (_tenantCache.GetOrNull(tenantId) == null)
        {
            return null;
        }

        string path;
        using (var uow = UnitOfWorkManager.Begin())
        using (UnitOfWorkManager.Current.SetTenantId(tenantId))
        {
            path = await resolvePath();
            await uow.CompleteAsync();
        }

        return path == null ? null : await _imageStore.ReadOrNullAsync(tenantId, path);
    }

    /// <summary>"00001.00002.00003" -> "00001", "00001.00002", "00001.00002.00003".</summary>
    public static List<string> AncestorOrSelfCodes(string code)
    {
        var segments = code.Split('.');
        var codes = new List<string>(segments.Length);
        for (var i = 1; i <= segments.Length; i++)
        {
            codes.Add(string.Join(".", segments, 0, i));
        }

        return codes;
    }

    private static string NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
}
