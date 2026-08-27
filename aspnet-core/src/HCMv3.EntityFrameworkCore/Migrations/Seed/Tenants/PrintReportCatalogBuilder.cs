using System;
using System.Linq;
using HCMv3.EntityFrameworkCore;
using HCMv3.ReportManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Tenants;

/// <summary>
/// Seeds the <see cref="PrintReport"/> catalog -- the rows the "Print Reports" launcher lists.
///
/// Only CATALOG ROWS are seeded. Whether a row is actually runnable is computed at request time from
/// <see cref="PortedReportCatalog"/> (the ported report classes in HCMv3.Reports), so seeding a row
/// for a legacy report that has not been ported yet is safe and deliberate: it shows up in the list
/// as "not yet available" rather than disappearing, which is what a migration status screen wants.
///
/// IDEMPOTENT: keyed on (TenantId, ReportName). Existing rows are left completely alone -- a tenant
/// may legitimately have renamed or deactivated a seeded row and re-running the seeder must not undo
/// that.
/// </summary>
public class PrintReportCatalogBuilder
{
    private readonly HCMv3DbContext _context;
    private readonly int _tenantId;

    /// <summary>
    /// (Code, ReportName, NameEn, NameAr, Category). ReportName must match the report class's own
    /// <c>ReportName</c> constant for rows that are ported -- it is what gets passed to /DXXRD and
    /// /DXXRDV as <c>reportUrl</c>.
    /// </summary>
    private static readonly (string Code, string ReportName, string NameEn, string NameAr, ReportCategory Category)[] Catalog =
    {
        // PORTED -- HCMv3.Reports.Payroll.rptPayslipEn.ReportName == "PayslipEn".
        // The literal is repeated here rather than referenced because HCMv3.EntityFrameworkCore does
        // not (and should not) reference the reports assembly.
        ("PR001", "PayslipEn", "Pay Slip English", "وصل الدفع - إنجليزي", ReportCategory.Default),

        // NOT YET PORTED -- catalog rows only, so the launcher can demonstrate the
        // "listed but not available" state. Do NOT add a report class for these here; add the class
        // to HCMv3.Reports and register it in HcmReportFactoryRegistry, and this row lights up.
        ("PR002", "SalaryCertificateEn", "Salary Certificate English", "شهادة راتب - إنجليزي", ReportCategory.Default),
        ("PR003", "SalaryCertificateAr", "Salary Certificate Arabic", "شهادة راتب - عربي", ReportCategory.Default),
        ("PR004", "PayslipAr", "Pay Slip Arabic", "وصل الدفع - عربي", ReportCategory.Default),

        // EmployeeInfo -- employee-lifecycle documents.
        ("PR010", "TerminationLetter", "Termination Letter", "خطاب إنهاء خدمة", ReportCategory.Default),
        ("PR011", "JoiningForm", "Joining Form", "نموذج مباشرة العمل", ReportCategory.Default),
        ("PR012", "FinalClearanceCertificate", "Final Clearance Certificate", "شهادة إخلاء طرف", ReportCategory.Default),
        ("PR013", "EmploymentNotice", "Employment Notice", "إشعار توظيف", ReportCategory.Default),
        ("PR014", "JobOffer", "Job Offer", "عرض عمل", ReportCategory.Default),
        ("PR015", "ProbationLetter", "Probation Letter", "خطاب فترة التجربة", ReportCategory.Default),
        ("PR016", "EmpStatusChange", "Employee Status Change", "تغيير حالة الموظف", ReportCategory.Default),
        ("PR017", "SettlementAcknowledgementDischargeForm", "Settlement Acknowledgement / Discharge Form", "إقرار تسوية وإخلاء طرف", ReportCategory.Default),
        ("PR018", "FinalSettlement", "Final Settlement", "التسوية النهائية", ReportCategory.Default),
        ("PR019", "FinalSettlementAr", "Final Settlement Arabic", "التسوية النهائية - عربي", ReportCategory.Default),

        // GovtForms -- Kuwait ministry / Shoun forms.
        ("PR100", "GovtAbsenceReportCancellation", "Absence Report Cancellation", "إلغاء تقرير غياب", ReportCategory.Default),
        ("PR101", "GovtAbsenceReportWaiver", "Absence Report Waiver", "تنازل عن تقرير غياب", ReportCategory.Default),
        ("PR102", "GovtAbsenceWaiverAuthorization", "Absence Waiver Authorization", "تفويض تنازل عن الغياب", ReportCategory.Default),
        ("PR103", "GovtAbsenceWaiverRequest", "Absence Waiver Request", "طلب تنازل عن الغياب", ReportCategory.Default),
        ("PR104", "GovtAcknowledgmentAndPledge", "Acknowledgment and Pledge", "إقرار وتعهد", ReportCategory.Default),
        ("PR105", "GovtBusinessVisitVisaExtensionPledge", "Business Visit Visa Extension Pledge", "تعهد تمديد تأشيرة زيارة عمل", ReportCategory.Default),
        ("PR106", "GovtCriminalRecordCertificateDelivery", "Criminal Record Certificate Delivery", "تسليم شهادة السجل الجنائي", ReportCategory.Default),
        ("PR107", "GovtDeclarationFinRights", "Declaration of Financial Rights", "إقرار بالحقوق المالية", ReportCategory.Default),
        ("PR108", "GovtEndorsementForVisitVisa", "Endorsement for Visit Visa", "تصديق تأشيرة زيارة", ReportCategory.Default),
        ("PR109", "GovtGeneralAdminResidenceAffairs", "General Administration of Residence Affairs", "الإدارة العامة لشؤون الإقامة", ReportCategory.Default),
        ("PR110", "GovtPayRequestShounRenewalEn", "Pay Request - Shoun Renewal", "طلب دفع - تجديد الشؤون", ReportCategory.Default),
        ("PR111", "GovtPayShounTerminationFormEn", "Pay Request - Shoun Termination", "طلب دفع - إنهاء الشؤون", ReportCategory.Default),
        ("PR112", "GovtPersonalPhotoSubmission", "Personal Photo Submission", "تقديم صورة شخصية", ReportCategory.Default),
        ("PR113", "GovtRJRequest", "RJ Request", "طلب RJ", ReportCategory.Default),
        ("PR114", "GovtVisaApplicationShounEn", "Visa Application - Shoun", "طلب تأشيرة - الشؤون", ReportCategory.Default),
        ("PR115", "GovtVisitVisaDeclarationAndPledge", "Visit Visa Declaration and Pledge", "إقرار وتعهد تأشيرة زيارة", ReportCategory.Default),
        ("PR116", "GovtVisitorSponsorshipComplianceAcknowledgment", "Visitor Sponsorship Compliance Acknowledgment", "إقرار الالتزام بكفالة الزائر", ReportCategory.Default),
        ("PR117", "GovtDrivingLicenseForm", "Driving License Form", "نموذج رخصة قيادة", ReportCategory.Default),
        ("PR118", "GovtEmploymentContract", "Employment Contract", "عقد عمل", ReportCategory.Default),
        ("PR119", "GovtEndOfService", "End of Service", "نهاية الخدمة", ReportCategory.Default),
        ("PR120", "GovtMedicalReport", "Medical Report", "تقرير طبي", ReportCategory.Default),
        ("PR121", "GovtNewVisaForm", "New Visa Form", "نموذج تأشيرة جديدة", ReportCategory.Default),
        ("PR122", "GovtPAManpower", "PA Manpower", "القوى العاملة", ReportCategory.Default),
        ("PR123", "GovtPayRequestShounEn", "Pay Request - Shoun", "طلب دفع - الشؤون", ReportCategory.Default),
        ("PR124", "GovtVisaRequestForm", "Visa Request Form", "نموذج طلب تأشيرة", ReportCategory.Default),
        ("PR125", "GovtShounPhotoApplication", "Shoun Photo Application", "طلب صورة الشؤون", ReportCategory.Default),

        // Bank -- salary disbursement files and transfer letters.
        ("PR200", "BankAndCashTransfer", "Bank and Cash Transfer", "تحويل بنكي ونقدي", ReportCategory.Default),
        ("PR201", "BankAndCashTransferBySponsor", "Bank and Cash Transfer by Sponsor", "تحويل بنكي ونقدي حسب الكفيل", ReportCategory.Default),
        ("PR202", "BankFile", "Bank File", "ملف البنك", ReportCategory.Default),
        ("PR203", "BankFileAr", "Bank File Arabic", "ملف البنك - عربي", ReportCategory.Default),
        ("PR204", "BankSupporting", "Bank Supporting", "مستند بنكي مساند", ReportCategory.Default),
        ("PR205", "BankTransfer", "Bank Transfer", "تحويل بنكي", ReportCategory.Default),
        ("PR206", "BankTransferBySponsor", "Bank Transfer by Sponsor", "تحويل بنكي حسب الكفيل", ReportCategory.Default),
        ("PR207", "CashFile", "Cash File", "ملف النقد", ReportCategory.Default),

        // QuotaProvision / Status / Time / Transaction.
        ("PR300", "IndemnityBreakdown", "Indemnity Breakdown", "تفصيل مكافأة نهاية الخدمة", ReportCategory.Default),
        ("PR301", "LeaveIndemnityStatement", "Leave and Indemnity Statement", "كشف الإجازات والمكافأة", ReportCategory.Default),
        ("PR302", "HireDeparture", "Hire and Departure", "التعيين والمغادرة", ReportCategory.Default),
        ("PR303", "EmployeeAttendance", "Employee Attendance", "حضور الموظف", ReportCategory.Default),
        ("PR304", "EmployeeOvertime", "Employee Overtime", "العمل الإضافي للموظف", ReportCategory.Default),
        ("PR305", "EmployeeDeduction", "Employee Deduction", "استقطاعات الموظف", ReportCategory.Default),
        ("PR306", "EmployeeLeave", "Employee Leave", "إجازات الموظف", ReportCategory.Default),
        ("PR307", "EmployeeLoan", "Employee Loan", "سلف الموظف", ReportCategory.Default)
    };

    public PrintReportCatalogBuilder(HCMv3DbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        var existing = _context.PrintReports
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == _tenantId)
            .Select(r => r.ReportName)
            .ToList();

        var added = false;

        foreach (var entry in Catalog)
        {
            if (existing.Any(name => string.Equals(name, entry.ReportName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _context.PrintReports.Add(new PrintReport
            {
                // SuppressAutoSetTenantId is on during seeding, so TenantId must be set explicitly.
                TenantId = _tenantId,
                Code = entry.Code,
                ReportName = entry.ReportName,
                NameEn = entry.NameEn,
                NameAr = entry.NameAr,
                ReportCategory = entry.Category,
                CustomPercentage = 0m,
                ReportCreatedOn = DateTime.Now,
                IsActive = true
            });

            added = true;
        }

        if (added)
        {
            _context.SaveChanges();
        }
    }
}
