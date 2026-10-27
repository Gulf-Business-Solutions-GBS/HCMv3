using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.QuotaProvision
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.QuotaProvision.rptLeaveIndemnityStatement</c> -- the
    /// combined statement of annual-leave and end-of-service provision. FINANCIAL OUTPUT.
    /// It nests two subreports: <see cref="srptLeaveStatement"/> and
    /// <see cref="srptIndemnityStatement"/>, and adds their totals to its own.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy | Replacement |
    /// |---|---|
    /// | <c>this.Scripts.OnBeforePrint = "rptLeaveIndemnityStatement_BeforePrint"</c> in the designer | REMOVED -- inert (no script source ships with the layout). |
    /// | designer <c>SelectQuery</c> over the TABLE "PayrollArea" feeding the PrmPayrollArea look-up | replaced by a <c>CustomSqlQuery</c> of the same NAME carrying <see cref="QuotaProvisionSql.PayrollAreaList"/>. A SelectQuery cannot express the mandatory tenant filter, and the table is now "PayrollAreas". Same substitution the payslip port made. |
    /// | <c>MyXtraReport</c> base | <see cref="HcmReportBase"/>. |
    ///
    /// NOTHING ELSE IN THIS CLASS TOUCHED XAF -- unusually for this batch, all of its arithmetic was
    /// already pure C# over report values, so <see cref="GetSuggestedLeaveDays"/> and the two
    /// PrintOnPage totals are ported verbatim.
    ///
    /// MULTI-TENANCY: <see cref="HcmReportBase.ApplyTenant"/> recurses into both subreports, so one
    /// call covers all three queries -- but it must be made before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptLeaveIndemnityStatement : HcmReportBase
    {
        public const string ReportName = "LeaveIndemnityStatement";

        public rptLeaveIndemnityStatement()
        {
            InitializeComponent();

            PrmEmpCode.Value = string.Empty;
            PrmMonth.Value = DateTime.Now.Month;
            PrmYear.Value = DateTime.Now.Year;

            EnsureHcmParameters();
        }

        // v17.2 -> v23.2: XRControl.BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs).
        private void lblHeader_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // The legacy source had the period suffix commented out; kept commented-out behaviour.
            lblHeader.Text = "STATEMENT OF LEAVE & INDEMNITY PROVISION";
        }

        /// <summary>
        /// Projects the annual-leave balance forward from the as-on date to a hypothetical last day
        /// in service. PORTED VERBATIM -- this is the leave half of a provision figure:
        ///     perDayQuota   = TotalDaysAllowed / days in the as-on YEAR (365 or 366)
        ///     projectedDays = lastDayInService - asOnDate            (calendar days, can be negative)
        ///     suggested     = currentBalance + projectedDays * perDayQuota
        /// Note it deliberately uses the CALENDAR YEAR of the as-on date for the denominator, not a
        /// 365 constant, so a leap year accrues very slightly slower per day. Kept.
        /// The catch-all returning <c>currentBalance</c> is also legacy behaviour.
        /// </summary>
        private double GetSuggestedLeaveDays(DateTime asOnDate, DateTime lastDayInService, double currentBalance, double totalDaysAllowed)
        {
            try
            {
                var startDate = new DateTime(asOnDate.Year, 1, 1);
                var endDate = new DateTime(asOnDate.Year, 12, 31);

                var totalDaysInYear = endDate.Subtract(startDate).TotalDays + 1;
                var perDayQuota = totalDaysAllowed / totalDaysInYear;
                var projectedDays = lastDayInService.Date.Subtract(asOnDate.Date).TotalDays;
                var futureAccrual = projectedDays * perDayQuota;

                return currentBalance + futureAccrual;
            }
            catch
            {
                return currentBalance;
            }
        }

        // v17.2 -> v23.2: as above. Body ported verbatim.
        private void lblTotalALAndIndemnityAmount_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                double totalDaysAllowed;
                double currentBalance;
                double currentLeaveAmount;
                DateTime lastDayInService;

                // as-on date = the last day of the selected month
                var asOnDate = new DateTime(
                    Convert.ToInt32(PrmYear.Value),
                    Convert.ToInt32(PrmMonth.Value),
                    DateTime.DaysInMonth(Convert.ToInt32(PrmYear.Value), Convert.ToInt32(PrmMonth.Value)));

                double.TryParse(Convert.ToString(GetCurrentColumnValue("AL_Closing")), out currentBalance);
                double.TryParse(Convert.ToString(GetCurrentColumnValue("TotalDaysAllowed")), out totalDaysAllowed);
                double.TryParse(Convert.ToString(GetCurrentColumnValue("AL_TotalProvision_Amount")), out currentLeaveAmount);
                DateTime.TryParse(Convert.ToString(PrmLastDayInService.Value), out lastDayInService);

                var suggestedLeaveDays = GetSuggestedLeaveDays(asOnDate, lastDayInService, currentBalance, totalDaysAllowed);

                SuggestedLeaveDays.Text = string.Format("{0:#,0.000}", suggestedLeaveDays);

                // the current provision's implied per-day value, reapplied to the projected days
                double perDayAmount = 0;
                if (currentBalance != 0)
                {
                    perDayAmount = currentLeaveAmount / currentBalance;
                }

                SuggestedLeaveAmount.Text = string.Format("{0:#,0.000}", suggestedLeaveDays * perDayAmount);
            }
            catch
            {
                // Legacy swallowed every exception here. Kept: a failed projection must not take the
                // whole statement down, and the labels simply stay at their design-time text.
            }
        }

        // Ported verbatim: leave provision (own band) + indemnity provision (from the subreport).
        private void lblTotalALAndIndemnityAmount_PrintOnPage(object sender, PrintOnPageEventArgs e)
        {
            try
            {
                var totalAnnualLeaveProvision = lblBalanceALAmount.Text != "" ? Convert.ToDouble(lblBalanceALAmount.Text) : 0;
                var totalIndemnityProvision = ((srptIndemnityStatement)this.srptIndemnityStatement.ReportSource).totalindAmout;

                lblTotalALAndIndemnityAmount.Text =
                    string.Format("{0:#,0.000}", totalAnnualLeaveProvision + totalIndemnityProvision);
            }
            catch
            {
            }
        }

        // Ported verbatim: the same sum, on the projected (suggested) figures.
        private void lblTotalSuggestedALAndIndemnityAmount_PrintOnPage(object sender, PrintOnPageEventArgs e)
        {
            try
            {
                var totalSuggestedAnnualLeaveProvision = SuggestedLeaveAmount.Text != "" ? Convert.ToDouble(SuggestedLeaveAmount.Text) : 0;
                var totalSuggestedIndemnityProvision = ((srptIndemnityStatement)this.srptIndemnityStatement.ReportSource).totalSuggestedindAmout;

                lblTotalSuggestedALAndIndemnityAmount.Text =
                    string.Format("{0:#,0.000}", totalSuggestedAnnualLeaveProvision + totalSuggestedIndemnityProvision);
            }
            catch
            {
            }
        }
    }
}
