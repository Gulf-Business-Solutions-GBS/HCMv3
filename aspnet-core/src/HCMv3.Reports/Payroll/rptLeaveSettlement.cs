using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Payroll.rptLeaveSettlement</c>.
    ///
    /// XAF CONSTRUCTS REPLACED
    ///
    /// | Legacy                                                | Replacement |
    /// |-------------------------------------------------------|-------------|
    /// | <c>SecuritySystem.CurrentUserName</c> baked into <c>LoggedUserNme.Text</c> in the ctor | <c>PrmLoggedUserName</c> report parameter, expression-bound to the label. Baking it in the ctor is why the legacy exported .repx carries the *exporting* developer's user name as static text. |
    /// | <c>rptLeaveSettlement_BeforePrint</c>: a 130-line <c>XPObjectSpaceProvider</c> block that re-queried SalaryRegister / EmployeeAbsence / EmployeeAbsAdjustment and then assigned <c>lblLeaveStartDate</c>, <c>lblLeaveEndDate</c>, <c>lblExpReturnDate</c>, <c>lblPaidLeave</c>, <c>lblUnpaidLeave</c> and <c>lblPaidLeave2</c> | REMOVED. Every one of those six values is already produced by the master query (<c>LeaveStartDate</c>, <c>LeaveEndDate</c>, <c>ExpReturnDate</c>, <c>PaidLeaveDays</c>, <c>UnpaidLeaveDays</c>), so the labels are expression-bound to those columns in the constructor below. This also removes the legacy defect where the handler ran ONCE per document rather than once per employee, so a multi-employee run printed the LAST employee's leave dates on every page. |
    /// | <c>srptLeaveSettlementBookingBalance</c> subreport | DROPPED -- BLOCKED. Its query reads <c>AbsenceRequest.BookingBalance</c> through <c>WorkflowRequest</c>; HCMv3 has <c>WorkflowRequests</c> but no <c>AbsenceRequest</c> entity and no <c>BookingBalance</c> column anywhere in the schema. Legacy hid this subreport unless a booking balance existed, so nothing renderable is lost today. See PORTING-NOTES-PayrollSettlement.md. |
    /// | <c>Scripts.OnBeforePrint</c> | dropped (v23.2 removed report scripts from the designer surface). |
    /// | <c>HCM10.Module.BusinessObjects.*</c> imports | gone; the report touches no entities at all, only SQL and parameters. |
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptLeaveSettlement : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "LeaveSettlementEn";

        public rptLeaveSettlement()
        {
            InitializeComponent();

            PrmPrintDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;
            PrmMonth.Value = DateTime.Now.Month;
            PrmYear.Value = DateTime.Now.Year;
            PrmPayrollRunType.Value = 1;

            // The legacy report carried a PrmSendMail flag that drove an AfterPrint SmtpClient send.
            // That path is not ported (mailing from a print event fires on every viewer page request);
            // pin it off and hide it so no one re-enables a control that now does nothing.
            PrmSendMail.Value = false;
            PrmSendMail.Visible = false;

            EnsureHcmParameters(withLoggedUserName: true);

            // Replaces "LoggedUserNme.Text = SecuritySystem.CurrentUserName" in the constructor.
            LoggedUserNme.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));

            BindLeaveSummaryLabels();
        }

        /// <summary>
        /// Replaces the legacy XPO BeforePrint handler. Each label is bound to the master-query column
        /// that already carries the value the handler recomputed.
        ///
        /// v23.2 TRAP: an <c>XRLabel.Text = "[Column]"</c> mail-merge silently renders blank for data
        /// fields, and a "!format" suffix inside a mail-merge string is ignored -- hence
        /// ExpressionBinding plus an explicit <c>TextFormatString</c>.
        /// </summary>
        private void BindLeaveSummaryLabels()
        {
            Bind(lblLeaveStartDate, "LeaveStartDate", "{0:dd/MM/yyyy}");
            Bind(lblLeaveEndDate, "LeaveEndDate", "{0:dd/MM/yyyy}");
            Bind(lblExpReturnDate, "ExpReturnDate", "{0:dd/MM/yyyy}");

            // The numeric labels already carry TextFormatString "{0:#0.00}" from the designer, which is
            // the format the legacy handler applied by hand.
            Bind(lblPaidLeave, "PaidLeaveDays", null);
            Bind(lblUnpaidLeave, "UnpaidLeaveDays", null);
            Bind(lblPaidLeave2, "PaidLeaveDays", null);
        }

        private static void Bind(XRLabel label, string column, string formatString)
        {
            label.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[" + column + "]"));

            if (formatString != null)
            {
                label.TextFormatString = formatString;
            }
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs). The legacy body is gone -- see the class remarks --
        // but the handler is kept because the designer file wires it.
        private void rptLeaveSettlement_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
