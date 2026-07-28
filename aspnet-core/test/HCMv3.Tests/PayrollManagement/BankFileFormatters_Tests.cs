using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.PayrollManagement.BankFiles;
using HCMv3.PayrollManagement.BankFiles.Formatters;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Golden-file style coverage for the 28 bank-transfer file formats ported from legacy
/// <c>HCM10.Web/UserControls/PayrollManagement/uscBankFile2.ascx.cs</c>.
///
/// These are PURE domain tests -- no database, no ABP container -- because <see cref="BankFileGenerator"/>
/// and every <see cref="IBankFileFormatter"/> are deliberately free of repository dependencies. That makes
/// per-format byte fidelity cheap to assert, which is the highest-value coverage here: a bank file is
/// rejected outright over a single misplaced pad character.
/// </summary>
public class BankFileFormatters_Tests
{
    private static readonly DateTime EndDate = new(2024, 10, 31);
    private static readonly DateTime GeneratedOn = new(2024, 11, 5, 9, 30, 15, 250);

    /// <summary>Two payees with contrasting bank/sponsor shapes, reused by every format test.</summary>
    private static BankFileContext Context(
        string payerBankCode,
        IEnumerable<BankFilePayeeRow> rows = null,
        Action<BankFileContext> configure = null)
    {
        var context = new BankFileContext
        {
            EndDate = EndDate,
            GeneratedOn = GeneratedOn,
            PayerBankCode = payerBankCode,
            PayerBankExternalCode = "008",
            PayerBankCompanyCode = "CMP1",
            SponsorBankAccountNumber = "3001017974",
            OrganizationBankAccountNo = "3001017974",
            SelectedSponsorMosalId = 552007152002,
            EmployeeBankFilterSelected = false,
            Rows = (rows ?? DefaultRows()).ToList()
        };

        context.GrandTotalAmountAllBanks = context.Rows.Sum(r => r.Amount);
        configure?.Invoke(context);
        return context;
    }

    private static List<BankFilePayeeRow> DefaultRows()
    {
        return new List<BankFilePayeeRow>
        {
            new()
            {
                EmployeeId = 11,
                EmployeeCode = "E001",
                FullNameEn = "Ali Hassan",
                CivilId = 290010112345,
                HasBankRecord = true,
                EmployeeBankId = 5,
                EmployeeBankCode = "NBK",
                EmployeeBankNameEn = "National Bank of Kuwait",
                EmployeeBankSwiftCode = "NBOKKWKW",
                BankBranch = "Salmiya",
                AccountNum = "1234567890",
                Iban = "KW81NBOK0000000000001234560101",
                PaymentType = PaymentType.Bank,
                PayeeBankCode = "101",
                PayeeBankNameEn = "National Bank of Kuwait",
                HasSponsor = true,
                SponsorNameEn = "Acme Holding",
                SponsorMosalId = 552007152002,
                Amount = 1056.54d,
                CurrentCompanyNameEn = "Acme Holding Company"
            },
            new()
            {
                EmployeeId = 22,
                EmployeeCode = "E002",
                FullNameEn = "Maria Gomez",
                CivilId = 285070298765,
                HasBankRecord = true,
                EmployeeBankId = 7,
                EmployeeBankCode = "GBK",
                EmployeeBankNameEn = "Gulf Bank",
                EmployeeBankSwiftCode = "GULBKWKW",
                BankBranch = "Hawally",
                AccountNum = "9876543210",
                Iban = "KW18BRGN0000000000000260072729",
                PaymentType = PaymentType.Bank,
                PayeeBankCode = "202",
                PayeeBankNameEn = "Gulf Bank",
                HasSponsor = false,
                SponsorMosalId = 0,
                Amount = 250d,
                CurrentCompanyNameEn = "Acme Holding Company"
            }
        };
    }

