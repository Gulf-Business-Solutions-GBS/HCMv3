using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class PrintReportPermissionDetailsAppService_Tests : ReportManagementAppServiceTestBase
{
    private PrintReportPermissionDetailsAppService CreateAppService()
    {
        return CreateService(() => new PrintReportPermissionDetailsAppService(FakeRepository.For<PrintReportPermissionDetail>(Context)));
    }

    private int CreatePermission(string roleName)
    {
        var roleId = CreateRole(roleName);
        var entity = new PrintReportPermission
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = roleName,
            RoleId = roleId
        };
        Context.PrintReportPermissions.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreatePrintReport(string nameEn)
    {
        var entity = new PrintReport
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.PrintReports.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Detail()
    {
        var permissionId = CreatePermission("PermRole1");
        var printReportId = CreatePrintReport("ReportX");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPrintReportPermissionDetailDto
        {
            PrintReportPermissionId = permissionId,
            PrintReportId = printReportId
        });

        var result = await service.GetAll(new GetAllPrintReportPermissionDetailsInput { PrintReportPermissionId = permissionId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].PrintReportName.ShouldBe("ReportX");
    }

    [Fact]
    public async Task Should_Throw_When_Report_Already_Assigned()
    {
        var permissionId = CreatePermission("PermRole2");
        var printReportId = CreatePrintReport("ReportY");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPrintReportPermissionDetailDto
        {
            PrintReportPermissionId = permissionId,
            PrintReportId = printReportId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPrintReportPermissionDetailDto
            {
                PrintReportPermissionId = permissionId,
                PrintReportId = printReportId
            }));
    }
}
