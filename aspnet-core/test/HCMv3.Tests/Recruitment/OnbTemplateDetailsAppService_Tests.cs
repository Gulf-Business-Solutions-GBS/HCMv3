using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class OnbTemplateDetailsAppService_Tests : RecruitmentAppServiceTestBase
{
    private OnbTemplateDetailsAppService CreateAppService()
    {
        return CreateService(() => new OnbTemplateDetailsAppService(FakeRepository.For<OnbTemplateDetails>(Context)));
    }

    private int CreateOnbTemplate()
    {
        var entity = new OnbTemplate
        {
            TenantId = AbpSession.TenantId ?? 1,
            TemplateId = 1,
            TemplateTitle = "Template"
        };
        Context.OnbTemplates.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_OnbTemplateDetails()
    {
        var templateId = CreateOnbTemplate();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOnbTemplateDetailsDto
        {
            OnbTemplateId = templateId,
            TemplateData = "{}"
        });

        var result = await service.GetAll(new GetAllOnbTemplateDetailsInput { OnbTemplateId = templateId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TemplateData.ShouldBe("{}");
    }
}
