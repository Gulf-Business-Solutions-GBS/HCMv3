using System;
using System.Collections.Generic;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.BankFiles;

/// <summary>
/// One flattened, already-resolved payee line fed to a <see cref="IBankFileFormatter"/>.
///
/// The Application layer resolves every FK (employee bank, payee-bank mapping, sponsor) BEFORE calling the
/// domain generator, so <c>HCMv3.Core</c> stays free of any repository/EF dependency -- matching the legacy
/// code-behind's per-row lookups at <c>uscBankFile2.ascx.cs:469-471</c> (EmployeeBank effective on EndDate),
/// <c>:601</c> (PayeeBankDetail by PayeeBank) and <c>:608-610</c> (EmployeeSponsor effective on EndDate).
/// </summary>
public class BankFilePayeeRow
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string FullNameEn { get; set; }

    public long CivilId { get; set; }

    /// <summary>True when an <c>EmployeeBank</c> row effective on EndDate exists (legacy <c>empBank != null</c>).</summary>
    public bool HasBankRecord { get; set; }

    /// <summary>Employee bank's <c>Bank.Id</c>; null when the EmployeeBank row has no Bank FK. Split key.</summary>
    public int? EmployeeBankId { get; set; }

    /// <summary>Employee bank's <c>Bank.Code</c>; null when the EmployeeBank row has no Bank FK.</summary>
    public string EmployeeBankCode { get; set; }

    public string EmployeeBankNameEn { get; set; }

    public string EmployeeBankSwiftCode { get; set; }

    public string BankBranch { get; set; }

    public string AccountNum { get; set; }

    public string Iban { get; set; }

    public PaymentType PaymentType { get; set; }

    /// <summary>True when the employee's bank IS the payer bank (legacy <c>selectedBank == empBank.Bank</c>).</summary>
    public bool EmployeeBankIsPayerBank { get; set; }

    /// <summary>
    /// <c>PayeeBankDetail.BankCode</c> for (payerBank -&gt; employeeBank); "" when unmapped.
    /// Legacy <c>uscBankFile2.ascx.cs:601-602</c>.
    /// </summary>
    public string PayeeBankCode { get; set; }

    /// <summary><c>PayeeBankDetail.Bank.NameEn</c>; legacy <c>uscBankFile2.ascx.cs:2131</c>.</summary>
    public string PayeeBankNameEn { get; set; }

    public bool HasSponsor { get; set; }

    public string SponsorNameEn { get; set; }

    public long SponsorMosalId { get; set; }

    /// <summary>Net pay: sum of wage type 9000 amounts for this employee.</summary>
    public double Amount { get; set; }

    /// <summary><c>EmployeeOnHold.BankTransfer == OnHoldStatus.Hold</c> effective on EndDate.</summary>
    public bool IsBankTransferOnHold { get; set; }

    /// <summary>Employee's current company org-unit name (GBK-T1 only, legacy <c>:1342</c>).</summary>
    public string CurrentCompanyNameEn { get; set; }

    /// <summary>
    /// How many <c>SalaryRegister</c> rows this employee has in the run.
    ///
    /// FAITHFULNESS DETAIL: legacy iterates TWO different collections depending on the format --
    /// <c>salRegList</c> (distinct Employee/ForPeriod/InPeriod, i.e. one line per employee) for most formats,
    /// but the raw <c>allSalRegList</c> (one line per REGISTER) for CBK-T3/T5/T6/T7
    /// (<c>uscBankFile2.ascx.cs:832, 912, 986, 1122</c>). Because the amount is always re-summed per employee,
    /// an employee holding two registers (e.g. Regular + OffCycle) yields TWO identical detail lines with the
    /// FULL amount each, and is counted twice in the footer. That is a legacy defect, but it is ported
    /// faithfully -- see <see cref="BankFileRowExpansion"/>.
    /// </summary>
    public int RegisterCount { get; set; } = 1;
}

/// <summary>
/// Helper for the "iterate allSalRegList" formats. See <see cref="BankFilePayeeRow.RegisterCount"/>.
/// </summary>
public static class BankFileRowExpansion
{
    /// <summary>Yields each row once per salary register, reproducing legacy's per-register iteration.</summary>
    public static IEnumerable<BankFilePayeeRow> PerRegister(IReadOnlyList<BankFilePayeeRow> rows)
    {
        foreach (var row in rows)
        {
            var times = row.RegisterCount < 1 ? 1 : row.RegisterCount;
            for (var i = 0; i < times; i++)
            {
                yield return row;
            }
        }
    }
}

