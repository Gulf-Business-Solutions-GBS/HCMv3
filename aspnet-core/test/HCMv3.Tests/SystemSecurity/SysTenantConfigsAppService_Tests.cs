using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class SysTenantConfigsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private SysTenantConfigsAppService CreateAppService()
    {
        return CreateService(() => new SysTenantConfigsAppService(FakeRepository.For<SysTenantConfig>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SysTenantConfig()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysTenantConfigDto
        {
            Key = "MaxLeaveCarryForwardDays",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            KeyValue = "15",
            ConfigCategory = TenantConfigCategory.Employee
        });

        var result = await service.GetAll(new GetAllSysTenantConfigsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Key.ShouldBe("MaxLeaveCarryForwardDays");
        result.Items[0].ConfigCategory.ShouldBe(TenantConfigCategory.Employee);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSysTenantConfigDto
            {
                Key = "K1",
                StartDate = new DateTime(2026, 1, 10),
                EndDate = new DateTime(2026, 1, 1),
                ConfigCategory = TenantConfigCategory.Payroll
            }));
    }

    [Fact]
    public async Task Should_Filter_By_ConfigCategory()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysTenantConfigDto
        {
            Key = "K1",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 2),
            ConfigCategory = TenantConfigCategory.Payroll
        });
        await service.CreateOrEdit(new CreateOrEditSysTenantConfigDto
        {
            Key = "K2",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 2),
            ConfigCategory = TenantConfigCategory.Report
        });

        var result = await service.GetAll(new GetAllSysTenantConfigsInput { ConfigCategory = TenantConfigCategory.Report });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Key.ShouldBe("K2");
    }

    [Fact]
    public async Task Should_Delete_SysTenantConfig()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSysTenantConfigDto
        {
            Key = "K1",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 2),
            ConfigCategory = TenantConfigCategory.Payroll
        });
        var created = await service.GetAll(new GetAllSysTenantConfigsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllSysTenantConfigsInput());
        result.TotalCount.ShouldBe(0);
    }
}
