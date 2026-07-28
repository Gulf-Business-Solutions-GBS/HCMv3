using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.PayrollManagement.Costing;

/// <summary>
/// One general-ledger journal-voucher line produced from the payroll cost register.
///
/// <para>This is the in-memory replacement for the untyped <c>System.Data.DataTable</c> that BOTH legacy
/// screens built in their private <c>ExportTable(...)</c> method:
/// <c>HCM10.Web/UserControls/PayrollManagement/uscCostingFile.ascx.cs</c> lines 1760-2174 and
/// <c>uscPostJV.ascx.cs</c> lines 1052-1343. The property names below map 1:1 onto the legacy
/// <c>DataColumn</c> names declared at <c>uscCostingFile.ascx.cs</c> lines 1804-1860; the exact legacy
/// column captions (which are NOT valid C# identifiers -- they contain spaces and dots) are preserved in
/// <see cref="JournalVoucherColumns"/> so the exported spreadsheet header row is byte-identical to the
/// legacy export.</para>
///
/// <para><b>Sign convention (legacy parity).</b> <see cref="Debit"/> and <see cref="Credit"/> are both
/// stored as NON-NEGATIVE magnitudes; the side is chosen by wage-type <see cref="BillingType"/>, never by
/// sign. Legacy <c>uscCostingFile.ascx.cs</c> lines 1904-1905:
/// <c>Debit = (BillingType == Earning || BillingType == Provision) ? Amount : 0</c> and
/// <c>Credit = (BillingType == Deduction) ? Amount : 0</c>. A negative <c>CostRegisterDetail.Amount</c>
/// therefore flows through as a negative debit, exactly as legacy did -- this port does NOT "fix" that by
/// flipping sides.</para>
/// </summary>
public class JournalVoucherLine
{
    /// <summary>Legacy column "Valid". Always written as empty string by legacy (line 1888).</summary>
    public string Valid { get; set; } = "";

    /// <summary>
    /// Legacy column "Company accounts" = <c>CostRegisterDetail.CostElement1.Code</c> (legacy line 1884),
    /// i.e. cost-element slot 1 is the FI company. Falls back to the payroll area's FI company on the
    /// synthetic Net Pay line (legacy line 2130).
    /// </summary>
    public string CompanyAccounts { get; set; }

    /// <summary>Legacy column "Journal batch number" = the single <c>AXBatch.JournalBatchNumber</c> row.</summary>
    public string JournalBatchNumber { get; set; }

    /// <summary>Legacy column "RecId". Sequential, 1-based, shared counter with <see cref="LineNumber"/>.</summary>
    public int RecId { get; set; }

    /// <summary>Legacy column "Line number". Always equal to <see cref="RecId"/> in legacy.</summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Legacy column "Line number.Date" -- the POSTING date formatted with legacy's
    /// <c>dateformat = "MM/dd/yyyy"</c> static (<c>uscCostingFile.ascx.cs</c> line 46). Note that
    /// <c>uscPostJV</c> used "dd/MM/yyyy" here instead (line 1213); see
    /// <see cref="JournalVoucherBuilder"/> remarks for how that divergence is resolved.
    /// </summary>
    public string LineNumberDate { get; set; }

    /// <summary>
    /// Legacy column "Line number.Account type" = <c>CostRegisterDetail.AccountInfo.AccountType.NameEn</c>
    /// ("Ledger" / "Bank" / "Customer"). Drives the D365 <c>AccountDisplayValue</c> assembly.
    /// </summary>
    public string LineNumberAccountType { get; set; }

    /// <summary>Legacy column "LedgerDimension.MainAccount" = <c>AccountInfo.Code</c>.</summary>
    public string LedgerDimensionMainAccount { get; set; }

    /// <summary>Legacy column "LedgerDimension.MainAccountName" = <c>AccountInfo.NameEn</c>.</summary>
    public string LedgerDimensionMainAccountName { get; set; }

    /// <summary>Legacy column "DefaultDimension.Location" = cost-element slot 7 code (legacy line 1896).</summary>
    public string DefaultDimensionLocation { get; set; }

    /// <summary>Legacy column "DefaultDimension.Employees" = cost-element slot 6 code (legacy line 1897).</summary>
    public string DefaultDimensionEmployees { get; set; }

    /// <summary>Legacy column "LedgerDimension.Location" = cost-element slot 2 code (legacy line 1898).</summary>
    public string LedgerDimensionLocation { get; set; }

    /// <summary>Legacy column "LedgerDimension.Brand" = cost-element slot 3 code (legacy line 1899).</summary>
    public string LedgerDimensionBrand { get; set; }

    /// <summary>Legacy column "LedgerDimension.Department" = cost-element slot 4 code (legacy line 1900).</summary>
    public string LedgerDimensionDepartment { get; set; }

