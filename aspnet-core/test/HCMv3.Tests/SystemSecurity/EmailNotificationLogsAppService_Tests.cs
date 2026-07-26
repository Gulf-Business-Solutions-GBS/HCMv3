using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class EmailNotificationLogsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private EmailNotificationLogsAppService CreateAppService()
    {
        return CreateService(() => new EmailNotificationLogsAppService(FakeRepository.For<EmailNotificationLog>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmailNotificationLog()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailNotificationLogDto
        {
            Date = new DateTime(2026, 1, 1),
            Email = "user@example.com",
            Source = "PayslipEngine",
            Subject = "Your payslip",
            IsSuccessful = true
        });

        var result = await service.GetAll(new GetAllEmailNotificationLogsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Email.ShouldBe("user@example.com");
        result.Items[0].IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Update_EmailNotificationLog()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailNotificationLogDto { Date = DateTime.Now, Email = "a@a.com" });
        var created = await service.GetAll(new GetAllEmailNotificationLogsInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditEmailNotificationLogDto
        {
            Id = id,
            Date = DateTime.Now,
            Email = "a@a.com",
            IsSuccessful = false,
            Exception = "SMTP timeout"
        });

        var edited = await service.GetEmailNotificationLogForEdit(new EntityDto(id));
        edited.EmailNotificationLog.Exception.ShouldBe("SMTP timeout");
    }

    [Fact]
    public async Task Should_Delete_EmailNotificationLog()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailNotificationLogDto { Date = DateTime.Now, Email = "a@a.com" });
        var created = await service.GetAll(new GetAllEmailNotificationLogsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllEmailNotificationLogsInput());
        result.TotalCount.ShouldBe(0);
    }
}
