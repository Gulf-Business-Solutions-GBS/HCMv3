using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Localization;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SurveyManagement;
using HCMv3.SurveyManagement.Dto;
using NSubstitute;
using Shouldly;
using Xunit;

// See ProjectCostEngineFakes for why a plain List<T>.AsQueryable() is a complete stand-in here.
using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.SurveyManagement;

/// <summary>
/// Unit tests for <see cref="EmployeeSurveyResponseAppService"/> -- the port of legacy
/// <c>HCM10.Web/UserControls/SurveyManagement/uscEmployeeSurvey.ascx.cs</c>.
/// </summary>
public class EmployeeSurveyResponseAppService_Tests
{
    private const int EmployeeId = 7;
    private const int SurveyId = 3;

    private readonly List<Survey> _surveys = new List<Survey>();
    private readonly List<SurveyDetail> _surveyDetails = new List<SurveyDetail>();
    private readonly List<EmployeeSurvey> _employeeSurveys = new List<EmployeeSurvey>();
    private readonly List<EmployeeSurveyDetail> _employeeSurveyDetails = new List<EmployeeSurveyDetail>();
    private readonly List<Employee> _employees = new List<Employee>();
    private readonly IEmployeeSessionService _employeeSessionService = Substitute.For<IEmployeeSessionService>();

    public EmployeeSurveyResponseAppService_Tests()
    {
        var employee = new Employee { Id = EmployeeId, TenantId = 1, Code = "E7", FullNameEn = "Seven" };
        _employees.Add(employee);
        _employeeSessionService.GetCurrentEmployeeAsync().Returns(Task.FromResult(employee));

        _surveys.Add(new Survey
        {
            Id = SurveyId,
            TenantId = 1,
            Code = "S1",
            NameEn = "Engagement",
            NameAr = "Engagement",
            StartDate = DateTime.Now.AddDays(-5),
            EndDate = DateTime.Now.AddDays(5)
        });

        // Deliberately inserted out of order so the OrderNo sort (legacy line 230) is exercised.
        _surveyDetails.Add(new SurveyDetail
        {
            Id = 20, TenantId = 1, SurveyId = SurveyId, OrderNo = 2,
            Question = "Pick one", QuestionType = QuestionType.RadioList, Options = "A;B;C"
        });
        _surveyDetails.Add(new SurveyDetail
        {
            Id = 10, TenantId = 1, SurveyId = SurveyId, OrderNo = 1,
            Question = "Free text", QuestionType = QuestionType.TextBox
        });
    }

    private EmployeeSurveyResponseAppService CreateService()
    {
        var service = new EmployeeSurveyResponseAppService(
            Fakes.RepoOver(_surveys),
            Fakes.RepoOver(_surveyDetails),
            Fakes.RepoOver(_employeeSurveys),
            Fakes.RepoOver(_employeeSurveyDetails),
            Fakes.RepoOver(_employees),
            _employeeSessionService);

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        return service;
    }

    /// <summary>Legacy lines 178-179: only surveys whose date range spans "now".</summary>
    [Fact]
    public async Task Should_Only_Return_Surveys_Open_Right_Now()
    {
        _surveys.Add(new Survey
        {
            Id = 4, TenantId = 1, Code = "OLD", NameEn = "Closed", NameAr = "Closed",
            StartDate = DateTime.Now.AddDays(-30), EndDate = DateTime.Now.AddDays(-10)
        });
        _surveys.Add(new Survey
        {
            Id = 5, TenantId = 1, Code = "FUT", NameEn = "Future", NameAr = "Future",
            StartDate = DateTime.Now.AddDays(10), EndDate = DateTime.Now.AddDays(30)
        });

        var service = CreateService();

        var surveys = await service.GetActiveSurveys();

        surveys.Count.ShouldBe(1);
        surveys[0].Id.ShouldBe(SurveyId);
    }

    /// <summary>Legacy <c>GetData</c> (lines 230-265): rows in OrderNo order with blank answers.</summary>
    [Fact]
    public async Task Should_Build_Questionnaire_In_OrderNo_Order_With_Blank_Answers()
    {
        var service = CreateService();

        var result = await service.GetEmployeeSurveyQuestionnaire(
            new GetEmployeeSurveyQuestionnaireInput { SurveyId = SurveyId });

        result.EmployeeId.ShouldBe(EmployeeId);
        result.AlreadySubmitted.ShouldBeFalse();
        result.EmployeeCodeWithName.ShouldBe("E7 - Seven");

        result.Questions.Count.ShouldBe(2);
        result.Questions[0].No.ShouldBe(1);
        result.Questions[0].SurveyDetailId.ShouldBe(10);
        result.Questions[0].QuestionType.ShouldBe(QuestionType.TextBox);
        result.Questions[0].Answer.ShouldBe(string.Empty);

        result.Questions[1].No.ShouldBe(2);
        result.Questions[1].SurveyDetailId.ShouldBe(20);
        // The raw option string is passed through untouched -- legacy's editors split it on ';'.
        result.Questions[1].Options.ShouldBe("A;B;C");
    }

