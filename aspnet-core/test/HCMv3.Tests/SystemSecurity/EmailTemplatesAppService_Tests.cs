using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class EmailTemplatesAppService_Tests : SystemSecurityAppServiceTestBase
{
    private EmailTemplatesAppService CreateAppService()
    {
        return CreateService(() => new EmailTemplatesAppService(FakeRepository.For<EmailTemplate>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmailTemplate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailTemplateDto
        {
            NameEn = "Welcome Email",
            NameAr = "بريد الترحيب",
            Subject = "Welcome",
            EmailBodyHtml = "<p>Hi</p>"
        });

        var result = await service.GetAll(new GetAllEmailTemplatesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Welcome Email");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailTemplateDto { NameEn = "A", NameAr = "A" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmailTemplateDto { NameEn = "A", NameAr = "Other" }));
    }

    [Fact]
    public async Task Should_Throw_When_Code_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailTemplateDto { Code = "C1", NameEn = "A", NameAr = "A" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmailTemplateDto { Code = "C1", NameEn = "B", NameAr = "B" }));
    }

    [Fact]
    public async Task Should_Update_EmailTemplate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailTemplateDto { NameEn = "A", NameAr = "A" });
        var created = await service.GetAll(new GetAllEmailTemplatesInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditEmailTemplateDto { Id = id, NameEn = "A", NameAr = "A", Subject = "Updated" });

        var edited = await service.GetEmailTemplateForEdit(new EntityDto(id));
        edited.EmailTemplate.Subject.ShouldBe("Updated");
    }

    [Fact]
    public async Task Should_Delete_EmailTemplate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailTemplateDto { NameEn = "A", NameAr = "A" });
        var created = await service.GetAll(new GetAllEmailTemplatesInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllEmailTemplatesInput());
        result.TotalCount.ShouldBe(0);
    }
}
