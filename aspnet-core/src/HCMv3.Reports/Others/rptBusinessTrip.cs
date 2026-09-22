namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptBusinessTrip</c> --
    /// the employee information sheet issued for a business trip.
    ///
    /// It is rptEmployeeProfile's layout and query with three deliberate differences, all of them
    /// legacy, not port artefacts: no payroll-area / employee-group columns; residency number and
    /// expiry come from the visa/residency record rather than the civil-ID document; and the
    /// passport shown must be CURRENTLY VALID. See <see cref="OthersAdminSql.BusinessTripQuery"/>.
    ///
    /// XAF CONSTRUCTS REPLACED: identical to <see cref="rptEmployeeProfile"/> -- base class,
    /// connection name, SQL out of the .resx, image out of the .resx, plus the new tenant
    /// parameter. The legacy constructor body was a single commented-out line
    /// (<c>//this.PrmDate.Value = DateTime.Now.Date;</c>), so PrmDate keeps its designer default
    /// here; that is faithful.
    ///
    /// SUBREPORTS: same three as rptEmployeeProfile, and the same two invisible ones are not
    /// ported. See the remarks on <see cref="rptEmployeeProfile"/>.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report returns
    /// nothing.
    /// </summary>
    public partial class rptBusinessTrip : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "BusinessTrip";

        public rptBusinessTrip()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            // The photo is on disk, not in the database: the query returns its path, and the picture box
            // loads it from the tenant's folder as each row prints.
            BindStoredImage(xrPictureBox1, "PhotoPath");

            xrPictureBox2.Image = OthersAdminImageStore.Load(OthersAdminImageStore.BusinessTripLogo);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptBusinessTrip(string employeeCode)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
        }
    }
}
