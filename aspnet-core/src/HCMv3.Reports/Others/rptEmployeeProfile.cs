using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptEmployeeProfile</c> --
    /// the one-page employee dossier (identity block, org assignment, contact details, identity
    /// documents, bank details, last increment, and a current basic-pay breakdown subreport).
    ///
    /// The legacy code-behind was four lines; almost all of the de-XAF-ing here happened in the
    /// layout and the SQL, not in this file.
    ///
    /// | Legacy                                              | Replacement |
    /// |-----------------------------------------------------|-------------|
    /// | base class <c>MyXtraReport</c> (XAF)                 | <see cref="HcmReportBase"/>. |
    /// | <c>EmpProfDS.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>; the run-time connection is pushed in by the caller through <see cref="HcmReportBase.ApplyConnectionString"/>. |
    /// | SQL in the report's <c>.resx</c>                     | <see cref="OthersAdminSql"/>, so it is reviewable and diffable. |
    /// | <c>xrPictureBox2.Image</c> from the <c>.resx</c>     | <see cref="OthersAdminImageStore"/>; a .resx <c>System.Drawing.Bitmap</c> entry breaks the multi-targeted build. |
    /// | (nothing)                                           | <see cref="HcmReportBase.EnsureHcmParameters"/> -- the hidden tenant parameter. |
    ///
    /// SUBREPORTS. Three in the legacy layout, all preserved:
    ///   * <c>xrSubreport3</c> -&gt; <see cref="srptEmpProfileWageDetail"/>, VISIBLE -- the
    ///     wage-type breakdown table.
    ///   * <c>xrSubreport1</c> -&gt; srptEmployeeProfileNetPay and <c>xrSubreport2</c> -&gt;
    ///     srptEmployeeProfile, both <c>Visible = false</c> in the legacy layout. Those two
    ///     subreport CLASSES are NOT ported (nothing renders them, and porting a report that is
    ///     never drawn is dead code), so the two placeholders are left with no ReportSource. They
    ///     stay in the layout at their original position and size so the band geometry is
    ///     unchanged. See "Layout deviations" in PORTING-NOTES-OthersAdmin.md.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report returns
    /// nothing.
    /// </summary>
    public partial class rptEmployeeProfile : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmployeeProfile";

        public rptEmployeeProfile()
        {
            InitializeComponent();

            // Legacy constructor body, verbatim.
            PrmDate.Value = DateTime.Now.Date;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            // The photo is on disk, not in the database: the query returns its path, and the picture box
            // loads it from the tenant's folder as each row prints.
            BindStoredImage(xrPictureBox1, "PhotoPath", "LegacyPhoto");

            xrPictureBox2.Image = OthersAdminImageStore.Load(OthersAdminImageStore.EmployeeProfileLogo);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptEmployeeProfile(string employeeCode, DateTime printDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = printDate;
        }
    }
}
