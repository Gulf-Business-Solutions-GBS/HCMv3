using System;
using System.Threading.Tasks;
using HCMv3.SurveyManagement;
using HCMv3.SurveyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SurveyManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSurveysAppService_Tests : SurveyManagementAppServiceTestBase
{
    private EmployeeSurveysAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSurveysAppService(FakeRepository.For<EmployeeSurvey>(Context)));
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
    public async Task Should_Create_And_Retrieve_EmployeeSurvey()
    {
        var employeeId = CreateEmployee();
        var surveyId = CreateSurvey();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSurveyDto
        {
            EmployeeId = employeeId,
            SurveyId = surveyId
        });

        var result = await service.GetAll(new GetAllEmployeeSurveysInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].SurveyId.ShouldBe(surveyId);
    }
}
