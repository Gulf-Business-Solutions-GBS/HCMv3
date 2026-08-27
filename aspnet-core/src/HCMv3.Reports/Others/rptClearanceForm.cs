using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>rptClearanceForm</c>.
    ///
    /// (The legacy file declared <c>namespace HCM10.Module.Reports.GovtForms</c> while sitting in the
    /// Reports\Others folder -- a copy-paste leftover. It is an internal HR clearance form, not a
    /// government form, so the port lands in <c>HCMv3.Reports.Others</c> with the rest of the folder.)
    ///
    /// A full-page scan of the printed clearance form with the employee's details positioned over it.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                        | Replacement |
    /// |---------------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                                      | <see cref="HcmReportBase"/>. |
    /// | <c>SqlDataSource.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report <c>.resx</c>                                | <see cref="OthersHrFormsSql.ClearanceFormQuery"/>. |
    /// | <c>xrPictureBox1.Image</c> from the <c>.resx</c>              | <see cref="OthersHrFormsImageStore"/>. |
    /// | client-side <c>FilterString = "[Code] In (?PrmEmpCode)"</c>   | <c>emp.Code = @PrmEmpCode</c> in SQL, and the query's PrmEmpCode QueryParameter -- commented out in the legacy designer -- is restored. See OthersHrFormsSql rewrite note R7: a client-side filter over a raw SqlDataSource fetches every tenant's rows before discarding them. |
    ///
    /// The legacy code-behind was a bare <c>InitializeComponent()</c>; it did not even set PrmDate,
    /// so the parameter reached the viewer at <c>DateTime.MinValue</c> and SqlClient would reject it
    /// if anything bound it. Nothing binds it into SQL, but it is initialised to today here for the
    /// same reason <see cref="HcmReportBase.ClampDateParameters"/> exists.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptClearanceForm : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "ClearanceForm";

        public rptClearanceForm()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = OthersHrFormsImageStore.Load(
                OthersHrFormsImageStore.ClearanceFormBackgroundResourceName);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptClearanceForm(string employeeCode, DateTime printDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = printDate;
        }
    }
}
