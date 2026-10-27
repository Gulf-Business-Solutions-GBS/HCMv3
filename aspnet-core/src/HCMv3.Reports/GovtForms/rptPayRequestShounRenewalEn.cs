using System;

namespace HCMv3.Reports.GovtForms
{
    /// <summary>
    /// HCMv3 port of the legacy report <c>HCM10.Module.Reports.rptPayRequestShounRenewalEn</c>
    /// (source file lives under Reports/GovtForms even though its legacy namespace does not).
    ///
    /// A two-page Kuwait Shu'oon (residency affairs) renewal application. Both pages are full-page
    /// scans of the official form -- note the deliberately enormous <c>BottomMargin</c> band
    /// (2265pt) that the legacy designer used to carry page two. Layout reproduced verbatim: this
    /// is exactly the kind of "looks wrong, is required" geometry that must not be tidied.
    ///
    /// The legacy class derived from plain <c>XtraReport</c>, not from <c>MyXtraReport</c>, so it
    /// had no XAF surface of its own; it is rebased on <see cref="HcmReportBase"/> purely to pick
    /// up the tenant parameter and the connection-string plumbing.
    ///
    /// PORT NOTES
    ///   * <c>FilterString = "[Code] = ?EmployeeCode"</c> -- a client-side filter over a raw
    ///     SqlDataSource, which fetches every employee (in HCMv3, of every tenant) and then throws
    ///     them away. Pushed into the query's WHERE clause.
    ///   * four <c>CalculatedField</c>s pointed at data members ("Sponsor", "VisaInfo") that this
    ///     report's single-query data source never had, so they could never resolve. No label binds
    ///     any of them. Retargeted to "Query" against real columns rather than deleted, so the
    ///     designer surface still matches the legacy one.
    ///   * the two form scans moved out of the <c>.resx</c>; see <see cref="GovtFormsImageStore"/>.
    ///     Together they are ~10.7 MB and are shipped uncompressed -- the labels are aligned
    ///     against these exact images.
    ///   * <c>Version = "16.2"</c> -> <c>"23.2"</c>.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPayRequestShounRenewalEn : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "GovtPayRequestShounRenewalEn";

        public rptPayRequestShounRenewalEn()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = GovtFormsImageStore.Load("rptPayRequestShounRenewalEn", "xrPictureBox1");
            xrPictureBox2.Image = GovtFormsImageStore.Load("rptPayRequestShounRenewalEn", "xrPictureBox2");
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptPayRequestShounRenewalEn(string employeeCode)
            : this()
        {
            EmployeeCode.Value = employeeCode ?? string.Empty;
        }
    }
}
