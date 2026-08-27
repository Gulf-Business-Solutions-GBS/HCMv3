using System;

namespace HCMv3.Reports.GovtForms
{
    /// <summary>
    /// HCMv3 port of the legacy report <c>HCM10.Module.Reports.rptPayShounTerminationFormEn</c>
    /// (source file lives under Reports/GovtForms even though its legacy namespace does not).
    ///
    /// A Kuwait Shu'oon termination form: one full-page scan with four Arabic RTL labels over the
    /// blanks. Layout reproduced verbatim.
    ///
    /// THE ONE REPORT IN THIS BATCH WITH NO SQL AT ALL. The legacy version bound a
    /// <c>DevExpress.Persistent.Base.ReportsV2.CollectionDataSource</c> to the XPO entity
    /// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.Employee</c> and navigated
    /// association properties straight from the layout
    /// (<c>OrgAssignment.OrganizationUnit.NameAr</c>), narrowing to one employee with a client-side
    /// <c>FilterString = "[Code] = ?EmployeeCode"</c>.
    ///
    /// None of that survives: a DevExpress-constructed report in HCMv3 has no XPO session and no
    /// ABP DI container, and a client-side filter over a data source that is now SQL would fetch
    /// every employee of every tenant before discarding them. The collection source is replaced by
    /// a <c>SqlDataSource</c> whose query projects exactly what the four labels bound -- including
    /// columns literally named <c>FullNameArCal</c> and <c>PrintedDate</c>, so the two legacy
    /// <c>CalculatedField</c>s could be dropped without touching a single DataBinding.
    ///
    /// KNOWN GAP: <c>xrLabel2</c> printed the organisation unit's ARABIC name. HCMv3's
    /// <c>AbpOrganizationUnits</c> has only <c>DisplayName</c> -- no NameEn/NameAr pair -- so the
    /// display name is used and the Arabic org name is unavailable. Flagged, not faked.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPayShounTerminationFormEn : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "GovtPayShounTerminationFormEn";

        public rptPayShounTerminationFormEn()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = GovtFormsImageStore.Load("rptPayShounTerminationFormEn", "xrPictureBox1");
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptPayShounTerminationFormEn(string employeeCode)
            : this()
        {
            EmployeeCode.Value = employeeCode ?? string.Empty;
        }
    }
}
