using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.SurveyManagement;
using HCMv3.SurveyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SurveyManagement;

// ReSharper disable once InconsistentNaming
public class SurveyDetailsAppService_Tests : SurveyManagementAppServiceTestBase
{
    private SurveyDetailsAppService CreateAppService()
    {
        return CreateService(() => new SurveyDetailsAppService(FakeRepository.For<SurveyDetail>(Context)));
    }

    private int CreateSurvey()
    {
        var entity = new Survey
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "S1",
            NameEn = "Survey",
            NameAr = "استبيان",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        };
        Context.Surveys.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Auto_Assign_OrderNo()
    {
        var surveyId = CreateSurvey();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSurveyDetailDto
        {
            SurveyId = surveyId,
            Question = "How satisfied are you?",
            QuestionType = QuestionType.TextBox
        });

        await service.CreateOrEdit(new CreateOrEditSurveyDetailDto
        {
            SurveyId = surveyId,
            Question = "Would you recommend us?",
            QuestionType = QuestionType.TextBox
        });

        var result = await service.GetAll(new GetAllSurveyDetailsInput { SurveyId = surveyId });
        result.TotalCount.ShouldBe(2);
        result.Items[0].OrderNo.ShouldBe(1);
        result.Items[1].OrderNo.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Throw_When_Options_Blank_For_RadioList()
    {
        var surveyId = CreateSurvey();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSurveyDetailDto
            {
                SurveyId = surveyId,
                Question = "Pick one",
                QuestionType = QuestionType.RadioList
            }));
    }

    [Fact]
    public async Task Should_Create_When_Options_Provided_For_CheckboxList()
    {
        var surveyId = CreateSurvey();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSurveyDetailDto
        {
            SurveyId = surveyId,
            Question = "Pick many",
            QuestionType = QuestionType.CheckboxList,
            Options = "A;B;C"
        });

        var result = await service.GetAll(new GetAllSurveyDetailsInput { SurveyId = surveyId });
        result.TotalCount.ShouldBe(1);
    }
}
