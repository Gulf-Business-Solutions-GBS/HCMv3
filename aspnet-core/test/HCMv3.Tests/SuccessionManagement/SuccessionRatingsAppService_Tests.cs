using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.SuccessionManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SuccessionManagement;

// ReSharper disable once InconsistentNaming
public class SuccessionRatingsAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private SuccessionRatingsAppService CreateAppService()
    {
        return CreateService(() => new SuccessionRatingsAppService(FakeRepository.For<SuccessionRating>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SuccessionRating()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Exceeds Expectations", NameAr = "يفوق التوقعات" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "SR1", NameEn = "Exceeds", NameAr = "يفوق" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "SR1", NameEn = "Meets", NameAr = "يلبي" }));
    }
}
