using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Payroll.rptSubordPayslipAr</c> --
    /// the Arabic manager's view of subordinates' payslips. Arabic sibling of
    /// <see cref="rptSubordPayslipEn"/>; read that class's remarks too.
    ///
    /// SUBREPORT REUSE: all five subreports are the ALREADY PORTED Arabic classes, INSTANTIATED, not
    /// cloned -- <see cref="srptPaySlipEarningAr"/>, <see cref="srptPaySlipDeductionAr"/>,
    /// <see cref="srptPayslipAttendenceSummaryAr"/>, <see cref="srptPayslipWorkingDaysSummaryAr"/>,
    /// <see cref="srptPayslipIndemnityDaysSummaryAr"/>. No new srpt* file was created.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                    | Replacement |
    /// |-----------------------------------------------------------|-------------|
    /// | <c>SecuritySystem.CurrentUserName</c> assigned to <c>LoggedUserNme.Text</c> in the ctor | <c>PrmLoggedUserName</c> report parameter, expression-bound to the label. |
    /// | <c>SysService.IsAdminRole(...)</c> hiding <c>EmployeeCode</c> and pinning it to the caller | NOT ported -- authorization belongs in the calling layer, behind ABP permissions. Same call as rptPayslipEn / rptPayslipAr / rptSubordPayslipEn. |
    /// | <c>FontCollection1</c> / <c>FontCollection2</c>: static <c>PrivateFontCollection</c>s loading <c>BFantezy.ttf</c> and <c>BNaznnBd.ttf</c> through <c>HttpContext.Current.Server.MapPath("~/Contents/fonts/...")</c>, then assigned to eleven labels in the constructor | NOT ported. Those two font files are not in the HCMv3 repo, <c>HttpContext.Current</c> does not exist on ASP.NET Core, and the call throws a NullReferenceException in any non-request context -- which is exactly why this report fails to construct in the legacy exporter. The eleven labels keep the Arial/Tahoma faces their layout already declares. *** LAYOUT DEVIATION: the eleven date labels render in Arial rather than BFantezy/BNaznnBd. *** rptPayslipAr hit the same dead code, but there every consumer line was already commented out, so it lost nothing. |
    /// | <c>ComponentResourceManager.GetString(...)</c> for the FilterString and the three Arabic month expressions | moved out of the .resx: the filter strings into <see cref="PayslipVariantSql"/>, the month expressions REUSED from the committed <see cref="PayslipArSql"/> (they are byte-identical to this report's, legacy typo and all). |
    ///
    /// MULTI-TENANCY: both queries carry <c>@TenantId</c>, and each of the five subreports brings its
    /// own. Call <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptSubordPayslipAr : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SubordPayslipAr";

        public rptSubordPayslipAr()
        {
            InitializeComponent();

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
        ///
        /// Note there is no runType argument: unlike the English sibling, the Arabic layout never
        /// carried a RunType parameter and never bound one into its subreports. See the port notes.
        /// </summary>
        public rptSubordPayslipAr(string managerCode, int subordinateId, int month, int year)
            : this()
        {
            EmployeeCode.Value = managerCode ?? string.Empty;
            Subordinates.Value = subordinateId;
            PeriodMonth.Value = month;
            PeriodYear.Value = year;
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs). Wired on the Detail band only, as in the legacy
        // designer.
        private void rptSubordPayslipAr_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FilterString = Convert.ToString(Subordinates.Value) == "-1"
                ? PayslipVariantSql.SubordPayslipArAllFilter
                : PayslipVariantSql.SubordPayslipArFilter;
        }
    }
}
