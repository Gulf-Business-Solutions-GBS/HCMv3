using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class RosterGroupsAppService_Tests : TimeManagementAppServiceTestBase
{
    private RosterGroupsAppService CreateAppService()
    {
        return CreateService(() => new RosterGroupsAppService(FakeRepository.For<RosterGroup>(Context)));
    }

    [Fact]
    public async Task Should_Create_RosterGroup()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterGroupDto
        {
            Code = "RG1",
            NameEn = "Night Team",
            NameAr = "فريق الليل"
        });

        var result = await service.GetAll(new GetAllRosterGroupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Night Team");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterGroupDto
        {
            Code = "RG2",
            NameEn = "Team A",
            NameAr = "فريق أ"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditRosterGroupDto
            {
                Code = "RG2",
                NameEn = "Team B",
                NameAr = "فريق ب"
            }));
    }
}
