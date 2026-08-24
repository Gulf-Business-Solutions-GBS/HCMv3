using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Payroll Sheet grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollSheet.ascx</c>).
///
/// ATYPICAL FILTER PANEL: legacy's panel is Payroll Area / Run Type / Reason Type / Month / Year /
/// TO Month / TO Year / Employee Code (uscRptPayrollSheet.ascx.cs:606-638) -- a MONTH RANGE rather than a
/// date range, and the TO half only counts when <c>ManageReportSettings.ShowPeriodInPayrollSheet</c> is on
/// (lines 652-671). Of the base filters only <see cref="GridReportInputBase.EmployeeCode"/> is used; the
/// date range and employee status are hidden on the Angular screen.
/// </summary>
public class GetPayrollSheetReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Null means the CURRENT month.</summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Null means the CURRENT year.</summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>
    /// Legacy <c>txtMonthTo</c>. IGNORED unless <c>ManageReportSettings.ShowPeriodInPayrollSheet</c> is on,
    /// in which case the report covers every month from (Month, Year) to (ToMonth, ToYear) inclusive.
    /// </summary>
    [Range(1, 12)]
    public int? ToMonth { get; set; }

    /// <summary>Legacy <c>txtYearTo</c>. See <see cref="ToMonth"/>.</summary>
    [Range(1900, 2999)]
    public int? ToYear { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Null means "all run types".</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>
    /// Legacy <c>cmbReasonType</c>, which held the reason type CODE and resolved it before filtering
    /// (lines 2863-2869). This port takes the id directly -- same filter, one query fewer.
    /// </summary>
    public int? PayrollReasonTypeId { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>, likewise taken here as an id rather than a code.</summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>
    /// Legacy ordered the sheet by employee Code inside each period (line 2895); the port makes that the
    /// deterministic default that server-side paging requires.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// Read result of the Payroll Sheet report. It is not a bare <c>PagedResultDto</c> because this report's
/// column set is DATA-DRIVEN: legacy built one column per wage type at render time (lines 2830-2846), so
/// the column names have to travel with the rows.
/// </summary>
public class PayrollSheetReportOutput : PagedResultDto<PayrollSheetReportRowDto>
{
    /// <summary>
    /// Legacy's <c>"Act " + wageTypeName</c> columns (line 2831): one per wage type appearing in ANY
    /// employee basic pay, ordered by <c>WageType.PayrollOrder</c>. Keys of
    /// <see cref="PayrollSheetReportRowDto.ActualBasicPayAmounts"/> (WITHOUT the "Act " prefix).
    /// </summary>
    public List<string> ActualBasicPayColumns { get; set; } = new();

    /// <summary>
    /// Legacy's earning/deduction columns (lines 2838-2845): the wage types present in the filtered
    /// registers whose <c>BillingType</c> is Earning or Deduction, that do not have
    /// <c>DontImpactNetPay</c>, and that carry a non-zero Rate, Number or Amount -- ordered by
    /// <c>PayrollOrder</c>. Keys of <see cref="PayrollSheetReportRowDto.WageAmounts"/>.
    /// </summary>
    public List<string> EarningDeductionColumns { get; set; } = new();

    /// <summary>
    /// The subset of <see cref="EarningDeductionColumns"/> that is also an <c>AbsenceType</c> wage type;
    /// legacy gave each of those an extra <c>"{name} Days"</c> column (line 2841). Keys of
    /// <see cref="PayrollSheetReportRowDto.WageDays"/>.
    /// </summary>
    public List<string> LeaveDaysColumns { get; set; } = new();
}

/// <summary>
/// One grid row of legacy <c>Table1()</c>'s "For ALL" branch (uscRptPayrollSheet.ascx.cs:2675-3348).
///
/// GRAIN: one row per (Employee, period) -- legacy looped the month list and grouped by employee inside
/// each month (lines 2851-2895).
///
/// SCOPE DECISION -- THE SIX CLIENT FORKS ARE NOT PORTED. Legacy <c>Table1()</c> is a chain of
/// <c>if (clientId == "004") ... else if ("007") ... ("008") ... ("019") ... ("024") ... ("026") ... else</c>
/// (lines 674, 851, 1102, 1518, 1942, 2282, 2675) keyed on
/// <c>ManagePayrollSettings.ClientId</c>: seven completely different sheets in one method. This port
/// implements the <c>else</c> branch -- the generic sheet every client without a fork gets, and the only
/// one whose columns are derived from data rather than from one customer's wage-type catalogue. The six
/// forks are per-customer report customizations; each is its own report and must be ported (or replaced by
/// this generic sheet plus column selection) as a separate, individually-specified piece of work.
/// <c>uscRptPayrollSheet_Custom023</c> -- a seventh fork that was split into its own control -- IS ported,
/// as <c>PayrollSheetCustom023ReportAppService</c>.
/// </summary>
public class PayrollSheetReportRowDto : EntityDto<int>
{
    /// <summary>
    /// Legacy "SrNo" column (line 2856) -- a 1-based running number stamped while filling the table.
    /// Recomputed per page here, offset by the page's skip count so it keeps meaning the position in the
    /// overall result.
    /// </summary>
    public int SerialNumber { get; set; }

    /// <summary>Legacy "Code" column -- <c>Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- <c>FullNameAr</c> when Arabic, else <c>FullNameEn</c>.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "CivilId" column.</summary>
    public long CivilId { get; set; }

    /// <summary>
    /// The register period this row belongs to (<c>SalaryRegister.ForPeriod</c>, always equal to
    /// <c>InPeriod</c> here). Legacy had no such column -- it rendered
    /// <see cref="MonthYear"/> instead -- but the raw date is projected so the grid can sort by it.
    /// </summary>
    public DateTime Period { get; set; }

    /// <summary>Legacy "MonthYear" column -- <c>InPeriod.ToString("MMM-yyyy").ToUpper()</c> (line 2862).</summary>
    public string MonthYear { get; set; }

    /// <summary>Legacy "PayrollRunType" column, rendered as the enum name.</summary>
    public string PayrollRunType { get; set; }

    /// <summary>Legacy "PayrollReasonType" column -- legacy hard-coded <c>NameEn</c> (line 2884).</summary>
    public string PayrollReasonType { get; set; }

    /// <summary>Legacy "OffCyclePaymentDate" column.</summary>
    public DateTime OffCyclePaymentDate { get; set; }

    /// <summary>
    /// Legacy "PayrollArea" column, from <c>Employee.CurrentPayrollArea.Name</c> (line 2869), i.e. the
    /// assignment current AS OF TODAY -- not as of the reported period.
    /// </summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy "JobTitle" column, from <c>Employee.CurrentJobtitle</c> (line 2870): as of TODAY.</summary>
    public string JobTitle { get; set; }

    /// <summary>Legacy "WorkLocation" column, from <c>Employee.CurrentWorkLocation</c> (line 2887): as of TODAY.</summary>
    public string WorkLocation { get; set; }

    /// <summary>Legacy "CostCenterCode" column, from the assignment current at the ROW'S PERIOD (line 2886).</summary>
    public string CostCenterCode { get; set; }

    /// <summary>Legacy "CostCenter" column -- same assignment, localized name.</summary>
    public string CostCenterName { get; set; }

    /// <summary>
    /// Legacy OrgLevel1..OrgLevel7 columns, taken from the assignment current as of the FROM month-end
    /// (<c>empOrgAss</c>, lines 2679-2681 -- the whole range's first month, even for later rows).
    /// </summary>
    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>
    /// Legacy "SalaryVia" column (lines 2921-2931): the employee bank's Bank CODE when the payment type is
    /// BANK, otherwise the literal <c>"Cheque"</c> -- including when the employee has no bank record at all.
    /// </summary>
    public string SalaryVia { get; set; }

    /// <summary>Legacy "PaymentType" column.</summary>
    public string PaymentType { get; set; }

    /// <summary>Legacy "AccountNo" column.</summary>
    public string AccountNo { get; set; }

    /// <summary>Legacy "IBANNumber" column.</summary>
    public string IbanNumber { get; set; }

    /// <summary>Legacy "PayerBank" column -- <c>PayerBank.Code + "-" + PayerBank.NameEn</c> (line 2929).</summary>
    public string PayerBank { get; set; }

    /// <summary>
    /// Legacy "MOSALID" column, from the sponsor record current at the row's period, falling back to the
    /// sponsor with the LATEST EndDate when there is none (lines 2934-2944).
    /// </summary>
    public long MosalId { get; set; }

    /// <summary>Legacy "NameOfSponsor" column -- same record and same fallback as <see cref="MosalId"/>.</summary>
    public string NameOfSponsor { get; set; }

    /// <summary>
    /// Legacy "Currency" column: the currency of the FIRST basic-pay detail line that has one
    /// (lines 2957-2963). Empty when the employee has no basic pay current at the row's period.
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// Legacy "TotalSalary" column -- <c>EmployeeBasicPay.Amount</c> of the basic pay current at the row's
    /// period, and 0 when there is none (line 2947/2965).
    /// </summary>
    public double TotalSalary { get; set; }

    /// <summary>Legacy "WorkingDays" column -- the <c>Number</c> of the register's wage type 1000 line (line 2891).</summary>
    public double WorkingDays { get; set; }

    /// <summary>
    /// Legacy's <c>"Act {wageTypeName}"</c> cells: the employee's CONTRACTUAL basic-pay amount per wage
    /// type. Keyed by wage type name (no "Act " prefix); see
    /// <see cref="PayrollSheetReportOutput.ActualBasicPayColumns"/>.
    /// </summary>
    public Dictionary<string, double> ActualBasicPayAmounts { get; set; } = new();

    /// <summary>
    /// Legacy's earning/deduction cells: the register line's <c>Amount</c> per wage type, keyed by wage
    /// type name. See <see cref="PayrollSheetReportOutput.EarningDeductionColumns"/>.
    /// </summary>
    public Dictionary<string, double> WageAmounts { get; set; } = new();

    /// <summary>
    /// Legacy's <c>"{wageTypeName} Days"</c> cells: the same register line's <c>Number</c>, but only for
    /// wage types that are also absence types. See <see cref="PayrollSheetReportOutput.LeaveDaysColumns"/>.
    /// </summary>
    public Dictionary<string, double> WageDays { get; set; } = new();

    /// <summary>
    /// Legacy "Difference" column (line 3346): <c>TotalSalary - NetPay</c>, where NetPay is the cell of the
    /// column literally named <c>"Net Pay"</c> -- and 0 when no wage type is named that, since legacy
    /// guarded with <c>dt.Columns.Contains("Net Pay")</c> (line 2848). A tenant whose net-pay wage type is
    /// named anything else therefore gets Difference == TotalSalary; that is legacy behaviour and is pinned
    /// by a test.
    /// </summary>
    public double Difference { get; set; }
}
