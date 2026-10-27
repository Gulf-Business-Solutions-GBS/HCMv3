using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Payroll.rptLeaveEncashment</c>.
    ///
    /// XAF CONSTRUCTS REPLACED
    ///
    /// | Legacy                                        | Replacement |
    /// |-----------------------------------------------|-------------|
    /// | <c>string CurrentUserName = SecuritySystem.CurrentUserName</c> and <c>string UserNameLogin</c> fields | REMOVED. Both were dead: the legacy constructor never assigned them to anything, so <c>lblUserlogin</c> printed its literal design-time text ("xrLabel2") on every run. Here the label is bound to the <c>PrmLoggedUserName</c> parameter, matching every other ported report. LEGACY DEFECT FIXED -- see PORTING-NOTES-PayrollSettlement.md. |
    /// | <c>HCM10.Module.BusinessObjects.*</c> imports  | gone; the report touches no entities at all, only SQL and parameters. |
    ///
    /// MULTI-TENANCY: the single query carries <c>@TenantId</c> on every table reference. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report returns
    /// nothing.
    /// </summary>
    public partial class rptLeaveEncashment : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "LeaveEncashment";

        public rptLeaveEncashment()
        {
            InitializeComponent();

            PrmPrintDate.Value = DateTime.Now.Date;
            PrmEmpCode.Value = string.Empty;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;

            EnsureHcmParameters(withLoggedUserName: true);

            // Legacy left this label showing its design-time placeholder text; bind it to the
            // logged-in-user parameter the caller supplies.
            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }
    }
}
