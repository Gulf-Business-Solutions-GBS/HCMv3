namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptProbationaryReportForm</c>.
    ///
    /// A full-page scan of the printed probationary-report form with four data labels positioned
    /// over it: employee name, job title, employee code and joining date.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                          | Replacement |
    /// |-----------------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                                        | <see cref="HcmReportBase"/>. |
    /// | <c>SqlDataSource.ConnectionName = "HCM10ConnectionString"</c>   | <see cref="HcmReportBase.DesignTimeConnectionName"/>; the run-time connection is pushed in by the caller through <see cref="HcmReportBase.ApplyConnectionString"/>. |
    /// | SQL in the report <c>.resx</c>                                  | <see cref="OthersHrFormsSql.ProbationaryReportFormQuery"/>. |
    /// | <c>xrPictureBox1.Image</c> from the <c>.resx</c>                | <see cref="OthersHrFormsImageStore"/>; a .resx entry typed <c>System.Drawing.Bitmap</c> breaks this project's plain net8.0 target. |
    /// | empty <c>rptProbationaryReportForm_BeforePrint</c> handler      | dropped. The legacy method body was <c>{ }</c>; nothing else referenced it. |
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report returns
    /// nothing.
    /// </summary>
    public partial class rptProbationaryReportForm : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "ProbationaryReportForm";

        public rptProbationaryReportForm()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = OthersHrFormsImageStore.Load(
                OthersHrFormsImageStore.ProbationaryFormBackgroundResourceName);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptProbationaryReportForm(string employeeCode)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
        }
    }
}
