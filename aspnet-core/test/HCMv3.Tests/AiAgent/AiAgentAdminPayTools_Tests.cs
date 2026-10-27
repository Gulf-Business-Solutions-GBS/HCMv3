using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.AiAgent;
using HCMv3.AiAgent.Actions;
using HCMv3.AiAgent.Dto;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// The administrator's pay tools, end to end through the real admin services: "add 50 KD car allowance to
/// 12001 from 1 October", "50 KD traffic violation deduction", "100 KD bonus". As with leave, nothing is
/// written until Confirm -- and a basic pay change must end the old record, or payroll would pick between two
/// overlapping records at random.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AiAgentAdminPayTools_Tests : AiAgentTestBase
{
    private static readonly DateTime January = new(2026, 1, 1);
    private static readonly DateTime October = new(2026, 10, 1);

    private int _employeeId;
    private int _kwdId;
    private int _basicSalaryId;
    private int _housingId;
    private int _carAllowanceId;
    private int _bonusId;
    private int _trafficViolationId;
    private int _basicPayId;

    public AiAgentAdminPayTools_Tests()
    {
        _employeeId = CreateEmployee("12001");
        _kwdId = CreateCurrency("KWD");

        _basicSalaryId = CreateWageType("1000", "Basic Salary", "الراتب الأساسي", BillingType.Earning, inBasicPay: true);
        _housingId = CreateWageType("1010", "Housing Allowance", "بدل سكن", BillingType.Earning, inBasicPay: true);
        _carAllowanceId = CreateWageType("1020", "Car Allowance", "بدل سيارة", BillingType.Earning, inBasicPay: true);
        CreateWageType("1030", "Old Allowance", "بدل قديم", BillingType.Earning, inBasicPay: true, active: false);
        _bonusId = CreateWageType("2000", "Bonus", "مكافأة", BillingType.Earning, inBasicPay: false);
        _trafficViolationId = CreateWageType("3000", "Traffic Violation", "مخالفة مرورية", BillingType.Deduction, inBasicPay: false);

        _basicPayId = CreateBasicPay(January, (_basicSalaryId, 800), (_housingId, 150));
    }

    private async Task<SendAiMessageOutput> Ask(string tool, string argumentsJson, string message = "please")
    {
        Model.CallsTool(tool, argumentsJson).RepliesWith("Please confirm below.");
        return await Service.SendMessage(new SendAiMessageInput { Message = message });
    }

    private string LastToolResult() => Model.Requests.Last().Messages.Last().Content;

    private static string Change(string wageType, double? amount = null, string change = null) =>
        "{" + $@"""wageType"":""{wageType}""" + (amount.HasValue ? $@",""amount"":{amount}" : "") + (change != null ? $@",""change"":""{change}""" : "") + "}";

    private Task<SendAiMessageOutput> AskForBasicPayChange(DateTime effective, params string[] changes) =>
        Ask("prepare_basic_pay_change",
            $@"{{""employeeCode"":""12001"",""effectiveDate"":""{effective:yyyy-MM-dd}"",""changes"":[{string.Join(",", changes)}]}}",
            "add 50kd car allowance in basic pays to employee 12001 starting 01/10/2026");

    // ------------------------------------------------------------------ basic pay: drafting

    [Fact]
    public async Task Should_Draft_A_New_Record_That_Copies_Every_Line_And_Adds_The_Allowance()
    {
        var output = await AskForBasicPayChange(October, Change("car allowance", 50));

        LastToolResult().ShouldContain("draft_ready");
        LastToolResult().ShouldContain(@"""currentRecordEnds"":""2026-09-30""");

        var card = output.AssistantMessage.PendingAction;
        card.ActionType.ShouldBe(ChangeBasicPayAiPendingActionHandler.Type);
        card.Title.ShouldBe("Change basic pay");
        Detail(card, "Employee").ShouldStartWith("12001 · ");
        Detail(card, "Current record ends").ShouldBe("30 Sep 2026");
        Detail(card, "Basic Salary").ShouldBe("800.000");
        Detail(card, "Housing Allowance").ShouldBe("150.000");
        Detail(card, "Car Allowance").ShouldBe("— → 50.000");
        Detail(card, "Total").ShouldBe("950.000 → 1,000.000");

        // Nothing written yet.
        UsingDbContext(context =>
        {
            context.Set<EmployeeBasicPay>().Count(b => b.EmployeeId == _employeeId).ShouldBe(1);
            context.Set<EmployeeBasicPayDetail>().Count(d => d.EmployeeBasicPayId == _basicPayId).ShouldBe(2);
        });
    }

    [Fact]
    public async Task Should_Ask_Whether_To_Increase_Or_Replace_An_Allowance_The_Employee_Already_Has()
    {
        var output = await AskForBasicPayChange(October, Change("housing allowance", 50));

        LastToolResult().ShouldContain("already has Housing Allowance of 150.000");
        LastToolResult().ShouldContain("increase it by 50.000 (to 200.000) or set it to 50.000");
        output.AssistantMessage.PendingAction.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Increase_Decrease_And_Remove_Lines()
    {
        var output = await AskForBasicPayChange(October,
            Change("Housing Allowance", 25, "increase_by"),
            Change("1000", 100, "decrease_by"),
            Change("car allowance", 40, "set_to"));

        var card = output.AssistantMessage.PendingAction;
        Detail(card, "Housing Allowance").ShouldBe("150.000 → 175.000");
        Detail(card, "Basic Salary").ShouldBe("800.000 → 700.000");
        Detail(card, "Car Allowance").ShouldBe("— → 40.000");

        var removal = await AskForBasicPayChange(October, Change("housing", change: "remove"));
        Detail(removal.AssistantMessage.PendingAction, "Housing Allowance").ShouldBe("150.000 → —");
    }

    [Fact]
    public async Task Should_Refuse_A_Wage_Type_That_Is_Not_Allowed_In_Basic_Pay()
    {
        var output = await AskForBasicPayChange(October, Change("Bonus", 100));

        LastToolResult().ShouldContain("not_possible");
        LastToolResult().ShouldContain("'Bonus' is not a basic pay wage type");
        output.AssistantMessage.PendingAction.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_An_Inactive_Wage_Type()
    {
        await AskForBasicPayChange(October, Change("Old Allowance", 20));

        LastToolResult().ShouldContain("'Old Allowance' is not a basic pay wage type");
    }

    [Fact]
    public async Task Should_Ask_When_The_Wage_Type_Is_Ambiguous()
    {
        await AskForBasicPayChange(October, Change("allowance", 20, "set_to"));

        LastToolResult().ShouldContain("matches more than one wage type");
        LastToolResult().ShouldContain("Car Allowance");
        LastToolResult().ShouldContain("Housing Allowance");
    }

    [Fact]
    public async Task Should_Ask_When_The_Employee_Code_Does_Not_Exist()
    {
        Model.CallsTool("prepare_basic_pay_change",
            $@"{{""employeeCode"":""99999"",""effectiveDate"":""2026-10-01"",""changes"":[{Change("car allowance", 50)}]}}").RepliesWith("Which employee?");

        await Service.SendMessage(new SendAiMessageInput { Message = "add car allowance to 99999" });

        LastToolResult().ShouldContain("No employee has the code '99999'");
    }

    [Fact]
    public async Task Should_Ask_For_The_Start_Date_When_It_Was_Not_Given()
    {
        await Ask("prepare_basic_pay_change", $@"{{""employeeCode"":""12001"",""changes"":[{Change("car allowance", 50)}]}}");

        LastToolResult().ShouldContain("the date the change starts");
    }

    [Fact]
    public async Task Should_Accept_A_Single_Change_Sent_Without_The_Array()
    {
        // What small models actually do.
        var output = await Ask("prepare_basic_pay_change",
            @"{""employeeCode"":""12001"",""effectiveDate"":""01/10/2026"",""wageType"":""Car Allowance"",""amount"":""50 KD""}");

        Detail(output.AssistantMessage.PendingAction, "Car Allowance").ShouldBe("— → 50.000");
    }

    [Fact]
    public async Task Should_Refuse_A_Change_Before_A_Later_Record()
    {
        CreateBasicPay(new DateTime(2026, 12, 1), (_basicSalaryId, 900));

        await AskForBasicPayChange(October, Change("car allowance", 50));

        LastToolResult().ShouldContain("already has a basic pay record starting 2026-12-01");
    }

    [Fact]
    public async Task Should_Refuse_Without_Every_Permission_Confirming_Would_Need()
    {
        AbpSession.UserId = CreateUser(AppPermissions.Ess, AppPermissions.Ess_AiAgent,
            AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Create);

        await AskForBasicPayChange(October, Change("car allowance", 50));

        LastToolResult().ShouldContain("does not have the 'Pages.PersonnelAdministration.EmployeeBasicPays.Edit' permission");
    }

    // ------------------------------------------------------------------ basic pay: confirming

    [Fact]
    public async Task Confirming_Should_End_The_Old_Record_And_Open_A_New_One_With_Every_Line()
    {
        var output = await AskForBasicPayChange(October, Change("car allowance", 50));

        var result = await Service.ConfirmAction(new EntityDto(output.AssistantMessage.PendingAction.Id));

        result.Action.Status.ShouldBe(AiPendingActionStatus.Completed, result.Action.ResultMessage);

        UsingDbContext(context =>
        {
            var records = context.Set<EmployeeBasicPay>().Where(b => b.EmployeeId == _employeeId).OrderBy(b => b.StartDate).ToList();
            records.Count.ShouldBe(2);

            records[0].EndDate.ShouldBe(new DateTime(2026, 9, 30));
            records[1].StartDate.ShouldBe(October);
            records[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
            records[1].Amount.ShouldBe(1000);

            var lines = context.Set<EmployeeBasicPayDetail>().Where(d => d.EmployeeBasicPayId == records[1].Id).ToList();
            lines.Select(l => (l.WageTypeId, l.Amount)).ShouldBe(new[]
            {
                (_basicSalaryId, 800d), (_housingId, 150d), (_carAllowanceId, 50d)
            }, ignoreOrder: true);
            lines.ShouldAllBe(l => l.CurrencyId == _kwdId);

            // The old record's own lines are untouched.
            context.Set<EmployeeBasicPayDetail>().Count(d => d.EmployeeBasicPayId == records[0].Id).ShouldBe(2);
        });
    }

    [Fact]
    public async Task Confirming_Should_Edit_In_Place_When_A_Record_Already_Starts_That_Day()
    {
        var output = await AskForBasicPayChange(January, Change("car allowance", 50), Change("housing allowance", change: "remove"));
        Detail(output.AssistantMessage.PendingAction, "Updates the record starting").ShouldBe("1 Jan 2026");

        var result = await Service.ConfirmAction(new EntityDto(output.AssistantMessage.PendingAction.Id));

        result.Action.Status.ShouldBe(AiPendingActionStatus.Completed, result.Action.ResultMessage);
        UsingDbContext(context =>
        {
            context.Set<EmployeeBasicPay>().Count(b => b.EmployeeId == _employeeId).ShouldBe(1);
            context.Set<EmployeeBasicPayDetail>().Where(d => d.EmployeeBasicPayId == _basicPayId && !d.IsDeleted)
                .Select(d => d.WageTypeId).ToList().ShouldBe(new[] { _basicSalaryId, _carAllowanceId }, ignoreOrder: true);
        });
    }

    [Fact]
    public async Task Confirming_Should_Change_Nothing_If_Basic_Pay_Was_Edited_After_The_Draft()
    {
        var output = await AskForBasicPayChange(October, Change("car allowance", 50));

        // Someone raises housing on the Basic Pay screen before the card is confirmed.
        UsingDbContext(context =>
        {
            context.Set<EmployeeBasicPayDetail>().Single(d => d.EmployeeBasicPayId == _basicPayId && d.WageTypeId == _housingId).Amount = 200;
        });

        var result = await Service.ConfirmAction(new EntityDto(output.AssistantMessage.PendingAction.Id));

        result.Action.Status.ShouldBe(AiPendingActionStatus.Failed);
        result.Action.ResultMessage.ShouldContain("changed after this draft");

        UsingDbContext(context =>
        {
            var records = context.Set<EmployeeBasicPay>().Where(b => b.EmployeeId == _employeeId).ToList();
            records.Count.ShouldBe(1);
            records[0].EndDate.ShouldBe(new DateTime(9999, 12, 31)); // not ended: all or nothing
        });
    }

    // ------------------------------------------------------------------ one-time payments

    [Fact]
    public async Task Should_Draft_And_Record_A_Traffic_Violation_Deduction_Only_On_Confirm()
    {
        var output = await Ask("prepare_one_time_payment",
            @"{""employeeCode"":""12001"",""wageType"":""traffic violation deduction"",""amount"":50,""paymentDate"":""2026-10-15"",""comments"":""Traffic violation""}",
            "add 50kd deduction to employee 12001 as traffic violation deduction");

        var card = output.AssistantMessage.PendingAction;
        card.Title.ShouldBe("One-time deduction");
        Detail(card, "Wage type").ShouldBe("3000 · Traffic Violation");
        Detail(card, "Amount").ShouldBe("50.000 KWD");
        UsingDbContext(context => context.Set<EmployeePayment>().Count(p => p.EmployeeId == _employeeId).ShouldBe(0));

        var result = await Service.ConfirmAction(new EntityDto(card.Id));

        result.Action.Status.ShouldBe(AiPendingActionStatus.Completed, result.Action.ResultMessage);
        result.Message.Content.ShouldBe("Done. The deduction for employee 12001 has been recorded.");
        UsingDbContext(context =>
        {
            var payment = context.Set<EmployeePayment>().Single(p => p.EmployeeId == _employeeId);
            payment.WageTypeId.ShouldBe(_trafficViolationId);
            payment.Amount.ShouldBe(50);
            payment.PaymentDate.ShouldBe(new DateTime(2026, 10, 15));
            payment.UnitType.ShouldBe(UnitType.Amount);
            payment.CurrencyId.ShouldBe(_kwdId);
            payment.Comments.ShouldBe("Traffic violation");
        });
    }

    [Fact]
    public async Task A_Bonus_Should_Be_Described_As_A_Payment()
    {
        var output = await Ask("prepare_one_time_payment",
            @"{""employeeCode"":""12001"",""wageType"":""bonus"",""amount"":100,""paymentDate"":""2026-10-31""}");

        output.AssistantMessage.PendingAction.Title.ShouldBe("One-time payment");
    }

    [Fact]
    public async Task Should_Ask_For_What_Is_Missing_From_A_Payment()
    {
        await Ask("prepare_one_time_payment", @"{""employeeCode"":""12001"",""amount"":100}");

        LastToolResult().ShouldContain("the wage type");
        LastToolResult().ShouldContain("the payment date");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task Should_Refuse_An_Amount_That_Is_Not_Above_Zero(double amount)
    {
        var output = await Ask("prepare_one_time_payment",
            $@"{{""employeeCode"":""12001"",""wageType"":""Traffic Violation"",""amount"":{amount},""paymentDate"":""2026-10-15""}}");

        LastToolResult().ShouldContain("must be above zero");
        output.AssistantMessage.PendingAction.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Point_Out_A_Possible_Duplicate_Payment()
    {
        UsingDbContext(context => context.Set<EmployeePayment>().Add(new EmployeePayment
        {
            TenantId = TenantId,
            EmployeeId = _employeeId,
            WageTypeId = _bonusId,
            Amount = 100,
            PaymentDate = new DateTime(2026, 10, 31)
        }));

        await Ask("prepare_one_time_payment", @"{""employeeCode"":""12001"",""wageType"":""Bonus"",""amount"":100,""paymentDate"":""2026-10-31""}");

        LastToolResult().ShouldContain("possibleDuplicate");
    }

    // ------------------------------------------------------------------ who gets the tools

    [Fact]
    public async Task An_Administrator_Should_Get_The_Admin_Tools_And_Be_Told_So()
    {
        Model.RepliesWith("hi");
        await Service.SendMessage(new SendAiMessageInput { Message = "hi" });

        var request = Model.Requests.Single();
        request.Tools.Select(t => t.Name).ShouldContain("prepare_basic_pay_change");
        request.Tools.Select(t => t.Name).ShouldContain("prepare_one_time_payment");
        request.Tools.Select(t => t.Name).ShouldContain("find_employee");
        request.Messages[0].Content.ShouldContain("HR/payroll administrator");
    }

    [Fact]
    public async Task An_Employee_Should_Not_Get_The_Admin_Tools()
    {
        AbpSession.UserId = CreateUser(AppPermissions.Ess, AppPermissions.Ess_AiAgent, AppPermissions.Ess_MyLeave);
        Model.RepliesWith("hi");

        await Service.SendMessage(new SendAiMessageInput { Message = "add 100kd bonus to 12001" });

        var request = Model.Requests.Single();
        request.Tools.Select(t => t.Name).ShouldNotContain("prepare_basic_pay_change");
        request.Tools.Select(t => t.Name).ShouldNotContain("prepare_one_time_payment");
        request.Tools.Select(t => t.Name).ShouldNotContain("find_employee");
        request.Messages[0].Content.ShouldContain("Refuse any other request about other employees");
    }

    // ------------------------------------------------------------------ helpers

    private static string Detail(AiPendingActionDto card, string label) =>
        card.Details.SingleOrDefault(d => d.Label == label)?.Value
        ?? throw new ShouldAssertException($"No '{label}' on the card. It has: {string.Join(", ", card.Details.Select(d => d.Label + "=" + d.Value))}");

    private int CreateCurrency(string code)
    {
        Currency currency = null;
        UsingDbContext(context =>
        {
            currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "Kuwaiti Dinar",
                NameAr = "دينار كويتي",
                DecimalCount = 3
            }).Entity;
        });
        return currency.Id;
    }

    private int CreateWageType(string code, string nameEn, string nameAr, BillingType billingType, bool inBasicPay, bool active = true)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = nameEn,
                NameAr = nameAr,
                BillingType = billingType,
                IncludeInBasicPay = inBasicPay,
                Status = active
            }).Entity;
        });
        return wageType.Id;
    }

    private int CreateBasicPay(DateTime start, params (int WageTypeId, double Amount)[] lines)
    {
        EmployeeBasicPay record = null;
        UsingDbContext(context =>
        {
            record = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = _employeeId,
                StartDate = start,
                EndDate = new DateTime(9999, 12, 31),
                Amount = lines.Sum(l => l.Amount),
                PayType = PayType.Regular,
                PayInterval = PayInterval.Monthly
            }).Entity;
        });

        UsingDbContext(context =>
        {
            foreach (var (wageTypeId, amount) in lines)
            {
                context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
                {
                    TenantId = TenantId,
                    EmployeeBasicPayId = record.Id,
                    WageTypeId = wageTypeId,
                    Amount = amount,
                    CurrencyId = _kwdId
                });
            }
        });

        return record.Id;
    }
}
