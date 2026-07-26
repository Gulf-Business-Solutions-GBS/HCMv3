using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class EmailTokensAppService_Tests : SystemSecurityAppServiceTestBase
{
    private EmailTokensAppService CreateAppService()
    {
        return CreateService(() => new EmailTokensAppService(FakeRepository.For<EmailToken>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmailToken()
    {
        var templateId = CreateEmailTemplate();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailTokenDto
        {
            TokenName = "EmployeeName",
            TokenValue = "[EmployeeName]",
            EmailTemplateId = templateId
        });

        var result = await service.GetAll(new GetAllEmailTokensInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].TokenName.ShouldBe("EmployeeName");
        result.Items[0].EmailTemplateId.ShouldBe(templateId);
    }

    [Fact]
    public async Task Should_Throw_When_TokenName_Duplicate()
    {
        var templateId = CreateEmailTemplate();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailTokenDto { TokenName = "T1", EmailTemplateId = templateId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmailTokenDto { TokenName = "T1", EmailTemplateId = templateId }));
    }

    [Fact]
    public async Task Should_Filter_By_EmailTemplateId()
    {
        var templateId1 = CreateEmailTemplate();
        var templateId2 = CreateEmailTemplate();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailTokenDto { TokenName = "T1", EmailTemplateId = templateId1 });
        await service.CreateOrEdit(new CreateOrEditEmailTokenDto { TokenName = "T2", EmailTemplateId = templateId2 });

        var result = await service.GetAll(new GetAllEmailTokensInput { EmailTemplateId = templateId2 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TokenName.ShouldBe("T2");
    }

    [Fact]
    public async Task Should_Delete_EmailToken()
    {
        var templateId = CreateEmailTemplate();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmailTokenDto { TokenName = "T1", EmailTemplateId = templateId });
        var created = await service.GetAll(new GetAllEmailTokensInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllEmailTokensInput());
        result.TotalCount.ShouldBe(0);
    }
}
