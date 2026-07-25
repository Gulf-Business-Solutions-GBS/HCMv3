using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class PrintReportPermissionsAppService_Tests : ReportManagementAppServiceTestBase
{
    private PrintReportPermissionsAppService CreateAppService()
    {
        return CreateService(() => new PrintReportPermissionsAppService(FakeRepository.For<PrintReportPermission>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PrintReportPermission()
    {
        var roleId = CreateRole("ReportAdmin1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPrintReportPermissionDto { RoleId = roleId });

        var result = await service.GetAll(new GetAllPrintReportPermissionsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].RoleId.ShouldBe(roleId);
        result.Items[0].RoleName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_Role_Already_Assigned()
    {
        var roleId = CreateRole("ReportAdmin2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPrintReportPermissionDto { RoleId = roleId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPrintReportPermissionDto { RoleId = roleId }));
    }
}