    /// <summary>Legacy column "LedgerDimension.Employees" = cost-element slot 5 code (legacy line 1901).</summary>
    public string LedgerDimensionEmployees { get; set; }

    /// <summary>
    /// Legacy column "Currency". NOTE the legacy quirk faithfully reproduced here: although the projection
    /// computes <c>Currency = CostRegisterDetail.Currency.NameEn</c> (line 1902), the value actually written
    /// to the row comes from the EMPLOYEE'S BASIC-PAY currency short form (lines 1993-2001), and is left
    /// blank when the employee has no basic pay covering the period.
    /// </summary>
    public string Currency { get; set; } = "";

    /// <summary>Legacy column "Exchange rate". Always empty string in the export (legacy line 1903).</summary>
    public string ExchangeRate { get; set; } = "";

    /// <summary>Legacy column "Debit". Non-negative magnitude; see class remarks on sign convention.</summary>
    public double Debit { get; set; }

    /// <summary>Legacy column "Credit". Non-negative magnitude; see class remarks on sign convention.</summary>
    public double Credit { get; set; }

    /// <summary>Legacy column "Description" = <c>WageType.NameEn</c>, or the literal "Net Pay" on the net line.</summary>
    public string Description { get; set; }

    /// <summary>Legacy column "Voucher". Always empty string.</summary>
    public string Voucher { get; set; } = "";

    /// <summary>
    /// Legacy column "Offset account type" = <c>EmployeeWageAccount.OffsetAccountInfo.AccountType.NameEn</c>.
    /// Legacy leaves the EXPORTED cell blank for normal lines (the assignment is commented out at line 2009)
    /// but uses the value to populate the account type of the generated offset line (line 2041).
    /// </summary>
    public string OffsetAccountType { get; set; } = "";

    /// <summary>
    /// Legacy column "OffsetLedgerDimension.MainAccount" = <c>EmployeeWageAccount.OffsetAccountInfo.Code</c>.
    /// Also the D365 <c>OffsetAccountDisplayValue</c> posted per line.
    /// </summary>
    public string OffsetLedgerDimensionMainAccount { get; set; } = "";

    /// <summary>Legacy column "Offset company accounts". Always empty string.</summary>
    public string OffsetCompanyAccounts { get; set; } = "";

    /// <summary>
    /// Legacy column "BillingType". Either the wage type's <see cref="HCMv3.PayrollManagement.BillingType"/>
    /// name, or the literal "Provision" when the wage type code is in
    /// <see cref="JournalVoucherBuilder.ProvisionWageTypeCodes"/> (legacy line 1916). Used to split the
    /// salary JV from the provision JV when
    /// <c>ManageFIPostingSettings.SeparateProvisionPosting</c> is on. Stripped from the spreadsheet export
    /// (legacy lines 1742-1743).
    /// </summary>
    public string BillingType { get; set; }

    /// <summary>
    /// Legacy column "IsMerged". Marks a line that ACCUMULATES several employees'/wage-types' offset
    /// amounts into a single GL row. Stripped from the spreadsheet export (legacy lines 1744-1745).
    /// </summary>
    public bool IsMerged { get; set; }

    /// <summary>Legacy column "TransDate" -- the posting date as a real <c>DateTime</c> (legacy line 2014).</summary>
    public DateTime TransDate { get; set; }

    /// <summary>Legacy column "EmpCode" = <c>Employee.Code</c>; empty string on the Net Pay line.</summary>
    public string EmpCode { get; set; } = "";
}

/// <summary>
/// The literal legacy spreadsheet column captions, in legacy declaration order
/// (<c>uscCostingFile.ascx.cs</c> lines 1804-1860). Kept as constants so the exported header row is
/// identical to what finance already receives from the legacy screen.
/// </summary>
public static class JournalVoucherColumns
{
    public const string Valid = "Valid";
    public const string CompanyAccounts = "Company accounts";
    public const string JournalBatchNumber = "Journal batch number";
    public const string RecId = "RecId";
    public const string LineNumber = "Line number";
    public const string LineNumberDate = "Line number.Date";
    public const string LineNumberAccountType = "Line number.Account type";
    public const string LedgerDimensionMainAccount = "LedgerDimension.MainAccount";
    public const string LedgerDimensionMainAccountName = "LedgerDimension.MainAccountName";
    public const string DefaultDimensionLocation = "DefaultDimension.Location";
    public const string DefaultDimensionEmployees = "DefaultDimension.Employees";
    public const string LedgerDimensionLocation = "LedgerDimension.Location";
    public const string LedgerDimensionBrand = "LedgerDimension.Brand";
    public const string LedgerDimensionDepartment = "LedgerDimension.Department";
    public const string LedgerDimensionEmployees = "LedgerDimension.Employees";
    public const string Currency = "Currency";
    public const string ExchangeRate = "Exchange rate";
    public const string Debit = "Debit";
    public const string Credit = "Credit";
    public const string Description = "Description";
    public const string Voucher = "Voucher";
    public const string OffsetAccountType = "Offset account type";
    public const string OffsetLedgerDimensionMainAccount = "OffsetLedgerDimension.MainAccount";
    public const string OffsetCompanyAccounts = "Offset company accounts";
    public const string BillingType = "BillingType";
    public const string IsMerged = "IsMerged";
    public const string TransDate = "TransDate";
    public const string EmpCode = "EmpCode";

