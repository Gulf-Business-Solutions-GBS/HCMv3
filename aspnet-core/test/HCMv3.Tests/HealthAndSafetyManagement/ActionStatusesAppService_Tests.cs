using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class ActionStatusesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private ActionStatusesAppService CreateAppService()
    {
        return CreateService(() => new ActionStatusesAppService(FakeRepository.For<ActionStatus>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ActionStatus()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Open",
            NameAr = "مفتوح"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Open");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "1", NameEn = "Open", NameAr = "مفتوح" });

        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "1", NameEn = "Closed", NameAr = "مغلق" }));
    }
}
