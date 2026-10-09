using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.Ess;

/// <summary>
/// The fixtures the letter and document draft tests share: an employee, a document type, a one-step chain
/// whose approver may send back, and a way to ask a handler for its detail projection.
/// </summary>
public abstract class EssRequestDraftTestBase : AppTestBase
{
    protected readonly IWorkflowApprovalService Approvals;

    protected EssRequestDraftTestBase()
    {
        LoginAsDefaultTenantAdmin();
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

    protected int CreateDocumentType(string nameEn, bool isLetterType)
    {
        DocumentType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn + " (ar)",
                IsLetterType = isLetterType
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>Turns workflow on for one catalog type, pointing it at a one-step chain with a real approver.</summary>
    protected int EnableWorkflowFor(string typeCode, string approverCode)
    {
        var approverId = CreateEmployee(approverCode);

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
                Code = "DRF-" + Guid.NewGuid().ToString("N").Substring(0, 6),
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

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == typeCode);
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    protected static GetMyRequestsInput ListInput(bool includeHistory = true) =>
        new GetMyRequestsInput { IncludeHistory = includeHistory, MaxResultCount = 50, SkipCount = 0 };

    protected WorkflowRequest RequestRow(int requestId)
    {
        return UsingDbContext(context => context.Set<WorkflowRequest>()
            .IgnoreQueryFilters()
            .Single(r => r.Id == requestId));
    }

    protected bool IsWaitingOn(int approverId, int requestId)
    {
        return UsingDbContext(c => c.Set<WorkFlowApprover>()
            .Any(a => a.WorkflowRequestId == requestId && a.ApproverId == approverId && !a.IsApproved));
    }

    /// <summary>The handler's own projection of a request, read inside a unit of work as the engine reads it.</summary>
    protected async Task<WorkflowRequestDetail> BuildDetailAsync(int requestId, int actingEmployeeId)
    {
        using (var unitOfWork = Resolve<IUnitOfWorkManager>().Begin())
        {
            var request = await Resolve<IRepository<WorkflowRequest>>().GetAsync(requestId);
            var handler = Resolve<IWorkflowTransactionHandlerResolver>().Resolve(request.RequestType);

            var detail = await handler.BuildDetailAsync(new WorkflowTransactionContext(request, actingEmployeeId));

            await unitOfWork.CompleteAsync();

            return detail;
        }
    }

    protected static string Value(WorkflowRequestDetail detail, string name) =>
        detail.Fields.Single(f => f.Name == name).Value;
}