    /// <summary>
    /// Export order for the spreadsheet, with <see cref="BillingType"/> and <see cref="IsMerged"/> removed
    /// -- legacy drops exactly those two before binding the grid (<c>uscCostingFile.ascx.cs</c> lines
    /// 1742-1745).
    /// </summary>
    public static readonly string[] ExportOrder =
    {
        Valid, CompanyAccounts, JournalBatchNumber, RecId, LineNumber, LineNumberDate,
        LineNumberAccountType, LedgerDimensionMainAccount, LedgerDimensionMainAccountName,
        DefaultDimensionLocation, DefaultDimensionEmployees, LedgerDimensionLocation,
        LedgerDimensionBrand, LedgerDimensionDepartment, LedgerDimensionEmployees, Currency,
        ExchangeRate, Debit, Credit, Description, Voucher, OffsetAccountType,
        OffsetLedgerDimensionMainAccount, OffsetCompanyAccounts, TransDate, EmpCode
    };
}

/// <summary>Input for both the cost engine and the JV builder -- the legacy 9-element <c>args</c> array.</summary>
public class CostingRunInput
{
    /// <summary>Legacy <c>args[4]</c>: the payroll area NAME (legacy matched <c>PayrollArea.Name</c>, not code).</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>
    /// Legacy <c>args[1]</c>: comma-separated employee codes, or empty for "all employees in the payroll
    /// area". Parsed by <c>GetEmpCodeList</c> (legacy <c>uscCostingFile.ascx.cs</c> lines 2489-2498).
    /// </summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy <c>args[2]</c>: the pay-in period date (normalised to the last day of that month).</summary>
    public DateTime PayInDate { get; set; }

    /// <summary>Legacy <c>args[5]</c>: the GL posting date stamped on every JV line.</summary>
    public DateTime PostingDate { get; set; }

    /// <summary>Legacy <c>args[0]</c>: "R" -> Regular, "O" -> OffCycle.</summary>
    public PayrollRunType PayrollRunType { get; set; }
}

/// <summary>Per-employee outcome of a cost-engine run, mirroring legacy's <c>ProjectLog</c> rows.</summary>
public class CostingRunLogEntry
{
    public int? EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    /// <summary>Legacy <c>ProjectLog.Status</c>: 0 = success, 1 = failed.</summary>
    public int Status { get; set; }

    public string Message { get; set; }
}

/// <summary>Aggregate result of a cost-engine run.</summary>
public class CostingRunResult
{
    public int TotalEmployees { get; set; }

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    /// <summary>True when the run stopped early because a <c>ProjectLog.StopFlag</c> row was present.</summary>
    public bool Aborted { get; set; }

    public List<CostingRunLogEntry> Entries { get; set; } = new List<CostingRunLogEntry>();
}

/// <summary>Result of building the JV line set, plus its debit/credit balance check.</summary>
public class JournalVoucherResult
{
    public List<JournalVoucherLine> Lines { get; set; } = new List<JournalVoucherLine>();

    /// <summary>
    /// Sum of <see cref="JournalVoucherLine.Debit"/> rounded to whole units -- legacy validated balance with
    /// <c>Math.Round(dt.Compute("SUM(Debit)",""))</c>, i.e. ZERO decimal places
    /// (<c>uscCostingFile.ascx.cs</c> line 1719).
    /// </summary>
    public double TotalDebit => Math.Round(Lines.Sum(l => l.Debit));

    /// <summary>Sum of <see cref="JournalVoucherLine.Credit"/>, rounded the same way (legacy line 1720).</summary>
    public double TotalCredit => Math.Round(Lines.Sum(l => l.Credit));

    /// <summary>
    /// Legacy balance test, verbatim: <c>debitamt != creditamt</c> AFTER whole-unit rounding
    /// (legacy line 1722). Sub-unit rounding remainders are therefore tolerated by design, exactly as in
    /// legacy -- see <see cref="JournalVoucherBuilder"/> remarks.
    /// </summary>
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < double.Epsilon;
}
