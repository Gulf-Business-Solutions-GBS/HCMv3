using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers IT service requests through the workflow engine -- the second transaction type that is routed for
/// approval but is NOT self-service, after personnel actions.
///
/// <para>
/// The classification is a finding about the legacy system rather than a preference: <c>HCM10.ESS</c>
/// contains a view, a controller and a model for an HR service request and nothing whatsoever for an IT one,
/// which only ever had a back-office screen. So this type is submitted from its admin application service
/// through <c>IAdminTransactionSubmitter</c>, and its catalog row deliberately carries no ESS route. That
/// pairing is what the last test here pins down: a route with no ESS service would put a dead tile on the
/// landing page, and an ESS service with no route would be an unreachable endpoint.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class ITServiceWorkflow_Tests : AppTestBase
{
    private readonly IEmployeeITServicesAppService _itServices;

    public ITServiceWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _itServices = Resolve<IEmployeeITServicesAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code)
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
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateITServiceType()
    {
        ITServiceType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<ITServiceType>().Add(new ITServiceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Laptop Replacement",
                NameAr = "استبدال حاسوب"
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>Turns IT services on for approval with a real approver holding the step's role.</summary>
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
                Code = "ITS-STD",
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

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeITService");
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static CreateOrEditEmployeeITServiceDto ServiceFor(int employeeId, int serviceTypeId) =>
        new CreateOrEditEmployeeITServiceDto
        {
            EmployeeId = employeeId,
            ITServiceTypeId = serviceTypeId,
            ServiceDate = DateTime.Today.AddDays(5),
            Comments = "Screen is cracked"
        };

    private EmployeeITService ServiceRow(int employeeId) =>
        UsingDbContext(c => c.Set<EmployeeITService>().IgnoreQueryFilters().First(s => s.EmployeeId == employeeId));

    [Fact]
    public async Task Should_Record_Immediately_While_The_Type_Is_Not_Routed()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install -- and the
        // pre-workflow behaviour. Switching workflow off has to stay a real option, not a half-working one.
        var employeeId = CreateEmployee("ITS100");

        await _itServices.CreateOrEdit(ServiceFor(employeeId, CreateITServiceType()));

        ServiceRow(employeeId).WorkflowStatus.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>().Count(r => r.RequestType == "EmployeeITService")).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Route_For_Approval_Once_The_Type_Is_Enabled()
    {
        var employeeId = CreateEmployee("ITS110");
        EnableWorkflow("ITS110APP");
        var typeId = CreateITServiceType();

        await _itServices.CreateOrEdit(ServiceFor(employeeId, typeId));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "EmployeeITService"));

        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.RequestNumber.ShouldStartWith("ITS-" + DateTime.Today.Year);

        // The request is about the SUBJECT employee, not whoever raised it on their behalf.
        request.EmployeeId.ShouldBe(employeeId);

        // Routing needs the sub-type, or a chain configured per service type could never match.
        request.RequestSubTypeId.ShouldBe(typeId);
    }

    [Fact]
    public async Task An_Unapproved_Request_Should_Be_Invisible_To_The_Rest_Of_The_System()
    {
        var employeeId = CreateEmployee("ITS120");
        EnableWorkflow("ITS120APP");

        await _itServices.CreateOrEdit(ServiceFor(employeeId, CreateITServiceType()));

        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeITService>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            // ApprovedOnly is ON here, exactly as any other screen would see it.
            repository.GetAll().Any(s => s.EmployeeId == employeeId)
                .ShouldBeFalse("An unapproved IT service request was visible to the rest of the system.");
        }
    }

    [Fact]
    public async Task Approving_Should_Make_The_Request_Real_And_Nothing_More()
    {
        // Legacy's approval branch was an empty if-block, so approval marks the row approved and has no
        // other side effect. Asserting the absence keeps a future handler from quietly inventing one.
        var employeeId = CreateEmployee("ITS130");
        var approverId = EnableWorkflow("ITS130APP");

        await _itServices.CreateOrEdit(ServiceFor(employeeId, CreateITServiceType()));

        var requestId = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.RequestType == "EmployeeITService").Id);

        await Resolve<HCMv3.Workflow.Services.IWorkflowApprovalService>()
            .ApproveAsync(requestId, approverId, "Approved");

        var row = ServiceRow(employeeId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        row.WorkflowRequestId.ShouldBe(requestId);

        UsingDbContext(c => c.Set<EmployeeITService>().IgnoreQueryFilters()
            .Count(s => s.EmployeeId == employeeId)).ShouldBe(1);
    }

    [Fact]
    public async Task The_Summary_Should_Say_What_Is_Being_Decided()
    {
        var employeeId = CreateEmployee("ITS140");
        EnableWorkflow("ITS140APP");

        await _itServices.CreateOrEdit(ServiceFor(employeeId, CreateITServiceType()));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "EmployeeITService"));

        request.SummaryEn.ShouldStartWith("Laptop Replacement");
        request.SummaryEn.ShouldContain(DateTime.Today.AddDays(5).ToString("dd MMM yyyy"));
    }

    [Fact]
    public void IT_Services_Should_Not_Be_Offered_As_Self_Service()
    {
        // Legacy has no ESS surface for this type at all -- no view, no controller, no model -- so it is
        // raised on the employee's behalf and must never appear on a tile list. A route here with no
        // MyITServiceAppService behind it would be a dead tile; the two go together or not at all.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeITService"));

        type.EssRoute.ShouldBeNull();
        type.EssPermissionName.ShouldBeNull();
        type.IsEssEnabled.ShouldBeFalse();
    }
}
