using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.BankFiles;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The run summary stored on every bank-file log row -- legacy <c>uscBankFile.GetParameterString()</c>
/// (<c>uscBankFile.ascx.cs:2813-2856</c>) -- and the Payment Type radio's rule. Pure, no database.
/// </summary>
// ReSharper disable once InconsistentNaming
public class BankFileRunParameters_Tests
{
    [Fact]
    public void Should_Write_Every_Filter_In_Legacy_Order_And_Wording()
    {
        var parameters = new BankFileRunParameters
        {
            PayrollAreaName = "Main",
            PeriodEndText = "31/08/2026",
            PaymentBankText = "CBK-Commercial Bank",
            PayrollRunTypeText = BankFileRunParameters.RunTypeText("A"),
            PaymentTypeText = BankFileRunParameters.PaymentTypeTextOf(PaymentType.Bank),
            PeriodTypeText = BankFileRunParameters.PeriodTypeTextOf(BankFilePeriodType.Other),
            SponsorText = "Acme Holding",
            SponsorBankText = "CBK (3001017974)",
            MosalIdText = "552007152002",
            EmployeeCodes = "E001,E002",
            EmployeeBankText = "NBK-National Bank of Kuwait",
            SplitBankFiles = true
        };

        parameters.ToLegacyString().ShouldBe(
            "Payroll Area : Main | Period : 31/08/2026 | Payment Bank : CBK-Commercial Bank | " +
            "Payroll RunType : Regular+OffCycle | Payment Type : BANK | Period Type : Other | Sponsor : Acme Holding | " +
            "Sponsor Bank (A/C) : CBK (3001017974) | Mosal Id : 552007152002 | Employee Code : E001,E002 | " +
            "Employee Bank : NBK-National Bank of Kuwait | Generate Separate Bank File(Employee Bank wise) : YES");
    }

    [Fact]
    public void Should_Leave_Out_Blank_Filters_But_Always_Say_Payment_Type_And_Split()
    {
        new BankFileRunParameters().ToLegacyString().ShouldBe(
            "Payment Type : ALL | Generate Separate Bank File(Employee Bank wise) : NO");
    }

    [Theory]
    [InlineData("R", "Regular")]
    [InlineData("O", "OffCycle")]
    [InlineData("A", "Regular+OffCycle")]
    [InlineData("RO", "Regular+OffCycle")]
    public void Should_Name_The_Run_Type_As_Legacys_Combo_Did(string value, string expected)
    {
        BankFileRunParameters.RunTypeText(value).ShouldBe(expected);
    }

    [Fact]
    public void Should_Leave_Out_The_Period_Type_For_A_Caller_That_Gave_Dates()
    {
        BankFileRunParameters.PeriodTypeTextOf(null).ShouldBeNull();
        BankFileRunParameters.PeriodTypeTextOf(BankFilePeriodType.Current).ShouldBe("Current");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(PaymentType.Na, null)]
    [InlineData(PaymentType.Bank, PaymentType.Bank)]
    [InlineData(PaymentType.Cash, PaymentType.Cash)]
    public void Should_Read_Only_Bank_And_Cash_As_A_Payment_Type_Filter(PaymentType? selected, PaymentType? expected)
    {
        BankFilePaymentTypeFilter.FilterOf(selected).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, "ALL")]
    [InlineData(PaymentType.Na, "ALL")]
    [InlineData(PaymentType.Bank, "BANK")]
    [InlineData(PaymentType.Cash, "CASH")]
    public void Should_Name_The_Payment_Type_As_Legacys_Radios(PaymentType? selected, string expected)
    {
        BankFileRunParameters.PaymentTypeTextOf(selected).ShouldBe(expected);
    }
}
