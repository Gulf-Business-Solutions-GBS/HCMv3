using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class PayslipConfigsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private PayslipConfigsAppService CreateAppService()
    {
        return CreateService(() => new PayslipConfigsAppService(FakeRepository.For<PayslipConfig>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PayslipConfig()
    {
        var templateId = CreateEmailTemplate();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayslipConfigDto { EmailTemplateId = templateId });

        var result = await service.GetAll(new GetAllPayslipConfigsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmailTemplateId.ShouldBe(templateId);
    }

    [Fact]
    public async Task Should_Throw_When_Second_Row_Created()
    {
        var templateId1 = CreateEmailTemplate();
        var templateId2 = CreateEmailTemplate();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayslipConfigDto { EmailTemplateId = templateId1 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPayslipConfigDto { EmailTemplateId = templateId2 }));
    }

    [Fact]
    public async Task Should_Allow_Editing_The_Single_Existing_Row()
    {
        var templateId1 = CreateEmailTemplate();
        var templateId2 = CreateEmailTemplate();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayslipConfigDto { EmailTemplateId = templateId1 });
        var created = await service.GetAll(new GetAllPayslipConfigsInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditPayslipConfigDto { Id = id, EmailTemplateId = templateId2 });

        var edited = await service.GetPayslipConfigForEdit(new EntityDto(id));
        edited.PayslipConfig.EmailTemplateId.ShouldBe(templateId2);
    }

    [Fact]
    public async Task Should_Delete_PayslipConfig()
    {
        var templateId = CreateEmailTemplate();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayslipConfigDto { EmailTemplateId = templateId });
        var created = await service.GetAll(new GetAllPayslipConfigsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllPayslipConfigsInput());
        result.TotalCount.ShouldBe(0);
    }
}
