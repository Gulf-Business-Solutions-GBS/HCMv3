using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class PunchNotificationLogsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private PunchNotificationLogsAppService CreateAppService()
    {
        return CreateService(() => new PunchNotificationLogsAppService(FakeRepository.For<PunchNotificationLog>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PunchNotificationLog()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPunchNotificationLogDto
        {
            EmployeeDutyPeriodId = 1,
            Type = "MissedPunch",
            Date = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllPunchNotificationLogsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Type.ShouldBe("MissedPunch");
        result.Items[0].EmailNotificationLogId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Filter_By_EmployeeDutyPeriodId()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPunchNotificationLogDto { EmployeeDutyPeriodId = 1, Date = DateTime.Now });
        await service.CreateOrEdit(new CreateOrEditPunchNotificationLogDto { EmployeeDutyPeriodId = 2, Date = DateTime.Now });

        var result = await service.GetAll(new GetAllPunchNotificationLogsInput { EmployeeDutyPeriodId = 2 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeDutyPeriodId.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Delete_PunchNotificationLog()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPunchNotificationLogDto { EmployeeDutyPeriodId = 1, Date = DateTime.Now });
        var created = await service.GetAll(new GetAllPunchNotificationLogsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllPunchNotificationLogsInput());
        result.TotalCount.ShouldBe(0);
    }
}
