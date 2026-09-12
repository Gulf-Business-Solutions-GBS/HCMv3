using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.Threading;
using Abp.UI;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetupV2;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetupV2;

/// <summary>
/// Paying the first payroll through a bank. Load Employee Data takes each employee's bank and IBAN; Step 4
/// makes the month's salary file from the paying bank in one of its layouts, through the same generator the
/// Bank File screen uses, and says who the file leaves out.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2BankFile_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private const string Nbk = "National Bank of Kuwait";
    private const string Cbk = "Commercial Bank of Kuwait";

    /// <summary>The published example of a Kuwaiti IBAN: KW, two check digits, a four-letter bank code, 22 more.</summary>
    private const string Iban = "KW81CBKU0000000000001234560101";

    private static readonly DateTime PayDate = new(2026, 9, 30);
    private static readonly DateTime Joined = new(2020, 1, 15);
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2BankFile_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- Load Employee Data: bank and IBAN

    [Fact]
    public async Task Should_Record_The_Bank_And_Iban_An_Employee_Is_Paid_Into()
    {
        GivenTheReferenceData();

        var output = await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234", Nbk, "kw81 cbku 0000 0000 0000 1234 5601 01")));

        output.Rows.Single().Succeeded.ShouldBeTrue(output.Rows.Single().Message);
        UsingDbContext(context =>
        {
            var employee = context.Employees.Single(e => e.TenantId == TenantId && e.Code == "E001");
            var bank = context.EmployeeBanks.Single(b => b.EmployeeId == employee.Id);
            context.Banks.Single(b => b.Id == bank.BankId).NameEn.ShouldBe(Nbk);
            bank.IBAN.ShouldBe(Iban);
            bank.PaymentType.ShouldBe(PaymentType.Bank);
            bank.StartDate.ShouldBe(Joined);
            bank.EndDate.ShouldBe(OpenEnded);
            bank.PayerBankId.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Should_Record_The_Account_Number_When_The_Sheet_Gives_One()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban, "0123456789")));

        UsingDbContext(context => context.EmployeeBanks.Single(b => b.TenantId == TenantId).AccountNum.ShouldBe("0123456789"));
        (await _goLive.GetLoadedEmployees(DefaultArea)).Employees.Single().AccountNumber.ShouldBe("0123456789");
    }

    /// <summary>An account number filled in on the employee screen is not wiped by a sheet that leaves it blank.</summary>
    [Fact]
    public async Task Should_Keep_An_Account_Number_The_Sheet_Does_Not_Give()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban, "0123456789")));

        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234", Cbk, Iban)));

        UsingDbContext(context => context.EmployeeBanks.Single(b => b.TenantId == TenantId).AccountNum.ShouldBe("0123456789"));
    }

    [Fact]
    public async Task Should_Load_An_Employee_Without_A_Bank_And_Record_None()
    {
        GivenTheReferenceData();

        var output = await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234")));

        output.Rows.Single().Succeeded.ShouldBeTrue();
        UsingDbContext(context => context.EmployeeBanks.Count(b => b.TenantId == TenantId).ShouldBe(0));
    }

    [Theory]
    [InlineData("Bank of Nowhere", Iban, "GoLiveV2RowBankUnknown", "Bank of Nowhere")]
    [InlineData(Nbk, null, "GoLiveV2RowIbanMissing", null)]
    [InlineData(null, Iban, "GoLiveV2RowBankMissing", null)]
    [InlineData(Nbk, "KW81CBKU000000000000123456", "GoLiveV2RowIbanInvalid", "KW81CBKU000000000000123456")]
    [InlineData(Nbk, "not an iban", "GoLiveV2RowIbanInvalid", "not an iban")]
    public async Task Should_Refuse_A_Row_Whose_Bank_Details_Cannot_Pay(string bank, string iban, string reasonKey, string shown)
    {
        GivenTheReferenceData();

        var output = await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234", bank, iban)));

        var row = output.Rows.Single();
        row.Succeeded.ShouldBeFalse();
        row.Message.ShouldBe(Localized(reasonKey, shown));
        UsingDbContext(context => context.Employees.Count(e => e.TenantId == TenantId && e.Code == "E001").ShouldBe(0));
    }

    /// <summary>The grid is where a mistyped IBAN is fixed: loading the sheet again corrects the one record.</summary>
    [Fact]
    public async Task Should_Correct_The_Bank_Details_When_The_Sheet_Is_Loaded_Again()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234", Nbk, "KW81CBKU0000000000001234560199")));

        var again = await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234", Cbk, Iban)));

        again.Rows.Single().Succeeded.ShouldBeTrue(again.Rows.Single().Message);
        UsingDbContext(context =>
        {
            var bank = context.EmployeeBanks.Single(b => b.TenantId == TenantId);
            bank.IBAN.ShouldBe(Iban);
            context.Banks.Single(b => b.Id == bank.BankId).NameEn.ShouldBe(Cbk);
        });
    }

    /// <summary>An employee loaded before the grid had bank columns gets a bank record on the next load.</summary>
    [Fact]
    public async Task Should_Add_Bank_Details_To_An_Employee_Loaded_Without_Them()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234")));

        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban)));

        UsingDbContext(context => context.EmployeeBanks.Single(b => b.TenantId == TenantId).IBAN.ShouldBe(Iban));
    }

    [Fact]
    public async Task Should_Show_The_Bank_And_Iban_When_The_Step_Is_Reopened()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban)));

        var loaded = await _goLive.GetLoadedEmployees(DefaultArea);

        var row = loaded.Employees.Single();
        row.BankName.ShouldBe(Nbk);
        row.Iban.ShouldBe(Iban);
    }

    [Fact]
    public async Task Should_List_The_Banks_By_Name_For_The_Grid()
    {
        GivenTheReferenceData();

        var names = (await _goLive.GetBankNames()).Items;

        names.ShouldContain(Nbk);
        names.ShouldContain("Warba Bank");
        names.ShouldBe(names.OrderBy(n => n).ToList());
    }

    // ---------------------------------------------------------------- Step 4: what can be made

    [Fact]
    public async Task Should_Offer_The_Kuwait_Banks_With_Their_Layouts()
    {
        GivenTheReferenceData();

        var setup = await _goLive.GetBankFileSetup(SetupInput());

        var nbk = setup.PayingBanks.Single(b => b.Name == Nbk);
        nbk.Code.ShouldBe("NBK");
        nbk.Layouts.Select(l => l.FormatCode).ShouldBe(new[] { "NBK-G1", "NBK-G3", "NBK-G2" });
        nbk.SelectedLayout.ShouldBe("NBK-G1");
        // NBK-G2 writes an NBK customer's account number rather than their IBAN, and says so.
        nbk.Layouts.Single(l => l.FormatCode == "NBK-G2").NeedsEmployeeAccountNumber.ShouldBeTrue();
        nbk.Layouts.Single(l => l.FormatCode == "NBK-G1").NeedsEmployeeAccountNumber.ShouldBeFalse();

        var cbk = setup.PayingBanks.Single(b => b.Name == Cbk);
        cbk.SelectedLayout.ShouldBe("CBK-G1");
        var cbkT4 = cbk.Layouts.Single(l => l.FormatCode == "CBK-T4");
        cbkT4.IsText.ShouldBeTrue();
        cbkT4.NeedsCompanyAccount.ShouldBeTrue();
        cbk.Layouts.Single(l => l.FormatCode == "CBK-G1").NeedsCompanyAccount.ShouldBeFalse();
        cbk.Layouts.Single(l => l.FormatCode == "CBK-T1").NeedsEmployeeAccountNumber.ShouldBeTrue();
        // Keyed off a sponsor's bank account, which V2 does not set up.
        cbk.Layouts.ShouldNotContain(l => l.FormatCode == "CBK-T7");

        // No layout has ever been written for these two, so they cannot pay a bank file.
        setup.PayingBanks.ShouldNotContain(b => b.Name == "Bank of Bahrain and Kuwait");
        setup.PayingBanks.ShouldNotContain(b => b.Name == "Ahli United Bank");
    }

    /// <summary>A tenant seeded before the Kuwait pack used bank codes still gets its banks offered, by name.</summary>
    [Fact]
    public async Task Should_Offer_A_Bank_Seeded_With_A_Numeric_Code_By_Its_Name()
    {
        GivenTheReferenceData();
        GivenTheBankCode(Nbk, "1");

        var setup = await _goLive.GetBankFileSetup(SetupInput());

        setup.PayingBanks.Single(b => b.Name == Nbk).Code.ShouldBe("NBK");
    }

    [Fact]
    public async Task Should_List_Who_Has_No_Bank_Details()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(
            Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban),
            Row("E002", "Sara Khalid", "290011501235")));

        var setup = await _goLive.GetBankFileSetup(SetupInput());

        setup.EmployeesWithBank.ShouldBe(1);
        var missing = setup.EmployeesWithoutBank.Single();
        missing.EmployeeCode.ShouldBe("E002");
        missing.EmployeeName.ShouldBe("Sara Khalid");
        missing.Reason.ShouldBe(GoLiveV2BankFileLeftOutReason.NoBankDetails);
    }

    // ---------------------------------------------------------------- Step 4: making the file

    [Fact]
    public async Task Should_Make_The_Salary_File_For_Everyone_Paid_With_Bank_Details()
    {
        await GivenTheFirstPayrollHasRun(
            Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban),
            Row("E002", "Sara Khalid", "290011501235"));

        var file = await _goLive.GenerateBankFile(Generate(Nbk, "NBK-G1"));

        file.FormatCode.ShouldBe("NBK-G1");
        file.FileName.ShouldEndWith(".xlsx");
        file.Base64Content.ShouldNotBeNullOrEmpty();
        file.RecordCount.ShouldBe(1);
        file.TotalAmount.ShouldBe(500, 0.001);
        file.BankCodeCorrectedFrom.ShouldBeNull();

        var leftOut = file.LeftOut.Single();
        leftOut.EmployeeCode.ShouldBe("E002");
        leftOut.Reason.ShouldBe(GoLiveV2BankFileLeftOutReason.NoBankDetails);
    }

    /// <summary>
    /// The generator picks a bank's layouts by the bank's code. A bank seeded as "1" is corrected to "NBK" --
    /// found by name -- before the file is made, and the result says so.
    /// </summary>
    [Fact]
    public async Task Should_Correct_A_Numeric_Bank_Code_Before_Making_The_File()
    {
        await GivenTheFirstPayrollHasRun(Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban));
        GivenTheBankCode(Nbk, "1");

        var file = await _goLive.GenerateBankFile(Generate(Nbk, "NBK-G1"));

        file.RecordCount.ShouldBe(1);
        file.BankCodeCorrectedFrom.ShouldBe("1");
        UsingDbContext(context => context.Banks.Single(b => b.TenantId == TenantId && b.NameEn == Nbk).Code.ShouldBe("NBK"));
    }

    /// <summary>The layout is saved on the bank, so the Bank File screen makes the same file afterwards.</summary>
    [Fact]
    public async Task Should_Set_The_Bank_To_The_Layout_Chosen()
    {
        await GivenTheFirstPayrollHasRun(Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban));

        var file = await _goLive.GenerateBankFile(Generate(Nbk, "NBK-G3"));

        file.FormatCode.ShouldBe("NBK-G3");
        UsingDbContext(context =>
        {
            var bank = context.Banks.Single(b => b.TenantId == TenantId && b.NameEn == Nbk);
            context.BankFiles.Single(f => f.Id == bank.BankFileId).NameEn.ShouldBe("NBK-G3");
        });

        (await _goLive.GetBankFileSetup(SetupInput())).PayingBanks.Single(b => b.Name == Nbk).SelectedLayout.ShouldBe("NBK-G3");
    }

    [Fact]
    public async Task Should_Save_The_Company_Account_A_Text_Layout_Needs()
    {
        await GivenTheFirstPayrollHasRun(Row("E001", "Ahmed Ali", "290011501234", Cbk, Iban));
        var input = Generate(Cbk, "CBK-T4");
        input.CompanyAccountNo = "1234567890";

        var file = await _goLive.GenerateBankFile(input);

        file.ContentType.ShouldBe("text/plain");
        file.RecordCount.ShouldBe(1);
        Encoding.UTF8.GetString(Convert.FromBase64String(file.Base64Content)).ShouldContain("1234567890");
        UsingDbContext(context =>
        {
            var bankId = context.Banks.Single(b => b.TenantId == TenantId && b.NameEn == Cbk).Id;
            context.OrganizationBankInfos.Single(o => o.TenantId == TenantId && o.BankId == bankId).AccountNo.ShouldBe("1234567890");
        });

        (await _goLive.GetBankFileSetup(SetupInput())).PayingBanks.Single(b => b.Name == Cbk).CompanyAccountNo.ShouldBe("1234567890");
    }

    [Fact]
    public async Task Should_Save_The_Bank_And_Company_Codes_A_Layout_Needs()
    {
        await GivenTheFirstPayrollHasRun(Row("E001", "Ahmed Ali", "290011501234", "Burgan Bank", Iban));
        var input = Generate("Burgan Bank", "BUR-G2");
        input.BankCode = "BRGN";
        input.CompanyCode = "C042";

        var file = await _goLive.GenerateBankFile(input);

        file.RecordCount.ShouldBe(1);
        UsingDbContext(context =>
        {
            var bank = context.Banks.Single(b => b.TenantId == TenantId && b.NameEn == "Burgan Bank");
            bank.ExternalCode.ShouldBe("BRGN");
            bank.CompanyCode.ShouldBe("C042");
        });
    }

    [Fact]
    public async Task Should_Refuse_A_Layout_Without_The_Company_Account_It_Needs()
    {
        await GivenTheFirstPayrollHasRun(Row("E001", "Ahmed Ali", "290011501234", Cbk, Iban));

        var refused = await Should.ThrowAsync<UserFriendlyException>(() => _goLive.GenerateBankFile(Generate(Cbk, "CBK-T4")));

        refused.Message.ShouldBe(Localized("GoLiveV2BankFileNeedsCompanyAccount", "CBK-T4"));
    }

    [Fact]
    public async Task Should_Refuse_Another_Banks_Layout()
    {
        await GivenTheFirstPayrollHasRun(Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban));

        var refused = await Should.ThrowAsync<UserFriendlyException>(() => _goLive.GenerateBankFile(Generate(Nbk, "CBK-G1")));

        refused.Message.ShouldBe(Localized("GoLiveV2BankFileLayoutNotForBank", "CBK-G1", Nbk));
    }

    [Fact]
    public async Task Should_Refuse_A_Bank_No_Layout_Exists_For()
    {
        await GivenTheFirstPayrollHasRun(Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban));

        var refused = await Should.ThrowAsync<UserFriendlyException>(
            () => _goLive.GenerateBankFile(Generate("Ahli United Bank", "NBK-G1")));

        refused.Message.ShouldBe(Localized("GoLiveV2BankFileBankHasNoLayout", "Ahli United Bank"));
    }

    /// <summary>A file with nobody in it is refused, with the reason, rather than downloaded empty.</summary>
    [Fact]
    public async Task Should_Refuse_When_Nobody_Can_Be_Paid_By_Bank()
    {
        await GivenTheFirstPayrollHasRun(Row("E001", "Ahmed Ali", "290011501234"));

        var refused = await Should.ThrowAsync<UserFriendlyException>(() => _goLive.GenerateBankFile(Generate(Nbk, "NBK-G1")));

        refused.Message.ShouldBe(Localized("GoLiveV2BankFileNobodyToPay", "September 2026"));
    }

    [Fact]
    public async Task Should_Leave_Out_Someone_The_Month_Did_Not_Pay()
    {
        await GivenTheFirstPayrollHasRun(Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban));
        // Loaded after the run: in the payroll area, with a bank, but not paid for September.
        await _goLive.LoadEmployees(Load(Row("E002", "Sara Khalid", "290011501235", Nbk, "KW81CBKU0000000000001234560102")));

        var file = await _goLive.GenerateBankFile(Generate(Nbk, "NBK-G1"));

        file.RecordCount.ShouldBe(1);
        var leftOut = file.LeftOut.Single();
        leftOut.EmployeeCode.ShouldBe("E002");
        leftOut.Reason.ShouldBe(GoLiveV2BankFileLeftOutReason.NotPaid);
    }

    /// <summary>
    /// A layout that pays into account numbers reaches only the employees who have one. The rest are named,
    /// with that as the reason, rather than written into the file as blank lines.
    /// </summary>
    [Fact]
    public async Task Should_Leave_Out_Someone_Without_An_Account_Number_On_A_Layout_That_Pays_Into_Them()
    {
        await GivenTheFirstPayrollHasRun(
            Row("E001", "Ahmed Ali", "290011501234", Cbk, Iban, "0123456789"),
            Row("E002", "Sara Khalid", "290011501235", Cbk, "KW81CBKU0000000000001234560102"));
        var input = Generate(Cbk, "CBK-T1");
        input.CompanyAccountNo = "1234567890";

        var file = await _goLive.GenerateBankFile(input);

        file.RecordCount.ShouldBe(1);
        var leftOut = file.LeftOut.Single();
        leftOut.EmployeeCode.ShouldBe("E002");
        leftOut.Reason.ShouldBe(GoLiveV2BankFileLeftOutReason.NoAccountNumber);
    }

    [Fact]
    public async Task Should_Count_Who_Has_An_Account_Number()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(
            Row("E001", "Ahmed Ali", "290011501234", Nbk, Iban, "0123456789"),
            Row("E002", "Sara Khalid", "290011501235", Nbk, "KW81CBKU0000000000001234560102")));

        var setup = await _goLive.GetBankFileSetup(SetupInput());

        setup.EmployeesWithBank.ShouldBe(2);
        setup.EmployeesWithAccountNumber.ShouldBe(1);
    }

    // ---------------------------------------------------------------- arrange

    private async Task GivenTheFirstPayrollHasRun(params GoLiveV2EmployeeRowDto[] rows)
    {
        GivenTheReferenceData();
        var load = await _goLive.LoadEmployees(Load(rows));
        load.Rows.ShouldAllBe(r => r.Succeeded);

        var run = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);
    }

    private void GivenTheReferenceData()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }
    }

    private void GivenTheBankCode(string bankName, string code) =>
        UsingDbContext(context => context.Banks.Single(b => b.TenantId == TenantId && b.NameEn == bankName).Code = code);

    private GoLiveV2GenerateBankFileInput Generate(string bankName, string formatCode)
    {
        var bankId = UsingDbContext(context => context.Banks.Single(b => b.TenantId == TenantId && b.NameEn == bankName).Id);
        return new GoLiveV2GenerateBankFileInput
        {
            PayDate = PayDate,
            PayrollAreaName = DefaultArea,
            PaymentBankId = bankId,
            FormatCode = formatCode,
        };
    }

    private static GoLiveV2BankFileSetupInput SetupInput() => new() { PayDate = PayDate, PayrollAreaName = DefaultArea };

    private static GoLiveV2LoadEmployeesInput Load(params GoLiveV2EmployeeRowDto[] rows) =>
        new() { OrganizationUnitName = "ABC Holding", PayrollAreaName = DefaultArea, Employees = new List<GoLiveV2EmployeeRowDto>(rows) };

    private static GoLiveV2EmployeeRowDto Row(
        string code,
        string name,
        string civilId,
        string bank = null,
        string iban = null,
        string accountNumber = null) =>
        new()
        {
            Code = code,
            FullName = name,
            JoiningDate = Joined,
            CivilId = civilId,
            BasicSalary = 500,
            BankName = bank,
            Iban = iban,
            AccountNumber = accountNumber,
        };

    private string Localized(string key, params object[] args) =>
        string.Format(Resolve<ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName, key), args);
}
