using System;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.EmployeeInfo.rptJoiningForm</c>.
    ///
    /// XAF -> ABP: the legacy code-behind was 20 lines -- <c>InitializeComponent()</c> plus an
    /// empty <c>BeforePrint</c> handler. All XAF machinery lived in the shared <c>MyXtraReport</c>
    /// base class and is gone; see the <see cref="HcmReportBase"/> class remarks. Report-level
    /// row-level security is NOT ported: it is an authorization decision for the calling layer.
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptJoiningForm : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "JoiningForm";

        /// <summary>
        /// Name of the as-of date parameter. See the constructor for why this report has to create
        /// it at run time.
        /// </summary>
        public const string AsOfDateParameterName = "PrmDate";

        public rptJoiningForm()
        {
            InitializeComponent();

            // *** LEGACY DEFECT FIXED. *** The legacy query binds a query parameter to
            // "[Parameters.PrmDate]" but the report declares NO PrmDate parameter at all -- every
            // sibling report in the folder does. The binding therefore resolved to nothing.
            // The parameter is added here rather than in the designer file so it survives a
            // designer round-trip, exactly as HcmReportBase.EnsureHcmParameters does.
            if (Parameters[AsOfDateParameterName] == null)
            {
                Parameters.Add(new DevExpress.XtraReports.Parameters.Parameter
                {
                    Name = AsOfDateParameterName,
                    Description = "Date",
                    Type = typeof(DateTime),
                    Value = DateTime.Now.Date
                });
            }

            EnsureHcmParameters();

            // Letterhead moved out of the .resx; see EmployeeInfoResources.
            xrPictureBox1.Image = EmployeeInfoResources.LoadImage(EmployeeInfoResources.JoiningFormLetterhead);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptJoiningForm(string employeeCode, DateTime asOfDate) : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            Parameters[AsOfDateParameterName].Value = asOfDate;
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs).
        private void rptJoiningForm_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // The legacy body was empty. The handler is kept because the designer file wires it.
        }
    }
}
