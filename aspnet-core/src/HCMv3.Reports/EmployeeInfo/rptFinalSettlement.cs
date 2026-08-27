using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.EmployeeInfo.rptFinalSettlement</c>.
    ///
    /// This is the ONLY report in the EmployeeInfo folder whose code-behind did real work -- the
    /// other twelve are bare <c>InitializeComponent()</c> calls. 63 lines, of which:
    ///
    /// | Legacy | Disposition |
    /// |---|---|
    /// | <c>string CurrentUserName = SecuritySystem.CurrentUserName</c> as a FIELD INITIALISER, then a <c>new XPObjectSpaceProvider(ConfigurationManager.ConnectionStrings["HCM10ConnectionString"])</c> in the constructor to look the user up through <c>GetObjectsQuery&lt;PermissionPolicyUser&gt;</c> and <c>GetObjectsQuery&lt;Employee&gt;</c> and turn it into an employee full name | Replaced by the <see cref="HcmReportBase.LoggedUserNameParameter"/> report parameter, expression-bound to <c>lblUserlogin</c>. A DevExpress-constructed report has no DI container, so the caller -- which does have an <c>IAbpSession</c> and the user/employee repositories -- resolves the name and pushes it in via <see cref="HcmReportBase.ApplyLoggedUserName"/>. This also removes the connection string from the report entirely. |
    /// | <c>BeforePrint</c>: a SECOND XPObjectSpaceProvider, opened per print, to fetch the employee's JoiningDate and LastDayInService and set <c>lblYearMonthdays.Text = GetYearMonthDay(...)</c> | Ported, but sourced from the report's OWN data row via <c>GetCurrentColumnValue</c> instead of a second database connection. Note the report's SQL also computes a <c>YearMonthdays</c> column with a different (nested-DATEDIFF) formula; the label deliberately keeps the base-class calculation, which is what the legacy report actually rendered. |
    /// | <c>HCM10.Module.BusinessObjects.*</c> imports | gone; the report touches no entities at all, only SQL and parameters. |
    ///
    /// Report-level row-level security is NOT ported: whether a user may pick an arbitrary employee
    /// code is an authorization decision, and baking it into report layout means a designer edit
    /// can revoke it.
    ///
    /// *** THIS REPORT RENDERS EMPTY ON THE CURRENT HCMv3 DATABASE, LEGITIMATELY. *** It requires
    /// an <c>EmployeeFnFPayments</c> row and a salary register whose payroll reason type is
    /// 'End Of Service'. HCMv3 has zero EmployeeFnFPayments rows and a single placeholder
    /// PayrollReasonTypes row. That is missing data, not a broken binding.
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptFinalSettlement : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "FinalSettlement";

        public rptFinalSettlement()
        {
            InitializeComponent();
            PrmPrintDate.Value = DateTime.Now.Date;

            EnsureHcmParameters(withLoggedUserName: true);

            // Replaces the constructor's XPObjectSpaceProvider user lookup; see the class remarks.
            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptFinalSettlement(string employeeCode, DateTime printDate) : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmPrintDate.Value = printDate;
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs).
        private void rptFinalSettlement_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FinalSettlementServiceSpan.Apply(this, lblYearMonthdays, GetYearMonthDay);
        }
    }

    /// <summary>
    /// Shared by rptFinalSettlement and rptFinalSettlementAr, whose legacy code-behinds were
    /// character-for-character identical apart from the class name.
    /// </summary>
    internal static class FinalSettlementServiceSpan
    {
        /// <summary>
        /// Sets the service-span label from the CURRENT data row rather than from a second database
        /// connection, which is what the legacy <c>BeforePrint</c> opened.
        ///
        /// Silent no-op when either date is missing -- the legacy code guarded LastDayInService the
        /// same way (<c>if (LastDayInService != default(DateTime))</c>), leaving whatever the
        /// designer put in the label. That matters on HCMv3 today, where EmployeeSeparations is
        /// empty.
        /// </summary>
        internal static void Apply(XtraReport report, XRLabel label, Func<DateTime, DateTime, string> format)
        {
            if (report == null || label == null)
            {
                return;
            }

            var joining = AsDate(report.GetCurrentColumnValue("JoiningDate"));
            var lastDay = AsDate(report.GetCurrentColumnValue("LastDayInService"));

            if (joining.HasValue && lastDay.HasValue)
            {
                label.Text = format(joining.Value, lastDay.Value);
            }
        }

        private static DateTime? AsDate(object value)
        {
            return value is DateTime date && date != default(DateTime) ? date : (DateTime?)null;
        }
    }
}
