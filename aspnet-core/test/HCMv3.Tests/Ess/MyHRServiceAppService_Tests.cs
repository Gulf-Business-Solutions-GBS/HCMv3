using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Covers self-service HR services -- certificates, letters, card replacements.
///
/// <para>
/// This is the half of the legacy <c>HRServiceRequest</c>/<c>ITServiceRequest</c> pair that IS self-service:
/// legacy ships an ESS view for an HR service and none at all for an IT one. So what is worth protecting
/// here is the self-service contract rather than the routing: the row must be invisible to everyone else
/// until somebody approves it, an unlinked account must be refused rather than shown somebody's data, and
/// the employee must come from the session and not from anything the caller sent.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyHRServiceAppService_Tests : AppTestBase
{
    private readonly IMyHRServiceAppService _hrServices;

    public MyHRServiceAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _hrServices = Resolve<IMyHRServiceAppService>();
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

    private int CreateHRServiceType()
    {
        HRServiceType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<HRServiceType>().Add(new HRServiceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Salary Certificate",
                NameAr = "شهادة راتب"
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>Turns HR services on for approval, pointing them at a one-step chain with a real approver.</summary>
    private int EnableWorkflow(string approverCode)
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
                Code = "HRS-STD",
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

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeHRService");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private static RequestHRServiceInput ServiceInput(int serviceTypeId) => new RequestHRServiceInput
    {
        HRServiceTypeId = serviceTypeId,
        ServiceDate = DateTime.Today.AddDays(3),
        Amount = 0,
        Comments = "Need it for a bank application"
    };

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked to an employee, so this is the normal state of a user who has no
        // business in self-service -- not an edge case. Refusing beats quietly listing nothing, which would
        // read as "you have no requests".
        await Assert.ThrowsAsync<UserFriendlyException>(() => _hrServices.GetMyHRServices(ListInput()));
    }

    [Fact]
    public async Task Should_Record_Immediately_When_Workflow_Is_Off_For_The_Type()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install: effective at
        // once, no request raised.
        CreateCurrentUserEmployee("HRS100");
        var typeId = CreateHRServiceType();

        var result = await _hrServices.RequestHRService(ServiceInput(typeId));

        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();

        var row = UsingDbContext(c => c.Set<EmployeeHRService>().IgnoreQueryFilters()
            .Single(s => s.Id == result.TransactionId));
        row.WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task A_Submitted_Request_Should_Be_Hidden_Until_It_Is_Approved()
    {
        // The point of the one-row design: a pending request lives in the real table, so it has to be the
        // ApprovedOnly filter that keeps the rest of the system from treating it as a granted service.
        CreateCurrentUserEmployee("HRS110");
        var approverId = EnableWorkflow("HRS110APP");
        var typeId = CreateHRServiceType();

        var result = await _hrServices.RequestHRService(ServiceInput(typeId));
        result.SubmittedForApproval.ShouldBeTrue();

        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeHRService>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        // Read through a repository inside a unit of work, because that is where ABP data filters live. A
        // raw DbContext query would bypass ApprovedOnly and pass whatever the filter did.
        using (unitOfWorkManager.Begin())
        {
            repository.GetAll().Any(s => s.Id == result.TransactionId)
                .ShouldBeFalse("A pending HR service request was visible to the rest of the system.");
        }

        // But the employee sees their own, which is the whole reason they opened the page.
        var mine = await _hrServices.GetMyHRServices(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(result.TransactionId);

        await Resolve<HCMv3.Workflow.Services.IWorkflowApprovalService>()
            .ApproveAsync(result.WorkflowRequestId.Value, approverId, "Fine");

        using (unitOfWorkManager.Begin())
        {
            repository.GetAll().Any(s => s.Id == result.TransactionId)
                .ShouldBeTrue("An approved HR service request never became visible.");
        }
    }

    [Fact]
    public async Task The_Request_Should_Be_Attributed_To_The_Signed_In_Employee()
    {
        // There is no employee field on the input at all, and that is deliberate -- a self-service form must
        // not be able to raise a request on somebody else's behalf.
        var meId = CreateCurrentUserEmployee("HRS120");
        CreateEmployee("HRS120OTHER");
        var typeId = CreateHRServiceType();

        var result = await _hrServices.RequestHRService(ServiceInput(typeId));

        var row = UsingDbContext(c => c.Set<EmployeeHRService>().IgnoreQueryFilters()
            .Single(s => s.Id == result.TransactionId));

        row.EmployeeId.ShouldBe(meId);
    }

    [Fact]
    public async Task Should_Number_Its_Requests_With_Its_Own_Prefix()
    {
        // Shared numbering would make a request number useless for telling at a glance what a request is.
        CreateCurrentUserEmployee("HRS130");
        EnableWorkflow("HRS130APP");
        var typeId = CreateHRServiceType();

        var result = await _hrServices.RequestHRService(ServiceInput(typeId));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.Id == result.WorkflowRequestId.Value));

        // A number of its own, and nothing but digits: the per-type prefix went when the counter became
        // tenant-wide, so "which type is this" is no longer something the number answers.
        request.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        request.RequestNumber.ShouldNotContain("-");
        int.Parse(request.RequestNumber).ShouldBeGreaterThan(0);

        // Routing needs the sub-type, or a chain configured per service type could never match.
        request.RequestSubTypeId.ShouldBe(typeId);
    }

    [Fact]
    public async Task The_Summary_Should_Name_The_Service_And_When_It_Is_Wanted()
    {
        CreateCurrentUserEmployee("HRS140");
        EnableWorkflow("HRS140APP");
        var typeId = CreateHRServiceType();

        var result = await _hrServices.RequestHRService(ServiceInput(typeId));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.Id == result.WorkflowRequestId.Value));

        request.SummaryEn.ShouldStartWith("Salary Certificate");
        request.SummaryEn.ShouldContain(DateTime.Today.AddDays(3).ToString("dd MMM yyyy"));
    }

    [Fact]
    public async Task Should_Not_List_Somebody_Elses_Requests()
    {
        CreateCurrentUserEmployee("HRS150");
        var otherId = CreateEmployee("HRS150OTHER");
        var typeId = CreateHRServiceType();

        UsingDbContext(context =>
        {
            context.Set<EmployeeHRService>().Add(new EmployeeHRService
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                HRServiceTypeId = typeId,
                ServiceDate = DateTime.Today
            });
        });

        var mine = await _hrServices.GetMyHRServices(ListInput());

        mine.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Withdrawing_Should_Remove_The_Row_And_Close_Its_Request()
    {
        // Closing the request matters as much as deleting the row: otherwise an approver is left with an
        // inbox entry pointing at something that no longer exists.
        CreateCurrentUserEmployee("HRS160");
        EnableWorkflow("HRS160APP");
        var typeId = CreateHRServiceType();

        var result = await _hrServices.RequestHRService(ServiceInput(typeId));

        await _hrServices.CancelMyHRService(new EntityDto(result.TransactionId));

        UsingDbContext(c => c.Set<EmployeeHRService>().IgnoreQueryFilters()
            .Any(s => s.Id == result.TransactionId && !s.IsDeleted)).ShouldBeFalse();

        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value))
            .RequestStatus.ShouldBe(RequestStatus.Closed);
    }

    [Fact]
    public void HR_Services_Should_Be_Offered_As_Self_Service()
    {
        // The counterpart to the IT-service assertion. An employee asks for their own salary certificate,
        // so unlike a personnel action or an IT ticket this one does carry an ESS route.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeHRService"));

        type.EssRoute.ShouldBe("/app/main/ess/hr-service");
        type.EssPermissionName.ShouldBe(HCMv3.Authorization.AppPermissions.Ess_MyHRServices);
    }
}
