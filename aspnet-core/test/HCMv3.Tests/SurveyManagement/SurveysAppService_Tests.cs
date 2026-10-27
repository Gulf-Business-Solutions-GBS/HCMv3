using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.SurveyManagement;
using HCMv3.SurveyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SurveyManagement;

// ReSharper disable once InconsistentNaming
public class SurveysAppService_Tests : SurveyManagementAppServiceTestBase
{
    private SurveysAppService CreateAppService()
    {
        return CreateService(() => new SurveysAppService(FakeRepository.For<Survey>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Survey()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSurveyDto
        {
            NameEn = "Employee Satisfaction 2026",
            NameAr = "استبيان رضا الموظفين 2026",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        });

        var result = await service.GetAll(new GetAllSurveysInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Employee Satisfaction 2026");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSurveyDto
        {
            NameEn = "Survey A",
            NameAr = "A",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSurveyDto
            {
                NameEn = "Survey A",
                NameAr = "B",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 31)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Before_StartDate()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSurveyDto
            {
                NameEn = "Survey B",
                NameAr = "B",
                StartDate = new DateTime(2026, 2, 1),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }
}
