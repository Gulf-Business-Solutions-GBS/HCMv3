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
public class RiskOfLossesAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private RiskOfLossesAppService CreateAppService()
    {
        return CreateService(() => new RiskOfLossesAppService(FakeRepository.For<RiskOfLoss>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RiskOfLoss()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "High Risk", NameAr = "خطر مرتفع" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Auto_Generate_Code_When_Blank()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "5", NameEn = "Existing", NameAr = "موجود" });

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Auto Coded", NameAr = "مشفر تلقائيا" });

        var all = await service.GetAll(new GetAllSimpleLookupsInput());
        all.Items.Single(i => i.NameEn == "Auto Coded").Code.ShouldBe("6");
    }
}
