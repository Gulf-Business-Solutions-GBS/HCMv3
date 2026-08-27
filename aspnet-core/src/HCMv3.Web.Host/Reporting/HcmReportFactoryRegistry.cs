using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.XtraReports.UI;
using HCMv3.ReportManagement;
using HCMv3.Reports.EmployeeInfo;
using HCMv3.Reports.Others;
using HCMv3.Reports.Payroll;

namespace HCMv3.Web.Reporting;

/// <summary>
/// The CODE-SIDE half of the report catalog: report name -&gt; a factory that constructs the ported
/// <c>XtraReport</c> class for it.
///
/// The DATA-SIDE half is the <see cref="PrintReport"/> table, which decides which reports a tenant
/// is *offered*. The two are deliberately separate:
///   * a <see cref="PrintReport"/> row with no entry here is a legacy report whose class has not
///     been ported yet -- it is listed as "not yet available" and refuses to render with a clear
///     message (see <see cref="HcmReportStorageWebExtension"/>);
///   * an entry here with no <see cref="PrintReport"/> row is a ported report the tenant has not
///     been given -- it does not appear in any list, but it is still a valid URL for the designer,
///     so a developer can open it.
///
/// ADD A PORTED REPORT HERE and nowhere else. The keys are mirrored into
/// <see cref="PortedReportCatalog"/> at start-up so the Application layer can compute
/// <c>isAvailable</c> without referencing HCMv3.Reports.
/// </summary>
public static class HcmReportFactoryRegistry
{
    private static readonly IReadOnlyDictionary<string, Func<XtraReport>> Factories =
        new Dictionary<string, Func<XtraReport>>(StringComparer.OrdinalIgnoreCase)
        {
            [rptPayslipEn.ReportName] = () => new rptPayslipEn(),
            [rptPayslipAr.ReportName] = () => new rptPayslipAr(),
            [rptSalaryCertificateEn.ReportName] = () => new rptSalaryCertificateEn(),

            // The legacy class is named rptSalaryCertificate, which reads like a shared base for the
            // *En variant. It is not: it is the Arabic certificate (see the class remarks), so the
            // port is named for what it is while keeping the catalog key the seed data already uses.
            [rptSalaryCertificateAr.ReportName] = () => new rptSalaryCertificateAr(),

            // EmployeeInfo -- employee-lifecycle documents. Three more reports in that legacy folder
            // (Absence / Leave / ReturnFromLeave request forms) are NOT here and cannot be ported yet:
            // they read the self-service request entities (LeaveRequest, AbsenceRequest,
            // ReturnFromLeaveRequest), which do not exist in HCMv3. Same missing module that already
            // blocks three Transaction grid reports and the Workflow LeaveRequest report.
            [rptTerminationLetter.ReportName] = () => new rptTerminationLetter(),
            [rptJoiningForm.ReportName] = () => new rptJoiningForm(),
            [rptFinalClearanceCertificate.ReportName] = () => new rptFinalClearanceCertificate(),
            [rptEmploymentNotice.ReportName] = () => new rptEmploymentNotice(),
            [rptJobOffer.ReportName] = () => new rptJobOffer(),
            [rptProbation_Letter.ReportName] = () => new rptProbation_Letter(),
            [rptEmpStatusChange.ReportName] = () => new rptEmpStatusChange(),
            [rptSettlementAcknowledgement_DischargeForm.ReportName] = () => new rptSettlementAcknowledgement_DischargeForm(),
            [rptFinalSettlement.ReportName] = () => new rptFinalSettlement(),
            [rptFinalSettlementAr.ReportName] = () => new rptFinalSettlementAr()
        };

    /// <summary>Report names that can actually be constructed.</summary>
    public static IReadOnlyCollection<string> ReportNames => Factories.Keys.ToList();

    public static bool Contains(string reportName) =>
        !string.IsNullOrWhiteSpace(reportName) && Factories.ContainsKey(reportName);

    public static bool TryCreate(string reportName, out XtraReport report)
    {
        report = null;

        if (string.IsNullOrWhiteSpace(reportName) || !Factories.TryGetValue(reportName, out var factory))
        {
            return false;
        }

        report = factory();
        return true;
    }

    /// <summary>
    /// Publishes the ported report names to <see cref="PortedReportCatalog"/> so layers that cannot
    /// reference HCMv3.Reports (notably HCMv3.Application) can still answer "is this runnable?".
    /// Called once from Web.Host start-up.
    /// </summary>
    public static void PublishToPortedReportCatalog() =>
        PortedReportCatalog.SetPortedReportNames(Factories.Keys);
}
