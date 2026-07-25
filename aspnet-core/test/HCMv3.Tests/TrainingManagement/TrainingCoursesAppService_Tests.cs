using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TrainingManagement;
using HCMv3.TrainingManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TrainingManagement;

// ReSharper disable once InconsistentNaming
public class TrainingCoursesAppService_Tests : TrainingManagementAppServiceTestBase
{
    private TrainingCoursesAppService CreateAppService()
    {
        return CreateService(() => new TrainingCoursesAppService(FakeRepository.For<TrainingCourse>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TrainingCourse()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTrainingCourseDto
        {
            NameEn = "First Aid",
            NameAr = "الإسعافات الأولية",
            DeliveryMethod = TrainingDeliveryMethod.Classroom,
            Fees = 100,
            Active = true
        });

        var result = await service.GetAll(new GetAllTrainingCoursesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("First Aid");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
        result.Items[0].DeliveryMethod.ShouldBe(TrainingDeliveryMethod.Classroom);
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTrainingCourseDto { NameEn = "IT", NameAr = "IT" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTrainingCourseDto { NameEn = "IT", NameAr = "Other" }));
    }

    [Fact]
    public async Task Should_Throw_When_Code_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTrainingCourseDto { Code = "C1", NameEn = "A", NameAr = "A" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTrainingCourseDto { Code = "C1", NameEn = "B", NameAr = "B" }));
    }
}
