using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Legacy <c>cmbType</c> of the Payroll JV (Custom 017) screen (<c>BindType()</c>, line 241).
///
/// INERT: unlike Custom 019 and Custom 023, the LIVE <c>Table1()</c> of Custom 017 (lines 558-1011)
/// never inspects <c>cmbType</c> -- it always builds the Dynamics AX journal-import layout. The combo
/// only reaches the execution-log parameter string (line 547) and the export FILE NAME (line 1605). It
/// is kept here for both of those, and pinned by a test.
/// </summary>
public enum PayrollJVCustom017Format
{
    MainFormat = 0,

    ProvisionFormat = 1,

    MissingAccounts = 2,

    EmployeeWiseBreakDown = 3
}

/// <summary>
/// Filters of the Payroll JV (Custom 017) report -- legacy <c>uscRptPayrollJV_Custom017.ascx</c> lines
/// 21-42: <c>txtMonth</c>, <c>txtYear</c>, <c>cmbRunType</c>, <c>cmbPayrollArea</c>, <c>cmbType</c> and
/// <c>txtEmpCode</c>. The inherited date range and employee-status combo are not part of this panel and
/// are ignored; the inherited <see cref="GridReportInputBase.EmployeeCode"/> IS used.
/// </summary>
public class GetPayrollJVCustom017ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Blank meant "current month".</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank meant "current year".</summary>
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>, applied at legacy line 653.</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>
    /// Legacy <c>cmbPayrollArea</c>. Besides filtering the salary register through the employee's org
    /// assignment (legacy lines 659-668) this ALSO supplies the closing Net Pay line's company, bank and
    /// currency (legacy lines 985-991) -- so leaving it blank produces a Net Pay line with those three
    /// fields empty, exactly as legacy did.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Legacy <c>cmbType</c>. See <see cref="PayrollJVCustom017Format"/> -- it is inert.</summary>
    public PayrollJVCustom017Format Format { get; set; } = PayrollJVCustom017Format.MainFormat;

    protected override string DefaultSorting => "LineNumber ASC";
}

/// <summary>
/// One line of the Dynamics AX journal-import file the Payroll JV (Custom 017) report produces --
/// legacy <c>Table1()</c> (<c>uscRptPayrollJV_Custom017.ascx.cs</c> lines 558-1011), whose
/// <c>DataTable</c> has the 27 columns declared at legacy lines 594-620.
///
/// The layout is an AX import contract, so the property names deliberately mirror the AX field names
/// rather than being renamed to HCM vocabulary.
/// </summary>
public class PayrollJVCustom017ReportRowDto : EntityDto<int>
{
    /// <summary>
    /// Legacy "Oid" column. DECLARED BUT NEVER FILLED by legacy's fill loop (lines 844-880), so it is
    /// always blank. Kept as the stable sort key here and NOT shown as a data column.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy "Valid": always empty string.</summary>
    public string Valid { get; set; } = string.Empty;

    /// <summary>Legacy "Company accounts" = <c>CostElement1.Code</c> (legacy line 799).</summary>
    public string CompanyAccounts { get; set; }

    /// <summary>Legacy "Journal batch number", taken from the single <c>AXBatch</c> row.</summary>
    public string JournalBatchNumber { get; set; }

    /// <summary>Legacy "RecId": the running line counter.</summary>
    public int RecId { get; set; }

    /// <summary>Legacy "Line number": the same running counter as <see cref="RecId"/>.</summary>
    public int AxLineNumber { get; set; }

    /// <summary>
    /// Legacy "Line number.Date": the month end formatted "MM/dd/yyyy" (legacy <c>dateformat</c>, line
    /// 54). Emitted as TEXT because AX's importer expects that exact string.
    /// </summary>
    public string LineNumberDate { get; set; }

    /// <summary>Legacy "Line number.Account type" = the account's <c>AccountType.NameEn</c>.</summary>
    public string LineNumberAccountType { get; set; }

    /// <summary>Legacy "LedgerDimension.MainAccount" = <c>AccountInfo.Code</c>.</summary>
    public string LedgerDimensionMainAccount { get; set; }

    /// <summary>Legacy "LedgerDimension.MainAccountName" = <c>AccountInfo.NameEn</c>.</summary>
    public string LedgerDimensionMainAccountName { get; set; }

    /// <summary>Legacy "DefaultDimension.Location" = <c>CostElement7.Code</c>.</summary>
    public string DefaultDimensionLocation { get; set; }

    /// <summary>Legacy "DefaultDimension.Employees" = <c>CostElement6.Code</c>, or CostElement5 for
    /// Customer accounts (legacy line 855).</summary>
    public string DefaultDimensionEmployees { get; set; }

    /// <summary>Legacy "LedgerDimension.Location" = <c>CostElement2.Code</c>, blanked for Customer
    /// accounts.</summary>
    public string LedgerDimensionLocation { get; set; }

    /// <summary>Legacy "LedgerDimension.Brand" = <c>CostElement3.Code</c>.</summary>
    public string LedgerDimensionBrand { get; set; }

    /// <summary>Legacy "LedgerDimension.Department" = <c>CostElement4.Code</c>, blanked for Customer
    /// accounts.</summary>
    public string LedgerDimensionDepartment { get; set; }

    /// <summary>Legacy "LedgerDimension.Employees" = <c>CostElement5.Code</c>, blanked for Customer
    /// accounts.</summary>
    public string LedgerDimensionEmployees { get; set; }

    /// <summary>
    /// Legacy "Currency": the currency short form of ANY basic-pay detail of the employee current at the
    /// month end (legacy line 803).
    /// </summary>
    public string Currency { get; set; }

    /// <summary>Legacy "Exchange rate": always empty string.</summary>
    public string ExchangeRate { get; set; } = string.Empty;

    /// <summary>Legacy "Debit", rounded to 3 decimals (legacy <c>decimalPlaces</c>, line 53).</summary>
    public double Debit { get; set; }

    /// <summary>Legacy "Credit", rounded to 3 decimals.</summary>
    public double Credit { get; set; }

    /// <summary>Legacy "Description" = the wage type's English name (or "Net Pay" on the closing line).</summary>
    public string Description { get; set; }

    /// <summary>Legacy "Voucher": always empty string.</summary>
    public string Voucher { get; set; } = string.Empty;

    /// <summary>
    /// Legacy "Offset account type". DECLARED BUT NEVER FILLED on posting rows -- see the app service
    /// remarks; legacy writes the offset account onto its OWN row instead.
    /// </summary>
    public string OffsetAccountType { get; set; } = string.Empty;

    /// <summary>Legacy "OffsetLedgerDimension.MainAccount". Never filled on posting rows.</summary>
    public string OffsetLedgerDimensionMainAccount { get; set; } = string.Empty;

    /// <summary>Legacy "Offset company accounts": always empty string.</summary>
    public string OffsetCompanyAccounts { get; set; } = string.Empty;

    /// <summary>
    /// Legacy "BillingType": the literal "Provision" for the two provision-increment wage types
    /// (1066/1067), otherwise the wage type's billing type name.
    /// </summary>
    public string BillingType { get; set; }

    /// <summary>Legacy "IsMerged": true on a row that collapses several offset postings.</summary>
    public bool IsMerged { get; set; }
}

/// <summary>Paged result plus the server-side column list that drives both the grid and the xlsx.</summary>
public class PayrollJVCustom017ReportOutput : PagedResultDto<PayrollJVCustom017ReportRowDto>
{
    public List<PayrollJVReportColumnDto> Columns { get; set; } = new();
}
