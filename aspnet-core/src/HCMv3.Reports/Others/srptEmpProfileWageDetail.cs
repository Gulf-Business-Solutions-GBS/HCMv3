namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF subreport
    /// <c>HCM10.Module.Reports.Others.srptEmpProfileWageDetail</c>: the current basic-pay
    /// wage-type breakdown table (wage type / amount / currency, with a group total) printed
    /// inside <see cref="rptEmployeeProfile"/> and <see cref="rptBusinessTrip"/>.
    ///
    /// It is a report in its own right, not a nested class, because both parents set it as an
    /// <c>XRSubreport.ReportSource</c> and DevExpress needs a public parameterless type there.
    ///
    /// PARAMETERS: <c>PrmEmpCode</c> and <c>PrmDate</c>, both bound by the parent. PrmDate is
    /// bound but unused -- the legacy SQL never referenced it either. See the note in
    /// <see cref="OthersAdminSql.EmployeeWageDetailQuery"/>.
    ///
    /// MULTI-TENANCY: <see cref="HcmReportBase.EnsureHcmParameters"/> is called here as well as on
    /// the parents. A subreport carries its OWN SqlDataSource, so its query needs its own
    /// <c>@TenantId</c> QueryParameter; <see cref="HcmReportBase.ApplyTenant"/> walks nested
    /// subreports and sets the value on each.
    /// </summary>
    public partial class srptEmpProfileWageDetail : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmpProfileWageDetail";

        public srptEmpProfileWageDetail()
        {
            InitializeComponent();

            // MANDATORY -- see the class remarks.
            EnsureHcmParameters();
        }
    }
}
