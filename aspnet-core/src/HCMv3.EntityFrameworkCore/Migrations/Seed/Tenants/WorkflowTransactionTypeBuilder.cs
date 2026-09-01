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

        // NOT self-service either, and on the same kind of evidence: legacy HCM10.ESS contains no view,
        // controller or model for an IT service -- only a back-office screen. It is raised on the employee's
        // behalf, so it is routed for approval without ever appearing on a tile list.
        ("EmployeeITService", "IT Service Request", "طلب خدمة تقنية المعلومات", "ITServiceType", null, null, "flaticon-computer", 130)
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
