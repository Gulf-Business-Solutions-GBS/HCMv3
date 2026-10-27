using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PARatingsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _settings =
        NSubstitute.Substitute.For<HCMv3.SystemSettings.Services.ISysSettingsAccessor>();

    private PARatingsAppService CreateAppService(bool marksInReview = false)
    {
        NSubstitute.SubstituteExtensions.Returns(_settings.GetAppraisalSettingsAsync(),
            Task.FromResult(new HCMv3.SystemSettings.ManageAppraisalSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EnableMarksInPAReview = marksInReview
            }));

        return CreateService(() => new PARatingsAppService(FakeRepository.For<PARating>(Context), _settings));
    }

    /// <summary>Legacy PARating.PerformValidation: with marks scoring on, rating ranges may not overlap.</summary>
    [Fact]
    public async Task With_Marks_In_Review_On_Should_Refuse_An_Overlapping_Range()
    {
        var service = CreateAppService(marksInReview: true);
        await service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "Good", NameAr = "جيد", FromRange = 60, ToRange = 79 });

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "Very Good", NameAr = "جيد جداً", FromRange = 75, ToRange = 89 }));
        exception.Message.ShouldContain("overlaps");

        await service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "Excellent", NameAr = "ممتاز", FromRange = 80, ToRange = 100 });
    }

    [Fact]
    public async Task With_Marks_In_Review_Off_Overlapping_Ranges_Are_Accepted()
    {
        var service = CreateAppService(marksInReview: false);
        await service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "Good", NameAr = "جيد", FromRange = 60, ToRange = 79 });
        await service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "Very Good", NameAr = "جيد جداً", FromRange = 75, ToRange = 89 });

        (await service.GetAll(new GetAllPARatingsInput())).TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PARating()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPARatingDto
        {
            NameEn = "Excellent",
            NameAr = "ممتاز",
            IsCommentRequired = true,
            Status = true,
            MaxLimit = 5,
            FromRange = 4,
            ToRange = 5
        });

        var result = await service.GetAll(new GetAllPARatingsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Excellent");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Delete_PARating()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "A", NameAr = "A" });

        var all = await service.GetAll(new GetAllPARatingsInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPARatingsInput());
        afterDelete.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "Excellent", NameAr = "A1" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "Excellent", NameAr = "A2" }));
    }
}
