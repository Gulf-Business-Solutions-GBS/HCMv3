using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Payroll.rptSocialSecurity</c> --
    /// the PIFSS (Kuwait Public Institution for Social Security) statutory contribution return.
    ///
    /// THIS IS NOT A PAYSLIP. It shares a folder and a base class with the payslip family and
    /// nothing else: no subreports at all, its own master query, and a landscape grid layout of
    /// employee-vs-employer contributions against claims. It is in this batch only because it sat in
    /// the same legacy folder.
    ///
    /// *** THE WAGE-TYPE TRAP -- CHECKED EXPLICITLY, NOT ASSUMED ***
    /// The payslip queries carry a legacy literal <c>WageType = 1</c> that is a raw legacy PRIMARY
    /// KEY meaning Code '1000' (Basic Salary), and PayslipSql rewrites it accordingly. This report's
    /// literals are the OPPOSITE case: <c>WageType.Code IN ('1')</c> and <c>IN ('3')</c> are genuine
    /// CODES, and in HCMv3 those codes are Employee PIFSS Ded and Employer PIFSS Ded respectively.
    /// Verified against <c>HCMv3.Core/PayrollManagement/Services/EmployeePifssDeductionWageTypeCalculator.cs</c>
    /// ("Wage code 1 -- Employee PIFSS Ded") and <c>EmployerPifssDeductionWageTypeCalculator.cs</c>
    /// ("Wage code 3 -- Employer PIFSS Ded"). They are therefore carried across UNCHANGED. Applying
    /// the payslip's '1' -> '1000' rewrite here would report gross basic salary as a social-security
    /// contribution. See the wage-type block in <see cref="PayslipVariantSql"/>.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                    | Replacement |
    /// |-----------------------------------------------------------|-------------|
    /// | field initialiser <c>string CurrentUserName = SecuritySystem.CurrentUserName.ToString();</c> assigned to <c>lblUserlogin.Text</c> in the ctor | <c>PrmLoggedUserName</c> report parameter, expression-bound to the label. As a FIELD INITIALISER this ran before the constructor body and threw outside a request context, which is why the legacy report could not be exported headlessly. |
    /// | unused field <c>string UserNameLogin = ""</c> | dropped. |
    /// | <c>ComponentResourceManager.GetString("CalcHeaders.Expression")</c> | the CalculatedField is dropped entirely -- see the port note below. |
    ///
    /// LAYOUT DEVIATIONS (both recorded in PORTING-NOTES-PayslipVariants.md):
    ///   * the dead <c>CalcHeaders</c> CalculatedField is removed. Its DataMember was "PayslipQuery"
    ///     (a query this report does not have) and its expression referenced
    ///     <c>[Parameters].[PrmPeriodMonth]</c> (a parameter this report does not have). Nothing
    ///     bound it -- the header text is assigned in <see cref="lblHeader_BeforePrint"/> -- and in
    ///     v23.2 an unresolvable parameter reference throws at document creation.
    ///   * the two SqlDataSource master/detail Relations are removed for the same reason; one of
    ///     them keyed on a column ("DivisionEn") that no query in this report ever produced.
    ///
    /// MULTI-TENANCY: all four queries carry <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report returns
    /// nothing.
    /// </summary>
    public partial class rptSocialSecurity : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SocialSecurity";

        public rptSocialSecurity()
        {
            InitializeComponent();

            // Legacy constructor body, verbatim: default the return to LAST month, which is the
            // month whose contributions are actually being filed.
            PrmSheetDate.Value = DateTime.Now;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmPayrollRunType.Value = 0;

            EnsureHcmParameters(withLoggedUserName: true);

            // Replaces "this.lblUserlogin.Text = CurrentUserName" in the constructor.
            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        /// <summary>Convenience overload for programmatic callers.</summary>
        public rptSocialSecurity(int month, int year, int runType, string payrollArea, string company, string department)
            : this()
        {
            PrmMonth.Value = month;
            PrmYear.Value = year;
            PrmPayrollRunType.Value = runType;
            PrmPayrollArea.Value = payrollArea ?? string.Empty;
            PrmCompany.Value = company ?? string.Empty;
            PrmDepartment.Value = department ?? string.Empty;
        }

        // v17.2 -> v23.2: control-level BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs). Body ported verbatim -- GetMonthNameYear is one
        // of the three formatting helpers that survived into HcmReportBase.
        private void lblHeader_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            lblHeader.Text = "PIFSS REPORT FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
        }
    }
}
