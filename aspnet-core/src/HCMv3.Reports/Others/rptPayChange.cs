using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptPayChange</c>.
    ///
    /// A landscape grid listing every salary change in a window: the new basic-pay header broken into
    /// five wage buckets, the previous total, the delta, and a Remarks column naming which buckets
    /// moved. Two report modes, selected by <c>PrmReportType</c>: 1 = the currently effective header,
    /// 2 = headers whose StartDate falls inside PrmStartDate..PrmEndDate.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                        | Replacement |
    /// |---------------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                                      | <see cref="HcmReportBase"/>. |
    /// | <c>SqlDataSource.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report <c>.resx</c> (2 queries) and inline in the designer (2 more) | <see cref="OthersHrFormsSql.PayChangeQuery"/>, <see cref="OthersHrFormsSql.PayChangeCompanyQuery"/>, <see cref="OthersHrFormsSql.PayChangeCostCenterQuery"/>, <see cref="OthersHrFormsSql.PayChangePayrollAreaQuery"/>. |
    /// | <c>CalcHeaders.Expression</c> from the <c>.resx</c>           | inlined verbatim in the designer file. |
    /// | three empty <c>BeforePrint</c> handlers (report, Detail, GroupHeader1) | dropped, along with their wiring. All three bodies were empty or entirely commented out: <c>Detail_BeforePrint</c> held a commented-out 25-rows-per-page page break, <c>GroupHeader1_BeforePrint</c> was <c>{ }</c>, and <c>rptPayChange_BeforePrint</c> was a commented-out month-name switch that built <c>lblHeader.Text</c>. |
    /// | <c>SecuritySystem.CurrentUserName</c> (commented out in the legacy ctor) | not restored. Nothing in the layout prints it. |
    ///
    /// v23.2 CONSTRAINTS HIT (see OthersHrFormsSql rewrite note R6): the legacy query used
    /// <c>REPLACE()</c> to trim the Remarks list, which v23.2 <c>CustomSqlQuery</c> rejects outright.
    /// It is rewritten as a CASE/LEFT producing byte-identical output, legacy quirks included.
    ///
    /// DEAD LAYOUT CARRIED OVER: the report declares twenty-odd calculated fields copied from the
    /// payslip report (DataMembers "PayslipQuery", "SalaryRegisterDetail", "Employee.Absence",
    /// "Query" -- none of which is a query on this report's data source). No control binds any of
    /// them. They are kept rather than deleted because deleting them is a layout change; see the
    /// porting notes.
    ///
    /// MULTI-TENANCY: all four queries carry <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPayChange : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayChange";

        public rptPayChange()
        {
            InitializeComponent();

            // Legacy defaults, verbatim: today for both ends of the window, Status = 1 (Active) and
            // ReportType = 1 (currently effective header).
            PrmStartDate.Value = DateTime.Now.Date;
            PrmEndDate.Value = DateTime.Now.Date;
            PrmStatus.Value = 1;
            PrmReportType.Value = 1;

            // The three optional string filters are bound straight into SQL as @Prm... and the query
            // treats "" as "no filter". They are initialised so a caller that never sets them does
            // not send NULL into an ISNULL(...) <> '' comparison whose semantics differ.
            PrmEmpCode.Value = string.Empty;
            PrmPayrollArea.Value = string.Empty;
            PrmCostCenter.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for callers that already know the window.</summary>
        public rptPayChange(DateTime startDate, DateTime endDate, int reportType)
            : this()
        {
            PrmStartDate.Value = startDate;
            PrmEndDate.Value = endDate;
            PrmReportType.Value = reportType;
        }
    }
}
