using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Integrations.Services;
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

    private TimeAttAPI StoredRow() => Context.Set<TimeAttAPI>().Single();

    [Fact]
    public async Task Should_Store_Password_Encrypted()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(ValidDto());

        var stored = StoredRow().Password;
        stored.ShouldNotBe("secret");
        IntegrationSecret.Reveal(stored).ShouldBe("secret");
    }

    [Fact]
    public async Task Should_Not_Return_Password_For_Edit()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(ValidDto());

        var output = await service.GetTimeAttAPIForEdit(new EntityDto(StoredRow().Id));

        output.TimeAttAPI.Password.ShouldBeNull();
        output.IsPasswordSet.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Keep_Saved_Password_When_Edit_Leaves_It_Blank()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(ValidDto());
        var before = StoredRow().Password;

        var dto = ValidDto();
        dto.Id = StoredRow().Id;
        dto.Password = "";
        dto.Username = "user2";
        await service.CreateOrEdit(dto);

        StoredRow().Username.ShouldBe("user2");
        StoredRow().Password.ShouldBe(before);
    }

    [Fact]
    public async Task Should_Encrypt_New_Password_On_Edit()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(ValidDto());

        var dto = ValidDto();
        dto.Id = StoredRow().Id;
        dto.Password = "changed";
        await service.CreateOrEdit(dto);

        StoredRow().Password.ShouldNotBe("changed");
        IntegrationSecret.Reveal(StoredRow().Password).ShouldBe("changed");
    }

    [Fact]
    public async Task Should_Throw_When_Creating_Without_Password()
    {
        var service = CreateAppService();

        var dto = ValidDto();
        dto.Password = "";

        await Should.ThrowAsync<UserFriendlyException>(async () => await service.CreateOrEdit(dto));
    }
}
