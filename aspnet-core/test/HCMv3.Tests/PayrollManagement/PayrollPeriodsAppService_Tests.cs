using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class PayrollPeriodsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private PayrollPeriodsAppService CreateAppService()
    {
        return CreateService(() => new PayrollPeriodsAppService(FakeRepository.For<PayrollPeriod>(Context)));
    }

    [Fact]
    public async Task Should_Create_PayrollPeriod()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "Jan-Dec",
            NameEn = "January to December",
            NameAr = "يناير إلى ديسمبر"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("January to December");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            Code = "Feb-Jan",
            NameEn = "February to January",
            NameAr = "فبراير إلى يناير"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
            {
                Code = "Feb-Jan",
                NameEn = "Duplicate",
                NameAr = "مكرر"
            }));
    }
}