    /// <summary>Legacy lines 240-244: stored answers are merged onto the question rows.</summary>
    [Fact]
    public async Task Should_Merge_Existing_Answers_Into_The_Questionnaire()
    {
        _employeeSurveys.Add(new EmployeeSurvey { Id = 1, TenantId = 1, EmployeeId = EmployeeId, SurveyId = SurveyId });
        _employeeSurveyDetails.Add(new EmployeeSurveyDetail
        {
            Id = 1, TenantId = 1, EmployeeSurveyId = 1, QuestionId = 20, Answer = "B"
        });

        var service = CreateService();

        var result = await service.GetEmployeeSurveyQuestionnaire(
            new GetEmployeeSurveyQuestionnaireInput { SurveyId = SurveyId });

        result.AlreadySubmitted.ShouldBeTrue();
        result.Questions.Single(q => q.SurveyDetailId == 20).Answer.ShouldBe("B");
        result.Questions.Single(q => q.SurveyDetailId == 10).Answer.ShouldBe(string.Empty);
    }

    /// <summary>Legacy lines 296-333: the header is created on first submit, one detail row per answer.</summary>
    [Fact]
    public async Task Should_Create_Header_And_Detail_Rows_On_First_Submit()
    {
        var service = CreateService();

        await service.SubmitEmployeeSurvey(new SubmitEmployeeSurveyInput
        {
            SurveyId = SurveyId,
            Answers = new List<EmployeeSurveyAnswerInput>
            {
                new EmployeeSurveyAnswerInput { SurveyDetailId = 10, Answer = "Hello" },
                new EmployeeSurveyAnswerInput { SurveyDetailId = 20, Answer = "A,C" }
            }
        });

        _employeeSurveys.Count.ShouldBe(1);
        _employeeSurveys[0].EmployeeId.ShouldBe(EmployeeId);

        _employeeSurveyDetails.Count.ShouldBe(2);
        _employeeSurveyDetails.Single(d => d.QuestionId == 10).Answer.ShouldBe("Hello");
        // A multi-select answer is stored comma-joined, exactly as legacy built it (lines 105-111).
        _employeeSurveyDetails.Single(d => d.QuestionId == 20).Answer.ShouldBe("A,C");
    }

    /// <summary>Legacy lines 312-332: re-submitting UPDATES the existing rows rather than adding more.</summary>
    [Fact]
    public async Task Should_Update_Existing_Answers_On_Resubmit()
    {
        _employeeSurveys.Add(new EmployeeSurvey { Id = 1, TenantId = 1, EmployeeId = EmployeeId, SurveyId = SurveyId });
        _employeeSurveyDetails.Add(new EmployeeSurveyDetail
        {
            Id = 1, TenantId = 1, EmployeeSurveyId = 1, QuestionId = 10, Answer = "Old"
        });

        var service = CreateService();

        await service.SubmitEmployeeSurvey(new SubmitEmployeeSurveyInput
        {
            SurveyId = SurveyId,
            Answers = new List<EmployeeSurveyAnswerInput>
            {
                new EmployeeSurveyAnswerInput { SurveyDetailId = 10, Answer = "New" }
            }
        });

        _employeeSurveys.Count.ShouldBe(1);
        _employeeSurveyDetails.Count.ShouldBe(1);
        _employeeSurveyDetails[0].Answer.ShouldBe("New");
    }

    /// <summary>Legacy lines 123-127: nothing to save is a no-op, not an error.</summary>
    [Fact]
    public async Task Should_Do_Nothing_When_No_Answers_Are_Posted()
    {
        var service = CreateService();

        await service.SubmitEmployeeSurvey(new SubmitEmployeeSurveyInput { SurveyId = SurveyId });

        _employeeSurveys.ShouldBeEmpty();
        _employeeSurveyDetails.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy line 289 would NullReference on a question id from another survey; this port rejects it.
    /// </summary>
    [Fact]
    public async Task Should_Reject_An_Answer_For_A_Question_From_Another_Survey()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.SubmitEmployeeSurvey(
            new SubmitEmployeeSurveyInput
            {
                SurveyId = SurveyId,
                Answers = new List<EmployeeSurveyAnswerInput>
                {
                    new EmployeeSurveyAnswerInput { SurveyDetailId = 999, Answer = "x" }
                }
            }));
    }

    [Fact]
    public async Task Should_Reject_An_Unknown_Survey()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.GetEmployeeSurveyQuestionnaire(
            new GetEmployeeSurveyQuestionnaireInput { SurveyId = 999 }));
    }

    /// <summary>Legacy lines 150-167: the current user's own employee record is the default.</summary>
    [Fact]
    public async Task Should_Fail_When_The_Current_User_Is_Not_Linked_To_An_Employee()
    {
        _employeeSessionService.GetCurrentEmployeeAsync().Returns(Task.FromResult<Employee>(null));
        var service = CreateService();

        await Assert.ThrowsAsync<UserFriendlyException>(() => service.GetEmployeeSurveyQuestionnaire(
            new GetEmployeeSurveyQuestionnaireInput { SurveyId = SurveyId }));
    }
}
