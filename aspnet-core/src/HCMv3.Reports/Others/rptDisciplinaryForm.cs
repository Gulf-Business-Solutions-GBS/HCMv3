using System;
using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptDisciplinaryForm</c>.
    ///
    /// A full-page scan of the printed (Arabic) disciplinary form. Four XRShape rings mark which
    /// occurrence this is (1st..4th) and six small stamp images mark which penalty was applied; the
    /// code-behind decides which of the ten to show.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                        | Replacement |
    /// |---------------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                                      | <see cref="HcmReportBase"/>. |
    /// | <c>SqlDataSource.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report <c>.resx</c>                                | <see cref="OthersHrFormsSql.DisciplinaryFormQuery"/>. |
    /// | nine <c>.Image</c> entries in the <c>.resx</c>                | <see cref="OthersHrFormsImageStore"/>. The eight stamp images were byte-identical, so one copy ships and all eight boxes load it. |
    /// | <c>new XPObjectSpaceProvider(...)</c> + <c>GetObjectsQuery&lt;DisciplinaryRequest&gt;()</c> inside <c>BeforePrint</c> | removed. A DevExpress-constructed report has no DI container, so it can resolve neither ABP repositories nor a connection string; everything it needs now arrives through the data row. See <see cref="OthersHrFormsSql.DisciplinaryFormQuery"/> for the two columns added to carry it. |
    /// | report-level <c>BeforePrint</c>                               | moved to <see cref="Detail_BeforePrint"/>. Report-level BeforePrint fires once, before any data row is current, so it could not read the printed row at all -- the legacy handler only worked because it ran its own XPO query. The band-level handler is correct for both. |
    /// | <c>PrintEventHandler(PrintEventArgs)</c>                      | <c>BeforePrintEventHandler(CancelEventArgs)</c> (v17.2 -> v23.2). |
    ///
    /// ENTITY SUBSTITUTION: legacy <c>DisciplinaryRequest</c> (a self-service workflow request) does
    /// not exist in HCMv3; the admin-side <c>EmployeeDisciplinaries</c> is used instead, and
    /// <c>PrmRequestOid</c> now addresses <c>EmployeeDisciplinaries.Id</c>. See the query summary.
    ///
    /// BLOCKED (partial): <c>xrSubreport1</c> pointed at <c>srptDisciplinaryFormTotalCount</c>, which
    /// is not part of this batch and has not been ported. The subreport keeps its position, size and
    /// parameter bindings but has no ReportSource, so it renders empty. See the porting notes.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptDisciplinaryForm : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "DisciplinaryForm";

        // The legacy code-behind compared rec.DisciplinaryDeductionType -- a free-text string on the
        // request -- against these six literals. HCMv3 normalises the concept into the
        // DisciplinaryDeductionTypes lookup, so the comparison is now against that lookup's NameEn.
        // The literals (including their inconsistent casing of "hours"/"Hours") are reproduced
        // exactly; a lookup row named differently simply shows no stamp, which is what the legacy
        // report did with an unrecognised string.
        private const string DeductionWrittenWarning = "Written warning";
        private const string Deduction04Hours = "Deduction 4 hours";
        private const string Deduction08Hours = "Deduction 8 Hours";
        private const string Deduction16Hours = "Deduction 16 Hours";
        private const string Deduction24Hours = "Deduction 24 Hours";
        private const string DeductionSeparation = "Separation from service";

        public rptDisciplinaryForm()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = OthersHrFormsImageStore.Load(
                OthersHrFormsImageStore.DisciplinaryFormBackgroundResourceName);

            // All eight stamp boxes carried the same PNG in the legacy .resx (hash-verified).
            var mark = OthersHrFormsImageStore.Load(OthersHrFormsImageStore.DisciplinaryMarkResourceName);
            pbDED_Warn.Image = mark;
            pbDED_04.Image = mark;
            pbDED_08.Image = mark;
            pbDED_16.Image = mark;
            pbDED_24.Image = mark;
            pbDED_Sep.Image = mark;
            pblast.Image = mark;
            pbLast2.Image = mark;
        }

        /// <summary>Convenience overload for callers that already know the disciplinary record.</summary>
        public rptDisciplinaryForm(int disciplinaryId, DateTime printDate)
            : this()
        {
            PrmRequestOid.Value = disciplinaryId;
            PrmDate.Value = printDate;
        }

        /// <summary>
        /// Shows the occurrence ring and the penalty stamp that apply to the printed row.
        ///
        /// Legacy logic, preserved exactly: <c>int Count = rec.CurrentDeductionCount + 1;</c> then one
        /// ring visible per value 1..4, one stamp visible per matched deduction-type string, and
        /// <c>pblast</c> / <c>pbLast2</c> unconditionally hidden. The two always-hidden boxes look
        /// like an abandoned feature but are reproduced rather than deleted -- deleting them is a
        /// layout change.
        ///
        /// The inputs now come from the data row: <c>PriorDeductionCount</c> replaces
        /// <c>CurrentDeductionCount</c> (see the query summary for how it is derived and why that is
        /// a deviation) and <c>DeductionTypeNameEn</c> replaces the free-text
        /// <c>DisciplinaryDeductionType</c>.
        /// </summary>
        private void Detail_BeforePrint(object sender, CancelEventArgs e)
        {
            var count = TryGetColumn("PriorDeductionCount", out var priorCount)
                ? Convert.ToInt32(priorCount) + 1
                : 0;

            xrFirstCount.Visible = count == 1;
            xrSecondCount.Visible = count == 2;
            xrThirdCount.Visible = count == 3;
            xrFourthCount.Visible = count == 4;

            var deductionType = TryGetColumn("DeductionTypeNameEn", out var name)
                ? Convert.ToString(name)
                : string.Empty;

            pbDED_Warn.Visible = deductionType == DeductionWrittenWarning;
            pbDED_04.Visible = deductionType == Deduction04Hours;
            pbDED_08.Visible = deductionType == Deduction08Hours;
            pbDED_16.Visible = deductionType == Deduction16Hours;
            pbDED_24.Visible = deductionType == Deduction24Hours;
            pbDED_Sep.Visible = deductionType == DeductionSeparation;

            // Legacy: unconditionally hidden.
            pblast.Visible = false;
            pbLast2.Visible = false;
        }

        /// <summary>
        /// Reads a column of the row currently being printed, tolerating a missing column or a
        /// DBNull -- the form must still print for a record with no deduction type.
        ///
        /// NOTE: GetCurrentColumnValue returns <c>DBNull.Value</c>, never null, so the check below is
        /// deliberately against both.
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
