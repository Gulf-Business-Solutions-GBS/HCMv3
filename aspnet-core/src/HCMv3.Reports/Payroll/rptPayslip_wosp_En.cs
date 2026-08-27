using System;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.rptPayslip_wosp_En</c> --
    /// "payslip WITHOUT salary particulars": the standard employee header block plus the earnings and
    /// deductions columns, and nothing else.
    ///
    /// SUBREPORT REUSE -- two, both INSTANTIATED, neither cloned:
    /// <see cref="srptPaySlipEarningEn"/> and <see cref="srptPaySlipDeductionEn"/>, the same committed
    /// classes rptPayslipEn uses. The legacy layout bound them through parameters named
    /// "EmployeeCode" / "ForPeriod"; the committed HCMv3 subreports expose the four-parameter
    /// contract (EmpCode/empcode, ForPeriod, InPeriod, RunType), so the ParameterBindings in the
    /// designer file were retargeted. The subreports themselves are untouched.
    ///
    /// THE ONE REPORT IN THIS BATCH WHOSE DATA SOURCE HAD TO BE REBUILT
    /// The legacy code-behind was four lines -- <c>InitializeComponent()</c> and nothing else -- so
    /// there is almost no XAF to strip out of the C#. All the XAF was in the LAYOUT: the report bound
    /// to a <c>DevExpress.Persistent.Base.ReportsV2.CollectionDataSource</c> over the XPO class
    /// <c>SalaryRegister</c>, and each control walked an XPO association path
    /// (<c>Employee.Bank.Bank.NameEn</c>, and so on). CollectionDataSource is an XAF type that
    /// HCMv3.Reports must not reference, so it is replaced by a <c>SqlDataSource</c> carrying
    /// <see cref="PayslipVariantSql.PayslipWospQuery"/>, and all sixteen XRBindings are re-pointed at
    /// query-qualified column names. Five XPO-only CalculatedFields were dropped in the process --
    /// three of them (OFFCurrentMonthCal, SLCal, CalNetPay) became SQL columns with identical
    /// arithmetic, two (CalEarning, CalDeduct) were already dead. Each removal is annotated in the
    /// designer file, and all of it is listed under "Layout deviations" in the port notes.
    ///
    /// KNOWN GAP: the "Calendar Days" cell prints blank. HCMv3's EmployeeAttendances carries no
    /// CalendarDays column, and substituting a different attendance measure into a payslip would be
    /// worse than a blank. See PayslipVariantSql.
    ///
    /// MULTI-TENANCY: the master query carries <c>@TenantId</c>, and so do both subreports. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report returns
    /// nothing.
    /// </summary>
    public partial class rptPayslip_wosp_En : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayslipWithoutSalaryParticularsEn";

        public rptPayslip_wosp_En()
        {
            InitializeComponent();

            // The legacy ctor set nothing at all; the "last month" period default matches every
            // other report in the payslip family so the designer preview is not empty.
            EmployeeCode.Value = string.Empty;
            PeriodMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PeriodYear.Value = DateTime.Now.AddMonths(-1).Year;

            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for programmatic callers.</summary>
        public rptPayslip_wosp_En(string employeeCode, int month, int year)
            : this()
        {
            EmployeeCode.Value = employeeCode ?? string.Empty;
            PeriodMonth.Value = month;
            PeriodYear.Value = year;
        }
    }
}
