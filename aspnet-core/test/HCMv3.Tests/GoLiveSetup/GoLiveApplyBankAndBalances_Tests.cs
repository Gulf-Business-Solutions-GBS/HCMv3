using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// The S3.3 bank and S3.4 opening-balance stages. Both key off an employee, so every test applies the
/// employee sheet first -- which is also the ordering the stage list guarantees.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveApplyBankAndBalances_Tests : AppTestBase
{
    private readonly IGoLiveApplyAppService _apply;
    private readonly IGoLiveStateAppService _state;

    public GoLiveApplyBankAndBalances_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _apply = Resolve<IGoLiveApplyAppService>();
        _state = Resolve<IGoLiveStateAppService>();

        var seeder = Resolve<IReferenceDataSeeder>();
        var tenantId = AbpSession.TenantId ?? 1;
        AsyncHelper.RunSync(() => seeder.SeedPackAsync("universal", tenantId));
        AsyncHelper.RunSync(() => seeder.SeedPackAsync("kuwait", tenantId));
    }

    private Task SaveStep(string stepId, params string[] rows) =>
        _state.SaveStep(new SaveGoLiveStepInput
        {
            StepId = stepId,
            Payload = "{\"rows\":[" + string.Join(",", rows) + "]}",
        });

    private Task SaveEmployee(string code) =>
        SaveStep(
            "S3.1",
            $@"{{""code"":""{code}"",""nameEn"":""Ahmed Ali"",""nameAr"":""أحمد علي"",""gender"":""Male"",
                ""birthDate"":""1984-01-27"",""nationality"":""Egyptian"",""civilId"":""284012703579"",
                ""mobile"":""99709947"",""joiningDate"":""2019-03-01"",""active"":""Yes""}}");

    private static string BankRow(string code, string paidBy = "Bank transfer",
        string bank = "Gulf Bank", string iban = "KW81CBKU0000000000001234560101") =>
        $@"{{""code"":""{code}"",""paidBy"":""{paidBy}"",""bank"":""{bank}"",""iban"":""{iban}""}}";

    private static string BalanceRow(string code, string leaveDays = "21.5", string loanBalance = null) =>
        loanBalance == null
            ? $@"{{""code"":""{code}"",""leaveDays"":{leaveDays}}}"
            : $@"{{""code"":""{code}"",""leaveDays"":{leaveDays},""loanBalance"":{loanBalance},""loanInstalment"":50}}";

    private GoLiveApplyStageDto Stage(GoLiveApplyResultDto result, string stepId) =>
        result.Stages.Single(s => s.StepId == stepId);

    // ---------------------------------------------------------------- bank

    [Fact]
    public async Task Should_Create_A_Bank_Record_For_Each_Employee()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.3", BankRow("1001"));

        var result = await _apply.Apply();

        Stage(result, "S3.3").Created.ShouldBe(1);
        result.IsClean.ShouldBeTrue();

        UsingDbContext(context =>
        {
            var bank = context.EmployeeBanks.Single();
            bank.PaymentType.ShouldBe(PaymentType.Bank);
            bank.IBAN.ShouldBe("KW81CBKU0000000000001234560101");
            bank.BankId.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Should_Date_The_Bank_Record_From_The_Joining_Date()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.3", BankRow("1001"));

        await _apply.Apply();

        UsingDbContext(context =>
        {
            var bank = context.EmployeeBanks.Single();
            bank.StartDate.Date.ShouldBe(new System.DateTime(2019, 3, 1));
            bank.EndDate.Year.ShouldBe(9999);
        });
    }

    [Fact]
    public async Task Should_Accept_A_Cash_Paid_Employee_Without_A_Bank_Or_Iban()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.3", $@"{{""code"":""1001"",""paidBy"":""Cash""}}");

        var result = await _apply.Apply();

        result.IsClean.ShouldBeTrue();
        UsingDbContext(context =>
        {
            var bank = context.EmployeeBanks.Single();
            bank.PaymentType.ShouldBe(PaymentType.Cash);
            bank.BankId.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Should_Require_An_Iban_When_Paid_By_Transfer()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.3", $@"{{""code"":""1001"",""paidBy"":""Bank transfer"",""bank"":""Gulf Bank""}}");

        var result = await _apply.Apply();

        Stage(result, "S3.3").Failed.ShouldBe(1);
        Stage(result, "S3.3").Issues.Single().Message.ShouldContain("IBAN is required");
    }

    [Fact]
    public async Task Should_Report_An_Unknown_Bank()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.3", BankRow("1001", bank: "Bank of Atlantis"));

        var result = await _apply.Apply();

        Stage(result, "S3.3").Issues.Single().Message.ShouldContain("Bank of Atlantis");
    }

    [Fact]
    public async Task Should_Report_A_Bank_Row_For_An_Employee_That_Does_Not_Exist()
    {
        await SaveStep("S3.3", BankRow("9999"));

        var result = await _apply.Apply();

        Stage(result, "S3.3").Failed.ShouldBe(1);
        Stage(result, "S3.3").Issues.Single().Message.ShouldContain("Apply the employee sheet first");
    }

    [Fact]
    public async Task Should_Update_The_Bank_Record_Rather_Than_Adding_A_Second()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.3", BankRow("1001"));
        await _apply.Apply();

        await SaveStep("S3.3", BankRow("1001", iban: "KW81CBKU0000000000009999999999"));
        var second = await _apply.Apply();

        Stage(second, "S3.3").Updated.ShouldBe(1);
        UsingDbContext(context =>
        {
            context.EmployeeBanks.Count().ShouldBe(1);
            context.EmployeeBanks.Single().IBAN.ShouldEndWith("9999999999");
        });
    }

    // ---------------------------------------------------------------- balances

    [Fact]
    public async Task Should_Create_A_Leave_Opening_Balance()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.4", BalanceRow("1001"));

        var result = await _apply.Apply();

        Stage(result, "S3.4").Created.ShouldBe(1);
        result.IsClean.ShouldBeTrue();

        UsingDbContext(context =>
        {
            var transfer = context.TimeTransfers.Single();
            transfer.TransferCount.ShouldBe(21.5);
        });
    }

    [Fact]
    public async Task Should_Use_The_Seeded_Annual_Leave_Transfer_Type()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.4", BalanceRow("1001"));

        await _apply.Apply();

        UsingDbContext(context =>
        {
            var transfer = context.TimeTransfers.Single();
            var type = context.TimeTransferTypes.Single(t => t.Id == transfer.TransferTypeId);
            type.NameEn.ShouldBe("Annual Leave Opening Balance");
        });
    }

    [Fact]
    public async Task Should_Seed_The_Transfer_Types_With_The_Universal_Pack()
    {
        UsingDbContext(context =>
            context.TimeTransferTypes.Count().ShouldBe(3));
    }

    [Fact]
    public async Task Should_Skip_A_Row_That_Names_An_Employee_But_Carries_No_Balances()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.4", $@"{{""code"":""1001"",""name"":""Ahmed Ali""}}");

        var result = await _apply.Apply();

        Stage(result, "S3.4").Skipped.ShouldBe(1);
        Stage(result, "S3.4").Created.ShouldBe(0);
        Stage(result, "S3.4").Failed.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Accept_A_Negative_Leave_Balance()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.4", BalanceRow("1001", leaveDays: "-3"));

        var result = await _apply.Apply();

        result.IsClean.ShouldBeTrue();
        UsingDbContext(context => context.TimeTransfers.Single().TransferCount.ShouldBe(-3));
    }

    [Fact]
    public async Task Should_Update_The_Leave_Balance_Rather_Than_Adding_A_Second()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.4", BalanceRow("1001", leaveDays: "21.5"));
        await _apply.Apply();

        await SaveStep("S3.4", BalanceRow("1001", leaveDays: "30"));
        await _apply.Apply();

        UsingDbContext(context =>
        {
            context.TimeTransfers.Count().ShouldBe(1);
            context.TimeTransfers.Single().TransferCount.ShouldBe(30);
        });
    }

    [Fact]
    public async Task Should_Explain_A_Loan_Balance_With_No_Loan_Type_Configured()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.4", BalanceRow("1001", loanBalance: "450"));

        var result = await _apply.Apply();

        Stage(result, "S3.4").Failed.ShouldBe(1);
        Stage(result, "S3.4").Issues.Single().Message.ShouldContain("no loan type configured");
    }

    [Fact]
    public async Task Should_Create_A_Loan_When_The_Tenant_Has_Exactly_One_Loan_Type()
    {
        UsingDbContext(context =>
        {
            context.LoanTypes.Add(new LoanType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "STAFF",
                NameEn = "Staff Loan",
                NameAr = "قرض الموظفين",
            });
        });

        await SaveEmployee("1001");
        await SaveStep("S3.4", BalanceRow("1001", loanBalance: "450"));

        var result = await _apply.Apply();

        result.IsClean.ShouldBeTrue();
        UsingDbContext(context =>
        {
            var loan = context.EmployeeLoans.Single();
            loan.DueAmount.ShouldBe(450);
            loan.FixedAmount.ShouldBe(50);
        });
    }

    // ---------------------------------------------------------------- ordering and preview

    [Fact]
    public async Task Should_Run_Employees_Before_The_Sheets_That_Depend_On_Them()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.3", BankRow("1001"));
        await SaveStep("S3.4", BalanceRow("1001"));

        // One run: the employee does not exist beforehand, so this only passes if the stage order holds.
        var result = await _apply.Apply();

        result.IsClean.ShouldBeTrue();
        result.TotalCreated.ShouldBe(4); // + the org assignment
        result.Stages.Select(s => s.Target).ShouldBe(new[]
        {
            "Configuration", "WageType", "Employee", "EmployeeOrgAssignment",
            "EmployeeBasicPay", "EmployeeBank", "OpeningBalance",
        });
    }

    [Fact]
    public async Task Should_Write_Nothing_On_Preview()
    {
        await SaveEmployee("1001");
        await SaveStep("S3.3", BankRow("1001"));
        await SaveStep("S3.4", BalanceRow("1001"));

        var preview = await _apply.Preview();

        preview.TotalCreated.ShouldBe(4); // + the org assignment
        UsingDbContext(context =>
        {
            context.EmployeeBanks.ShouldBeEmpty();
            context.TimeTransfers.ShouldBeEmpty();
        });
    }
}
