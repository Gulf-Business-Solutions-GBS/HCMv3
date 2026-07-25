using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class FirstAidMedicinesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private FirstAidMedicinesAppService CreateAppService()
    {
        return CreateService(() => new FirstAidMedicinesAppService(FakeRepository.For<FirstAidMedicine>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_FirstAidMedicine()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test FirstAidMedicine",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test FirstAidMedicine");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
