using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.rptSubordPayslipEn</c> -- a
    /// manager's view of the payslips of the employees who report to them.
    ///
    /// RELATIONSHIP TO <see cref="rptPayslipEn"/>
    /// This is the base English payslip with two differences and nothing else: a different master
    /// query (driven from EmployeeManagers, so one row per manager/subordinate/period rather than
    /// one per employee/period), and a SUBSET of the subreports -- five instead of nine. Every one of
    /// those five is the ALREADY PORTED subreport class, INSTANTIATED, not cloned:
    /// <see cref="srptPaySlipEarningEn"/>, <see cref="srptPaySlipDeductionEn"/>,
    /// <see cref="srptPayslipAttendenceSummaryEn"/>, <see cref="srptPayslipWorkingDaysSummaryEn"/> and
    /// <see cref="srptPayslipIndemnityDaysSummaryEn"/>. Their parameter contracts
    /// (EmpCode/empcode, ForPeriod, InPeriod, RunType) are satisfied by ParameterBindings onto this
    /// report's master query, exactly as rptPayslipEn does it. NO new srpt* file was created.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                    | Replacement |
    /// |-----------------------------------------------------------|-------------|
    /// | <c>SecuritySystem.CurrentUserName</c> assigned to <c>LoggedUserNme.Text</c> in the ctor | <c>PrmLoggedUserName</c> report parameter, expression-bound to the label -- same fix as rptPayslipEn. Assigning it in the ctor is why an exported layout carries the exporting developer's user name as static text. |
    /// | <c>SysService.IsAdminRole((PermissionPolicyUser)SecuritySystem.CurrentUser)</c> hiding <c>EmployeeCode</c> and pinning it to the caller's own user name | NOT ported. Whether a user may type an arbitrary manager code is an authorization decision and belongs behind ABP permissions in the calling layer; baking it into report layout means a designer edit can revoke it. Identical call to the one rptPayslipEn / rptPayslipAr made. NOTE the legacy logic was also inverted -- it pinned the parameter for ADMINS and left it free for everyone else. |
    /// | <c>ComponentResourceManager.GetString("$this.FilterString")</c> in <c>BeforePrint</c> | the two filter strings moved out of the .resx into <see cref="PayslipVariantSql"/>. This project carries no .resx at all (a Bitmap entry in one breaks the multi-targeted build). |
    ///
    /// WHAT WAS DELIBERATELY *NOT* REWRITTEN: the period/subordinate selection stays a DevExpress
    /// <c>FilterString</c> rather than becoming SQL predicates the way rptPayslipEn's did. The
    /// FilterString here is not XAF -- it is plain DevExpress criteria over calculated fields that
    /// the layout also uses -- and, more importantly, the master query has to return the synthetic
    /// "All" union row for the Subordinates combo to offer an All entry. Filtering the period in SQL
    /// would still leave the client-side row exclusion in place, so nothing would be gained.
    ///
    /// MULTI-TENANCY: both queries carry <c>@TenantId</c>, and every one of the five subreports
    /// brings its own. Call <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>
    /// or the report returns nothing.
    /// </summary>
    public partial class rptSubordPayslipEn : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SubordPayslipEn";

        public rptSubordPayslipEn()
        {
            InitializeComponent();

            // Legacy defaulted nothing here; the period defaults match rptPayslipEn's "last month"
            // convenience so the designer preview is not empty. Every real caller overwrites them.
            PeriodMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PeriodYear.Value = DateTime.Now.AddMonths(-1).Year;

            EnsureHcmParameters(withLoggedUserName: true);

            // Replaces "this.LoggedUserNme.Text = SecuritySystem.CurrentUserName" in the constructor.
            LoggedUserNme.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        /// <summary>
        /// Convenience overload for programmatic callers. <paramref name="managerCode"/> is the
        /// MANAGER's employee code; <paramref name="subordinateId"/> is an Employees.Id, or -1 for
        /// "all of this manager's subordinates".
        /// </summary>
        public rptSubordPayslipEn(string managerCode, int subordinateId, int month, int year, int runType)
            : this()
        {
            EmployeeCode.Value = managerCode ?? string.Empty;
            subordinates.Value = subordinateId;
            PeriodMonth.Value = month;
            PeriodYear.Value = year;
            RunType.Value = runType;
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs). The handler is wired at BOTH report and Detail
        // band level in the legacy designer; that is preserved, so it runs twice -- harmless, since
        // it only assigns FilterString.
        private void rptSubordPayslipEn_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Legacy behaviour, verbatim: -1 in the Subordinates combo means "every subordinate of
            // this manager", which swaps the per-subordinate predicate for a per-manager one.
            FilterString = Convert.ToString(subordinates.Value) == "-1"
                ? PayslipVariantSql.SubordPayslipEnAllFilter
                : PayslipVariantSql.SubordPayslipEnFilter;
        }
    }
}
