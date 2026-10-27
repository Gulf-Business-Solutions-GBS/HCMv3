using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service exit clearance, and the part of it that is easy to get wrong: who sees which checklist
/// lines.
///
/// <para>
/// The premise these tests defend is that there is NO bespoke multi-department sign-off. IT, Finance and HR
/// all hold the record at once because the step is configured <c>ApprovalRequire.Parallel</c> -- ordinary
/// engine behaviour, tested here only to prove the clearance type actually gets it. What is specific to
/// clearance is the narrowing: <c>EmployeeWorkflowRole</c> says which roles an approver holds,
/// <c>ClearanceRequestFieldDetail.WorkflowRoleId</c> says which role owns each line, and the intersection is
/// their part of the form. Finance must not see IT's questions and, more importantly, must not be able to
/// answer them by posting the field id straight at the save method.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyClearanceAppService_Tests : AppTestBase
{
    private readonly IMyClearanceAppService _clearances;
    private readonly IWorkflowApprovalService _approval;

    public MyClearanceAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _clearances = Resolve<IMyClearanceAppService>();
        _approval = Resolve<IWorkflowApprovalService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- fixtures

    private int CreateEmployee(string code, long? userId = null)
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

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    /// <summary>
    /// Hands the signed-in user's identity to another employee, so an approver-facing call can be driven
    /// through the real application service rather than around it. Only one employee may hold the link at a
    /// time, which is exactly what makes it a faithful stand-in for signing in as somebody else.
    /// </summary>
    private void ActAs(int employeeId)
    {
        var userId = AbpSession.UserId;

        UsingDbContext(context =>
        {
            foreach (var employee in context.Set<Employee>().IgnoreQueryFilters()
                         .Where(e => e.UserId == userId).ToList())
            {
                employee.UserId = null;
            }

            context.SaveChanges();

            var target = context.Set<Employee>().IgnoreQueryFilters().Single(e => e.Id == employeeId);
            target.UserId = userId;
        });
    }

    private int CreateWorkflowRole(string name)
    {
        var roleId = 0;

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = name,
                NameAr = name
            }).Entity;

            context.SaveChanges();
            roleId = role.Id;
        });

        return roleId;
    }

    private void GiveRole(int employeeId, int roleId)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                WorkflowRoleId = roleId,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });
        });
    }

    private int CreateClearanceType(string name, bool includeInWorkflow = true)
    {
        var typeId = 0;

        UsingDbContext(context =>
        {
            var type = context.Set<ClearanceRequestType>().Add(new ClearanceRequestType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = name,
                NameAr = name,
                IncludeInWorkflow = includeInWorkflow
            }).Entity;

            context.SaveChanges();
            typeId = type.Id;
        });

        return typeId;
    }

    /// <summary>A checklist line, and the department (role) that owns it.</summary>
    private int CreateChecklistLine(int clearanceTypeId, string name, int ownerRoleId)
    {
        var fieldId = 0;

        UsingDbContext(context =>
        {
            var field = context.Set<ClearanceRequestField>().Add(new ClearanceRequestField
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = name,
                NameAr = name,
                ClearanceRequestTypeId = clearanceTypeId,
                IsActive = true
            }).Entity;

            context.SaveChanges();
            fieldId = field.Id;

            context.Set<ClearanceRequestFieldDetail>().Add(new ClearanceRequestFieldDetail
            {
                TenantId = TenantId,
                ClearanceRequestFieldId = fieldId,
                WorkflowRoleId = ownerRoleId
            });
        });

        return fieldId;
    }

    /// <summary>
    /// Turns workflow on for the clearance type, pointing it at a one-step chain whose step is configured
    /// <paramref name="approvalRequire"/> and resolved from <paramref name="stepRoleId"/> -- so every
    /// current holder of that role becomes an approver of the same step.
    /// </summary>
    private void EnableClearanceWorkflow(int stepRoleId, ApprovalRequire approvalRequire = ApprovalRequire.Parallel)
    {
        UsingDbContext(context =>
        {
            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "CLR-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
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
                WorkflowRoleId = stepRoleId,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = approvalRequire,
                CanReject = true,
                CanSendBack = true
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeClearance");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private static RequestClearanceInput ClearanceInput(int typeId, DateTime start, DateTime end) =>
        new RequestClearanceInput
        {
            ClearanceRequestTypeId = typeId,
            StartDate = start,
            EndDate = end,
            Comments = "Leaving at the end of the month"
        };

    private EmployeeClearance ClearanceRow(int id) =>
        UsingDbContext(c => c.Set<EmployeeClearance>().IgnoreQueryFilters().Single(x => x.Id == id));

    private WorkflowRequest RequestRow(int id) =>
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == id));

    /// <summary>
    /// The full scene: a leaver, an IT approver and a Finance approver who both hold the step's role, one
    /// checklist line owned by each department, and a parallel one-step chain.
    /// </summary>
    private sealed class Scene
    {
        public int LeaverId;
        public int ItApproverId;
        public int FinanceApproverId;
        public int ClearanceTypeId;
        public int ItLineId;
        public int FinanceLineId;
    }

    private Scene BuildScene(string prefix, ApprovalRequire approvalRequire = ApprovalRequire.Parallel)
    {
        var scene = new Scene
        {
            LeaverId = CreateCurrentUserEmployee(prefix + "LEAVER"),
            ItApproverId = CreateEmployee(prefix + "IT"),
            FinanceApproverId = CreateEmployee(prefix + "FIN")
        };

        // One role puts both of them on the step -- that is all "parallel" needs. Their DEPARTMENT roles are
        // separate, and are what decide which questions each of them is asked.
        var stepRoleId = CreateWorkflowRole("Clearance step");
        var itRoleId = CreateWorkflowRole("IT");
        var financeRoleId = CreateWorkflowRole("Finance");

        GiveRole(scene.ItApproverId, stepRoleId);
        GiveRole(scene.ItApproverId, itRoleId);
        GiveRole(scene.FinanceApproverId, stepRoleId);
        GiveRole(scene.FinanceApproverId, financeRoleId);

        scene.ClearanceTypeId = CreateClearanceType("Resignation");
        scene.ItLineId = CreateChecklistLine(scene.ClearanceTypeId, "Laptop returned", itRoleId);
        scene.FinanceLineId = CreateChecklistLine(scene.ClearanceTypeId, "Final settlement calculated", financeRoleId);

        EnableClearanceWorkflow(stepRoleId, approvalRequire);

        return scene;
    }

    // ---------------------------------------------------------------- the checklist narrowing

    [Fact]
    public async Task An_Approver_Should_See_Only_The_Checklist_Lines_Their_Own_Department_Owns()
    {
        // THE test. Both approvers are on the same parallel step and both are looking at the same clearance;
        // what differs is only the roles they hold, and that alone must decide what each is asked.
        var scene = BuildScene("CLR100");

        var submission = await _clearances.RequestClearance(
            ClearanceInput(scene.ClearanceTypeId, DateTime.Today, DateTime.Today.AddDays(14)));

        ActAs(scene.ItApproverId);
        var itChecklist = await _clearances.GetClearanceChecklist(new Abp.Application.Services.Dto.EntityDto(submission.TransactionId));

        itChecklist.Lines.Select(l => l.ClearanceRequestFieldId).ShouldContain(scene.ItLineId);
        itChecklist.Lines.Select(l => l.ClearanceRequestFieldId).ShouldNotContain(scene.FinanceLineId,
            "IT was shown a line that Finance owns.");

        ActAs(scene.FinanceApproverId);
        var financeChecklist = await _clearances.GetClearanceChecklist(new Abp.Application.Services.Dto.EntityDto(submission.TransactionId));

        financeChecklist.Lines.Select(l => l.ClearanceRequestFieldId).ShouldContain(scene.FinanceLineId);
        financeChecklist.Lines.Select(l => l.ClearanceRequestFieldId).ShouldNotContain(scene.ItLineId,
            "Finance was shown a line that IT owns.");
    }

    [Fact]
    public async Task An_Approver_Who_Holds_No_Owning_Role_Should_Be_Shown_Nothing()
    {
        // On the step for another reason entirely -- a coordinator, say. An empty checklist is the correct
        // answer; showing them everything "because they are an approver" is how IT's questions end up
        // answered by somebody who never saw the laptop.
        var scene = BuildScene("CLR110");

        var bystanderId = CreateEmployee("CLR110BY");

        var submission = await _clearances.RequestClearance(
            ClearanceInput(scene.ClearanceTypeId, DateTime.Today, DateTime.Today.AddDays(7)));

        ActAs(bystanderId);
        var checklist = await _clearances.GetClearanceChecklist(new Abp.Application.Services.Dto.EntityDto(submission.TransactionId));

        checklist.Lines.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------- saving answers

    [Fact]
    public async Task Answers_Should_Save_While_The_Clearance_Sits_On_A_Parallel_Step()
    {
        var scene = BuildScene("CLR200");

        var submission = await _clearances.RequestClearance(
            ClearanceInput(scene.ClearanceTypeId, DateTime.Today, DateTime.Today.AddDays(10)));

        ActAs(scene.ItApproverId);

        await _clearances.SaveClearanceChecklist(new SaveClearanceChecklistInput
        {
            EmployeeClearanceId = submission.TransactionId,
            Answers = new List<ClearanceChecklistAnswerInput>
            {
                new ClearanceChecklistAnswerInput { ClearanceRequestFieldId = scene.ItLineId, Value = "Returned, asset tag 4471" }
            }
        });

        var saved = UsingDbContext(c => c.Set<EmployeeClearanceDetail>()
            .Single(d => d.EmployeeClearanceId == submission.TransactionId));

        saved.ClearanceRequestFieldId.ShouldBe(scene.ItLineId);
        saved.Value.ShouldBe("Returned, asset tag 4471");
        saved.ApproverId.ShouldBe(scene.ItApproverId, "The answer must record who gave it.");

        // And it comes back merged into the form, so an approver returning to it sees their own answer.
        var checklist = await _clearances.GetClearanceChecklist(new Abp.Application.Services.Dto.EntityDto(submission.TransactionId));
        checklist.Lines.Single(l => l.ClearanceRequestFieldId == scene.ItLineId).Value
            .ShouldBe("Returned, asset tag 4471");
        checklist.CanRecordAnswers.ShouldBeTrue();
    }

    [Fact]
    public async Task Answering_The_Same_Line_Twice_Should_Correct_It_Rather_Than_Duplicate_It()
    {
        // A department correcting its own answer before the step closes is ordinary; two rows for one line
        // would make "what did IT say" a question with two answers.
        var scene = BuildScene("CLR210");

        var submission = await _clearances.RequestClearance(
            ClearanceInput(scene.ClearanceTypeId, DateTime.Today, DateTime.Today.AddDays(10)));

        ActAs(scene.ItApproverId);

        await _clearances.SaveClearanceChecklist(new SaveClearanceChecklistInput
        {
            EmployeeClearanceId = submission.TransactionId,
            Answers = new List<ClearanceChecklistAnswerInput>
            {
                new ClearanceChecklistAnswerInput { ClearanceRequestFieldId = scene.ItLineId, Value = "Pending" }
            }
        });

        await _clearances.SaveClearanceChecklist(new SaveClearanceChecklistInput
        {
            EmployeeClearanceId = submission.TransactionId,
            Answers = new List<ClearanceChecklistAnswerInput>
            {
                new ClearanceChecklistAnswerInput { ClearanceRequestFieldId = scene.ItLineId, Value = "Returned" }
            }
        });

        var rows = UsingDbContext(c => c.Set<EmployeeClearanceDetail>()
            .Where(d => d.EmployeeClearanceId == submission.TransactionId).ToList());

        rows.Count.ShouldBe(1);
        rows[0].Value.ShouldBe("Returned");
    }

    [Fact]
    public async Task An_Approver_Should_Not_Be_Able_To_Answer_Another_Departments_Line()
    {
        // The screen hiding the line is a convenience. This is the rule -- the field id posted straight at
        // the save method is refused because the roles, not the request, decide what is answerable.
        var scene = BuildScene("CLR220");

        var submission = await _clearances.RequestClearance(
            ClearanceInput(scene.ClearanceTypeId, DateTime.Today, DateTime.Today.AddDays(10)));

        ActAs(scene.FinanceApproverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _clearances.SaveClearanceChecklist(new SaveClearanceChecklistInput
            {
                EmployeeClearanceId = submission.TransactionId,
                Answers = new List<ClearanceChecklistAnswerInput>
                {
                    new ClearanceChecklistAnswerInput { ClearanceRequestFieldId = scene.ItLineId, Value = "Returned" }
                }
            }));

        UsingDbContext(c => c.Set<EmployeeClearanceDetail>()
            .Any(d => d.EmployeeClearanceId == submission.TransactionId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Answers_Should_Be_Refused_When_The_Current_Step_Is_Not_Parallel()
    {
        // Same data, one setting different. A single-approver step is not a multi-department sign-off, so
        // there is nobody the checklist is being divided between.
        var scene = BuildScene("CLR230", ApprovalRequire.Single);

        var submission = await _clearances.RequestClearance(
            ClearanceInput(scene.ClearanceTypeId, DateTime.Today, DateTime.Today.AddDays(10)));

        ActAs(scene.ItApproverId);

        var checklist = await _clearances.GetClearanceChecklist(new Abp.Application.Services.Dto.EntityDto(submission.TransactionId));
        checklist.CanRecordAnswers.ShouldBeFalse();

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _clearances.SaveClearanceChecklist(new SaveClearanceChecklistInput
            {
                EmployeeClearanceId = submission.TransactionId,
                Answers = new List<ClearanceChecklistAnswerInput>
                {
                    new ClearanceChecklistAnswerInput { ClearanceRequestFieldId = scene.ItLineId, Value = "Returned" }
                }
            }));
    }

    // ---------------------------------------------------------------- the parallel step itself

    [Fact]
    public async Task The_Step_Should_Not_Complete_Until_Every_Parallel_Approver_Has_Acted()
    {
        // Ordinary engine behaviour, asserted here because it is the entire mechanism behind "IT, Finance
        // and HR each sign off". If it did not hold, the clearance would be approved the moment the fastest
        // department clicked, and the others would never be asked.
        var scene = BuildScene("CLR300");

        var submission = await _clearances.RequestClearance(
            ClearanceInput(scene.ClearanceTypeId, DateTime.Today, DateTime.Today.AddDays(30)));

        var requestId = submission.WorkflowRequestId.Value;

        var approvers = UsingDbContext(c => c.Set<WorkFlowApprover>()
            .Where(a => a.WorkflowRequestId == requestId).Select(a => a.ApproverId).ToList());

        approvers.ShouldContain(scene.ItApproverId);
        approvers.ShouldContain(scene.FinanceApproverId);

        await _approval.ApproveAsync(requestId, scene.ItApproverId, remark: "Laptop back.");

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Open,
            "The step closed on one department's approval; the others were never asked.");
        ClearanceRow(submission.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Open);

        await _approval.ApproveAsync(requestId, scene.FinanceApproverId, remark: "Settled.");

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Approved);
        ClearanceRow(submission.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    // ---------------------------------------------------------------- the handler's rules

    [Fact]
    public async Task A_Clearance_That_Ends_Before_It_Starts_Should_Be_Refused()
    {
        var scene = BuildScene("CLR400");

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _clearances.RequestClearance(
                ClearanceInput(scene.ClearanceTypeId, DateTime.Today.AddDays(10), DateTime.Today.AddDays(3))));
    }

    [Fact]
    public void The_Handler_Should_Refuse_An_End_Date_Before_The_Start_Whoever_Raised_It()
    {
        // Driven through the handler, because the service-level check above only guards the ESS form. The
        // rule has to hold for an HR-entered row later submitted for approval, or for an import.
        var leaverId = CreateCurrentUserEmployee("CLR410");
        var typeId = CreateClearanceType("Termination");

        var clearanceId = 0;
        var requestId = 0;

        UsingDbContext(context =>
        {
            var clearance = context.Set<EmployeeClearance>().Add(new EmployeeClearance
            {
                TenantId = TenantId,
                EmployeeId = leaverId,
                ClearanceRequestTypeId = typeId,
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(2),
                WorkflowStatus = RequestStatus.Draft
            }).Entity;

            context.SaveChanges();
            clearanceId = clearance.Id;

            var request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = leaverId,
                RequestType = HCMv3.PersonnelAdministration.Workflow.ClearanceWorkflowTransactionHandler.ClearanceRequestType,
                TargetEntityId = clearanceId,
                RequestNumber = "CLR-TEST-00001",
                RequestStatus = RequestStatus.Draft,
                WorkflowStep = 0
            }).Entity;

            context.SaveChanges();
            requestId = request.Id;
        });

        var handler = Resolve<IWorkflowTransactionHandlerResolver>()
            .Resolve(HCMv3.PersonnelAdministration.Workflow.ClearanceWorkflowTransactionHandler.ClearanceRequestType);

        handler.ShouldNotBeNull("The clearance type has no handler, so it would route with no validation.");

        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (var uow = unitOfWorkManager.Begin())
        {
            var request = Resolve<IRepository<WorkflowRequest>>().Get(requestId);
            var context = new WorkflowTransactionContext(request, leaverId);

            Should.Throw<UserFriendlyException>(() => handler.ValidateBeforeSubmitAsync(context));

            uow.Complete();
        }
    }

    // ---------------------------------------------------------------- the shared contract

    [Fact]
    public async Task A_Submitted_Clearance_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        // The single-table design's premise: the pending row lives in the real table, so it must be the
        // ApprovedOnly filter -- not a separate request table -- that keeps it out of everything else.
        var scene = BuildScene("CLR500");

        var submission = await _clearances.RequestClearance(
            ClearanceInput(scene.ClearanceTypeId, DateTime.Today, DateTime.Today.AddDays(20)));

        submission.SubmittedForApproval.ShouldBeTrue();

        var repository = Resolve<IRepository<EmployeeClearance>>();
        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            repository.GetAll().Any(c => c.Id == submission.TransactionId)
                .ShouldBeFalse("A pending clearance was visible to the rest of the system.");
        }

        // But the leaver sees their own.
        var mine = await _clearances.GetMyClearances(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(submission.TransactionId);

        // ...and it becomes visible once every department has signed.
        await _approval.ApproveAsync(submission.WorkflowRequestId.Value, scene.ItApproverId, "ok");
        await _approval.ApproveAsync(submission.WorkflowRequestId.Value, scene.FinanceApproverId, "ok");

        using (unitOfWorkManager.Begin())
        {
            repository.GetAll().Any(c => c.Id == submission.TransactionId)
                .ShouldBeTrue("An approved clearance was still hidden.");
        }
    }

    [Fact]
    public async Task The_Type_Dropdown_Should_Offer_Only_The_Types_Flagged_For_Workflow()
    {
        CreateCurrentUserEmployee("CLR600");

        var offered = CreateClearanceType("Resignation", includeInWorkflow: true);
        var notOffered = CreateClearanceType("Historical import", includeInWorkflow: false);

        var types = await _clearances.GetClearanceTypes();

        types.Select(t => t.Id).ShouldContain(offered);
        types.Select(t => t.Id).ShouldNotContain(notOffered);
    }

    [Fact]
    public async Task A_Type_Not_Flagged_For_Workflow_Should_Be_Refused_On_The_Write_Too()
    {
        // A list that filters paired with a save that does not is how a stale form raises a clearance
        // nobody can approve.
        CreateCurrentUserEmployee("CLR610");

        var notOffered = CreateClearanceType("Historical import", includeInWorkflow: false);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _clearances.RequestClearance(ClearanceInput(notOffered, DateTime.Today, DateTime.Today.AddDays(5))));
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked, so this is the state the shell must handle, not an edge case.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _clearances.GetMyClearances(ListInput()));
    }
}
