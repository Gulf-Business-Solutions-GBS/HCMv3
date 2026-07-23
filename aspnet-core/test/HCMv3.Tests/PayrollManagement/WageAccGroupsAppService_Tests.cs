using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class WageAccGroupsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private WageAccGroupsAppService CreateAppService()
    {
        return CreateService(() => new WageAccGroupsAppService(FakeRepository.For<WageAccGroup>(Context)));
    }

    [Fact]
    public async Task Should_Create_WageAccGroup()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "WAG1",
            NameEn = "Main Wage Account Group",
            NameAr = "مجموعة حسابات الأجور الرئيسية"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Main Wage Account Group");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "WAG2",
            NameEn = "Group A",
            NameAr = "مجموعة أ"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
            {
                Code = "WAG3",
                NameEn = "Group A",
                NameAr = "مجموعة أ2"
            }));
    }
}
