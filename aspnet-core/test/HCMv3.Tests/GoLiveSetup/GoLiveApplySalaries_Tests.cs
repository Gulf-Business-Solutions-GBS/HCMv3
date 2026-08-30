using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// The S1.3 wage type and S3.2 salary stages -- the chain that turns a wide salary sheet into a basic
/// pay header with one detail row per pay element.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveApplySalaries_Tests : AppTestBase
{
    private readonly IGoLiveApplyAppService _apply;
    private readonly IGoLiveStateAppService _state;

    public GoLiveApplySalaries_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _apply = Resolve<IGoLiveApplyAppService>();
        _state = Resolve<IGoLiveStateAppService>();

        AsyncHelper.RunSync(() =>
            Resolve<IReferenceDataSeeder>().SeedPackAsync("universal", AbpSession.TenantId ?? 1));
    }

    private Task SaveStep(string stepId, params string[] rows) =>
        _state.SaveStep(new SaveGoLiveStepInput
        {
            StepId = stepId,
            Payload = "{\"rows\":[" + string.Join(",", rows) + "]}",
        });

    private Task SaveAnswers(string stepId, string answersJson) =>
        _state.SaveStep(new SaveGoLiveStepInput { StepId = stepId, Payload = "{\"answers\":" + answersJson + "}" });

    private Task SaveEmployee(string code) =>
        SaveStep(
            "S3.1",
            $@"{{""code"":""{code}"",""nameEn"":""Ahmed Ali"",""nameAr"":""أحمد علي"",""gender"":""Male"",
                ""birthDate"":""1984-01-27"",""nationality"":""Egyptian"",""civilId"":""284012703579"",
                ""mobile"":""99709947"",""joiningDate"":""2019-03-01"",""active"":""Yes""}}");

    private static string PayElement(string name, string kind = "Earning", string basicPay = "Yes",
        string indemnity = "Yes", string leaveSalary = "Yes") =>
        $@"{{""name"":""{name}"",""nameAr"":""بند"",""kind"":""{kind}"",""basicPay"":""{basicPay}"",
             ""indemnity"":""{indemnity}"",""leaveSalary"":""{leaveSalary}""}}";

    private Task SaveStandardPayElements() =>
        SaveStep("S1.3", PayElement("Basic Salary"), PayElement("Housing Allowance"));

    private GoLiveApplyStageDto Stage(GoLiveApplyResultDto result, string stepId) =>
        result.Stages.Single(s => s.StepId == stepId);

    // ---------------------------------------------------------------- wage types

    [Fact]
    public async Task Should_Create_A_Wage_Type_Per_Pay_Element()
    {
        await SaveStandardPayElements();

        var result = await _apply.Apply();

        Stage(result, "S1.3").Created.ShouldBe(2);
        result.IsClean.ShouldBeTrue();

        UsingDbContext(context =>
        {
            var basic = context.WageTypes.Single(w => w.NameEn == "Basic Salary");
            basic.BillingType.ShouldBe(BillingType.Earning);
            basic.IncludeInIndemnity.ShouldBeTrue();
            basic.IncludeInBasicPay.ShouldBeTrue();
            basic.Status.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_Create_A_Deduction()
    {
        await SaveStep("S1.3", PayElement("Loan Deduction", kind: "Deduction", indemnity: "No", leaveSalary: "No"));

        await _apply.Apply();

        UsingDbContext(context =>
        {
            var wageType = context.WageTypes.Single(w => w.NameEn == "Loan Deduction");
            wageType.BillingType.ShouldBe(BillingType.Deduction);
            wageType.IncludeInIndemnity.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Should_Reject_A_Pay_Element_That_Is_Neither_Earning_Nor_Deduction()
    {
        await SaveStep("S1.3", PayElement("Something", kind: "Provision"));

        var result = await _apply.Apply();

        Stage(result, "S1.3").Failed.ShouldBe(1);
        Stage(result, "S1.3").Issues.Single().Message.ShouldContain("earning or a deduction");
    }

    [Fact]
    public async Task Should_Reject_A_Repeated_Pay_Element()
    {
        await SaveStep("S1.3", PayElement("Basic Salary"), PayElement("Basic Salary"));

        var result = await _apply.Apply();

        Stage(result, "S1.3").Created.ShouldBe(1);
        Stage(result, "S1.3").Failed.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Update_A_Wage_Type_Rather_Than_Duplicating_It()
    {
        await SaveStandardPayElements();
        await _apply.Apply();

        var second = await _apply.Apply();

        Stage(second, "S1.3").Created.ShouldBe(0);
        Stage(second, "S1.3").Updated.ShouldBe(2);
        UsingDbContext(context => context.WageTypes.Count(w => w.NameEn == "Basic Salary").ShouldBe(1));
    }

    // ---------------------------------------------------------------- Q2.04 becomes a setting

    [Fact]
    public async Task Should_Turn_The_Part_Month_Answer_Into_A_Billing_Method()
    {
        await SaveAnswers("S1.2", "{\"Q2.04\":\"Fixed30\"}");
        await SaveStandardPayElements();

        await _apply.Apply();

        UsingDbContext(context =>
        {
            var basic = context.WageTypes.Single(w => w.NameEn == "Basic Salary");
            basic.BillingMethod.ShouldBe(BillingMethod.FixedDaysPerMonth);
            basic.FixedDays.ShouldBe(30);
        });
    }

    [Fact]
    public async Task Should_Map_Each_Part_Month_Option_To_Its_Own_Billing_Method()
    {
        await SaveAnswers("S1.2", "{\"Q2.04\":\"WorkingDays\"}");
        await SaveStandardPayElements();

        await _apply.Apply();

        UsingDbContext(context =>
            context.WageTypes.Single(w => w.NameEn == "Basic Salary")
                .BillingMethod.ShouldBe(BillingMethod.WorkingDaysPerMonth));
    }

    [Fact]
    public async Task Should_Default_To_A_Fixed_Monthly_Amount_When_The_Question_Is_Unanswered()
    {
        await SaveStandardPayElements();

        await _apply.Apply();

        UsingDbContext(context =>
            context.WageTypes.Single(w => w.NameEn == "Basic Salary")
                .BillingMethod.ShouldBe(BillingMethod.FixedAmountPerMonth));
    }

    // ---------------------------------------------------------------- salaries

    [Fact]
    public async Task Should_Unpivot_A_Wide_Salary_Row_Into_Detail_Rows()
    {
        await SaveAnswers("S1.1", "{\"Q1.04\":\"Kuwaiti Dinar\"}");
        await SaveStandardPayElements();
        await SaveEmployee("1001");
        await SaveStep("S3.2", @"{""code"":""1001"",""basic"":650,""housing"":200}");

        var result = await _apply.Apply();

        result.IsClean.ShouldBeTrue();
        Stage(result, "S3.2").Created.ShouldBe(1);

        UsingDbContext(context =>
        {
            context.EmployeeBasicPays.Count().ShouldBe(1);
            var details = context.EmployeeBasicPayDetails.ToList();
            details.Count.ShouldBe(2);
            details.Select(d => d.Amount).OrderBy(a => a).ShouldBe(new[] { 200d, 650d });
        });
    }

    [Fact]
    public async Task Should_Attach_Each_Amount_To_The_Wage_Type_Its_Column_Names()
    {
        await SaveAnswers("S1.1", "{\"Q1.04\":\"Kuwaiti Dinar\"}");
        await SaveStandardPayElements();
        await SaveEmployee("1001");
        await SaveStep("S3.2", @"{""code"":""1001"",""basic"":650,""housing"":200}");

        await _apply.Apply();

        UsingDbContext(context =>
        {
            var housing = context.WageTypes.Single(w => w.NameEn == "Housing Allowance");
            var detail = context.EmployeeBasicPayDetails.Single(d => d.WageTypeId == housing.Id);
            detail.Amount.ShouldBe(200);
        });
    }

    [Fact]
    public async Task Should_Report_An_Amount_In_A_Column_With_No_Matching_Pay_Element()
    {
        await SaveAnswers("S1.1", "{\"Q1.04\":\"Kuwaiti Dinar\"}");
        // Only Basic Salary is declared, but the row also pays a transport allowance.
        await SaveStep("S1.3", PayElement("Basic Salary"));
        await SaveEmployee("1001");
        await SaveStep("S3.2", @"{""code"":""1001"",""basic"":650,""transport"":50}");

        var result = await _apply.Apply();

        Stage(result, "S3.2").Failed.ShouldBe(1);
        Stage(result, "S3.2").Issues.Single().Message.ShouldContain("Transport Allowance");
        UsingDbContext(context => context.EmployeeBasicPays.ShouldBeEmpty());
    }

    [Fact]
    public async Task Should_Skip_A_Salary_Row_With_No_Figures()
    {
        await SaveStandardPayElements();
        await SaveEmployee("1001");
        await SaveStep("S3.2", @"{""code"":""1001"",""name"":""Ahmed Ali""}");

        var result = await _apply.Apply();

        Stage(result, "S3.2").Skipped.ShouldBe(1);
        Stage(result, "S3.2").Failed.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Ignore_A_Zero_Amount()
    {
        await SaveAnswers("S1.1", "{\"Q1.04\":\"Kuwaiti Dinar\"}");
        await SaveStandardPayElements();
        await SaveEmployee("1001");
        await SaveStep("S3.2", @"{""code"":""1001"",""basic"":650,""housing"":0}");

        await _apply.Apply();

        UsingDbContext(context => context.EmployeeBasicPayDetails.Count().ShouldBe(1));
    }

    [Fact]
    public async Task Should_Run_Wage_Types_Before_Salaries_In_One_Pass()
    {
        await SaveAnswers("S1.1", "{\"Q1.04\":\"Kuwaiti Dinar\"}");
        await SaveStandardPayElements();
        await SaveEmployee("1001");
        await SaveStep("S3.2", @"{""code"":""1001"",""basic"":650}");

        // Nothing exists beforehand, so this only passes if wage types and employees both run first.
        var result = await _apply.Apply();

        result.IsClean.ShouldBeTrue();
        result.Stages.Select(s => s.StepId).ShouldBe(new[] { "S1.3", "S3.1", "S3.2", "S3.3", "S3.4" });
    }

    [Fact]
    public async Task Should_Write_Nothing_On_Preview()
    {
        await SaveAnswers("S1.1", "{\"Q1.04\":\"Kuwaiti Dinar\"}");
        await SaveStandardPayElements();
        await SaveEmployee("1001");
        await SaveStep("S3.2", @"{""code"":""1001"",""basic"":650}");

        var preview = await _apply.Preview();

        preview.TotalCreated.ShouldBe(4);
        UsingDbContext(context =>
        {
            context.WageTypes.ShouldBeEmpty();
            context.EmployeeBasicPays.ShouldBeEmpty();
        });
    }
}
