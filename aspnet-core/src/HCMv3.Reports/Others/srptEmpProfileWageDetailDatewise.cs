using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF subreport
    /// <c>HCM10.Module.Reports.Others.srptEmpProfileWageDetailDatewise</c> -- the wage-type breakdown
    /// of one basic-pay header, with a running total.
    ///
    /// Embedded TWICE by <see cref="rptSalaryIncrementLetter"/>: <c>xrSubreport1</c> bound to
    /// <c>Prev_BasicPayEffectivedate</c> (the old package) and <c>xrSubreport2</c> bound to
    /// <c>BasicPayEffectivedate</c> (the new one), side by side on the page.
    ///
    /// XAF CONSTRUCTS REPLACED:
    /// | Legacy | Replacement |
    /// |--------|-------------|
    /// | base class <c>MyXtraReport</c> | <see cref="HcmReportBase"/>. |
    /// | <c>srptEmpProfDS.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the <c>.resx</c> | <see cref="OthersCertificatesSql.EmployeeWageDetailDatewiseQuery"/>, shared verbatim with the parent's own <c>EmpSalaryDetail</c> query -- the two legacy copies were byte-identical apart from one commented-out line. |
    /// | <c>srptEmpProfDS.ResultSchemaSerializable</c> | dropped -- removed from the v23.2 API. |
    ///
    /// The legacy code-behind was a bare constructor; there is no business logic to port.
    ///
    /// PARAMETER CLAMPING: <c>PrmDate</c> arrives from a ParameterBinding onto a master-query column.
    /// When the master query returns no row -- an employee with no previous basic pay, for instance --
    /// the binding supplies nothing and the parameter falls back to <see cref="DateTime.MinValue"/>,
    /// which SqlClient rejects outright. <see cref="HcmReportBase.EnsureHcmParameters"/> hooks
    /// DataSourceDemanded to clamp it to SQL Server's minimum, turning a hard failure into an empty
    /// subreport. That is exactly the case this subreport hits on a first-ever increment.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. <see cref="HcmReportBase.ApplyTenant"/> on
    /// the PARENT report reaches this one, because it walks nested subreports.
    /// </summary>
    public partial class srptEmpProfileWageDetailDatewise : HcmReportBase
    {
        public srptEmpProfileWageDetailDatewise()
        {
            InitializeComponent();

            // MANDATORY even in a subreport: EnsureHcmParameters adds this report's own hidden
            // PrmTenantId, wires the TenantId query parameter, and installs the DateTime clamp
            // described in the class remarks.
            EnsureHcmParameters();
        }
    }
}
