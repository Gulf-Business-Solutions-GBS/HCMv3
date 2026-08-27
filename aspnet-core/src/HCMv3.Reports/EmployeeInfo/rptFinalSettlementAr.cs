using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.EmployeeInfo.rptFinalSettlementAr</c> -- the Arabic twin of
    /// <see cref="rptFinalSettlement"/>.
    ///
    /// The two legacy code-behinds were character-for-character identical apart from the class
    /// name, and the two legacy queries differ in exactly three projected expressions (wage-type
    /// name, currency short form, bank name switch to their Arabic columns). Both are therefore
    /// shared here: the SQL through <c>rptFinalSettlementSql.QueryAr1</c>, the BeforePrint body
    /// through <c>FinalSettlementServiceSpan</c>. See the <see cref="rptFinalSettlement"/> class
    /// remarks for the full XAF -> ABP disposition table.
    ///
    /// *** THIS REPORT RENDERS EMPTY ON THE CURRENT HCMv3 DATABASE, LEGITIMATELY *** -- same
    /// reason as the English variant: no EmployeeFnFPayments rows and no 'End Of Service' payroll
    /// reason type.
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptFinalSettlementAr : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "FinalSettlementAr";

        public rptFinalSettlementAr()
        {
            InitializeComponent();
            PrmPrintDate.Value = DateTime.Now.Date;

            EnsureHcmParameters(withLoggedUserName: true);

            // Replaces the constructor's XPObjectSpaceProvider user lookup; see rptFinalSettlement.
            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptFinalSettlementAr(string employeeCode, DateTime printDate) : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmPrintDate.Value = printDate;
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs).
        private void rptFinalSettlementAr_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FinalSettlementServiceSpan.Apply(this, lblYearMonthdays, GetYearMonthDay);
        }
    }
}
