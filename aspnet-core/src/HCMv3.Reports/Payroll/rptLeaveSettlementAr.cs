using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Payroll.rptLeaveSettlementAr</c>.
    ///
    /// The Arabic twin of <see cref="rptLeaveSettlement"/>. The legacy pair differed only in the
    /// name columns the master query projects (FullNameAr / Bank.NameAr / NameAr / CurrencyShortFormAr)
    /// and in which earning/deduction subreports are embedded; the working-days summary subreport is
    /// the SAME English one in both, exactly as in the legacy layout.
    ///
    /// See <see cref="rptLeaveSettlement"/> for the full list of XAF constructs replaced -- the same
    /// applies here, except that the Arabic report never embedded the booking-balance subreport.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptLeaveSettlementAr : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "LeaveSettlementAr";

        public rptLeaveSettlementAr()
        {
            InitializeComponent();

            PrmPrintDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;
            PrmMonth.Value = DateTime.Now.Month;
            PrmYear.Value = DateTime.Now.Year;
            PrmPayrollRunType.Value = 1;

            PrmSendMail.Value = false;
            PrmSendMail.Visible = false;

            EnsureHcmParameters(withLoggedUserName: true);

            LoggedUserNme.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));

            BindLeaveSummaryLabels();
        }

        /// <summary>Replaces the legacy XPO BeforePrint handler -- see rptLeaveSettlement for details.</summary>
        private void BindLeaveSummaryLabels()
        {
            Bind(lblLeaveStartDate, "LeaveStartDate", "{0:dd/MM/yyyy}");
            Bind(lblLeaveEndDate, "LeaveEndDate", "{0:dd/MM/yyyy}");
            Bind(lblExpReturnDate, "ExpReturnDate", "{0:dd/MM/yyyy}");
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

        // v17.2 -> v23.2 signature change; legacy body removed (see the class remarks).
        private void rptLeaveSettlementAr_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