/// <summary>
/// Everything a formatter needs beyond the payee rows. Mirrors the legacy code-behind's captured locals.
/// </summary>
public class BankFileContext
{
    /// <summary>Legacy <c>EndDate</c> -- the period end, used for every header/footer date token.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Payer (payment) <c>Bank.Code</c>, e.g. "NBK", "CBK", "BURBD". Selects the switch branch.</summary>
    public string PayerBankCode { get; set; }

    /// <summary>Payer <c>Bank.ExternalCode</c>.</summary>
    public string PayerBankExternalCode { get; set; }

    /// <summary>Payer <c>Bank.CompanyCode</c> (BUR-G2 only, legacy <c>:2058</c>).</summary>
    public string PayerBankCompanyCode { get; set; }

    /// <summary>
    /// <c>SponsorBank.AccountNumber</c> for the chosen sponsor bank. CBK-T7 header only (legacy <c>:809</c>).
    /// </summary>
    public string SponsorBankAccountNumber { get; set; }

    /// <summary>
    /// <c>OrganizationBankInfo.AccountNo</c> for the company-level org unit. Used by the CBK-T1..T6 headers
    /// (legacy <c>:896-901</c> etc.).
    /// </summary>
    public string OrganizationBankAccountNo { get; set; }

    /// <summary>
    /// MOSAL ID of the SELECTED sponsor filter (BUR-T1 detail line, legacy <c>:1995</c>) -- note this is the
    /// screen's sponsor combo, not the employee's own sponsor. Null when no sponsor was selected.
    /// </summary>
    public long? SelectedSponsorMosalId { get; set; }

    /// <summary>
    /// True when the screen's Employee Bank filter had a selection. Drives BUR-T1's header quirk at
    /// legacy <c>:1967</c> (<c>EmpBankExtCode = empBank2 == null ? SponsorBankExtCode : ""</c>).
    /// </summary>
    public bool EmployeeBankFilterSelected { get; set; }

    /// <summary>
    /// Grand total across ALL registers before per-bank splitting -- CBK-G2's total row uses the unsplit sum
    /// (legacy <c>:781</c> sums <c>salRegDetailList</c>, which is never re-filtered per split bank).
    /// </summary>
    public double GrandTotalAmountAllBanks { get; set; }

    /// <summary>
    /// "Now" at generation time. Legacy calls <c>DateTime.Now</c> inline in the GBK-T1 header
    /// (<c>uscBankFile2.ascx.cs:1314</c>) and in every output file name; hoisted onto the context so the
    /// domain stays deterministic and golden-file testable.
    /// </summary>
    public DateTime GeneratedOn { get; set; } = DateTime.Now;

    public IReadOnlyList<BankFilePayeeRow> Rows { get; set; } = new List<BankFilePayeeRow>();
}

/// <summary>Whether a format emits a fixed-width text file or a tabular grid (rendered to a spreadsheet).</summary>
public enum BankFileDocumentKind
{
    Text = 0,
    Grid = 1
}

/// <summary>A produced bank file, still abstract -- byte rendering happens in the Application layer.</summary>
public class BankFileDocument
{
    public BankFileDocumentKind Kind { get; set; }

    public string FormatCode { get; set; }

    /// <summary>Populated when <see cref="Kind"/> is <see cref="BankFileDocumentKind.Text"/>.</summary>
    public string TextContent { get; set; }

    /// <summary>Column headers, in order. Populated for <see cref="BankFileDocumentKind.Grid"/>.</summary>
    public List<string> Columns { get; set; } = new();

    /// <summary>
    /// Cell values per row, aligned to <see cref="Columns"/>. <c>null</c> means an empty cell (legacy
    /// <c>DBNull</c>). Numerics are boxed <c>double</c>/<c>int</c> so the spreadsheet keeps them numeric.
    /// </summary>
    public List<List<object>> Rows { get; set; } = new();

    /// <summary>
    /// Number of payment lines the file carries, as the format itself counted them -- for a text file the
    /// figure its trailer states. <c>null</c> when the format does not say, and the caller counts grid rows.
    /// </summary>
    public int? RecordCount { get; set; }

    /// <summary>
    /// Sum of the amounts the file pays, as the format itself added them up -- for a text file the trailer
    /// total. <c>null</c> when the format does not say, and the caller sums the grid's amount column.
    /// </summary>
    public double? TotalAmount { get; set; }
}
