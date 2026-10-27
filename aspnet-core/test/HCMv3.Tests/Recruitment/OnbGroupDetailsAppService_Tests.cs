using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class OnbGroupDetailsAppService_Tests : RecruitmentAppServiceTestBase
{
    private OnbGroupDetailsAppService CreateAppService()
    {
        return CreateService(() => new OnbGroupDetailsAppService(FakeRepository.For<OnbGroupDetail>(Context)));
    }

    private int CreateOnbGroup()
    {
        var entity = new OnbGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Group",
            NameAr = "مجموعة"
        };
        Context.OnbGroups.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_OnbGroupDetail()
    {
        var groupId = CreateOnbGroup();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOnbGroupDetailDto
        {
            OnbGroupId = groupId
        });

        var result = await service.GetAll(new GetAllOnbGroupDetailsInput { OnbGroupId = groupId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].OnbGroupId.ShouldBe(groupId);
    }
}
