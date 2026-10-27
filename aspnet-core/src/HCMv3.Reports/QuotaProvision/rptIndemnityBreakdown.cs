using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.QuotaProvision
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.QuotaProvision.rptIndemnityBreakdown</c> -- the
    /// end-of-service settlement breakdown handed to a leaving employee, one row per service slab.
    /// FINANCIAL OUTPUT.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy | Replacement |
    /// |---|---|
    /// | <c>BeforePrint</c> opened <c>new XPObjectSpaceProvider(ConfigurationManager.ConnectionStrings["HCM10ConnectionString"])</c> and ran two <c>GetObjectsQuery&lt;&gt;</c> lookups -- <c>Employee.JoiningDate</c> (fetched and then never used) and <c>EmployeeSeparation</c> -- purely to print "Resignation" or "Termination" in <c>xrLabel61</c> | The separation type is now PROJECTED BY THE QUERY (<c>SeparationType</c>, added by this port) and read with <c>GetCurrentColumnValue</c>. A DevExpress-constructed report has no DI container and must not open its own database connection. |
    /// | <c>throw new Exception("Employee Exit not created for this Employee.")</c> when no EmployeeSeparation row exists | NOT PORTED AS A THROW. See <see cref="rptIndemnityBreakdown_BeforePrint"/> -- it now prints an empty separation type. Throwing from BeforePrint inside the Web Document Viewer turns a missing-data condition into an HTTP 500 on every page request, and the same message is unreachable for the operator. The calling app service should validate and refuse before rendering. |
    /// | <c>this.Scripts.OnBeforePrint = "rptLeaveIndemnityStatement_BeforePrint"</c> in the designer -- note it names the SIBLING report's handler | REMOVED. It was already inert (no script source ships with the layout), and it pointed at the wrong report anyway. |
    /// | <c>lblTotalALAndIndemnityAmount_BeforePrint</c> body | ENTIRELY COMMENTED OUT in the legacy source. Nothing to port. |
    /// | <c>lblTotalALAndIndemnityAmount_PrintOnPage</c> body | A try/catch around an empty try/catch around nothing, declaring one unused local. Dead. Kept as an empty handler because the designer wires it. |
    ///
    /// *** BEFORE RELYING ON THE AMOUNTS, READ the "AS AT TODAY, NOT AS AT A CHOSEN MONTH" note on
    /// <see cref="QuotaProvisionSql.IndemnityBreakdownQuery"/>. *** Against HCMv3 data the unclamped
    /// contract end date (9999-12-31) inflates the settlement by orders of magnitude. That is
    /// reproduced legacy behaviour and needs a product decision, not a quiet fix.
    /// </summary>
    public partial class rptIndemnityBreakdown : HcmReportBase
    {
        public const string ReportName = "IndemnityBreakdown";

        /// <summary>
        /// Mirrors the legacy <c>HCM10.Module.BusinessObjects.PersonnelAdministration.SeparationType</c>
        /// enum, which is not being ported. Only these two members are used here.
        /// </summary>
        private const int SeparationTypeResignation = 0;

        public rptIndemnityBreakdown()
        {
            InitializeComponent();

            PrmEmpCode.Value = string.Empty;

            EnsureHcmParameters();
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        private void rptIndemnityBreakdown_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Legacy read this off EmployeeSeparation through XPO; it is now a query column.
            var separationType = GetCurrentColumnValue("SeparationType");

            // Legacy threw "Employee Exit not created for this Employee." here. See the class remarks
            // for why that is not ported: a blank label is a far better failure mode inside a web
            // report viewer than a 500, and the guard belongs in the calling app service.
            // NOTE the DBNull check as well as the null check: GetCurrentColumnValue returns
            // DBNull.Value (not null) for a NULL column, and Convert.ToInt32(DBNull) throws.
            // That is exactly the state HCMv3 is in today -- EmployeeSeparations is empty.
            xrLabel61.Text = separationType == null || separationType == DBNull.Value
                ? string.Empty
                : Convert.ToInt32(separationType) == SeparationTypeResignation ? "Resignation" : "Termination";
        }

        // Legacy body was empty. Kept because the designer wires it.
        private void lblHeader_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        // Legacy body was entirely commented out. Kept because the designer wires it.
        private void lblTotalALAndIndemnityAmount_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        // Legacy body was dead code (nested empty try/catch). Kept because the designer wires it.
        private void lblTotalALAndIndemnityAmount_PrintOnPage(object sender, PrintOnPageEventArgs e)
        {
        }
    }
}
