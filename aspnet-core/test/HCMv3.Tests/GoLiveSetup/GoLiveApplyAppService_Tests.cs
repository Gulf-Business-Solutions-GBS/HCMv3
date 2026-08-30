using System.Linq;
using Abp.Threading;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

// ReSharper disable once InconsistentNaming
public class GoLiveApplyAppService_Tests : AppTestBase
{
    private readonly IGoLiveApplyAppService _apply;
    private readonly IGoLiveStateAppService _state;

    public GoLiveApplyAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _apply = Resolve<IGoLiveApplyAppService>();
        _state = Resolve<IGoLiveStateAppService>();

        // Apply resolves nationality by name, so the tenant needs the reference pack.
        AsyncHelper.RunSync(() => Resolve<IReferenceDataSeeder>().SeedPackAsync("universal", AbpSession.TenantId ?? 1));
    }

    /// <summary>A well-formed employee row as the grid would have stored it.</summary>
    private static string Row(
        string code,
        string nameEn = "Ahmed Ali",
        string nameAr = "أحمد علي",
        string nationality = "Egyptian",
        string birthDate = "1984-01-27",
        string joiningDate = "2019-03-01",
        string civilId = "284012703579",
        string mobile = "99709947",
        string active = "Yes")
    {
        return $@"{{""code"":""{code}"",""nameEn"":""{nameEn}"",""nameAr"":""{nameAr}"",""gender"":""Male"",
                   ""birthDate"":""{birthDate}"",""nationality"":""{nationality}"",""civilId"":""{civilId}"",
                   ""mobile"":""{mobile}"",""joiningDate"":""{joiningDate}"",""active"":""{active}""}}";
    }

    private Task SaveEmployees(params string[] rows) =>
        _state.SaveStep(new SaveGoLiveStepInput
        {
            StepId = "S3.1",
            Payload = "{\"rows\":[" + string.Join(",", rows) + "]}",
        });

    private int EmployeeCount() => UsingDbContext(context => context.Employees.Count());

    /// <summary>
    /// Apply runs several stages now, and two of them read S3.1 -- the employee itself and its org
    /// assignment -- so the target is what identifies a stage, not the step it reads.
    /// </summary>
    private static GoLiveApplyStageDto EmployeeStage(GoLiveApplyResultDto result) =>
        result.Stages.Single(s => s.Target == nameof(Employee));

    // ---------------------------------------------------------------- nothing to do

    [Fact]
    public async Task Should_Do_Nothing_When_No_Draft_Was_Saved()
    {
        var result = await _apply.Apply();

        result.TotalCreated.ShouldBe(0);
        result.TotalFailed.ShouldBe(0);
        result.IsClean.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Ignore_Blank_Padding_Rows()
    {
        await SaveEmployees(Row("1001"), "{\"code\":null,\"nameEn\":null}");

        var result = await _apply.Apply();

        EmployeeStage(result).Created.ShouldBe(1);
        result.TotalFailed.ShouldBe(0);
    }

    // ---------------------------------------------------------------- preview

    [Fact]
    public async Task Should_Write_Nothing_On_Preview()
    {
        await SaveEmployees(Row("1001"), Row("1002"));
        var before = EmployeeCount();

        var result = await _apply.Preview();

        result.Applied.ShouldBeFalse();
        EmployeeStage(result).Created.ShouldBe(2);
        EmployeeCount().ShouldBe(before);
    }

    [Fact]
    public async Task Should_Report_The_Same_Numbers_On_Preview_And_Apply()
    {
        await SaveEmployees(Row("1001"), Row("1002"), Row("1003"));

        var preview = await _apply.Preview();
        var applied = await _apply.Apply();

        applied.TotalCreated.ShouldBe(preview.TotalCreated);
        applied.TotalFailed.ShouldBe(preview.TotalFailed);
    }

    // ---------------------------------------------------------------- creating

    [Fact]
    public async Task Should_Create_Employees_From_The_Draft()
    {
        await SaveEmployees(Row("1001"), Row("1002", nameEn: "Bader Naser"));

        var result = await _apply.Apply();

        EmployeeStage(result).Created.ShouldBe(2);
        result.IsClean.ShouldBeTrue();

        UsingDbContext(context =>
        {
            var employee = context.Employees.Single(e => e.Code == "1001");
            employee.FirstNameEn.ShouldBe("Ahmed");
            employee.LastNameEn.ShouldBe("Ali");
            employee.Status.ShouldBe(EmployeeStatus.Active);
            employee.Mobile.ShouldBe(99709947);
        });
    }

    [Fact]
    public async Task Should_Resolve_Nationality_By_Name()
    {
        await SaveEmployees(Row("1001", nationality: "Egyptian"));

        await _apply.Apply();

        UsingDbContext(context =>
        {
            var employee = context.Employees.Single(e => e.Code == "1001");
            var egyptian = context.Nationalities.Single(n => n.NameEn == "Egyptian");
            employee.NationalityId.ShouldBe(egyptian.Id);
        });
    }

    [Fact]
    public async Task Should_Mark_A_Leaver_Inactive()
    {
        await SaveEmployees(Row("1001", active: "No"));

        await _apply.Apply();

        UsingDbContext(context =>
            context.Employees.Single(e => e.Code == "1001").Status.ShouldBe(EmployeeStatus.Inactive));
    }

    // ---------------------------------------------------------------- idempotency

    [Fact]
    public async Task Should_Update_Rather_Than_Duplicate_On_A_Second_Run()
    {
        await SaveEmployees(Row("1001"));
        await _apply.Apply();

        var second = await _apply.Apply();

        EmployeeStage(second).Created.ShouldBe(0);
        EmployeeStage(second).Updated.ShouldBe(1);
        EmployeeCount().ShouldBe(1);
    }

    [Fact]
    public async Task Should_Carry_An_Edit_Through_On_A_Re_Run()
    {
        await SaveEmployees(Row("1001", nameEn: "Ahmed Ali"));
        await _apply.Apply();

        await SaveEmployees(Row("1001", nameEn: "Ahmed Hassan Ali"));
        await _apply.Apply();

        UsingDbContext(context =>
        {
            var employee = context.Employees.Single(e => e.Code == "1001");
            employee.LastNameEn.ShouldBe("Hassan Ali");
        });
    }

    // ---------------------------------------------------------------- per-row failures

    [Fact]
    public async Task Should_Report_A_Bad_Row_And_Keep_Going()
    {
        await SaveEmployees(Row("1001"), Row("1002", nationality: "Atlantean"), Row("1003"));

        var result = await _apply.Apply();

        EmployeeStage(result).Created.ShouldBe(2);
        result.TotalFailed.ShouldBe(1);
        result.IsClean.ShouldBeFalse();

        var issue = EmployeeStage(result).Issues.Single();
        issue.Row.ShouldBe(2);
        issue.Key.ShouldBe("1002");
        issue.Message.ShouldContain("Atlantean");

        EmployeeCount().ShouldBe(2);
    }

    [Fact]
    public async Task Should_Reject_A_Duplicate_Code_Within_The_Same_Sheet()
    {
        await SaveEmployees(Row("1001"), Row("1001", nameEn: "Someone Else"));

        var result = await _apply.Apply();

        EmployeeStage(result).Created.ShouldBe(1);
        result.TotalFailed.ShouldBe(1);
        EmployeeStage(result).Issues.Single().Message.ShouldContain("more than once");
    }

    [Fact]
    public async Task Should_Report_A_Missing_Employee_Code()
    {
        await SaveEmployees("{\"nameEn\":\"No Code\",\"nationality\":\"Egyptian\"}");

        var result = await _apply.Apply();

        result.TotalFailed.ShouldBe(1);
        EmployeeStage(result).Issues.Single().Message.ShouldContain("code is missing");
    }

    [Fact]
    public async Task Should_Report_A_Missing_Required_Field()
    {
        await SaveEmployees("{\"code\":\"1001\",\"nationality\":\"Egyptian\"}");

        var result = await _apply.Apply();

        result.TotalFailed.ShouldBe(1);
        EmployeeStage(result).Issues.Single().Message.ShouldContain("English name");
    }

    [Fact]
    public async Task Should_Report_An_Unparseable_Date()
    {
        await SaveEmployees(Row("1001", birthDate: "not a date"));

        var result = await _apply.Apply();

        result.TotalFailed.ShouldBe(1);
        EmployeeStage(result).Issues.Single().Message.ShouldContain("Date of birth");
    }

    [Fact]
    public async Task Should_Surface_The_Application_Services_Own_Validation()
    {
        // Joining date before birth date is a rule the employee app service owns, not this stage.
        await SaveEmployees(Row("1001", birthDate: "1998-07-03", joiningDate: "1995-01-01"));

        var result = await _apply.Apply();

        result.TotalFailed.ShouldBe(1);
        EmployeeStage(result).Issues.ShouldNotBeEmpty();
        EmployeeCount().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Strip_Punctuation_From_A_Mobile_Number()
    {
        await SaveEmployees(Row("1001", mobile: "+965 9970-9947"));

        await _apply.Apply();

        UsingDbContext(context =>
            context.Employees.Single(e => e.Code == "1001").Mobile.ShouldBe(96599709947));
    }

    [Fact]
    public async Task Should_Survive_A_Payload_It_Cannot_Parse()
    {
        await _state.SaveStep(new SaveGoLiveStepInput { StepId = "S3.1", Payload = "not json" });

        var result = await _apply.Apply();

        result.TotalCreated.ShouldBe(0);
        result.TotalFailed.ShouldBe(0);
    }
}
