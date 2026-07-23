using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class TimeAttAPIsAppService_Tests : TimeManagementAppServiceTestBase
{
    private TimeAttAPIsAppService CreateAppService()
    {
        return CreateService(() => new TimeAttAPIsAppService(FakeRepository.For<TimeAttAPI>(Context)));
    }

    private static CreateOrEditTimeAttAPIDto ValidDto()
    {
        return new CreateOrEditTimeAttAPIDto
        {
            TokenUrl = "https://example.com/token",
            Username = "user1",
            Password = "secret",
            TransactionsUrl = "https://example.com/transactions",
            NoOfRecords = 100,
            DevicesUrl = "https://example.com/devices"
        };
    }

    [Fact]
    public async Task Should_Create_TimeAttAPI()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(ValidDto());

        var result = await service.GetAll(new GetAllTimeAttAPIsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Username.ShouldBe("user1");
    }

    [Fact]
    public async Task Should_Throw_When_NoOfRecords_Is_Zero()
    {
        var service = CreateAppService();

        var dto = ValidDto();
        dto.NoOfRecords = 0;

        await Should.ThrowAsync<UserFriendlyException>(async () => await service.CreateOrEdit(dto));
    }
}
