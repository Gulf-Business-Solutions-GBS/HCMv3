using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HCMv3.PayrollManagement.BankFiles;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The record count and total each layout reports about itself. The screen showed 0 as the total of every text
/// file because the service could only add up a spreadsheet's amount column; each text layout now states the
/// figures its own trailer prints. Pure, no database.
/// </summary>
// ReSharper disable once InconsistentNaming
public class BankFileDocumentTotals_Tests
{
    private static readonly string[] TextFormats =
    {
        BankFileFormatCodes.CbkT1, BankFileFormatCodes.CbkT2, BankFileFormatCodes.CbkT3, BankFileFormatCodes.CbkT4,
        BankFileFormatCodes.CbkT5, BankFileFormatCodes.CbkT6, BankFileFormatCodes.CbkT7,
        BankFileFormatCodes.GbkT1, BankFileFormatCodes.BurT1, BankFileFormatCodes.BurBdT1
    };

    public static IEnumerable<object[]> TextFormatCases() => TextFormats.Select(f => new object[] { f });

    private static BankFileContext Context(IEnumerable<BankFilePayeeRow> rows) =>
        new()
        {
            EndDate = new DateTime(2024, 10, 31),
            GeneratedOn = new DateTime(2024, 11, 5, 9, 30, 0),
            PayerBankCode = "CBK",
            PayerBankExternalCode = "008",
            OrganizationBankAccountNo = "3001017974",
            SponsorBankAccountNumber = "3001017974",
            Rows = rows.ToList()
        };

    private static BankFilePayeeRow Payee(int id, double amount, string account = "1234567890", string iban = "KW81NBOK0000000000001234560101") =>
        new()
        {
            EmployeeId = id,
            EmployeeCode = "E" + id.ToString("000"),
            FullNameEn = "Payee " + id,
            CivilId = 290010100000 + id,
            HasBankRecord = true,
            EmployeeBankId = 5,
            EmployeeBankCode = "NBK",
            AccountNum = account,
            Iban = iban,
            PaymentType = PaymentType.Bank,
            PayeeBankCode = "101",
            Amount = amount
        };

    private static List<BankFilePayeeRow> Payees() => new()
    {
        Payee(1, 1056.54),
        Payee(2, 250.125),
        // Never paid by any text layout: nothing to transfer.
        Payee(3, 0)
    };

    [Theory]
    [MemberData(nameof(TextFormatCases))]
    public void Every_Text_Format_Should_Report_What_It_Pays(string formatCode)
    {
        var doc = new BankFileGenerator().GenerateByFormatCode(Context(Payees()), formatCode);

        doc.RecordCount.ShouldBe(2);
        doc.TotalAmount.ShouldNotBeNull();
        doc.TotalAmount.Value.ShouldBe(1306.665, 0.0005);
    }

    [Theory]
    [MemberData(nameof(TextFormatCases))]
    public void Every_Text_Format_Should_Report_Zero_For_An_Empty_File(string formatCode)
    {
        var doc = new BankFileGenerator().GenerateByFormatCode(Context(new List<BankFilePayeeRow>()), formatCode);

        doc.RecordCount.ShouldBe(0);
        doc.TotalAmount.ShouldBe(0d);
    }

    [Fact]
    public void The_Reported_Total_Should_Be_The_One_The_Trailer_Prints()
    {
        var doc = new BankFileGenerator().GenerateByFormatCode(Context(Payees()), BankFileFormatCodes.CbkT1);

        var trailer = doc.TextContent.Split('\n').Select(l => l.TrimEnd('\r')).Last(l => l.Length > 0);
        trailer.ShouldEndWith(Math.Round(doc.TotalAmount!.Value, 3).ToString("0.000", CultureInfo.InvariantCulture));
        trailer.ShouldContain(doc.RecordCount!.Value.ToString().PadLeft(5, '0'));
    }

    [Fact]
    public void A_Per_Register_Format_Should_Count_And_Total_Each_Line_It_Writes()
    {
        // LEGACY AS-IS: CBK-T3 writes one line per salary register, each with the employee's FULL amount, so an
        // employee with two registers is paid twice in the file. The figures reported are the file's own.
        var twoRegisters = Payee(1, 100);
        twoRegisters.RegisterCount = 2;

        var doc = new BankFileGenerator().GenerateByFormatCode(Context(new[] { twoRegisters }), BankFileFormatCodes.CbkT3);

        doc.RecordCount.ShouldBe(2);
        doc.TotalAmount.ShouldBe(200d);
    }

    [Fact]
    public void Cbk_G2_Should_Not_Count_Its_Grand_Total_Row_As_A_Payment()
    {
        var rows = Payees();
        var context = Context(rows);
        context.GrandTotalAmountAllBanks = rows.Sum(r => r.Amount);

        var doc = new BankFileGenerator().GenerateByFormatCode(context, BankFileFormatCodes.CbkG2);

        doc.Rows.Count.ShouldBe(4); // three payees + the grand-total row
        doc.RecordCount.ShouldBe(3);
        doc.TotalAmount!.Value.ShouldBe(1306.665, 0.0005);
    }

    [Fact]
    public void A_Grid_Format_Without_Its_Own_Figures_Should_Leave_Them_To_The_Caller()
    {
        var doc = new BankFileGenerator().GenerateByFormatCode(Context(Payees()), BankFileFormatCodes.NbkG1);

        doc.RecordCount.ShouldBeNull();
        doc.TotalAmount.ShouldBeNull();
    }
}
