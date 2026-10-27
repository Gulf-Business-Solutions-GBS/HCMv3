using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.Net.Mail;
using Castle.MicroKernel.Registration;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using HCMv3.SystemSettings;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class EmailServiceTests : AppTestBase
{
    private readonly IEmailSender _fakeEmailSender;
    private readonly IEmailService _emailService;

    public EmailServiceTests()
    {
        LoginAsDefaultTenantAdmin();

        _fakeEmailSender = Substitute.For<IEmailSender>();
        _fakeEmailSender.SendAsync(Arg.Any<MailMessage>()).Returns(Task.CompletedTask);
        LocalIocManager.IocContainer.Register(Component.For<IEmailSender>().Instance(_fakeEmailSender).IsDefault());

        _emailService = Resolve<IEmailService>();
    }

    private int CreateNationality()
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });
        return nationality.Id;
    }

    private Employee CreateEmployee(string code, string email, EmployeeStatus status = EmployeeStatus.Active)
    {
        var nationalityId = CreateNationality();
        Employee employee = null;

        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "First_" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                Email = email,
                Status = status,
                NationalityId = nationalityId
            }).Entity;
        });

        return employee;
    }

    private EmailTemplate CreateEmailTemplate(string subject, string bodyHtml)
    {
        EmailTemplate template = null;

        UsingDbContext(context =>
        {
            template = context.Set<EmailTemplate>().Add(new EmailTemplate
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "T",
                NameAr = "T",
                Subject = subject,
                EmailBodyHtml = bodyHtml
            }).Entity;
        });

        return template;
    }

    private void CreateEmailToken(int emailTemplateId, string tokenName, string tokenValue, bool objectKey = false)
    {
        UsingDbContext(context =>
        {
            context.Set<EmailToken>().Add(new EmailToken
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmailTemplateId = emailTemplateId,
                TokenName = tokenName,
                TokenValue = tokenValue,
                ObjectKey = objectKey
            });
        });
    }

    [Fact]
    public async Task Should_Send_Email_And_Log_Success()
    {
        var employee = CreateEmployee("EML100", "eml100@example.com");
        var template = CreateEmailTemplate("Hello $#EmployeeName#$", "<p>Body for $#EmployeeName#$</p>");
        CreateEmailToken(template.Id, "EmployeeName", "FirstNameEn");

        var result = await _emailService.SendEmailAsync(template, employee, employee);

        result.Status.ShouldBeTrue();
        result.EmailSubject.ShouldBe("Hello " + employee.FirstNameEn);

        await _fakeEmailSender.Received(1).SendAsync(Arg.Is<MailMessage>(m =>
            m.To.Count == 1 && m.To[0].Address == employee.Email));

        var log = UsingDbContext(context => context.Set<EmailNotificationLog>().FirstOrDefault());
        log.ShouldNotBeNull();
        log.IsSuccessful.ShouldBeTrue();
        log.Email.ShouldBe(employee.Email);
    }

    [Fact]
    public async Task Should_Fail_When_Recipient_Has_No_Email()
    {
        var employee = CreateEmployee("EML101", null);
        var template = CreateEmailTemplate("Subject", "Body");

        var result = await _emailService.SendEmailAsync(template, employee, employee);

        result.Status.ShouldBeFalse();

        await _fakeEmailSender.DidNotReceive().SendAsync(Arg.Any<MailMessage>());
    }

    [Fact]
    public async Task Should_Skip_Send_When_Email_Notification_Disabled()
    {
        UsingDbContext(context =>
        {
            context.Set<ManageSelfServiceSettings>().Add(new ManageSelfServiceSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmailNotification = false
            });
        });

        var employee = CreateEmployee("EML102", "eml102@example.com");
        var template = CreateEmailTemplate("Subject", "Body");

        var result = await _emailService.SendEmailAsync(template, employee, employee);

        result.Status.ShouldBeFalse();
        await _fakeEmailSender.DidNotReceive().SendAsync(Arg.Any<MailMessage>());

        var log = UsingDbContext(context => context.Set<EmailNotificationLog>().FirstOrDefault());
        log.ShouldNotBeNull();
        log.IsSuccessful.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Substitute_ObjectKey_Token_From_MergeValues()
    {
        var employee = CreateEmployee("EML103", "eml103@example.com");
        var template = CreateEmailTemplate("Notice", "<p>Value: $#CustomValue#$</p>");
        CreateEmailToken(template.Id, "CustomValue", "CustomValue", objectKey: true);

        var mergeValues = new Dictionary<string, object> { ["CustomValue"] = "42" };

        var result = await _emailService.SendEmailAsync(template, employee, employee, mergeValues);

        result.Status.ShouldBeTrue();
        result.EmailBody.ShouldContain("Value: 42");
    }

    [Fact]
    public async Task Should_Route_To_Employee_And_Manager_For_NotificationConfig()
    {
        var employee = CreateEmployee("EML104", "eml104@example.com");
        var manager = CreateEmployee("EML105", "eml105@example.com");

        UsingDbContext(context =>
        {
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employee.Id,
                ManagerId = manager.Id,
                StartDate = DateTime.Now.Date.AddDays(-1),
                EndDate = DateTime.Now.Date.AddDays(1)
            });
        });

        var template = CreateEmailTemplate("Notify", "Body");

        NotificationConfig config = null;
        UsingDbContext(context =>
        {
            config = context.Set<NotificationConfig>().Add(new NotificationConfig
            {
                TenantId = AbpSession.TenantId ?? 1,
                Name = "Test Config",
                ToEmployee = true,
                ToManager = true
            }).Entity;
        });

        await _emailService.SendEmailForNotificationConfigAsync(config, template, employee);

        await _fakeEmailSender.Received(1).SendAsync(Arg.Is<MailMessage>(m => m.To[0].Address == employee.Email));
        await _fakeEmailSender.Received(1).SendAsync(Arg.Is<MailMessage>(m => m.To[0].Address == manager.Email));
    }

    [Fact]
    public async Task Should_Return_Reflected_Property_Value_For_Nested_Path()
    {
        var employee = CreateEmployee("EML106", "eml106@example.com");

        var value = _emailService.GetPropertyValue(employee, "FirstNameEn");

        value.ShouldBe(employee.FirstNameEn);
    }
}
