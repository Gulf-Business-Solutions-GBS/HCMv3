using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.SurveyReport;
using HCMv3.GridReports.SurveyReport.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.SurveyManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EmployeeSurveyReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeSurveyReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeSurveyReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSurveyReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });

        return employee;
    }

    private Survey CreateSurvey(string code, DateTime start, DateTime end)
    {
        Survey survey = null;
        UsingDbContext(context =>
        {
            survey = context.Set<Survey>().Add(new Survey
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                StartDate = start,
                EndDate = end
            }).Entity;
        });
        return survey;
    }

    private SurveyDetail CreateQuestion(Survey survey, string text)
    {
        SurveyDetail detail = null;
        UsingDbContext(context =>
        {
            detail = context.Set<SurveyDetail>().Add(new SurveyDetail
            {
                TenantId = TenantId,
                SurveyId = survey.Id,
                OrderNo = 1,
                Question = text,
                QuestionType = QuestionType.TextBox
            }).Entity;
        });
        return detail;
    }

    private void CreateAnswer(Employee employee, Survey survey, SurveyDetail question, string answer)
    {
        UsingDbContext(context =>
        {
            var employeeSurvey = context.Set<EmployeeSurvey>().Add(new EmployeeSurvey
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SurveyId = survey.Id
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeSurveyDetail>().Add(new EmployeeSurveyDetail
            {
                TenantId = TenantId,
                EmployeeSurveyId = employeeSurvey.Id,
                QuestionId = question.Id,
                Answer = answer
            });
        });
    }

    private static GetEmployeeSurveyReportInput Input(DateTime from, DateTime to, string employeeCode = null, string surveyCode = null)
    {
        return new GetEmployeeSurveyReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            SurveyCode = surveyCode,
            MaxResultCount = 100
        };
    }

    [Fact]
    public async Task Should_Return_Only_Answers_Whose_Survey_Window_Overlaps_The_Requested_Range()
    {
        var inRange = CreateSurvey("ESV-IN", new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        var outOfRange = CreateSurvey("ESV-OUT", new DateTime(2023, 1, 1), new DateTime(2023, 1, 31));
        var employee = CreateEmployee("ESV-EMP");
        var q1 = CreateQuestion(inRange, "Q1");
        var q2 = CreateQuestion(outOfRange, "Q2");
        CreateAnswer(employee, inRange, q1, "A1");
        CreateAnswer(employee, outOfRange, q2, "A2");

        var result = await _appService.GetEmployeeSurveyReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "ESV-EMP"));

        var surveys = result.Items.Select(i => i.SurveyName).ToList();
        surveys.ShouldContain("ESV-IN-En");
        surveys.ShouldNotContain("ESV-OUT-En");
    }

    [Fact]
    public async Task Should_Filter_By_Survey_Code()
    {
        var surveyA = CreateSurvey("ESV-CA", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        var surveyB = CreateSurvey("ESV-CB", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        var employee = CreateEmployee("ESV-CODE");
        var qa = CreateQuestion(surveyA, "QA");
        var qb = CreateQuestion(surveyB, "QB");
        CreateAnswer(employee, surveyA, qa, "AA");
        CreateAnswer(employee, surveyB, qb, "AB");

        var result = await _appService.GetEmployeeSurveyReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), surveyCode: "ESV-CA"));

        var answers = result.Items.Select(i => i.Answer).ToList();
        answers.ShouldContain("AA");
        answers.ShouldNotContain("AB");
    }

    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var survey = CreateSurvey("ESV-HDR", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        var employee = CreateEmployee("ESV-HDR-EMP");
        var question = CreateQuestion(survey, "QH");
        CreateAnswer(employee, survey, question, "AH");

        var file = await _appService.GetEmployeeSurveyReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
