using System.Collections.Generic;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.BankFiles;

/// <summary>
/// The screen's choices for one bank-file run, already turned into the text legacy showed for them, and the
/// one-line summary legacy stored on the run's log row.
///
/// <para>Legacy <c>GetParameterString()</c> (<c>uscBankFile.ascx.cs:2813-2856</c>): "Label : value" parts in
/// a fixed order, joined with " | ". The labels are legacy's English literals on purpose -- the string is
/// stored data, read back as-is on the log screen, and it must read the same as the rows legacy wrote.</para>
/// </summary>
public class BankFileRunParameters
{
    /// <summary>Legacy <c>cmbPayrollArea.Text</c>.</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy <c>txtEndDate.Text</c>, dd/MM/yyyy.</summary>
    public string PeriodEndText { get; set; }

    /// <summary>Legacy <c>ctlPaymentBank.SelectedItem.Text</c>: <c>Code + "-" + NameEn</c>.</summary>
    public string PaymentBankText { get; set; }

    /// <summary>Legacy <c>cmbPayrollRunType.Text</c>: Regular / OffCycle / Regular+OffCycle.</summary>
    public string PayrollRunTypeText { get; set; }

    /// <summary>ALL / BANK / CASH.</summary>
    public string PaymentTypeText { get; set; }

    /// <summary>Legacy <c>rdCurrent.Text</c> / <c>rdOtherPeriod.Text</c>; omitted when the caller gave dates directly.</summary>
    public string PeriodTypeText { get; set; }

    /// <summary>Legacy <c>cmbSponsor.Text</c>.</summary>
    public string SponsorText { get; set; }

    /// <summary>Legacy <c>cmbSponsorBank.Text</c>: <c>Code + " (" + AccountNumber + ")"</c>.</summary>
    public string SponsorBankText { get; set; }

    /// <summary>Legacy <c>cmbMosalId.Text</c>.</summary>
    public string MosalIdText { get; set; }

    /// <summary>Legacy <c>txtEmpCode.Text</c>.</summary>
    public string EmployeeCodes { get; set; }

    /// <summary>Legacy <c>cmbEmpBank.Text</c>: <c>Code + "-" + NameEn</c>.</summary>
    public string EmployeeBankText { get; set; }

    /// <summary>Legacy <c>chkGenerateAllBankFiles.Checked</c>.</summary>
    public bool SplitBankFiles { get; set; }

    /// <summary>Legacy combo values "R" / "O" / "RO" (HCMv3's screen sends "A" for the last).</summary>
    public static string RunTypeText(string payrollRunType) => payrollRunType switch
    {
        "R" => "Regular",
        "O" => "OffCycle",
        _ => "Regular+OffCycle"
    };

    public static string PaymentTypeTextOf(PaymentType? paymentType) =>
        BankFilePaymentTypeFilter.FilterOf(paymentType) switch
        {
            PaymentType.Bank => "BANK",
            PaymentType.Cash => "CASH",
            _ => "ALL"
        };

    public static string PeriodTypeTextOf(BankFilePeriodType? periodType) => periodType switch
    {
        BankFilePeriodType.Current => "Current",
        BankFilePeriodType.Other => "Other",
        _ => null
    };

    /// <summary>Legacy order and labels, verbatim (including "Payroll RunType" and the missing space in "File(").</summary>
    public string ToLegacyString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(PayrollAreaName))
        {
            parts.Add("Payroll Area : " + PayrollAreaName);
        }

        if (!string.IsNullOrEmpty(PeriodEndText))
        {
            parts.Add("Period : " + PeriodEndText);
        }

        if (!string.IsNullOrEmpty(PaymentBankText))
        {
            parts.Add("Payment Bank : " + PaymentBankText);
        }

        if (!string.IsNullOrEmpty(PayrollRunTypeText))
        {
            parts.Add("Payroll RunType : " + PayrollRunTypeText);
        }

        parts.Add("Payment Type : " + (PaymentTypeText ?? "ALL"));

        // Legacy "Org Unit (Bank A/C)" sits here; its combo is hidden on the live screen, so it never appears.

        if (!string.IsNullOrEmpty(PeriodTypeText))
        {
            parts.Add("Period Type : " + PeriodTypeText);
        }

        if (!string.IsNullOrEmpty(SponsorText))
        {
            parts.Add("Sponsor : " + SponsorText);
        }

        if (!string.IsNullOrEmpty(SponsorBankText))
        {
            parts.Add("Sponsor Bank (A/C) : " + SponsorBankText);
        }

        if (!string.IsNullOrEmpty(MosalIdText))
        {
            parts.Add("Mosal Id : " + MosalIdText);
        }

        if (!string.IsNullOrEmpty(EmployeeCodes))
        {
            parts.Add("Employee Code : " + EmployeeCodes);
        }

        if (!string.IsNullOrEmpty(EmployeeBankText))
        {
            parts.Add("Employee Bank : " + EmployeeBankText);
        }

        parts.Add("Generate Separate Bank File(Employee Bank wise) : " + (SplitBankFiles ? "YES" : "NO"));

        return string.Join(" | ", parts);
    }
}

/// <summary>
/// The Payment Type radio (legacy <c>rdPaymentAll</c> / <c>rdPaymentBank</c> / <c>rdPaymentCash</c>,
/// <c>uscBankFile.ascx.cs:219-238</c>).
///
/// <para>ALL is NOT "no filter": it still keeps only employees with an employee-bank record in force on the
/// period's end date, of any payment type. BANK and CASH additionally require that record's payment type.
/// The screen cannot send NA, so NA (like no value) is read as ALL.</para>
/// </summary>
public static class BankFilePaymentTypeFilter
{
    /// <summary>The payment type to require, or <c>null</c> for ALL.</summary>
    public static PaymentType? FilterOf(PaymentType? selected) =>
        selected is PaymentType.Bank or PaymentType.Cash ? selected : null;
}
