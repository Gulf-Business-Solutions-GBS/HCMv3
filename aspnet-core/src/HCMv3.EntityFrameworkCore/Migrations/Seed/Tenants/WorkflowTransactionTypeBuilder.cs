using System;
using System.Linq;
using HCMv3.Authorization;
using HCMv3.EntityFrameworkCore;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Tenants;

/// <summary>
/// Registers the transaction types that CAN take part in workflow and Employee Self Service.
///
/// <para>
/// This is the catalog that replaced legacy <c>WorkflowGeneralService.GetRequestMaster</c>'s compiled-in
/// 28-case switch (<c>case "LeaveRequest": recCode = "4";</c>). Registering a type here is what allows the
/// resolver to recognise it at all; it is not what switches workflow on.
/// </para>
///
/// <para>
/// <b>Everything is seeded OFF.</b> <c>IsWorkflowEnabled</c> and <c>IsEssEnabled</c> are both false, and no
/// default approval chain is attached. Seeding them on would immediately start routing live transactions
/// through approval on the next deployment -- changing how leave, assets and loans behave for every tenant
/// without anyone asking for it. An admin turns each one on deliberately, after pointing it at a
/// <c>RequestMaster</c>; <c>WorkflowTransactionTypesAppService</c> refuses to enable a type that has nowhere
/// to route to.
/// </para>
///
/// <para>
/// IDEMPOTENT and non-destructive: keyed on (TenantId, Code), and existing rows are left completely alone.
/// Re-running must never switch a type back off, or undo the chain an admin selected.
/// </para>
/// </summary>
public class WorkflowTransactionTypeBuilder
{
    private readonly HCMv3DbContext _context;
    private readonly int _tenantId;

