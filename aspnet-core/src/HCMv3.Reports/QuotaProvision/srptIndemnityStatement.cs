using System;

namespace HCMv3.Reports.QuotaProvision
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.QuotaProvision.srptIndemnityStatement</c> -- the
    /// end-of-service indemnity slab table nested inside <see cref="rptLeaveIndemnityStatement"/>.
    /// FINANCIAL OUTPUT.
    ///
    /// ===================================================================================================
    /// THE ONE SUBSTANTIAL DE-XAF'ING IN THIS BATCH -- READ BEFORE CHANGING
    /// ===================================================================================================
    /// The legacy <c>lblBalanceIndemnityAmount_BeforePrint</c> was ~200 lines that, ON EVERY LABEL
    /// PRINT, opened <c>new XPObjectSpaceProvider(ConfigurationManager.ConnectionStrings
    /// ["HCM10ConnectionString"])</c> and ran two queries:
    ///   * <c>GetObjectsQuery&lt;IndemnityPayRule&gt;()</c> -> the statutory PayPercentage for the
    ///     employee's years of service and the chosen separation type;
    ///   * <c>GetObjectsQuery&lt;EmployeeSubGroupGratuity&gt;()</c> -> the gratuity slabs, then walked
    ///     them in C# accumulating <c>applicableYears * GratuityDays</c> (and
    ///     <c>* PerDayRate</c> for the money), spilling any service beyond the last slab onto that
    ///     slab's rate, and finally scaling both by <c>PayPercentage / 100</c>.
    /// The two results were written into <c>xrLabel34</c> (suggested indemnity DAYS) and
    /// <c>xrLabel35</c> (suggested indemnity AMOUNT).
    ///
    /// THAT CANNOT BE PORTED AS-IS. A DevExpress-constructed report has no ABP DI container, and a
    /// report layout must not open its own database connection -- least of all once per label print.
    ///
    /// WHAT REPLACES IT: the SAME RULE, EXPRESSED IN THE QUERY. <c>IndemnityStatementQuery</c> already
    /// computes <c>SuggestedTotalIndemnityDays</c> -- a window SUM of the slab-wise gratuity days
    /// evaluated at <c>@PrmLastDayInService</c>, scaled by <c>MAX(PayPercentage) OVER (PARTITION BY
    /// employee)</c> sourced from <c>IndemnityPayRuleDetails</c> keyed on <c>@PrmSeparationType</c>.
    /// So:
    ///     xrLabel34 (days)   = [SuggestedTotalIndemnityDays]
    ///     xrLabel35 (amount) = [SuggestedTotalIndemnityDays] * [PerDayRate]
    /// Both read from the current row rather than from a per-print database round trip.
    ///
    /// *** KNOWN SEMANTIC DIFFERENCE -- NEEDS SIGN-OFF BEFORE RELEASE. ***
    /// The C# walk allocated service in YEARS (<c>Math.Min(remainingYears, slabYears)</c>); the SQL
    /// allocates in DAYS against 365-day slabs. For service that lands mid-slab the two differ in the
    /// last decimal place or two. The two also disagree about service beyond the final slab: the C#
    /// spilled it onto the last slab's rate, while the SQL relies on the final open-ended slab (the
    /// one whose ChangeYear the query maps to 99 years) already covering it. Reconcile against a
    /// known settlement before this report is used to pay anyone.
    ///
    /// *** DATA GAP: IndemnityPayRules is EMPTY in HCMv3 *** -- PayPercentage falls back to the
    /// legacy default of 100, i.e. no statutory reduction. The legacy C# had the same fallback.
    /// ===================================================================================================
    ///
    /// <c>totalindAmout</c> / <c>totalSuggestedindAmout</c> are kept as public fields with their
    /// legacy (misspelled) names: <see cref="rptLeaveIndemnityStatement"/> reads them by name from
    /// its own PrintOnPage handlers to build the combined leave + indemnity totals.
    /// </summary>
    public partial class srptIndemnityStatement : HcmReportBase
    {
        public const string ReportName = "IndemnityStatementSub";

        /// <summary>Running indemnity provision total, read by the parent report. Legacy spelling kept.</summary>
        public double totalindAmout = 0;

        /// <summary>Running SUGGESTED indemnity total, read by the parent report. Legacy spelling kept.</summary>
        public double totalSuggestedindAmout = 0;

        public srptIndemnityStatement()
        {
            InitializeComponent();

            EnsureHcmParameters();
        }

        // v17.2 -> v23.2: XRControl.BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs).
        private void lblBalanceIndemnityAmount_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Legacy: totalindAmout = Convert.ToDouble(lblBalanceIndemnityAmount.Text) -- unguarded,
            // so an empty label threw and the whole ~200-line body was skipped by its catch.
            // TryParse instead; an unparseable label now leaves the running total at zero, which is
            // what the legacy catch effectively did anyway.
            double balance;
            double.TryParse(lblBalanceIndemnityAmount.Text, out balance);
            totalindAmout = balance;

            // The projected-indemnity calculation that used to live here, in C# over XPO, now comes
            // from the query. See the class remarks for the full explanation and the one known
            // semantic difference.
            double suggestedIndemnityDays;
            double perDayRate;
            double.TryParse(Convert.ToString(GetCurrentColumnValue("SuggestedTotalIndemnityDays")), out suggestedIndemnityDays);
            double.TryParse(Convert.ToString(GetCurrentColumnValue("PerDayRate")), out perDayRate);

            var suggestedIndemnityAmount = suggestedIndemnityDays * perDayRate;

            xrLabel35.Text = Math.Round(suggestedIndemnityAmount, 3).ToString("0.000");
            xrLabel34.Text = Math.Round(suggestedIndemnityDays, 3).ToString("0.000");
        }

        // v17.2 -> v23.2: as above.
        private void xrLabel35_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Legacy used an unguarded Convert.ToDouble here, which threw -- uncaught, this time --
            // whenever xrLabel35 was empty. TryParse; an unset label leaves the total at zero.
            double suggested;
            double.TryParse(xrLabel35.Text, out suggested);
            totalSuggestedindAmout = suggested;
        }
    }
}
