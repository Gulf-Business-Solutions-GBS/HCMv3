using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class RetroReasonLogsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private RetroReasonLogsAppService CreateAppService()
    {
        return CreateService(() => new RetroReasonLogsAppService(FakeRepository.For<RetroReasonLog>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RetroReasonLog()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRetroReasonLogDto
        {
            EmployeeId = employeeId,
            ChangedOn = new DateTime(2026, 1, 1),
            RetroDate = new DateTime(2025, 12, 1),
            EntityObjectName = "SalaryRegister",
            EntityObjectId = 10,
            RetroReason = "Salary correction"
        });

        var result = await service.GetAll(new GetAllRetroReasonLogsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].RetroReason.ShouldBe("Salary correction");
    }

    [Fact]
    public async Task Should_Filter_By_EmployeeId()
    {
        var employeeId1 = CreateEmployee();
        var employeeId2 = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRetroReasonLogDto
        {
            EmployeeId = employeeId1,
            ChangedOn = DateTime.Now,
            RetroDate = DateTime.Now
        });
        await service.CreateOrEdit(new CreateOrEditRetroReasonLogDto
        {
            EmployeeId = employeeId2,
            ChangedOn = DateTime.Now,
            RetroDate = DateTime.Now
        });

        var result = await service.GetAll(new GetAllRetroReasonLogsInput { EmployeeId = employeeId2 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId2);
    }

    [Fact]
    public async Task Should_Delete_RetroReasonLog()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRetroReasonLogDto
        {
            EmployeeId = employeeId,
            ChangedOn = DateTime.Now,
            RetroDate = DateTime.Now
        });
        var created = await service.GetAll(new GetAllRetroReasonLogsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllRetroReasonLogsInput());
        result.TotalCount.ShouldBe(0);
    }
}
