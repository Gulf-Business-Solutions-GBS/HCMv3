using System;
using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptPassportInOut</c> --
    /// the passport hand-out / hand-back acknowledgement form. The SQL supplies the employee's
    /// identity block; the movement rows are a pre-printed grid filled in on paper.
    ///
    /// | Legacy                                              | Replacement |
    /// |-----------------------------------------------------|-------------|
    /// | base class <c>MyXtraReport</c> (XAF)                 | <see cref="HcmReportBase"/>. |
    /// | <c>EmpDiscReportDS.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report's <c>.resx</c>                     | <see cref="OthersAdminSql"/>. |
    /// | <c>xrPictureBox1.Image</c> from the <c>.resx</c>     | <see cref="OthersAdminImageStore"/>; a .resx <c>System.Drawing.Bitmap</c> entry breaks the multi-targeted build. |
    /// | (nothing)                                           | <see cref="HcmReportBase.EnsureHcmParameters"/> -- the hidden tenant parameter. |
    ///
    /// PARAMETER QUIRK KEPT: the query resolves the employee's job title as of <c>PrmDate</c>, but
    /// the layout only ever offers <c>PrmFromDate</c> / <c>PrmToDate</c> to the user. PrmDate is
    /// therefore whatever its designer default is. That is the legacy behaviour and is not
    /// "fixed" here -- see "Defects carried over" in PORTING-NOTES-OthersAdmin.md.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPassportInOut : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PassportInOut";

        public rptPassportInOut()
        {
            InitializeComponent();

            // Legacy constructor body, verbatim.
            PrmFromDate.Value = DateTime.Now.Date;
            PrmToDate.Value = DateTime.Now.Date;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = OthersAdminImageStore.Load(OthersAdminImageStore.PassportInOutForm);
        }

        /// <summary>Convenience overload for callers that already know the employee and range.</summary>
        public rptPassportInOut(string employeeCode, DateTime fromDate, DateTime toDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmFromDate.Value = fromDate;
            PrmToDate.Value = toDate;
        }

        /// <summary>
        /// Empty in the legacy report too -- the handler was wired up in the designer and its body
        /// was never written. Kept so the designer's event wiring still resolves and so the next
        /// person can see it was deliberately empty, not lost in the port.
        ///
        /// v17.2 -&gt; v23.2: report BeforePrint is
        /// <c>BeforePrintEventHandler(CancelEventArgs)</c>, not
        /// <c>PrintEventHandler(PrintEventArgs)</c>.
        /// </summary>
        private void rptPassportInOut_BeforePrint(object sender, CancelEventArgs e)
        {
        }
    }
}