    private static string[] Lines(BankFileDocument doc)
    {
        return doc.TextContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    // =====================================================================================================
    // Format dispatch -- the legacy switch + else-if fallback chain
    // =====================================================================================================

    [Theory]
    // NBK :436-628
    [InlineData("NBK", BankFileFormatCodes.NbkG2, BankFileFormatCodes.NbkG2)]
    [InlineData("NBK", BankFileFormatCodes.NbkG3, BankFileFormatCodes.NbkG3)]
    [InlineData("NBK", null, BankFileFormatCodes.NbkG1)]
    [InlineData("NBK", "SOMETHING-ELSE", BankFileFormatCodes.NbkG1)]
    // CBK :633-1298
    [InlineData("CBK", BankFileFormatCodes.CbkG1, BankFileFormatCodes.CbkG1)]
    [InlineData("CBK", BankFileFormatCodes.CbkG2, BankFileFormatCodes.CbkG2)]
    [InlineData("CBK", BankFileFormatCodes.CbkT7, BankFileFormatCodes.CbkT7)]
    [InlineData("CBK", BankFileFormatCodes.CbkT6, BankFileFormatCodes.CbkT6)]
    [InlineData("CBK", BankFileFormatCodes.CbkT5, BankFileFormatCodes.CbkT5)]
    [InlineData("CBK", BankFileFormatCodes.CbkT4, BankFileFormatCodes.CbkT4)]
    [InlineData("CBK", BankFileFormatCodes.CbkT3, BankFileFormatCodes.CbkT3)]
    [InlineData("CBK", BankFileFormatCodes.CbkT2, BankFileFormatCodes.CbkT2)]
    [InlineData("CBK", null, BankFileFormatCodes.CbkT1)]
    // GB/GBK share one branch :1303-1510
    [InlineData("GBK", BankFileFormatCodes.GbkT1, BankFileFormatCodes.GbkT1)]
    [InlineData("GB", BankFileFormatCodes.GbkG2, BankFileFormatCodes.GbkG2)]
    [InlineData("GB", null, BankFileFormatCodes.GbkG1)]
    // ABK :1515-1789
    [InlineData("ABK", BankFileFormatCodes.AbkG3, BankFileFormatCodes.AbkG3)]
    [InlineData("ABK", BankFileFormatCodes.AbkG2, BankFileFormatCodes.AbkG2)]
    [InlineData("ABK", null, BankFileFormatCodes.AbkG1)]
    // BUR :1925-2161
    [InlineData("BUR", BankFileFormatCodes.BurT1, BankFileFormatCodes.BurT1)]
    [InlineData("BUR", BankFileFormatCodes.BurG2, BankFileFormatCodes.BurG2)]
    [InlineData("BUR", null, BankFileFormatCodes.BurG1)]
    // BURBD :2166-2309
    [InlineData("BURBD", BankFileFormatCodes.BurBdG1, BankFileFormatCodes.BurBdG1)]
    [InlineData("BURBD", null, BankFileFormatCodes.BurBdT1)]
    // Single-layout banks ignore the assigned format entirely (legacy comments the test out)
    [InlineData("NBR", null, BankFileFormatCodes.NbrG1)]
    [InlineData("KFH", "ANYTHING", BankFileFormatCodes.KfhG1)]
    [InlineData("BB", null, BankFileFormatCodes.BbG1)]
    [InlineData("KIB", BankFileFormatCodes.KibG1, BankFileFormatCodes.KibG1)]
    [InlineData("WRB", BankFileFormatCodes.WrbG1, BankFileFormatCodes.WrbG1)]
    public void Should_Resolve_Legacy_Format_Fallback_Chain(string payerBank, string assigned, string expected)
    {
        BankFileGenerator.ResolveFormatCode(payerBank, assigned).ShouldBe(expected);
    }

    [Theory]
    [InlineData("KIB")]
    [InlineData("WRB")]
    public void Should_Produce_No_File_For_Kib_Or_Wrb_When_No_Format_Assigned(string payerBank)
    {
        // Legacy :1798 / :1864 have NO else branch -- nothing is emitted at all.
        BankFileGenerator.ResolveFormatCode(payerBank, null).ShouldBeNull();
    }

    [Fact]
    public void Should_Throw_For_Unknown_Payer_Bank_Code()
    {
        Should.Throw<BankFileFormatNotFoundException>(
            () => BankFileGenerator.ResolveFormatCode("ZZZ", null));
    }

    [Fact]
    public void Should_Register_All_TwentyEight_Legacy_Formats()
    {
        BankFileFormatCodes.All.Count.ShouldBe(28);
        BankFileFormatCodes.All.Distinct().Count().ShouldBe(28);

        var registered = BankFileGenerator.DefaultFormatters().Select(f => f.FormatCode).ToList();
        registered.Count.ShouldBe(28);

        foreach (var code in BankFileFormatCodes.All)
        {
            registered.ShouldContain(code, $"No formatter registered for legacy format '{code}'.");
        }
    }

    // =====================================================================================================
    // NBK
    // =====================================================================================================

    [Fact]
    public void NbkG2_Should_Use_AccountNum_For_Nbk_Payees_And_Iban_For_Others()
    {
        var doc = new NbkG2Formatter().Build(Context("NBK"));

        doc.Kind.ShouldBe(BankFileDocumentKind.Grid);
        doc.Columns.Count.ShouldBe(7);
        doc.Columns[0].ShouldBe("Payment Serial Number");
        doc.Columns[3].ShouldBe("Account # for NBK A/C & IBAN for other Bank");

        doc.Rows[0][0].ShouldBe(1);
        doc.Rows[0][2].ShouldBe("290010112345");
        doc.Rows[0][3].ShouldBe("1234567890");           // NBK payee -> AccountNum
        doc.Rows[0][5].ShouldBe("KWD");
        doc.Rows[0][6].ShouldBe(1056.54d);

        doc.Rows[1][0].ShouldBe(2);
        doc.Rows[1][3].ShouldBe("KW18BRGN0000000000000260072729"); // non-NBK payee -> IBAN
    }

    [Fact]
    public void NbkG3_Should_Always_Use_Iban()
    {
        var doc = new NbkG3Formatter().Build(Context("NBK"));

        doc.FormatCode.ShouldBe(BankFileFormatCodes.NbkG3);
        doc.Rows[0][3].ShouldBe("KW81NBOK0000000000001234560101"); // IBAN even though the payee banks with NBK
    }

    [Fact]
    public void NbkG1_Should_Emit_Payee_Bank_Code_And_Blank_Mosal_When_No_Sponsor()
    {
        var doc = new NbkG1Formatter().Build(Context("NBK"));

        doc.Columns.ShouldBe(new List<string>
        {
            "Employee Number", "Employee Name", "Employee IBAN Number", "Payment Amount",
            "Bank Code", "Employee Civil ID", "MOSAL ID"
        });

        doc.Rows[0][4].ShouldBe("101");
        doc.Rows[0][6].ShouldBe("552007152002");
        doc.Rows[1][6].ShouldBeNull();  // no sponsor -> cell left empty
    }

    // =====================================================================================================
    // CBK -- the seven fixed-width text layouts are where pad fidelity matters most
    // =====================================================================================================

    [Fact]
    public void CbkT1_Should_Match_Legacy_Layout()
    {
        var lines = Lines(new CbkT1Formatter().Build(Context("CBK")));

        lines[0].ShouldBe("HDRINT 002 31102024 31102024 3001017974");
        // "DTLINT" + " 002 " + seq(5,'0') + 4 spaces + AccountNum padded left to 10 w/ SPACES + 5 spaces + amount(12,'0')
        lines[1].ShouldBe("DTLINT 002 00001    1234567890     00001056.540");
        lines[2].ShouldBe("DTLINT 002 00002    9876543210     00000250.000");
        lines[3].ShouldBe("TRLINT 002 00002        00001306.540");
    }

    [Fact]
    public void CbkT2_Should_Use_Abg_Tags_And_Fixed_Count_Token()
    {
        var lines = Lines(new CbkT2Formatter().Build(Context("CBK")));

        lines[0].ShouldBe("HDRABG 002  31102024 31102024 3001017974");
        lines[1].ShouldBe("DTLABG 002 99999 1234567890     00001056.540  Ali Hassan");
        lines[3].ShouldBe("TRLABG 002 00002  00001306.540");
    }

    [Fact]
    public void CbkT3_Should_Emit_Payee_Bank_Code_And_Iban()
    {
        var lines = Lines(new CbkT3Formatter().Build(Context("CBK")));

        lines[0].ShouldBe("HDRSMS 002  31102024 31102024 3001017974");
        lines[1].ShouldBe("DTLSMS 101 99999 KW81NBOK0000000000001234560101     00001056.540  Ali Hassan");
        lines[2].ShouldBe("DTLSMS 202 99999 KW18BRGN0000000000000260072729     00000250.000  Maria Gomez");
        lines[3].ShouldBe("TRLSMS 002 00002  00001306.540");
    }

    [Fact]
    public void CbkT4_Should_Use_Abc_Header_Sig_Detail_And_Eight_Space_Trailer()
    {
        var lines = Lines(new CbkT4Formatter().Build(Context("CBK")));

        lines[0].ShouldBe("HDRABC 002 31102024 31102024 3001017974");
        lines[1].ShouldBe("DTLSIG 002 00001    KW81NBOK0000000000001234560101     00001056.540  Ali Hassan");
        lines[3].ShouldBe("TRLABC 002 00002        00001306.540");
    }

    [Fact]
    public void CbkT5_Should_Differ_From_T3_Only_By_A_Single_Space_Before_The_Amount()
    {
        var t3 = Lines(new CbkT3Formatter().Build(Context("CBK")));
        var t5 = Lines(new CbkT5Formatter().Build(Context("CBK")));

        t5[0].ShouldBe(t3[0]);
        t5[3].ShouldBe(t3[3]);
        t5[1].ShouldBe("DTLSMS 101 99999 KW81NBOK0000000000001234560101 00001056.540  Ali Hassan");
        t3[1].ShouldBe(t5[1].Replace("0101 00001056", "0101     00001056"));
    }

    [Fact]
    public void CbkT6_Should_Insert_CivilId_Between_Amount_And_Name()
    {
        var lines = Lines(new CbkT6Formatter().Build(Context("CBK")));

        lines[0].ShouldBe("HDRSMS 002  31102024 31102024 3001017974");
        lines[1].ShouldBe("DTLSMS 101 99999 KW81NBOK0000000000001234560101 00001056.540 290010112345  Ali Hassan");
        lines[3].ShouldBe("TRLSMS 002 00002  00001306.540");
    }

    [Fact]
    public void CbkT7_Should_Use_SponsorBank_Account_And_Employee_Mosal_Id()
    {
        var lines = Lines(new CbkT7Formatter().Build(Context("CBK")));

        lines[0].ShouldBe("HDRABC 002 31102024 31102024 3001017974");
        lines[1].ShouldBe("DTL 552007152002 KW81NBOK0000000000001234560101 00001056.540 290010112345  Ali Hassan");
        // Second payee has NO sponsor -> MOSAL ID is a run of zeros (legacy Convert.ToString(0L)).
        lines[2].ShouldBe("DTL 000000000000 KW18BRGN0000000000000260072729 00000250.000 285070298765  Maria Gomez");
        lines[3].ShouldBe("TRLABC 002 00002 00001306.540");
    }

    [Fact]
    public void CbkT7_Should_Truncate_Names_Longer_Than_Sixty_Characters()
    {
        var longName = new string('X', 75);
        var rows = DefaultRows();
        rows[0].FullNameEn = longName;
        rows.RemoveAt(1);

        var lines = Lines(new CbkT7Formatter().Build(Context("CBK", rows)));

        lines[1].ShouldEndWith(new string('X', 60));
        lines[1].ShouldNotContain(new string('X', 61));
    }

    [Fact]
    public void CbkT7_Should_Skip_Rows_Without_An_Iban_Or_With_A_NonPositive_Amount()
    {
        var rows = DefaultRows();
        rows[0].Iban = "";        // no IBAN -> skipped
        rows[1].Amount = 0d;      // zero amount -> skipped

        var lines = Lines(new CbkT7Formatter().Build(Context("CBK", rows)));

        lines.Length.ShouldBe(2);                        // header + trailer only
        lines[1].ShouldBe("TRLABC 002 00000 00000000.000");
    }

    [Fact]
    public void CbkT3_Should_Emit_One_Detail_Line_Per_Salary_Register()
    {
        // Legacy :1122 iterates allSalRegList (per register), not the distinct employee list, so an employee
        // with two registers gets two identical FULL-amount lines. Ported faithfully.
        var rows = DefaultRows();
        rows[0].RegisterCount = 2;
        rows.RemoveAt(1);

        var lines = Lines(new CbkT3Formatter().Build(Context("CBK", rows)));

        lines.Length.ShouldBe(4);                        // header + 2 details + trailer
        lines[1].ShouldBe(lines[2]);
        lines[3].ShouldBe("TRLSMS 002 00002  00002113.080");
    }

    [Fact]
    public void CbkG1_Should_Order_By_Employee_Id_And_Not_Append_A_Total_Row()
    {
        var doc = new CbkG1Formatter().Build(Context("CBK"));

        doc.Columns[0].ShouldBe("Serial no.");
        doc.Columns[9].ShouldBe("Amount.");
        doc.Rows.Count.ShouldBe(2);
        doc.Rows[0][1].ShouldBe("E001");
        doc.Rows[0][8].ShouldBe("BANK");            // legacy PaymentType.ToString() was upper-case
        doc.Rows[1][4].ShouldBeNull();              // second payee has no sponsor
    }

    [Fact]
    public void CbkG2_Should_Append_An_Unsplit_Grand_Total_Row()
    {
        var context = Context("CBK", configure: c => c.GrandTotalAmountAllBanks = 9999.999d);
        var doc = new CbkG2Formatter().Build(context);

        doc.Rows.Count.ShouldBe(3);
        var total = doc.Rows[2];
        total.Take(9).ShouldAllBe(cell => cell == null);
        total[9].ShouldBe(9999.999d);
    }

    // =====================================================================================================
    // GBK
    // =====================================================================================================

    [Fact]
    public void GbkT1_Should_Use_Generation_Date_In_Header_And_Omit_The_Trailing_Newline()
    {
        var doc = new GbkT1Formatter().Build(Context("GBK"));
        var lines = Lines(doc);

        // Header date is DateTime.Now (legacy :1314), NOT the period end.
        lines[0].ShouldBe("HDR 051124 003 003 CBKS 90413911 S 1024 KWD");

        // 39-char name field, then IBAN, then amount(12,'0'), then the fixed token.
        lines[1].ShouldStartWith("DTL290010112345   Ali Hassan                             ");
        lines[1].ShouldContain("KW81NBOK000000000000123456010100001056.540 000000003Y111303268258");

        doc.TextContent.ShouldNotEndWith("\n");
        lines[3].ShouldStartWith("TRL 00000002 0000001306.540 000000000000000000000000000000");
    }

    [Fact]
    public void GbkT1_Should_Truncate_Names_Longer_Than_ThirtyNine_Characters()
    {
        var rows = DefaultRows();
        rows[0].FullNameEn = new string('Y', 50);
        rows.RemoveAt(1);

        var lines = Lines(new GbkT1Formatter().Build(Context("GBK", rows)));

        lines[1].ShouldStartWith("DTL290010112345   " + new string('Y', 39) + " KW81");
    }

    [Fact]
    public void GbkG1_Should_Skip_Cash_Payees_And_Emit_Six_Columns()
    {
        var rows = DefaultRows();
        rows[1].PaymentType = PaymentType.Cash;

        var doc = new GbkG1Formatter().Build(Context("GBK", rows));

        doc.Columns.Count.ShouldBe(6);
        doc.Rows.Count.ShouldBe(1);
        doc.Rows[0][5].ShouldBe("Yes");
    }

    [Fact]
    public void GbkG2_Should_Add_The_Two_Sponsor_Columns()
    {
        var doc = new GbkG2Formatter().Build(Context("GBK"));

        doc.Columns.Count.ShouldBe(8);
        doc.Columns[6].ShouldBe("MOSAL ID");
        doc.Columns[7].ShouldBe("Employer English Name");

        doc.Rows[0][6].ShouldBe(552007152002L);
        doc.Rows[0][7].ShouldBe("Acme Holding");
        doc.Rows[1][6].ShouldBeNull();
    }

    // =====================================================================================================
    // ABK
    // =====================================================================================================

    [Fact]
    public void AbkG3_Should_Not_Filter_OnHold_Or_Zero_Amount_Rows()
    {
        var rows = DefaultRows();
        rows[0].IsBankTransferOnHold = true;
        rows[1].Amount = 0d;

        var doc = new AbkG3Formatter().Build(Context("ABK", rows));

        doc.Rows.Count.ShouldBe(2);
        doc.Columns[3].ShouldBe("TYPE");
        doc.Rows[0][3].ShouldBe("SAL");
    }

    [Fact]
    public void AbkG2_Should_Drop_OnHold_And_ZeroAmount_Rows_And_Sort_By_BankCode_Then_EmployeeCode()
    {
        var rows = DefaultRows();
        rows[0].PayeeBankCode = "900";   // sorts AFTER "202"
        rows.Add(new BankFilePayeeRow
        {
            EmployeeId = 33, EmployeeCode = "E003", FullNameEn = "On Hold", CivilId = 1,
            HasBankRecord = true, EmployeeBankId = 5, EmployeeBankCode = "NBK",
            PayeeBankCode = "001", Amount = 10d, IsBankTransferOnHold = true
        });
        rows.Add(new BankFilePayeeRow
        {
            EmployeeId = 44, EmployeeCode = "E004", FullNameEn = "Zero Pay", CivilId = 2,
            HasBankRecord = true, EmployeeBankId = 5, EmployeeBankCode = "NBK",
            PayeeBankCode = "001", Amount = 0d
        });

        var doc = new AbkG2Formatter().Build(Context("ABK", rows));

        doc.Columns[0].ShouldBe("EmployeeCode");
        doc.Rows.Count.ShouldBe(2);
        doc.Rows[0][0].ShouldBe("E002");   // bank code 202 sorts before 900
        doc.Rows[1][0].ShouldBe("E001");
    }

    [Fact]
    public void AbkG1_Should_Drop_The_EmployeeCode_Column_But_Still_Sort_By_It()
    {
        var doc = new AbkG1Formatter().Build(Context("ABK"));

        doc.Columns.ShouldNotContain("EmployeeCode");
        doc.Columns.Count.ShouldBe(6);
        doc.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public void Abk_Should_Use_The_Payer_ExternalCode_When_The_Payee_Banks_With_The_Payer()
    {
        var rows = DefaultRows();
        rows[0].EmployeeBankIsPayerBank = true;
        rows.RemoveAt(1);

        var doc = new AbkG3Formatter().Build(Context("ABK", rows));

        doc.Rows[0][5].ShouldBe("008");   // payer bank ExternalCode, not the PayeeBankDetail code
    }

    // =====================================================================================================
    // KIB / WRB / NBR / KFH / BB
    // =====================================================================================================

    [Fact]
    public void KibG1_Should_Emit_Iban_Name_CivilId_Amount()
    {
        var doc = new KibG1Formatter().Build(Context("KIB"));

        doc.Columns.ShouldBe(new List<string> { "IBAN", "Name", "Civil ID", "Salary Amount" });
        doc.Rows[0][0].ShouldBe("KW81NBOK0000000000001234560101");
        doc.Rows[0][3].ShouldBe(1056.54d);
    }

    [Fact]
    public void WrbG1_Should_Lead_With_CivilId()
    {
        var doc = new WrbG1Formatter().Build(Context("WRB"));

        doc.Columns.ShouldBe(new List<string> { "Civil ID", "Name", "IBAN", "Salary Amount" });
        doc.Rows[0][0].ShouldBe("290010112345");
    }

    [Fact]
    public void NbrG1_Should_Emit_Branch_And_Swift_Code()
    {
        var doc = new NbrG1Formatter().Build(Context("NBR"));

        doc.Columns.Count.ShouldBe(9);
        doc.Rows[0][0].ShouldBe("E001");
        doc.Rows[0][5].ShouldBe("National Bank of Kuwait");
        doc.Rows[0][6].ShouldBe("Salmiya");
        doc.Rows[0][8].ShouldBe("NBOKKWKW");
    }

    [Fact]
    public void KfhG1_Should_Fall_Back_To_Iban_When_AccountNum_Is_Empty()
    {
        var rows = DefaultRows();
        rows[1].AccountNum = "";

        var doc = new KfhG1Formatter().Build(Context("KFH", rows));

        doc.Rows[0][3].ShouldBe("1234567890");
        doc.Rows[1][3].ShouldBe("KW18BRGN0000000000000260072729");
        doc.Rows[0][0].ShouldBe("1");
        doc.Rows[1][0].ShouldBe("2");
        doc.Rows[0][6].ShouldBeNull();   // "Section" is declared but never populated by legacy
    }

    [Fact]
    public void BbG1_Should_Emit_Three_Columns()
    {
        var doc = new BbG1Formatter().Build(Context("BB"));

        doc.Columns.ShouldBe(new List<string>
        {
            "Beneficiary Account IBAN", "Beneficiary Name", "Payment Amount"
        });
        doc.Rows.Count.ShouldBe(2);
    }

    // =====================================================================================================
    // BUR / BURBD
    // =====================================================================================================

    [Fact]
    public void BurT1_Should_Match_Legacy_Fixed_Width_Layout()
    {
        var lines = Lines(new BurT1Formatter().Build(Context("BUR")));

        lines[0].ShouldBe("HDR 311024 008 008 CBKS");

        // DTL + seq(15,'0') + name(40,' ') + IBAN(30,'0') + amount padLeft(12,'0') padRight(19,'0') + mosal(18,'0')
        var detail = lines[1];
        detail.ShouldStartWith("DTL000000000000001");
        detail.Substring(18, 40).ShouldBe("Ali Hassan".PadRight(40, ' '));
        detail.Substring(58, 30).ShouldBe("KW81NBOK0000000000001234560101");
        detail.Substring(88, 19).ShouldBe("00001056.5400000000");
        detail.Substring(107).ShouldBe("552007152002000000");

        lines[3].ShouldBe("TRL 00000002 000000000000000000000000001306.540");
    }

    [Fact]
    public void BurT1_Should_Blank_The_Employee_Bank_Code_When_An_Employee_Bank_Filter_Is_Chosen()
    {
        // Legacy :1967 -- EmpBankExtCode = empBank2 == null ? SponsorBankExtCode : ""
        var context = Context("BUR", configure: c => c.EmployeeBankFilterSelected = true);

        Lines(new BurT1Formatter().Build(context))[0].ShouldBe("HDR 311024 008  CBKS");
    }

    [Fact]
    public void BurG2_Should_Emit_Padded_Sequence_And_Payer_Company_And_Bank_Codes()
    {
        var doc = new BurG2Formatter().Build(Context("BUR"));

        doc.Rows[0][0].ShouldBe("DTL");
        doc.Rows[0][1].ShouldBe("00001");
        doc.Rows[1][1].ShouldBe("00002");
        doc.Rows[0][5].ShouldBe("CMP1");
        doc.Rows[0][6].ShouldBe("008");
        doc.Rows[0][7].ShouldBe("552007152002");
        doc.Rows[1][7].ShouldBeNull();
    }

    [Fact]
    public void BurG1_Should_Use_AccountNum_And_Payee_Bank_Name()
    {
        var doc = new BurG1Formatter().Build(Context("BUR"));

        doc.Columns[0].ShouldBe("Emp No.");
        doc.Rows[0][2].ShouldBe("1234567890");
        doc.Rows[0][4].ShouldBe("101");
        doc.Rows[0][5].ShouldBe("National Bank of Kuwait");
        doc.Rows[0][6].ShouldBe("552007152002");
    }

    [Fact]
    public void BurBdG1_Should_Use_The_Card_Account_Number()
    {
        var doc = new BurBdG1Formatter().Build(Context("BURBD"));

        doc.Columns[3].ShouldBe("B-Dinar Card Number");
        doc.Rows[0][0].ShouldBe("1");
        doc.Rows[0][1].ShouldBe("00001");
        doc.Rows[0][3].ShouldBe("1234567890");
        doc.Rows[0][5].ShouldBe("552007152002");
    }

    [Fact]
    public void BurBdT1_Should_Split_The_Amount_Into_Dinars_And_Fils()
    {
        var lines = Lines(new BurBdT1Formatter().Build(Context("BURBD")));

        lines[0].ShouldBe("00000RECAFCO".PadRight(50, ' ') + "102024".PadRight(43, ' '));

        var detail = lines[1];
        detail.ShouldStartWith("1");
        detail.Substring(1, 11).ShouldBe("0000000E001");
        detail.Substring(12, 40).ShouldBe("Ali Hassan".PadRight(40, ' '));
        detail.Substring(52, 16).ShouldBe("0000001234567890");
        detail.Substring(68, 10).ShouldBe("0000001056");   // dinars
        detail.Substring(78, 6).ShouldBe("540000");        // fils: 0.54 -> "54" -> padRight 6
        // Legacy PadLeft(9, '0') does NOT truncate, so a 12-digit MOSAL ID overflows the nominal 9-char
        // field and lengthens the line. Faithful to legacy :2273 -- flagged in the wave report.
        detail.Substring(84).ShouldBe("552007152002");

        // Second payee has a whole-dinar amount -> fils part is all zeros.
        lines[2].Substring(68, 10).ShouldBe("0000000250");
        lines[2].Substring(78, 6).ShouldBe("000000");

        lines[3].ShouldStartWith("2000020000001306540 " + BurBdT1Formatter.LegacyTrailerConstant);
    }

    [Fact]
    public void BurBdT1_Should_Skip_Payees_Without_A_Bank_Record()
    {
        var rows = DefaultRows();
        rows[0].HasBankRecord = false;

        var lines = Lines(new BurBdT1Formatter().Build(Context("BURBD", rows)));

        lines.Length.ShouldBe(3);   // header + 1 detail + trailer
        lines[1].ShouldContain("Maria Gomez");
    }

    // =====================================================================================================
    // Cross-cutting field formatting
    // =====================================================================================================

    [Theory]
    [InlineData(0d, 12, "00000000.000")]
    [InlineData(1056.54d, 12, "00001056.540")]
    [InlineData(1056.5432d, 12, "00001056.543")]
    [InlineData(999999.999d, 12, "00999999.999")]
    public void Amount3_Should_Round_To_Three_Decimals_And_Zero_Pad(double value, int width, string expected)
    {
        BankFileFieldFormat.Amount3(value, width).ShouldBe(expected);
    }

    [Fact]
    public void Amount3_Should_Use_An_Invariant_Decimal_Separator()
    {
        // Guards the deliberate deviation: a comma separator would produce a bank-rejected file.
        BankFileFieldFormat.Amount3(1234.5d, 12).ShouldNotContain(",");
        BankFileFieldFormat.Amount3(1234.5d, 12).ShouldBe("00001234.500");
    }

    [Fact]
    public void FixedWidth_Should_Truncate_Then_Pad()
    {
        BankFileFieldFormat.FixedWidth("abc", 5).ShouldBe("abc  ");
        BankFileFieldFormat.FixedWidth("abcdefgh", 5).ShouldBe("abcde");
        BankFileFieldFormat.FixedWidth(null, 3).ShouldBe("   ");
    }

    [Fact]
    public void PaymentTypeText_Should_Match_The_Legacy_Upper_Case_Enum_Names()
    {
        BankFileFieldFormat.PaymentTypeText(PaymentType.Bank).ShouldBe("BANK");
        BankFileFieldFormat.PaymentTypeText(PaymentType.Cash).ShouldBe("CASH");
        BankFileFieldFormat.PaymentTypeText(PaymentType.Na).ShouldBe("NA");
    }

    [Fact]
    public void Every_Grid_Format_Should_Emit_Cells_Aligned_To_Its_Columns()
    {
        foreach (var formatter in BankFileGenerator.DefaultFormatters())
        {
            var doc = formatter.Build(Context(formatter.PayerBankCode));
            if (doc.Kind != BankFileDocumentKind.Grid)
            {
                continue;
            }

            doc.Columns.Count.ShouldBeGreaterThan(0);
            foreach (var row in doc.Rows)
            {
                row.Count.ShouldBe(doc.Columns.Count,
                    $"{formatter.FormatCode}: row width {row.Count} != column count {doc.Columns.Count}");
            }
        }
    }

    [Fact]
    public void Every_Text_Format_Should_Emit_A_Header_And_A_Trailer_Even_With_No_Payees()
    {
        var empty = new List<BankFilePayeeRow>();

        foreach (var formatter in BankFileGenerator.DefaultFormatters())
        {
            var doc = formatter.Build(Context(formatter.PayerBankCode, empty));
            if (doc.Kind != BankFileDocumentKind.Text)
            {
                continue;
            }

            Lines(doc).Length.ShouldBe(2, $"{formatter.FormatCode} should still emit header + trailer.");
        }
    }
}
