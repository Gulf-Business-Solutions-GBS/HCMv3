using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class BulkUploadLogsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private BulkUploadLogsAppService CreateAppService()
    {
        return CreateService(() => new BulkUploadLogsAppService(FakeRepository.For<BulkUploadLog>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_BulkUploadLog()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditBulkUploadLogDto
        {
            LogType = BulkUploadLogType.ProfilePhoto,
            Message = "Uploaded 10 photos",
            RunTimeStamp = new DateTime(2026, 1, 1),
            EmployeeId = employeeId,
            InPeriod = new DateTime(2026, 1, 1),
            NoOfEmployee = 10
        });

        var result = await service.GetAll(new GetAllBulkUploadLogsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].LogType.ShouldBe(BulkUploadLogType.ProfilePhoto);
        result.Items[0].NoOfEmployee.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Filter_By_LogType()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditBulkUploadLogDto
        {
            LogType = BulkUploadLogType.ProfilePhoto,
            RunTimeStamp = DateTime.Now,
            EmployeeId = employeeId,
            InPeriod = DateTime.Now
        });

        var result = await service.GetAll(new GetAllBulkUploadLogsInput { LogType = BulkUploadLogType.ProfilePhoto });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Delete_BulkUploadLog()
    {
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditBulkUploadLogDto
        {
            LogType = BulkUploadLogType.ProfilePhoto,
            RunTimeStamp = DateTime.Now,
            EmployeeId = employeeId,
            InPeriod = DateTime.Now
        });
        var created = await service.GetAll(new GetAllBulkUploadLogsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllBulkUploadLogsInput());
        result.TotalCount.ShouldBe(0);
    }
}
