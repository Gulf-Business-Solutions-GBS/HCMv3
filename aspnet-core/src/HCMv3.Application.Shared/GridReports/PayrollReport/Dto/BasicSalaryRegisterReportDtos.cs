using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Basic Salary Register analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscBasicSalaryRegister.ascx</c>).
///
/// ATYPICAL FILTER PANEL: legacy's panel (uscBasicSalaryRegister.ascx:13-27) is <c>dtFrom</c>, <c>dtTo</c>
/// and a single <c>cmbSearch</c> Run Type combo -- there is NO <c>txtEmpCode</c> and NO
/// <c>cmbEmpStatus</c>, and <c>GetParameterStr</c> (lines 405-416) logs exactly Run Type / From / To.
/// The inherited <see cref="GridReportInputBase.EmployeeCode"/> and
/// <see cref="GridReportInputBase.EmployeeStatus"/> are therefore unused and the Angular screen hides them.
/// </summary>
public class GetBasicSalaryRegisterReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbSearch</c> / <c>Session[RunType]</c> (lines 495-508). Null means "all run types" --
    /// legacy branched on a blank combo and simply left the predicate off.
    /// </summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>
    /// Legacy applied no sort at all (the DataTable came back in group order). Employee code ascending is
    /// used here because server-side paging needs a deterministic order and the legacy in-memory grid did not.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscBasicSalaryRegister.ascx.cs:421-585).
///
/// GRAIN: one row per (Employee, <c>SalaryRegister.InPeriod</c>) -- legacy's group key is
/// <c>{ Employee, Code, Inperiod }</c> (lines 516-524).
///
/// LEGACY DEFECT REPRODUCED (line 521 vs 552-573): <c>Inperiod</c> is part of the group key but is NOT a
/// column of the output table, so an employee paid in several periods inside the window produces several
/// visually IDENTICAL rows with different numbers. <see cref="InPeriod"/> is still projected here (it is
/// the only thing that makes those rows distinguishable and it costs nothing) but it is deliberately NOT
/// in the exported column list, which stays legacy-exact.
///
/// Every money/day column is the wage-type line of that register: 1000 basic, 9000 net pay, 1003 annual
/// leave pay, 2000 late/early, 2002 PIFSS, 3000 gratuity provision, 3001 annual-leave provision,
/// 4010 annual-leave accrual, 4011 active days, 2007 absent-unpaid, 1017 miscellaneous pay,
/// 4021/4022 annual-leave opening/closing balance (lines 530-546).
/// </summary>
public class BasicSalaryRegisterReportRowDto : EntityDto<int>
{
    /// <summary>
    /// Legacy "SrNo" column (line 553) -- a 1-based running number stamped while filling the DataTable.
    /// Recomputed per page here; see the app service for why it cannot be a stable row value.
    /// </summary>
    public int SerialNumber { get; set; }

    /// <summary>Legacy "Code" column -- <c>Employee.Code</c>, or the literal <c>"0"</c> when the register had no employee.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- <c>FullNameAr</c> when Arabic, else <c>FullNameEn</c> (line 519).</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy group key <c>Inperiod</c>; NOT a legacy output column. See class remarks.</summary>
    public System.DateTime InPeriod { get; set; }

    /// <summary>Legacy "NetPay" -- wage type 9000.</summary>
    public double NetPay { get; set; }

    /// <summary>Legacy "BasicSalary" -- wage type 1000.</summary>
    public double BasicSalary { get; set; }

    /// <summary>Legacy "AnnualLeavePay" -- wage type 1003.</summary>
    public double AnnualLeavePay { get; set; }

    /// <summary>Legacy "LateEarlyDeduction" -- wage type 2000.</summary>
    public double LateEarlyDeduction { get; set; }

    /// <summary>Legacy "PIFSSContributionDed" -- wage type 2002.</summary>
    public double PifssContributionDed { get; set; }

    /// <summary>Legacy "GratuityProvision" -- wage type 3000.</summary>
    public double GratuityProvision { get; set; }

    /// <summary>Legacy "AnnualLeaveProvision" -- wage type 3001.</summary>
    public double AnnualLeaveProvision { get; set; }

    /// <summary>Legacy "AnnualLeaveCurrentMonthAccrual" -- wage type 4010.</summary>
    public double AnnualLeaveCurrentMonthAccrual { get; set; }

    /// <summary>Legacy "ActiveDaysinaMonth" -- wage type 4011.</summary>
    public double ActiveDaysInAMonth { get; set; }

    /// <summary>
    /// Legacy "PresentDays" column. LEGACY DEFECT REPRODUCED: the column is declared (line 452) but the
    /// projection has no <c>PresentDays</c> member and the fill loop never assigns it
    /// (lines 552-573), so legacy always rendered it EMPTY. It is always 0 here, pinned by a test.
    /// </summary>
    public double PresentDays { get; set; }

    /// <summary>Legacy "AbsentUnpaidDed" -- wage type 2007.</summary>
    public double AbsentUnpaidDed { get; set; }

    /// <summary>Legacy "MiscellaneousPay" -- wage type 1017.</summary>
    public double MiscellaneousPay { get; set; }

    /// <summary>Legacy "AnnualLeaveOpeningBal" -- wage type 4021.</summary>
    public double AnnualLeaveOpeningBal { get; set; }

    /// <summary>Legacy "AnnualLeaveClosingBal" -- wage type 4022.</summary>
    public double AnnualLeaveClosingBal { get; set; }
}
