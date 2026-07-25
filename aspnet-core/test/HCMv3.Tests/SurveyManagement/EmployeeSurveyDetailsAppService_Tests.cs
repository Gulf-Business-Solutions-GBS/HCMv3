using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.SurveyManagement;
using HCMv3.SurveyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SurveyManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSurveyDetailsAppService_Tests : SurveyManagementAppServiceTestBase
{
    private EmployeeSurveyDetailsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSurveyDetailsAppService(FakeRepository.For<EmployeeSurveyDetail>(Context)));
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

    private int CreateSurveyDetail(int surveyId)
    {
        var entity = new SurveyDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            SurveyId = surveyId,
            OrderNo = 1,
            Question = "How satisfied are you?",
            QuestionType = QuestionType.TextBox
        };
        Context.SurveyDetails.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateEmployeeSurvey(int employeeId, int surveyId)
    {
        var entity = new EmployeeSurvey
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            SurveyId = surveyId
        };
        Context.EmployeeSurveys.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeeSurveyDetail()
    {
        var employeeId = CreateEmployee();
        var surveyId = CreateSurvey();
        var questionId = CreateSurveyDetail(surveyId);
        var employeeSurveyId = CreateEmployeeSurvey(employeeId, surveyId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSurveyDetailDto
        {
            EmployeeSurveyId = employeeSurveyId,
            QuestionId = questionId,
            Answer = "Very satisfied"
        });

        var result = await service.GetAll(new GetAllEmployeeSurveyDetailsInput { EmployeeSurveyId = employeeSurveyId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Answer.ShouldBe("Very satisfied");
    }

    [Fact]
    public async Task Should_Throw_When_QuestionId_Is_Zero()
    {
        var employeeId = CreateEmployee();
        var surveyId = CreateSurvey();
        var employeeSurveyId = CreateEmployeeSurvey(employeeId, surveyId);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeSurveyDetailDto
            {
                EmployeeSurveyId = employeeSurveyId,
                QuestionId = 0,
                Answer = "N/A"
            }));
    }
}
