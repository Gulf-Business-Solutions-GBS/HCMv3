using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptHolidayCalendar</c> --
    /// the holiday dates of one holiday calendar over a chosen date range.
    ///
    /// | Legacy                                              | Replacement |
    /// |-----------------------------------------------------|-------------|
    /// | base class <c>MyXtraReport</c> (XAF)                 | <see cref="HcmReportBase"/>. |
    /// | <c>sqlDataSource1.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report's <c>.resx</c>                     | <see cref="OthersAdminSql"/>. |
    /// | the <c>QueryHoliday</c> designer <c>SelectQuery</c>  | a <c>CustomSqlQuery</c> -- a SelectQuery has no WHERE clause, so it could not be tenant-filtered and would have listed every tenant's calendars in the parameter panel. See <see cref="OthersAdminSql.HolidayCalendarLookupQuery"/>. |
    /// | (nothing)                                           | <see cref="HcmReportBase.EnsureHcmParameters"/> -- the hidden tenant parameter. |
    ///
    /// FILTERING IS DELIBERATELY LEFT CLIENT-SIDE. The report narrows to the date range and the
    /// chosen calendar with <c>FilterString</c>, not in SQL. That is normally worth pushing down
    /// (see SalaryCertificateSql rewrite note 3), but here the fetched set is one row per holiday
    /// date per tenant -- tens of rows -- and the SQL already carries the tenant predicate, so
    /// there is nothing to leak and nothing to gain.
    ///
    /// The three parameters keep their legacy names <c>From</c>, <c>To</c> and <c>Calendar</c>
    /// (not the Prm* convention) because <c>FilterString</c> references them by name.
    ///
    /// MULTI-TENANCY: both queries carry <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptHolidayCalendar : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "HolidayCalendar";

        public rptHolidayCalendar()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for callers that already know the range and calendar.</summary>
        public rptHolidayCalendar(DateTime fromDate, DateTime toDate, int holidayCalendarId)
            : this()
        {
            From.Value = fromDate;
            To.Value = toDate;
            Calendar.Value = holidayCalendarId;
        }
    }
}
