using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.XtraReports.UI;
using HCMv3.ReportManagement;
using HCMv3.Reports.Bank;
using HCMv3.Reports.EmployeeInfo;
using HCMv3.Reports.GovtForms;
using HCMv3.Reports.Others;
using HCMv3.Reports.Payroll;
using HCMv3.Reports.QuotaProvision;
using HCMv3.Reports.Status;
using HCMv3.Reports.Time;
using HCMv3.Reports.Transaction;

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
            [rptFinalSettlementAr.ReportName] = () => new rptFinalSettlementAr(),

            // GovtForms -- Kuwait ministry / Shoun forms. Each prints onto a scanned government form
            // image, so the layout is fixed by the scan and only the overlaid fields differ.
            [rptAbsenceReportCancellation.ReportName] = () => new rptAbsenceReportCancellation(),
            [rptAbsenceReportWaiver.ReportName] = () => new rptAbsenceReportWaiver(),
            [rptAbsenceWaiverAuthorization.ReportName] = () => new rptAbsenceWaiverAuthorization(),
            [rptAbsenceWaiverRequest.ReportName] = () => new rptAbsenceWaiverRequest(),
            [rptAcknowledgmentAndPledge.ReportName] = () => new rptAcknowledgmentAndPledge(),
            [rptBusinessVisitVisaExtensionPledge.ReportName] = () => new rptBusinessVisitVisaExtensionPledge(),
            [rptCriminalRecordCertificateDeliveryCertificate.ReportName] = () => new rptCriminalRecordCertificateDeliveryCertificate(),
            [rptDeclarationFinRights.ReportName] = () => new rptDeclarationFinRights(),
            [rptEndorsmentForVisitVisa.ReportName] = () => new rptEndorsmentForVisitVisa(),
            [rptGeneralAdministrationOfResidenceAffairs.ReportName] = () => new rptGeneralAdministrationOfResidenceAffairs(),
            [rptPayRequestShounRenewalEn.ReportName] = () => new rptPayRequestShounRenewalEn(),
            [rptPayShounTerminationFormEn.ReportName] = () => new rptPayShounTerminationFormEn(),
            [rptPersonalPhotoSubmissionForAction.ReportName] = () => new rptPersonalPhotoSubmissionForAction(),
            [rptRJRequest.ReportName] = () => new rptRJRequest(),
            [rptVisaApplicationShounEn.ReportName] = () => new rptVisaApplicationShounEn(),
            [rptVisitVisaDeclarationAndPledge.ReportName] = () => new rptVisitVisaDeclarationAndPledge(),
            [rptVisitorSponsorshipComplianceAcknowledgment.ReportName] = () => new rptVisitorSponsorshipComplianceAcknowledgment(),
            [rptWFDrivingLicenseForm.ReportName] = () => new rptWFDrivingLicenseForm(),
            [rptWFEmploymentContract.ReportName] = () => new rptWFEmploymentContract(),
            [rptWFEndOfService.ReportName] = () => new rptWFEndOfService(),
            [rptWFMedicalReport.ReportName] = () => new rptWFMedicalReport(),
            [rptWFNewVisaForm.ReportName] = () => new rptWFNewVisaForm(),
            [rptWFPAManpower.ReportName] = () => new rptWFPAManpower(),
            [rptWFPayRequestShounEn.ReportName] = () => new rptWFPayRequestShounEn(),
            [rptWFVisaRequestForm.ReportName] = () => new rptWFVisaRequestForm(),
            [rptWFshounPhotoApplication.ReportName] = () => new rptWFshounPhotoApplication(),

            // Bank -- salary disbursement files and transfer letters.
            [rptBankAndCashTransfer.ReportName] = () => new rptBankAndCashTransfer(),
            [rptBankAndCashTransferBySponsor.ReportName] = () => new rptBankAndCashTransferBySponsor(),
            [rptBankFile.ReportName] = () => new rptBankFile(),
            [rptBankFileAr.ReportName] = () => new rptBankFileAr(),
            [rptBankSupporting.ReportName] = () => new rptBankSupporting(),
            [rptBankTransfer.ReportName] = () => new rptBankTransfer(),
            [rptBankTransferBySponsor.ReportName] = () => new rptBankTransferBySponsor(),
            [rptCashFile.ReportName] = () => new rptCashFile(),

            // QuotaProvision / Status / Time / Transaction. The srpt* classes in those folders are
            // SUBREPORTS -- instantiated by their parent, never by name -- so they are deliberately
            // not registered here.
            [rptIndemnityBreakdown.ReportName] = () => new rptIndemnityBreakdown(),
            [rptLeaveIndemnityStatement.ReportName] = () => new rptLeaveIndemnityStatement(),
            [rptHireDeparture.ReportName] = () => new rptHireDeparture(),
            [rptEmployeeAttendance.ReportName] = () => new rptEmployeeAttendance(),
            [rptEmployeeOvertime.ReportName] = () => new rptEmployeeOvertime(),
            [rptEmployeeDeduction.ReportName] = () => new rptEmployeeDeduction(),
            [rptEmployeeLeave.ReportName] = () => new rptEmployeeLeave(),
            [rptEmployeeLoan.ReportName] = () => new rptEmployeeLoan(),

            // ---- WAVE TWO ----------------------------------------------------------------
            // Others -- employee letters, certificates and HR forms.
            [rptExperienceCertificate.ReportName] = () => new rptExperienceCertificate(),
            [rptSalaryContinuousCertificate.ReportName] = () => new rptSalaryContinuousCertificate(),
            [rptSalaryContinuousCertificateAr.ReportName] = () => new rptSalaryContinuousCertificateAr(),
            [rptWhomsoeverCertificate.ReportName] = () => new rptWhomsoeverCertificate(),
            [rptEmployeeConfirmationLetter.ReportName] = () => new rptEmployeeConfirmationLetter(),
            [rptSalaryIncrementLetter.ReportName] = () => new rptSalaryIncrementLetter(),
            [rptWarningLetter.ReportName] = () => new rptWarningLetter(),
            [rptClearanceForm.ReportName] = () => new rptClearanceForm(),
            [rptFinalClearance_EOS.ReportName] = () => new rptFinalClearance_EOS(),
            [rptDisciplinaryForm.ReportName] = () => new rptDisciplinaryForm(),
            [rptEmployeeWarningNotice.ReportName] = () => new rptEmployeeWarningNotice(),
            [rptProbationaryReportForm.ReportName] = () => new rptProbationaryReportForm(),
            [rptPayChange.ReportName] = () => new rptPayChange(),
            [rptEmploymentContract.ReportName] = () => new rptEmploymentContract(),
            [rptEmploymentContractAr023.ReportName] = () => new rptEmploymentContractAr023(),
            [rptEmploymentContractEn023.ReportName] = () => new rptEmploymentContractEn023(),

            // Others -- leave. rptAnnualLeaveRequest and rptLeaveForm are absent by design:
            // both are driven by the self-service LeaveRequest entity, which HCMv3 does not
            // have. Same gap that blocks rptTravelDeskReport and three Transaction reports.
            [rptAnnualLeaveSheet.ReportName] = () => new rptAnnualLeaveSheet(),
            [rptSickLeaveForm.ReportName] = () => new rptSickLeaveForm(),
            [rptSickLeaveMedicalForm.ReportName] = () => new rptSickLeaveMedicalForm(),
            [rptMonthlyLeaveAccrual.ReportName] = () => new rptMonthlyLeaveAccrual(),
            [rptMonthlyIndemnityAccrual.ReportName] = () => new rptMonthlyIndemnityAccrual(),

            // Others -- admin / employee data.
            [rptEmployeeProfile.ReportName] = () => new rptEmployeeProfile(),
            [rptEmpWorklocation.ReportName] = () => new rptEmpWorklocation(),
            [rptLocationWiseEmp.ReportName] = () => new rptLocationWiseEmp(),
            [rptHolidayCalendar.ReportName] = () => new rptHolidayCalendar(),
            [rptPassportInOut.ReportName] = () => new rptPassportInOut(),
            [rptPassportRequestForm.ReportName] = () => new rptPassportRequestForm(),
            [rptBusinessTrip.ReportName] = () => new rptBusinessTrip(),

            // Payroll -- payroll sheets. Seven legacy reports that were seven copy-pasted
            // SQL bodies; they now compose one shared set of query fragments.
            [rptPayrollSheet.ReportName] = () => new rptPayrollSheet(),
            [rptPayrollSheetSummary.ReportName] = () => new rptPayrollSheetSummary(),
            [rptPayrollSheetByCostCenter.ReportName] = () => new rptPayrollSheetByCostCenter(),
            [rptPayrollSheetByCostCenterPostingGroup.ReportName] = () => new rptPayrollSheetByCostCenterPostingGroup(),
            [rptPayrollSheetByCostCenterSummary.ReportName] = () => new rptPayrollSheetByCostCenterSummary(),
            [rptPayrollSheetBySponsor.ReportName] = () => new rptPayrollSheetBySponsor(),
            [rptSalaryByBudgetingGroup.ReportName] = () => new rptSalaryByBudgetingGroup(),

            // Payroll -- leave settlement. The five srptLeaveSettlement* subreports are
            // deliberately absent: each is instantiated by its parent's designer and is never
            // resolved by name. srptLeaveSettlementBookingBalance was not ported at all --
            // it exists only to print AbsenceRequest.BookingBalance, which HCMv3 has no
            // column for.
            [rptLeaveEncashment.ReportName] = () => new rptLeaveEncashment(),
            [rptLeaveSettlement.ReportName] = () => new rptLeaveSettlement(),
            [rptLeaveSettlementAr.ReportName] = () => new rptLeaveSettlementAr(),

            // Payroll -- payslip variants. These REUSE the committed srptPaySlip* subreports
            // by instantiation rather than cloning them. rptPayslip_wsp_En is absent: it and
            // its three subreports run stored procedures (usp_rpt_payslip,
            // usp_rpt_payslip_details) whose bodies exist only in the legacy database and
            // are nowhere in the legacy source tree.
            [rptSubordPayslipEn.ReportName] = () => new rptSubordPayslipEn(),
            [rptSubordPayslipAr.ReportName] = () => new rptSubordPayslipAr(),
            [rptSocialSecurity.ReportName] = () => new rptSocialSecurity(),
            [rptPayslip_wosp_En.ReportName] = () => new rptPayslip_wosp_En()
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
