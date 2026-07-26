using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class SysObjectsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private SysObjectsAppService CreateAppService()
    {
        return CreateService(() => new SysObjectsAppService(FakeRepository.For<SysObject>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SysObject()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysObjectDto
        {
            Name = "PayrollRun",
            FullName = "HCMv3.PayrollManagement.PayrollRun",
            IsPayrollSensitive = true,
            RespectPermission = true
        });

        var result = await service.GetAll(new GetAllSysObjectsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("PayrollRun");
        result.Items[0].IsPayrollSensitive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_Name_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysObjectDto { Name = "PayrollRun" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSysObjectDto { Name = "PayrollRun" }));
    }

    [Fact]
    public async Task Should_Update_SysObject()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysObjectDto { Name = "PayrollRun" });
        var created = await service.GetAll(new GetAllSysObjectsInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditSysObjectDto { Id = id, Name = "PayrollRun", MustRetroRun = true });

        var edited = await service.GetSysObjectForEdit(new EntityDto(id));
        edited.SysObject.MustRetroRun.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Delete_SysObject()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysObjectDto { Name = "PayrollRun" });
        var created = await service.GetAll(new GetAllSysObjectsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllSysObjectsInput());
        result.TotalCount.ShouldBe(0);
    }
}
