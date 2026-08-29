using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// End-to-end cover for self-service leave -- the point where every piece built for this feature meets:
/// employee resolution, the transaction-type catalog, the approval engine, the leave handler's validation,
/// and the ApprovedOnly filter that keeps a pending request away from payroll.
///
/// These are deliberately integration tests rather than mocks. The interesting failures are in the seams
/// between those parts, and a mocked chain would pass while the real one does not.
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyLeaveAppService_Tests : AppTestBase
{
    private readonly IMyLeaveAppService _myLeaveAppService;

    public MyLeaveAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _myLeaveAppService = Resolve<IMyLeaveAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

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

    private int CreateAbsenceType(string nameEn = "Annual")
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
        });

        return absenceType.Id;
    }

    private void CreateQuota(int employeeId, int absenceTypeId, double accrued, double used = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = DateTime.Today.AddYears(-1),
                EntitlementTo = DateTime.Today.AddYears(1),
                DeductionFrom = DateTime.Today.AddYears(-1),
                DeductionTo = DateTime.Today.AddYears(1),
                QuotaAccrued = accrued,
                QuotaUsed = used
            });
        });
    }

    /// <summary>
    /// Turns leave on for approval: a chain with one step, an approver holding the step's role, and the
    /// catalog row switched on and pointed at that chain. Mirrors what an admin does in the UI.
    /// </summary>
    private int EnableLeaveWorkflowWithApprover(string approverCode)
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
                Code = "LEAVE-STD",
                NameEn = "Standard Leave",
                NameAr = "Standard Leave",
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
                EndDate = DateTime.Today.AddYears(1)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAbsence");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static ApplyForLeaveInput Apply(int absenceTypeId, DateTime start, int days, string comments = null)
    {
        return new ApplyForLeaveInput
        {
            AbsenceTypeId = absenceTypeId,
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            Comments = comments
        };
    }

    private RequestStatus? StatusOf(int leaveId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>()
            .IgnoreQueryFilters().Single(a => a.Id == leaveId).WorkflowStatus);
    }

    [Fact]
    public async Task Should_Refuse_Everything_For_A_User_With_No_Employee_Record()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() => _myLeaveAppService.GetMyBalances());
    }

    [Fact]
    public async Task Should_Return_My_Balances_Including_Bookable()
    {
        var meId = CreateCurrentUserEmployee("MYL100");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20, used: 3);

        var balances = await _myLeaveAppService.GetMyBalances();

        var annual = balances.Single(b => b.AbsenceTypeId == typeId);
        annual.Remaining.ShouldBe(17);
        annual.Bookable.ShouldBe(17);
    }

    [Fact]
    public async Task Should_Record_Leave_Immediately_When_Workflow_Is_Off_For_The_Type()
    {
        // The catalog seeds every type dormant, so this is the default state -- and it must work, or
        // switching workflow off would be a half-feature.
        var meId = CreateCurrentUserEmployee("MYL200");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);

        var result = await _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 3));

        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();

        // Null status means effective immediately -- the pre-workflow behaviour.
        StatusOf(result.EmployeeAbsenceId).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Submit_For_Approval_When_Workflow_Is_On()
    {
        var meId = CreateCurrentUserEmployee("MYL300");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);
        var approverId = EnableLeaveWorkflowWithApprover("MYL300APPROVER");

        var result = await _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 3));

        result.SubmittedForApproval.ShouldBeTrue();
        result.WorkflowRequestId.ShouldNotBeNull();

        // Open, not Draft: created and submitted in one step.
        StatusOf(result.EmployeeAbsenceId).ShouldBe(RequestStatus.Open);

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.TargetEntityId.ShouldBe(result.EmployeeAbsenceId);
        request.ApproverId.ShouldBe(approverId);

        // The handler wrote the inbox label at submit, so the approver's list never touches this table.
        request.SummaryEn.ShouldNotBeNullOrWhiteSpace();
        request.SummaryEn.ShouldContain("Annual");
    }

    [Fact]
    public async Task A_Submitted_Request_Should_Be_Invisible_To_Ordinary_Queries()
    {
        // The whole point of the design, asserted from the outside: payroll and reports must not see this.
        var meId = CreateCurrentUserEmployee("MYL400");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);
        EnableLeaveWorkflowWithApprover("MYL400APPROVER");

        var result = await _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 3));

        var visibleToOrdinaryQuery = await WithUnitOfWorkAsync(() =>
            Resolve<Abp.Domain.Repositories.IRepository<EmployeeAbsence>>()
                .GetAll().Any(a => a.Id == result.EmployeeAbsenceId));

        visibleToOrdinaryQuery.ShouldBeFalse();

        // But the employee sees their own request.
        var mine = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput());
        mine.Items.ShouldContain(l => l.Id == result.EmployeeAbsenceId);
    }

    private async Task<T> WithUnitOfWorkAsync<T>(Func<T> action)
    {
        var uowManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();
        using (var uow = uowManager.Begin())
        {
            var result = action();
            await uow.CompleteAsync();
            return result;
        }
    }

    [Fact]
    public async Task A_Pending_Request_Should_Reduce_Bookable_But_Not_Used()
    {
        var meId = CreateCurrentUserEmployee("MYL500");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);
        EnableLeaveWorkflowWithApprover("MYL500APPROVER");

        await _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 4));

        var balance = (await _myLeaveAppService.GetMyBalances()).Single(b => b.AbsenceTypeId == typeId);

        balance.Used.ShouldBe(0);        // nothing consumed -- it may still be rejected
        balance.Pending.ShouldBe(4);
        balance.Remaining.ShouldBe(20);
        balance.Bookable.ShouldBe(16);
    }

    [Fact]
    public async Task Should_Refuse_A_Second_Request_That_Exceeds_What_Is_Left_Bookable()
    {
        // The case the bookable rule exists for: each request is within the quota balance on its own, but
        // not once the first is counted.
        var meId = CreateCurrentUserEmployee("MYL600");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 10);
        EnableLeaveWorkflowWithApprover("MYL600APPROVER");

        await _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 6));

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(30), 6)));

        exception.Message.ShouldContain("4");
    }

    [Fact]
    public async Task Should_Refuse_Leave_Overlapping_A_Pending_Request()
    {
        var meId = CreateCurrentUserEmployee("MYL700");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);
        EnableLeaveWorkflowWithApprover("MYL700APPROVER");

        var start = DateTime.Today.AddDays(10);
        await _myLeaveAppService.ApplyForLeave(Apply(typeId, start, 5));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(Apply(typeId, start.AddDays(2), 3)));
    }

    [Fact]
    public async Task Should_Refuse_An_End_Date_Before_The_Start_Date()
    {
        var meId = CreateCurrentUserEmployee("MYL800");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
            {
                AbsenceTypeId = typeId,
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(5)
            }));
    }

    [Fact]
    public async Task Should_Reject_An_Application_Beyond_Bookable_Before_It_Reaches_An_Approver()
    {
        // The validation runs inside the handler at submit, so the refusal happens before any approver is
        // assigned -- nobody is asked to review a request that was never valid.
        //
        // NOTE ON WHAT THIS DOES NOT ASSERT: create and submit share one unit of work, so in production the
        // failed application's leave row is rolled back with the exception. That cannot be verified here --
        // HCMv3TestBaseModule sets Configuration.UnitOfWork.IsTransactional = false, so the harness has no
        // transaction to roll back. Asserting the row count would test the harness, not the product.
        var meId = CreateCurrentUserEmployee("MYL900");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 3);
        EnableLeaveWorkflowWithApprover("MYL900APPROVER");

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 10)));

        exception.Message.ShouldContain("3");

        // No approver was ever asked to look at it.
        var approverRows = UsingDbContext(c => c.Set<WorkFlowApprover>().Count());
        approverRows.ShouldBe(0);
    }

    [Fact]
    public async Task A_Two_Step_Chain_Should_Not_Complete_On_The_First_Approval()
    {
        // Regression test for a bug that shipped because the Workflow tests' bespoke FakeRepository flushes
        // inside InsertAsync while the real ABP repository defers. RequestDetailsAppService counted steps
        // with a SQL COUNT straight after inserting one, so WorkflowSteps came back one short -- and
        // WorkflowApprovalService reads it as the total. On a two-step chain that meant the request was
        // marked fully approved the moment the FIRST approver acted and the second was never asked.
        //
        // Written here rather than in the Workflow tests deliberately: this file uses the REAL repositories,
        // which is the only place the deferred-write behaviour is visible.
        var meId = CreateCurrentUserEmployee("MYL1400");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);

        var firstApproverId = CreateEmployee("MYL1400APP1");
        var secondApproverId = CreateEmployee("MYL1400APP2");

        var detailsService = Resolve<HCMv3.Workflow.IRequestDetailsAppService>();
        var mastersService = Resolve<HCMv3.Workflow.IRequestMastersAppService>();

        int masterId = 0, role1Id = 0, role2Id = 0;
        UsingDbContext(context =>
        {
            var r1 = context.Set<WorkflowRole>().Add(new WorkflowRole { TenantId = TenantId, Code = "R1" + Guid.NewGuid().ToString("N").Substring(0, 4), NameEn = "A1", NameAr = "A1" }).Entity;
            var r2 = context.Set<WorkflowRole>().Add(new WorkflowRole { TenantId = TenantId, Code = "R2" + Guid.NewGuid().ToString("N").Substring(0, 4), NameEn = "A2", NameAr = "A2" }).Entity;
            var m = context.Set<RequestMaster>().Add(new RequestMaster { TenantId = TenantId, Code = "LEAVE-2STEP", NameEn = "Two Step", NameAr = "Two Step", WorkflowSteps = 0 }).Entity;
            context.SaveChanges();
            masterId = m.Id; role1Id = r1.Id; role2Id = r2.Id;

            context.Set<EmployeeWorkflowRole>().AddRange(
                new EmployeeWorkflowRole { TenantId = TenantId, EmployeeId = firstApproverId, WorkflowRoleId = r1.Id, StartDate = DateTime.Today.AddYears(-1), EndDate = DateTime.Today.AddYears(1) },
                new EmployeeWorkflowRole { TenantId = TenantId, EmployeeId = secondApproverId, WorkflowRoleId = r2.Id, StartDate = DateTime.Today.AddYears(-1), EndDate = DateTime.Today.AddYears(1) });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAbsence");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = m.Id;
        });

        // Added through the app service, so the step-count recalculation runs exactly as it does in the UI.
        foreach (var (step, roleId) in new[] { (1, role1Id), (2, role2Id) })
        {
            await detailsService.CreateOrEdit(new HCMv3.Workflow.Dto.CreateOrEditRequestDetailDto
            {
                RequestMasterId = masterId,
                Step = step,
                WorkflowRoleId = roleId,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });
        }

        var master = await mastersService.GetRequestMasterForEdit(new EntityDto(masterId));
        var stepsRecorded = UsingDbContext(c => c.Set<RequestMaster>().Single(m => m.Id == masterId).WorkflowSteps);
        stepsRecorded.ShouldBe(2, "both steps must be counted, or the chain ends early");

        var result = await _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 2));

        var approvalService = Resolve<HCMv3.Workflow.Services.IWorkflowApprovalService>();
        await approvalService.ApproveAsync(result.WorkflowRequestId.Value, firstApproverId, "step 1 ok");

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));

        // Still open, now waiting on the SECOND approver.
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.WorkflowStep.ShouldBe(2);
        request.ApproverId.ShouldBe(secondApproverId);

        // And the leave is still hidden from payroll, because it is not approved.
        StatusOf(result.EmployeeAbsenceId).ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task Should_Only_List_My_Own_Leave()
    {
        var meId = CreateCurrentUserEmployee("MYL1000");
        var otherId = CreateEmployee("MYL1000OTHER");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);

        await _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 2));

        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                AbsenceTypeId = typeId,
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(12),
                TotalLeaveDays = 3
            });
        });

        var mine = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput());

        mine.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Cancel_My_Own_Pending_Request()
    {
        var meId = CreateCurrentUserEmployee("MYL1100");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);
        EnableLeaveWorkflowWithApprover("MYL1100APPROVER");

        var result = await _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 3));

        await _myLeaveAppService.CancelMyLeave(new EntityDto(result.EmployeeAbsenceId));

        // Cancelling releases the bookable days it was holding.
        var balance = (await _myLeaveAppService.GetMyBalances()).Single(b => b.AbsenceTypeId == typeId);
        balance.Pending.ShouldBe(0);
        balance.Bookable.ShouldBe(20);
    }

    [Fact]
    public async Task Should_Refuse_To_Cancel_An_Approved_Leave()
    {
        // Once approved it is a real transaction others may have planned around; withdrawing it is an
        // amendment, not a cancellation.
        var meId = CreateCurrentUserEmployee("MYL1200");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 20);

        var result = await _myLeaveAppService.ApplyForLeave(Apply(typeId, DateTime.Today.AddDays(10), 3));

        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsence>().IgnoreQueryFilters()
                .Single(a => a.Id == result.EmployeeAbsenceId).WorkflowStatus = RequestStatus.Approved;
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.CancelMyLeave(new EntityDto(result.EmployeeAbsenceId)));
    }

    [Fact]
    public async Task Should_Refuse_To_Cancel_Someone_Elses_Leave()
    {
        CreateCurrentUserEmployee("MYL1300ME");
        var otherId = CreateEmployee("MYL1300OTHER");
        var typeId = CreateAbsenceType();

        // The entity must be captured and its Id read AFTER the block: UsingDbContext saves on exit, so
        // reading .Entity.Id inside the lambda yields 0 and the assertion silently tests the wrong thing.
        EmployeeAbsence othersLeave = null;
        UsingDbContext(context =>
        {
            othersLeave = context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                AbsenceTypeId = typeId,
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(12),
                TotalLeaveDays = 3,
                WorkflowStatus = RequestStatus.Open
            }).Entity;
        });

        var othersLeaveId = othersLeave.Id;

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _myLeaveAppService.CancelMyLeave(new EntityDto(othersLeaveId)));
    }
}
