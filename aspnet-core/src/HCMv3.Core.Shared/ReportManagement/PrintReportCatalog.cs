using System.Collections.Generic;

namespace HCMv3.ReportManagement;

/// <summary>
/// The standard Print Reports catalog every tenant starts with -- the rows the "Print Reports" launcher lists.
///
/// One list for both places that seed it: the startup seed (every existing tenant) and tenant creation, so the
/// two cannot drift apart. Whether a row is runnable is decided at request time from
/// <see cref="PortedReportCatalog"/>; a row for a report not yet ported shows as "not yet available".
///
/// ReportName must match the report class's own <c>ReportName</c> constant for ported reports -- it is what gets
/// passed to /DXXRD and /DXXRDV as <c>reportUrl</c>. The literals are repeated rather than referenced because
/// this assembly does not (and should not) reference the reports assembly.
/// </summary>
public static class PrintReportCatalog
{
    public static readonly IReadOnlyList<(string Code, string ReportName, string NameEn, string NameAr, ReportCategory Category)> Entries =
        new (string Code, string ReportName, string NameEn, string NameAr, ReportCategory Category)[]
    {
        // PORTED -- HCMv3.Reports.Payroll.rptPayslipEn.ReportName == "PayslipEn".
        // The literal is repeated here rather than referenced because HCMv3.Core.Shared does
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
        ("PR307", "EmployeeLoan", "Employee Loan", "سلف الموظف", ReportCategory.Default),

        // ---- WAVE TWO ------------------------------------------------------------------
        // Others -- employee letters, certificates and HR forms.
        ("PR400", "ExperienceCertificate", "Experience Certificate", "شهادة خبرة", ReportCategory.Default),
        ("PR401", "SalaryContinuousCertificate", "Salary Continuous Certificate", "شهادة استمرارية راتب", ReportCategory.Default),
        ("PR402", "SalaryContinuousCertificateAr", "Salary Continuous Certificate (Arabic)", "شهادة استمرارية راتب - عربي", ReportCategory.Default),
        ("PR403", "WhomsoeverCertificate", "To Whom It May Concern Certificate", "شهادة لمن يهمه الأمر", ReportCategory.Default),
        ("PR404", "EmployeeConfirmationLetter", "Employment Confirmation Letter", "خطاب تثبيت الموظف", ReportCategory.Default),
        ("PR405", "SalaryIncrementLetter", "Salary Increment Letter", "خطاب زيادة الراتب", ReportCategory.Default),
        ("PR406", "WarningLetter", "Warning Letter", "خطاب إنذار", ReportCategory.Default),
        ("PR407", "ClearanceForm", "Clearance Form", "نموذج إخلاء طرف", ReportCategory.Default),
        ("PR408", "FinalClearanceEos", "Final Clearance (End of Service)", "المخالصة النهائية لنهاية الخدمة", ReportCategory.Default),
        ("PR409", "DisciplinaryForm", "Disciplinary Form", "نموذج جزاء", ReportCategory.Default),
        ("PR410", "EmployeeWarningNotice", "Employee Warning Notice", "إشعار إنذار موظف", ReportCategory.Default),
        ("PR411", "ProbationaryReportForm", "Probationary Report Form", "نموذج تقرير فترة التجربة", ReportCategory.Default),
        ("PR412", "PayChange", "Pay Change", "تغيير الراتب", ReportCategory.Default),
        ("PR413", "EmploymentContract", "Employment Contract", "عقد عمل", ReportCategory.Default),
        ("PR414", "EmploymentContractAr023", "Employment Contract (Arabic)", "عقد عمل - عربي", ReportCategory.Default),
        ("PR415", "EmploymentContractEn023", "Employment Contract (English)", "عقد عمل - إنجليزي", ReportCategory.Default),

        // Others -- leave.
        ("PR420", "AnnualLeaveSheet", "Annual Leave Sheet", "كشف الإجازة السنوية", ReportCategory.Default),
        ("PR421", "SickLeaveForm", "Sick Leave Form", "نموذج الإجازة المرضية", ReportCategory.Default),
        ("PR422", "SickLeaveMedicalForm", "Sick Leave Medical Form", "النموذج الطبي للإجازة المرضية", ReportCategory.Default),
        ("PR423", "MonthlyLeaveAccrual", "Monthly Leave Accrual", "استحقاق الإجازة الشهري", ReportCategory.Default),
        ("PR424", "MonthlyIndemnityAccrual", "Monthly Indemnity Accrual", "استحقاق المكافأة الشهري", ReportCategory.Default),

        // Others -- admin / employee data.
        ("PR430", "EmployeeProfile", "Employee Profile", "ملف الموظف", ReportCategory.Default),
        ("PR431", "EmpWorklocation", "Employees by Work Location", "الموظفون حسب موقع العمل", ReportCategory.Default),
        ("PR432", "LocationWiseEmp", "Location Wise Employees", "الموظفون حسب الموقع", ReportCategory.Default),
        ("PR433", "HolidayCalendar", "Holiday Calendar", "تقويم العطلات", ReportCategory.Default),
        ("PR434", "PassportInOut", "Passport In / Out", "تسليم واستلام جواز السفر", ReportCategory.Default),
        ("PR435", "PassportRequestForm", "Passport Request Form", "نموذج طلب جواز السفر", ReportCategory.Default),
        ("PR436", "BusinessTrip", "Business Trip", "رحلة عمل", ReportCategory.Default),

        // Payroll -- payroll sheets.
        ("PR440", "PayrollSheet", "Payroll Sheet", "كشف الرواتب", ReportCategory.Default),
        ("PR441", "PayrollSheetSummary", "Payroll Sheet (Summary)", "كشف الرواتب - ملخص", ReportCategory.Default),
        ("PR442", "PayrollSheetByCostCenter", "Payroll Sheet by Cost Center", "كشف الرواتب حسب مركز التكلفة", ReportCategory.Default),
        ("PR443", "PayrollSheetByCostCenterPostingGroup", "Payroll Sheet by Cost Center and Posting Group", "كشف الرواتب حسب مركز التكلفة ومجموعة الترحيل", ReportCategory.Default),
        ("PR444", "PayrollSheetByCostCenterSummary", "Payroll Sheet by Cost Center (Summary)", "كشف الرواتب حسب مركز التكلفة - ملخص", ReportCategory.Default),
        ("PR445", "PayrollSheetBySponsor", "Payroll Sheet by Sponsor", "كشف الرواتب حسب الكفيل", ReportCategory.Default),
        ("PR446", "SalaryByBudgetingGroup", "Salary by Budgeting Group", "الرواتب حسب مجموعة الموازنة", ReportCategory.Default),

        // Payroll -- leave settlement.
        ("PR450", "LeaveEncashment", "Leave Encashment", "صرف بدل الإجازة", ReportCategory.Default),
        ("PR451", "LeaveSettlementEn", "Leave Settlement", "تسوية الإجازة", ReportCategory.Default),
        ("PR452", "LeaveSettlementAr", "Leave Settlement (Arabic)", "تسوية الإجازة - عربي", ReportCategory.Default),

        // Payroll -- payslip variants.
        ("PR460", "SubordPayslipEn", "Subordinates Payslip", "كشف رواتب المرؤوسين", ReportCategory.Default),
        ("PR461", "SubordPayslipAr", "Subordinates Payslip (Arabic)", "كشف رواتب المرؤوسين - عربي", ReportCategory.Default),
        ("PR462", "SocialSecurity", "PIFSS Social Security Report", "تقرير التأمينات الاجتماعية", ReportCategory.Default),
        ("PR463", "PayslipWithoutSalaryParticularsEn", "Payslip Without Salary Particulars", "كشف الراتب بدون تفاصيل", ReportCategory.Default)
    };
}
