using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.ReportManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
[Xunit.Collection(HCMv3.Tests.PortedReportCatalogCollection.Name)]
public class PrintReportLauncherAppService_Tests : ReportManagementAppServiceTestBase
{
    /// <summary>
    /// Test double that drives the two seams the real service reads from ASP.NET Zero's concrete
    /// UserManager / AbpUserRole table, neither of which exists in this bespoke Sqlite context.
    /// </summary>
    private class TestablePrintReportLauncherAppService : PrintReportLauncherAppService
    {
        public bool Unrestricted { get; set; }

        public List<int> RoleIds { get; set; } = new List<int>();

        public TestablePrintReportLauncherAppService(
            IRepository<PrintReport> printReports,
            IRepository<PrintReportPermission> permissions,
            IRepository<PrintReportPermissionDetail> permissionDetails)
            : base(printReports, permissions, permissionDetails, null)
        {
        }

        protected override Task<bool> IsCurrentUserUnrestrictedAsync() => Task.FromResult(Unrestricted);

        protected override Task<List<int>> GetCurrentUserRoleIdsAsync() => Task.FromResult(RoleIds);
    }

    private TestablePrintReportLauncherAppService CreateAppService()
    {
        return CreateService(() => new TestablePrintReportLauncherAppService(
            FakeRepository.For<PrintReport>(Context),
            FakeRepository.For<PrintReportPermission>(Context),
            FakeRepository.For<PrintReportPermissionDetail>(Context)));
    }

    private PrintReport AddReport(string code, string reportName, bool isActive = true)
    {
        var report = new PrintReport
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code + " EN",
            NameAr = code + " AR",
            ReportName = reportName,
            IsActive = isActive,
            ReportCategory = ReportCategory.Default
        };

        Context.PrintReports.Add(report);
        Context.SaveChanges();
        return report;
    }

    [Fact]
    public async Task Should_Return_Only_Active_Reports_For_An_Unrestricted_User()
    {
        AddReport("PR001", "PayslipEn");
        AddReport("PR002", "SalaryCertificateEn");
        AddReport("PR003", "RetiredReport", isActive: false);

        var service = CreateAppService();
        service.Unrestricted = true;

        var result = await service.GetRunnableReports();

        result.Select(r => r.Code).ShouldBe(new[] { "PR001", "PR002" });
    }

    [Fact]
    public async Task Should_Flag_IsAvailable_Only_For_Ported_Report_Classes()
    {
        AddReport("PR001", "PayslipEn");
        AddReport("PR002", "SalaryCertificateEn");

        PortedReportCatalog.SetPortedReportNames(new[] { "PayslipEn" });
        try
        {
            var service = CreateAppService();
            service.Unrestricted = true;

            var result = await service.GetRunnableReports();

            result.Single(r => r.Code == "PR001").IsAvailable.ShouldBeTrue();
            result.Single(r => r.Code == "PR002").IsAvailable.ShouldBeFalse();
        }
        finally
        {
            PortedReportCatalog.SetPortedReportNames(new string[0]);
        }
    }

    [Fact]
    public async Task Should_Expose_ReportCategory_As_Its_Enum_Name()
    {
        var report = AddReport("PR001", "PayslipEn");
        report.ReportCategory = ReportCategory.Custom;
        Context.SaveChanges();

        var service = CreateAppService();
        service.Unrestricted = true;

        var result = await service.GetRunnableReports();

        result.Single().ReportCategory.ShouldBe("Custom");
        result.Single().ReportName.ShouldBe("PayslipEn");
    }

    [Fact]
    public async Task Should_Return_Nothing_For_A_Restricted_User_Without_Grants()
    {
        AddReport("PR001", "PayslipEn");

        var service = CreateAppService();
        service.Unrestricted = false;
        service.RoleIds = new List<int> { 42 };

        var result = await service.GetRunnableReports();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Only_Granted_Reports_For_A_Restricted_User()
    {
        var granted = AddReport("PR001", "PayslipEn");
        AddReport("PR002", "SalaryCertificateEn");

        var roleId = CreateRole("PayrollClerk");

        var permission = new PrintReportPermission
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "PRP001",
            RoleId = roleId
        };
        Context.PrintReportPermissions.Add(permission);
        Context.SaveChanges();

        Context.PrintReportPermissionDetails.Add(new PrintReportPermissionDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            PrintReportPermissionId = permission.Id,
            PrintReportId = granted.Id
        });
        Context.SaveChanges();

        var service = CreateAppService();
        service.Unrestricted = false;
        service.RoleIds = new List<int> { roleId };

        var result = await service.GetRunnableReports();

        result.Single().Code.ShouldBe("PR001");
    }
}
