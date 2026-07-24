using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelPerDiemsAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelPerDiemsAppService CreateAppService()
    {
        return CreateService(() => new TravelPerDiemsAppService(FakeRepository.For<TravelPerDiem>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TravelPerDiem()
    {
        var from = CreateTravelDestination("Riyadh");
        var to = CreateTravelDestination("Jeddah");
        var groupId = CreateEmployeeGroup();
        var subGroupId = CreateEmployeeSubGroup(groupId);

        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelPerDiemDto
        {
            FromDestinationId = from,
            ToDestinationId = to,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId,
            Amount = 150
        });

        var result = await service.GetAll(new GetAllTravelPerDiemsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(150);
        result.Items[0].FromDestinationNameEn.ShouldBe("Riyadh");
        result.Items[0].ToDestinationNameEn.ShouldBe("Jeddah");
    }

    [Fact]
    public async Task Should_Throw_When_Combination_Already_Exists()
    {
        var from = CreateTravelDestination("Riyadh");
        var to = CreateTravelDestination("Jeddah");
        var groupId = CreateEmployeeGroup();
        var subGroupId = CreateEmployeeSubGroup(groupId);

        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelPerDiemDto
        {
            FromDestinationId = from,
            ToDestinationId = to,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId,
            Amount = 150
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelPerDiemDto
            {
                FromDestinationId = from,
                ToDestinationId = to,
                EmployeeGroupId = groupId,
                EmployeeSubGroupId = subGroupId,
                Amount = 200
            }));
    }
}
