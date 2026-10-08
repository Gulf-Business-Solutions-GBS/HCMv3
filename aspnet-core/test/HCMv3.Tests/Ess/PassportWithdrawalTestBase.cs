using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.Ess;

/// <summary>
/// The fixtures the passport custody, extension and reminder tests share: an employee, a document, a
/// one-step approval chain, and a withdrawal taken to approved.
/// </summary>
public abstract class PassportWithdrawalTestBase : AppTestBase
{
    protected readonly IMyPassportWithdrawalAppService Withdrawals;
    protected readonly IWorkflowApprovalService Approvals;

    protected PassportWithdrawalTestBase()
    {
        LoginAsDefaultTenantAdmin();
        Withdrawals = Resolve<IMyPassportWithdrawalAppService>();
        Approvals = Resolve<IWorkflowApprovalService>();
    }

    protected int TenantId => AbpSession.TenantId ?? 1;

    protected int CreateEmployee(string code, long? userId = null)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    protected int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    protected int CreateDocumentType(string code, string nameEn = "Passport")
    {
        DocumentType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = nameEn,
                NameAr = "جواز سفر"
            }).Entity;
        });

        return type.Id;
    }

    protected int CreateDocument(int employeeId, int documentTypeId, string documentNum = null)
    {
        EmployeeDocument document = null;
        UsingDbContext(context =>
        {
            document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = documentTypeId,
                DocumentNum = documentNum ?? "P" + Guid.NewGuid().ToString("N").Substring(0, 6),
                IssueDate = DateTime.Today.AddYears(-2),
                ExpiryDate = DateTime.Today.AddYears(5)
            }).Entity;
        });

        return document.Id;
    }

    /// <summary>A document of a type with a random code, so the tenant's type restriction does not apply.</summary>
    protected int CreatePassportFor(int employeeId)
    {
        return CreateDocument(employeeId, CreateDocumentType(Guid.NewGuid().ToString("N").Substring(0, 8)));
    }

    /// <summary>A one-step chain with a real approver, and the passport catalog row switched on.</summary>
    protected int EnableWorkflow(string approverCode, long? approverUserId = null)
    {
        var approverId = CreateEmployee(approverCode, approverUserId);

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "PPW-CUS",
                NameEn = "Standard",
                NameAr = "Standard",
                WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = approverId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeDocumentMvmt");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    protected static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    /// <summary>Out ten days ago by default, so a return dated today is neither early nor in the future.</summary>
    protected static RequestPassportWithdrawalInput WithdrawalInput(int documentId, int outInDays = -10, int backInDays = 4) =>
        new RequestPassportWithdrawalInput
        {
            EmployeeDocumentId = documentId,
            WithdrawDate = DateTime.Today.AddDays(outInDays),
            ReturnDate = DateTime.Today.AddDays(backInDays),
            WithdrawalReason = "Visa application"
        };

    protected async Task<MyRequestSubmissionDto> CreateApprovedWithdrawalAsync(
        int documentId, int approverId, int outInDays = -10, int backInDays = 4)
    {
        var result = await Withdrawals.RequestPassportWithdrawal(WithdrawalInput(documentId, outInDays, backInDays));

        await Approvals.ApproveAsync(result.WorkflowRequestId.Value, approverId, "ok");

        return result;
    }

    /// <summary>A movement written straight to the table, as one HR recorded or one already decided.</summary>
    protected int InsertMovement(
        int employeeId,
        int documentId,
        RequestStatus? status,
        int outInDays = -10,
        int backInDays = 4,
        int? guarantorId = null,
        DateTime? actualReturnDate = null,
        DateTime? overdueReminderSentOn = null)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var movement = context.Set<EmployeeDocumentMvmt>().Add(new EmployeeDocumentMvmt
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                EmployeeDocumentId = documentId,
                GuarantorId = guarantorId,
                WithdrawDate = DateTime.Today.AddDays(outInDays),
                ReturnDate = DateTime.Today.AddDays(backInDays),
                ActualReturnDate = actualReturnDate ?? default,
                OverdueReminderSentOn = overdueReminderSentOn,
                WorkflowStatus = status
            }).Entity;

            context.SaveChanges();
            id = movement.Id;
        });

        return id;
    }

    /// <summary>
    /// The row as stored, whatever its state. <c>IgnoreQueryFilters</c> because a superseded movement is
    /// soft-deleted and a pending one is hidden, and these tests assert on both.
    /// </summary>
    protected EmployeeDocumentMvmt MovementRow(int movementId)
    {
        return UsingDbContext(context => context.Set<EmployeeDocumentMvmt>()
            .IgnoreQueryFilters()
            .Single(m => m.Id == movementId));
    }

    protected WorkflowRequest RequestRow(int requestId)
    {
        return UsingDbContext(context => context.Set<WorkflowRequest>()
            .IgnoreQueryFilters()
            .Single(r => r.Id == requestId));
    }
}