    /// <summary>
    /// (Code, NameEn, NameAr, SubTypeSource, EssRoute, EssPermissionName, DisplayOrder).
    ///
    /// Code MUST equal the entity's own type name -- it is matched against
    /// <c>WorkflowRequest.RequestType</c> and <c>WorkflowGroupDetail.RequestTypeName</c>.
    ///
    /// The rows here are the entities that implement <c>IWorkflowEnabledEntity</c> today. A further type
    /// needs: the interface on the entity, a row here, and a handler -- no engine change.
    /// </summary>
    private static readonly (string Code, string NameEn, string NameAr, string SubTypeSource, string EssRoute, string EssPermissionName, string IconClass, int DisplayOrder)[] Catalog =
    {
        ("EmployeeAbsence", "Leave Request", "طلب إجازة", "AbsenceType", "/app/main/ess/leave", AppPermissions.Ess_MyLeave, "flaticon-calendar-3", 10),
        ("EmployeeAsset", "Asset Request", "طلب عهدة", null, "/app/main/ess/asset", AppPermissions.Ess_MyAssets, "flaticon-app", 20),
        ("EmployeeDocument", "Document Request", "طلب مستند", "DocumentType", "/app/main/ess/document", AppPermissions.Ess_MyDocuments, "flaticon-notepad", 30),
        ("EmployeeLoan", "Loan Request", "طلب قرض", "LoanType", "/app/main/ess/loan", AppPermissions.Ess_MyLoans, "flaticon-piggy-bank", 40),
        ("EmployeeOvertime", "Overtime Request", "طلب عمل إضافي", null, "/app/main/ess/overtime", AppPermissions.Ess_MyOvertime, "flaticon-alarm", 50),
        ("EmployeeTicket", "Air Ticket Request", "طلب تذكرة", null, "/app/main/ess/ticket", AppPermissions.Ess_MyTickets, "flaticon-airplane", 60),
        ("EmployeeAttendance", "Attendance Correction", "تصحيح الحضور", "AttendanceType", "/app/main/ess/attendance", AppPermissions.Ess_MyAttendance, "flaticon-time-3", 70),
        ("EmployeeBank", "Bank Detail Change", "تغيير بيانات البنك", null, "/app/main/ess/bank", AppPermissions.Ess_MyBank, "flaticon-coins", 80),
        ("TravelFeedback", "Travel Expense Claim", "مطالبة مصاريف السفر", null, "/app/main/ess/travel-claim", AppPermissions.Ess_MyTravelClaims, "flaticon-map", 90),

        // The sub-type is the document type, so withdrawing a passport can require a longer chain than
        // withdrawing a certificate. Note the Code is the transaction entity's name as always -- there is no
        // PassportWithdrawalRequest table here; the movement row IS the request.
        ("EmployeeDocumentMvmt", "Passport Withdrawal", "طلب سحب جواز السفر", "DocumentType", "/app/main/ess/passport-withdrawal", AppPermissions.Ess_MyPassportWithdrawals, "flaticon-suitcase", 95),

        // The permit itself is issued by the government, outside this system. What is routed here is the
        // company's own sign-off on the absence, which is why approving one writes nothing anywhere else.
        ("EmployeeExitPermit", "Exit Permit", "إذن خروج", null, "/app/main/ess/exit-permit", AppPermissions.Ess_MyExitPermits, "flaticon-interface-11", 97),

        // NOT self-service: HR raises a personnel action, never the employee. EssRoute and EssPermissionName
        // are deliberately null, so it can be routed for approval while never appearing on a tile list.
        ("EmployeeAction", "Personnel Action", "إجراء شخصي", "PersonalAction", null, null, "flaticon-users", 100),

        // Also NOT self-service: a manager or HR plans a department's headcount; an employee does not
        // request headcount about themselves. Routed because approval is what mints budgeted vacant
        // positions that recruitment then hires against.
        ("ManpowerPlanning", "Manpower Plan", "خطة القوى العاملة", null, null, null, "flaticon-network", 110),

        // Also not self-service. An appraisal is opened FOR an employee -- by HR for a cycle, or by a
        // manager for their team -- so it is submitted from the admin service. The employee's own part is
        // their self-rating, which is recorded on PAEmployeeReviewer/PAEmployeeRating rather than on the
        // appraisal row that routes. Sub-type is the FORM TYPE, matching legacy: a 360 form is approved by
        // different people from a probation form, whereas the cycle is only a period.
        ("PAEmployee", "Performance Appraisal", "تقييم الأداء", "FormType", null, null, "flaticon-star", 120),

        // Self-service: an employee asks for their own certificate, letter or card replacement.
        ("EmployeeHRService", "HR Service Request", "طلب خدمة موارد بشرية", "HRServiceType", "/app/main/ess/hr-service", AppPermissions.Ess_MyHRServices, "flaticon-notepad", 98),

        // Self-service: a salary certificate for a bank, an experience certificate for an embassy. The
        // sub-type is the DocumentType, because a salary certificate and a to-whom-it-may-concern letter are
        // not necessarily signed off by the same person. Note what approval buys here -- nothing is written
        // anywhere else; it is what unlocks printing the letter.
        ("EmployeeLetterRequest", "Letter Request", "طلب خطاب", "DocumentType", "/app/main/ess/letter", AppPermissions.Ess_MyLetterRequests, "flaticon-envelope", 140),

        // The missing-punch request (legacy SkipPunchRequest). Sub-type is the PUNCH TYPE, so a missing
        // arrival can be routed differently from a missing departure. Note the Code is TimeEvent: what is
        // approved is the clock event itself, and once granted the Time Engine recomputes the day from it --
        // there is no SkipPunchRequest table here.
        ("TimeEvent", "Missing Punch Request", "طلب بصمة مفقودة", "TransType", "/app/main/ess/punch", AppPermissions.Ess_MyPunches, "flaticon-clock", 150),

        // NOT self-service either, and on the same kind of evidence: legacy HCM10.ESS contains no view,
        // controller or model for an IT service -- only a back-office screen. It is raised on the employee's
        // behalf, so it is routed for approval without ever appearing on a tile list.
        ("EmployeeITService", "IT Service Request", "طلب خدمة تقنية المعلومات", "ITServiceType", null, null, "flaticon-computer", 130),

        // NOT self-service, on structural evidence: legacy auto-filled the REPORTER with the logged-in
        // employee while the SUBJECT was picked separately, so the two are never the same person. Nobody
        // raises a disciplinary record about themselves. Sub-type is the offence type, so a safety
        // violation can be routed differently from a late arrival -- and approving one is what decides the
        // penalty, not merely whether the record is filed.
        ("EmployeeDisciplinary", "Disciplinary Record", "إجراء تأديبي", "DisciplinaryType", null, null, "flaticon-warning", 160),

        // Exit clearance. Sub-type is the CLEARANCE TYPE, because a resignation clearance and an
        // end-of-contract one need not be signed by the same departments. The "IT, Finance and HR each sign
        // off" behaviour is nothing more than configuring this type's step as ApprovalRequire.Parallel --
        // there is no bespoke sign-off mechanism, and which checklist lines each approver is shown comes from
        // ClearanceRequestFieldDetail.WorkflowRoleId at render time.
        ("EmployeeClearance", "Exit Clearance", "إخلاء طرف", "ClearanceRequestType", "/app/main/ess/clearance", AppPermissions.Ess_MyClearances, "flaticon-list-3", 170),

        // The shift override (legacy RosterPlannerRequest). NOT self-service: a manager plans who works
        // which shift, and an employee does not request a change to somebody else's roster -- so EssRoute
        // and EssPermissionName are deliberately null, exactly as for EmployeeAction. The Code is the
        // transaction entity's name as always: there is no RosterPlannerRequest header/detail pair here, the
        // override row IS the request. Sub-type is the DUTY, so moving somebody onto nights can take a
        // longer chain than swapping one day shift for another. Note what approval buys -- until it is
        // granted the override is invisible to the roster, so nobody's calculated shift changes.
        ("EmployeeSubstituteDuty", "Shift Override", "تغيير الوردية", "Duty", null, null, "flaticon-calendar", 180),

        // Health reimbursement. Self-service: the employee paid the doctor and is claiming it back. The Code
        // is the transaction entity's name as always -- there is no HealthReimburseRequest table here, the
        // claim row IS the request, and nothing is created on approval. No sub-type: legacy routed every
        // medical claim the same way regardless of what it was for. Note what approval buys -- until it is
        // granted the claim is invisible to payroll AND counts only against what the employee may still
        // claim, never against what they have spent.
        ("EmployeeHealthReimburse", "Health Reimbursement Claim", "مطالبة تعويض طبي", null, "/app/main/ess/health-reimburse", AppPermissions.Ess_MyHealthReimburses, "flaticon-medical", 190),

        // Personal data update (legacy PersonalDataUpdateRequest). The ONE type in this catalog whose Code is
        // not the row that ends up changed: approving it writes to the EMPLOYEE MASTER RECORD, and the
        // proposal lives in its own EmployeePersonalDataChange table rather than on the employee row. That is
        // the deliberate exception to the single-row rule -- Employee is not IWorkflowEnabledEntity, because a
        // WorkflowStatus on it would hide a serving employee from payroll for as long as somebody's phone
        // number edit sat in an inbox. Reuses the existing Ess_MyProfile permission: this screen IS my
        // profile, and a new permission would have to be granted by every tenant before self service worked.
        // No sub-type -- legacy routed every personal data update the same way whatever was being changed.
        ("EmployeePersonalDataChange", "Personal Data Update", "تحديث البيانات الشخصية", null, "/app/main/ess/my-details", AppPermissions.Ess_MyProfile, "flaticon-user", 200),

        // Batch approval of calculated timesheet days (legacy TimeLogRequest). NOT self-service: a manager or
        // HR reviews the time engine's output for their people and proposes corrections -- an employee does
        // not approve their own attendance -- so EssRoute and EssPermissionName are null, as for
        // EmployeeAction. The ONLY type in this catalog with a header/detail pair behind it: what is approved
        // is a rectangle of employees by days, and WorkflowRequest.TargetEntityId addresses a single row, so
        // TimeLogApproval exists to BE that row (see the entity for the full argument). No sub-type: legacy
        // routed every time log request the same way.
        ("TimeLogApproval", "Time Log Approval", "اعتماد سجل الوقت", null, null, null, "flaticon-time-2", 210),

        // Delegation of duties (legacy DelegationRequest). Self-service: the employee going away asks a
        // colleague to cover. The Code is the transaction entity's name as always -- there is no
        // DelegationRequest table here, the delegation row IS the request, and legacy's request existed only
        // to copy itself onto an EmployeeDelegation on approval. No sub-type: legacy routed every delegation
        // the same way. Note what approval buys, and it is unlike every other row in this catalog: the
        // delegation is read BY the approval system itself (notification CC today, approver substitution
        // next), so until it is granted the cover simply does not exist as far as the engine is concerned --
        // which is what makes "asking for cover" and "having cover" two distinct states for the first time.
        ("EmployeeDelegation", "Delegation Request", "طلب تفويض", null, "/app/main/ess/delegation", AppPermissions.Ess_MyDelegations, "flaticon-users", 220),

        // Loan recovery exemption (legacy LoanExemptionRequest): skipping one month's instalment on a loan
        // already being repaid. The Code is the transaction entity's name as always -- there is no
        // LoanExemptionRequest table here, the exemption row IS the request, and legacy's request existed
        // only to copy itself onto an EmployeeLoanExemption on approval. Sub-type is the LOAN TYPE, as for
        // the loan itself, so deferring a month of a housing loan can need a longer chain than deferring a
        // month of a small advance. Note what approval buys, and it is money rather than paperwork: granting
        // an exemption pushes the loan's recovery end date out by the month skipped, so the instalment is
        // deferred rather than forgiven. Until it is granted the row is invisible and the schedule unchanged.
        ("EmployeeLoanExemption", "Loan Exemption Request", "طلب إعفاء من قسط القرض", "LoanType", "/app/main/ess/loan-exemption", AppPermissions.Ess_MyLoanExemptions, "flaticon-piggy-bank", 230),

        // The travel request (legacy TravelDeskRequest). Self-service: the employee asks to make the trip.
        // The Code is the transaction entity's name as always -- there is no TravelDeskRequest header,
        // detail and cost triple here, the Travel row IS the request and its legs and cost lines are created
        // with it, hidden by ApprovedOnly until the chain completes. Sub-type is the TRAVEL TYPE, so an
        // overseas trip can need a longer chain than a local one. Note what approval buys, and it is the
        // reason this type exists at all: TravelFeedback (the travel expense CLAIM, above) points at a
        // Travel row, and until now the only way one could exist was for an administrator to key it in --
        // an employee could claim for a trip nobody could request. Approving this writes nothing anywhere
        // else; the trip becoming visible is the trip being approved.
        ("Travel", "Travel Request", "طلب سفر", "TravelType", "/app/main/ess/travel", AppPermissions.Ess_MyTravel, "flaticon-airplane", 240),

        // Work assigned to an employee (legacy TaskRequest, legacy request code "17"). NOT self-service, and
        // for the plainest of reasons: a task is HANDED to somebody. Legacy keeps the creator (SysCreator)
        // and the subject (Employee) as different people and greys the subject, description and dates out
        // for anyone but the creator, so it was never a thing an employee raised about themselves --
        // EssRoute and EssPermissionName are null exactly as for EmployeeAction and ManpowerPlanning. The
        // Code is the transaction entity's name as always: there is no TaskRequest table here, the
        // assignment row IS the request. No sub-type: legacy routed every task through code "17" whatever
        // the work was. Note what approval buys, and it is nothing beyond visibility -- legacy had NO
        // approval side effect for this type at all, so the row becoming effective is the whole of it.
        ("EmployeeAssignedTask", "Task Assignment", "إسناد مهمة", null, null, null, "flaticon-list-3", 250),

        // The HR payment batch (legacy PaymentRequest). NOT self-service: HR pays a group of people a one-off
        // amount, an employee does not request one about themselves -- so EssRoute and EssPermissionName are
        // deliberately null, exactly as for EmployeeAction and ManpowerPlanning. No sub-type: legacy routed
        // every payment request the same way whatever wage type it paid.
        //
        // The Code is the entity's name as always, but READ THE ENTITY before assuming this is the shadow
        // shape the rest of this catalog exists to have removed. It is not. A batch is a HEADER OVER MANY
        // EMPLOYEES: one approval produces N EmployeePayment rows, one per line, so there is no single
        // pre-existing transaction row for TargetEntityId to point at. EmployeePaymentBatch is the PARENT of
        // EmployeePayment, not a mirror of it -- the same argument as TimeLogApproval above.
        //
        // What approval buys is money: it is the only thing that creates the EmployeePayment rows, and it
        // creates them exactly once. Legacy created them from OnSaving whenever an approved request happened
        // to be saved, so re-saving one paid everybody in it again.
        ("EmployeePaymentBatch", "Payment Batch", "دفعة رواتب", null, null, null, "flaticon-coins", 260)
    };

    public WorkflowTransactionTypeBuilder(HCMv3DbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        var existing = _context.WorkflowTransactionTypes
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == _tenantId)
            .Select(t => t.Code)
            .ToList();

        var added = false;

        foreach (var entry in Catalog)
        {
            if (existing.Any(code => string.Equals(code, entry.Code, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _context.WorkflowTransactionTypes.Add(new WorkflowTransactionType
            {
                // SuppressAutoSetTenantId is on during seeding, so TenantId must be set explicitly.
                TenantId = _tenantId,
                Code = entry.Code,
                NameEn = entry.NameEn,
                NameAr = entry.NameAr,
                SubTypeSource = entry.SubTypeSource,
                EssRoute = entry.EssRoute,
                EssPermissionName = entry.EssPermissionName,
                IconClass = entry.IconClass,
                DisplayOrder = entry.DisplayOrder,

                // Registered, but dormant -- see the class remarks. Turning these on is an admin decision.
                IsWorkflowEnabled = false,
                IsEssEnabled = false,
                DefaultRequestMasterId = null,

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
