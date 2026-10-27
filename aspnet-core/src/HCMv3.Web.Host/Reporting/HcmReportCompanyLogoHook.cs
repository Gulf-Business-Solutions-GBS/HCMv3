using System;
using Abp.Dependency;
using Abp.Threading;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Services;
using HCMv3.Reports;

namespace HCMv3.Web.Reporting;

/// <summary>
/// Gives the printed reports their company logo. The reports project references only DevExpress, so it is
/// handed <see cref="HcmReportBase.CompanyLogoLoader"/>, which maps each <see cref="CompanyLogoSource"/> to
/// <see cref="ICompanyLogoResolver"/>. The logo is read from the tenant file store, never from the database.
/// </summary>
public static class HcmReportCompanyLogoHook
{
    public static void Install(IIocResolver iocResolver)
    {
        HcmReportBase.CompanyLogoLoader = (tenantId, source, organizationUnitId) => Load(iocResolver, tenantId, source, organizationUnitId);
    }

    public static byte[] Load(IIocResolver iocResolver, int tenantId, CompanyLogoSource source, long? organizationUnitId)
    {
        using var resolver = iocResolver.ResolveAsDisposable<ICompanyLogoResolver>();

        return source switch
        {
            CompanyLogoSource.EmployeeCompany => AsyncHelper.RunSync(() =>
                resolver.Object.ReadEmployeeCompanyLogoAsync(tenantId, organizationUnitId)),
            CompanyLogoSource.FirstCompanyUnit => AsyncHelper.RunSync(() =>
                resolver.Object.ReadFirstUnitLogoAsync(tenantId, CompanyLogoResolver.CompanyUnitTypeName, null)),
            CompanyLogoSource.FirstLevel1Unit => AsyncHelper.RunSync(() =>
                resolver.Object.ReadFirstUnitLogoAsync(tenantId, null, OrganizationUnitType.Level1Code)),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
