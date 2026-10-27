using System;
using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptEmploymentContractEn023</c>.
    ///
    /// The English employment contract. Despite the "023" suffix this is a BASE report in the legacy
    /// tree, not a <c>_CustomNNN</c> customer variant, so it is ported alongside
    /// <see cref="rptEmploymentContract"/> and <see cref="rptEmploymentContractAr023"/>. The three
    /// share <see cref="OthersHrFormsSql.EmploymentContractWageQuery"/>, which was byte-identical in
    /// all three legacy .resx files.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                        | Replacement |
    /// |---------------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                                      | <see cref="HcmReportBase"/>. |
    /// | <c>SqlDataSource.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report <c>.resx</c>                                | <see cref="OthersHrFormsSql.EmploymentContractEn023Query"/> and <see cref="OthersHrFormsSql.EmploymentContractWageQuery"/>. |
    /// | seven <c>xrLabelNN.Text</c> entries in the <c>.resx</c>       | inlined verbatim as string literals in the designer file (contract clause text, not localisation). |
    /// | <c>SecuritySystem.CurrentUserName</c> read into a field and stamped onto <c>lblUserlogin</c> / <c>xrLabel29</c> in the ctor | the logged-in user now arrives as the <see cref="HcmReportBase.LoggedUserNameParameter"/> report parameter, pushed in by the caller through <see cref="HcmReportBase.ApplyLoggedUserName"/> -- the layer that actually has an ABP session. The two labels are stamped in <see cref="Detail_BeforePrint"/> instead of the ctor, so the parameter has been applied by then. |
    /// | <c>new XPObjectSpaceProvider(...)</c> + <c>GetObjectsQuery&lt;Employee&gt;()</c> / <c>GetObjectsQuery&lt;SalaryRegister&gt;()</c> inside <c>BeforePrint</c>, to find the latest regular payroll period and then the wage-type '1000' amount | removed; <c>Current_Salary</c> is projected by the SQL instead. A DevExpress-constructed report has no DI container, so it cannot resolve ABP repositories. See the note on <see cref="Detail_BeforePrint"/> for the one behavioural difference this creates. |
    /// | report-level <c>BeforePrint</c>                               | moved to <see cref="Detail_BeforePrint"/>. Report-level BeforePrint fires once, before any data row is current. |
    /// | <c>PrintEventHandler(PrintEventArgs)</c>                      | <c>BeforePrintEventHandler(CancelEventArgs)</c> (v17.2 -> v23.2). |
    /// | <c>rptEmploymentContractEn023(object[] args)</c> ctor          | dropped; it did nothing but <c>InitializeComponent()</c> plus the PrmFormDate assignment. |
    ///
    /// MULTI-TENANCY: both queries carry <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptEmploymentContractEn023 : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmploymentContractEn023";

        public rptEmploymentContractEn023()
        {
            InitializeComponent();

            PrmFormDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. "true" also adds PrmLoggedUserName, which the two footer stamps need.
            EnsureHcmParameters(withLoggedUserName: true);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptEmploymentContractEn023(string employeeCode, DateTime formDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmFormDate.Value = formDate;
        }

        /// <summary>
        /// Composes the basic-salary-in-words sentence and the two "printed by" footer stamps.
        ///
        /// Legacy text: <c>"(" + AmountInWords(amount) + " Kuwaiti Dinars only)."</c>, printed only
        /// when the employee had at least one regular payroll run -- the legacy guard was
        /// <c>effectiveDate != default(DateTime)</c>, where effectiveDate came from a SalaryRegister
        /// query, and the amount then came from the basic-pay header covering that period.
        ///
        /// *** BEHAVIOURAL DIFFERENCE, deliberate. *** HCMv3 projects <c>Current_Salary</c> from the
        /// basic-pay header effective TODAY, not from the header covering the last regular payroll
        /// period, and there is therefore no "has a regular payroll run" guard. Reproducing the
        /// legacy two-step would mean adding a SalaryRegisters lookup that no other column on this
        /// contract needs, and the legacy pairing was itself suspect: it read the period from
        /// SalaryRegister and the amount from BasicPay, which disagree whenever a salary changed
        /// after the last run. Instead the sentence is simply omitted when Current_Salary is NULL --
        /// i.e. when the employee has no basic pay in force. Flagged in the porting notes.
        /// </summary>
        private void Detail_BeforePrint(object sender, CancelEventArgs e)
        {
            lblBasicSalaryInWords.Text = string.Empty;

            if (TryGetColumn("Current_Salary", out var currentSalary))
            {
                var amount = Convert.ToDouble(currentSalary);
                lblBasicSalaryInWords.Text = "(" + AmountInWords(amount) + " Kuwaiti Dinars only).";
            }

            // Legacy: CurrentUserName + " " + DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"), stamped
            // on both footers. The user name now comes from the report parameter; the timestamp is
            // taken at print time rather than at construction time (a difference of milliseconds).
            var stamp = Convert.ToString(Parameters[LoggedUserNameParameter].Value)
                        + " " + DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");

            lblUserlogin.Text = stamp;
            xrLabel29.Text = stamp;
        }

        /// <summary>
        /// Reads a column of the row currently being printed, tolerating a missing column or a
        /// DBNull. NOTE: GetCurrentColumnValue returns <c>DBNull.Value</c>, never null.
        /// </summary>
        private bool TryGetColumn(string columnName, out object value)
        {
            value = null;

            try
            {
                value = GetCurrentColumnValue(columnName);
            }
            catch (Exception)
            {
                return false;
            }

            return value != null && value != DBNull.Value;
        }
    }
}
