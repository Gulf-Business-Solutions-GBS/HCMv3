using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Payroll Sheet (Custom 023) grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollSheet_Custom023.ascx</c>).
///
/// ATYPICAL FILTER PANEL: Payroll Area / Run Type / Reason Type / Month / Year / TO Month / TO Year --
/// and, unlike the generic <c>uscRptPayrollSheet</c>, NO employee-code box: <c>Table1()</c> never reads
/// <c>Session[employeeCode]</c> (uscRptPayrollSheet_Custom023.ascx.cs:884-905) and
/// <c>GetParameterStr</c> (lines 594-608) does not log one. All four inherited base filters are unused and
/// the Angular screen hides them.
/// </summary>
public class GetPayrollSheetCustom023ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>, blank meaning the CURRENT month (lines 519-521).</summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>, blank meaning the CURRENT year (lines 519-521).</summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>
    /// Legacy <c>txtMonthTo</c>. IGNORED unless <c>ManageReportSettings.ShowPeriodInPayrollSheet</c> is on
    /// (lines 620-623).
    /// </summary>
    [Range(1, 12)]
    public int? ToMonth { get; set; }

    /// <summary>Legacy <c>txtYearTo</c>. See <see cref="ToMonth"/>.</summary>
    [Range(1900, 2999)]
    public int? ToYear { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Null means "all run types".</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbReasonType</c>, taken here as an id rather than the legacy code.</summary>
    public int? PayrollReasonTypeId { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>, likewise taken here as an id rather than a code.</summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Legacy ordered by employee Code inside each period (line 986).</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// Read result of the Payroll Sheet (Custom 023) report. The column lists are FIXED for this customer, but
/// they still travel with the rows so the grid and the Excel export are driven by one server-side list --
/// the same contract the generic Payroll Sheet uses.
/// </summary>
public class PayrollSheetCustom023ReportOutput : PagedResultDto<PayrollSheetCustom023ReportRowDto>
{
    /// <summary>
    /// Legacy's seven hard-coded <c>"Act ..."</c> columns (lines 781-787), keyed by caption WITHOUT the
    /// "Act " prefix. Keys of <see cref="PayrollSheetCustom023ReportRowDto.ActualBasicPayAmounts"/>.
    /// </summary>
    public List<string> ActualBasicPayColumns { get; set; } = new();

    /// <summary>
    /// Legacy's 56 hard-coded earning/deduction/net columns (lines 792-849). Keys of
    /// <see cref="PayrollSheetCustom023ReportRowDto.WageAmounts"/>.
    /// </summary>
    public List<string> EarningDeductionColumns { get; set; } = new();
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptPayrollSheet_Custom023.ascx.cs:614-1195).
///
/// This control is the generic Payroll Sheet forked for client 023: identical fixed columns, identical
/// per-employee-per-period grain, but the data-driven wage-type columns are replaced by a hard-coded
/// catalogue of that customer's wage types (the generic loops are still in the file, commented out at
/// lines 779-780, 1111-1119).
///
/// LEGACY HARD-CODED DATABASE IDS -- THE ONE THING THAT CANNOT BE PORTED AS WRITTEN. The 56
/// earning/deduction cells are filled by <c>switch (wt.WageTypeOid) { case 1: ... case 435: ... }</c>
/// (lines 1121-1185): raw primary keys of the client-023 production database. HCMv3 assigns its own
/// WageType ids, so those numbers mean nothing here and reproducing them would put every amount in the
/// wrong column. The port matches on <c>WageType.NameEn</c> instead -- which is exactly what each legacy case
/// wrote as its column caption, so the two agree on any database whose wage-type names match the captions.
/// A wage type this customer renames silently drops out of the sheet, the same way an unmapped Oid did in
/// legacy. Pinned by <c>Should_Map_Earning_Columns_By_Wage_Type_Name_Not_By_Legacy_Oid</c>.
///
/// The seven <c>"Act ..."</c> basic-pay cells are NOT affected: legacy switched those on
/// <c>WageType.Code</c> (lines 1078-1088), which is portable master data.
/// </summary>
public class PayrollSheetCustom023ReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "SrNo" column (line 1010), offset by the page's skip count -- see the generic sheet.</summary>
    public int SerialNumber { get; set; }

    /// <summary>Legacy "Code" column.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- <c>FullNameAr</c> when Arabic, else <c>FullNameEn</c>.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "CivilId" column.</summary>
    public long CivilId { get; set; }

    /// <summary>The register period of this row; legacy rendered <see cref="MonthYear"/> instead.</summary>
    public DateTime Period { get; set; }

    /// <summary>Legacy "MonthYear" column -- <c>InPeriod.ToString("MMM-yyyy").ToUpper()</c>.</summary>
    public string MonthYear { get; set; }

    /// <summary>Legacy "PayrollRunType" column.</summary>
    public string PayrollRunType { get; set; }

    /// <summary>Legacy "PayrollReasonType" column -- hard-coded <c>NameEn</c> (line 947).</summary>
    public string PayrollReasonType { get; set; }

    /// <summary>Legacy "OffCyclePaymentDate" column.</summary>
    public DateTime OffCyclePaymentDate { get; set; }

    /// <summary>Legacy "PayrollArea" column, from the assignment current AS OF TODAY (line 916).</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy "JobTitle" column, likewise as of TODAY (line 917).</summary>
    public string JobTitle { get; set; }

    /// <summary>Legacy "WorkLocation" column, likewise as of TODAY (line 933).</summary>
    public string WorkLocation { get; set; }

    /// <summary>Legacy "CostCenterCode" column, from the assignment current at the ROW'S period (line 932).</summary>
    public string CostCenterCode { get; set; }

    /// <summary>Legacy "CostCenter" column -- same assignment, localized name.</summary>
    public string CostCenterName { get; set; }

    /// <summary>
    /// Legacy OrgLevel1..OrgLevel7 columns, from the assignment current as of the FROM month-end
    /// (<c>empOrgAss</c>, lines 632-634 -- the range's first month, even for later rows).
    /// </summary>
    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Legacy "SalaryVia" column: the bank code when paid by BANK, else the literal "Cheque".</summary>
    public string SalaryVia { get; set; }

    /// <summary>Legacy "PaymentType" column.</summary>
    public string PaymentType { get; set; }

    /// <summary>Legacy "AccountNo" column.</summary>
    public string AccountNo { get; set; }

    /// <summary>Legacy "IBANNumber" column.</summary>
    public string IbanNumber { get; set; }

    /// <summary>Legacy "PayerBank" column -- <c>Code + "-" + NameEn</c>.</summary>
    public string PayerBank { get; set; }

    /// <summary>Legacy "MOSALID" column, sponsor current at the period else the latest-ending sponsor.</summary>
    public long MosalId { get; set; }

    /// <summary>Legacy "NameOfSponsor" column -- same record and fallback as <see cref="MosalId"/>.</summary>
    public string NameOfSponsor { get; set; }

    /// <summary>Legacy "Currency" column -- the first basic-pay detail line that carries one (lines 1090-1096).</summary>
    public string Currency { get; set; }

    /// <summary>Legacy "TotalSalary" column -- the basic pay's Amount, 0 when there is none (line 1103).</summary>
    public double TotalSalary { get; set; }

    /// <summary>Legacy "WorkingDays" column -- the Number of the register's wage type 1000 line (line 935).</summary>
    public double WorkingDays { get; set; }

    /// <summary>
    /// Legacy's <c>"Act ..."</c> cells, keyed by caption. Filled from the basic-pay detail lines whose wage
    /// type CODE is one of 1000/1002/1031/1032/1035/1069/1070 (lines 1078-1088).
    /// </summary>
    public Dictionary<string, double> ActualBasicPayAmounts { get; set; } = new();

    /// <summary>
    /// Legacy's 56 fixed earning/deduction/net cells, keyed by caption. See the class remarks for why they
    /// are matched by wage type NAME rather than by the legacy Oid switch.
    /// </summary>
    public Dictionary<string, double> WageAmounts { get; set; } = new();

    /// <summary>
    /// Legacy "Difference" column (line 1188): <c>TotalSalary - "Net Pay"</c>. Here the "Net Pay" column is
    /// always declared (line 849), so unlike the generic sheet the subtraction always happens.
    /// </summary>
    public double Difference { get; set; }
}
