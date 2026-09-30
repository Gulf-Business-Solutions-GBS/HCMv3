using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Abp.Timing;
using ClosedXML.Excel;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Workflow;
using HCMv3.Dashboards.Workflow.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// The Workflow Requests Dashboard: requests raised by the employees <see cref="IDashboardScopeService"/> allows.
/// Rules: <see cref="WorkflowDashboardCalculator_Tests"/>. Periods run up to today, so ages are measured from now.
/// </summary>
public class WorkflowDashboardAppService_Tests
{
    private const int Manager = 9;

    private readonly List<Employee> _employees = new();
    private readonly List<EmployeeOrgAssignment> _assignments = new();
    private readonly List<WorkflowRequest> _requests = new();
    private readonly List<RequestLog> _logs = new();
    private readonly List<WorkFlowApprover> _approvers = new();
    private readonly List<WorkflowTransactionType> _types = new()
    {
        new WorkflowTransactionType { Id = 1, TenantId = 1, Code = "Leave", NameEn = "Leave request" }
    };
    private readonly List<OrganizationUnit> _units = new()
    {
        new OrganizationUnit(1, "Company") { Id = 1 },
        new OrganizationUnit(1, "Finance", 1) { Id = 2 }
    };

    private readonly IDashboardScopeService _scopeService = Substitute.For<IDashboardScopeService>();
    private readonly IRequestMasterResolver _resolver = Substitute.For<IRequestMasterResolver>();
    private readonly ITempFileCacheManager _tempFiles = Substitute.For<ITempFileCacheManager>();
    private readonly Dictionary<string, byte[]> _savedFiles = new();

    private readonly DateTime _today = Clock.Now.Date;

    public WorkflowDashboardAppService_Tests()
    {
        AddEmployee(1, unitId: 1);
        AddEmployee(2, unitId: 2);
        _employees.Add(new Employee { Id = Manager, TenantId = 1, Code = "M9", FullNameEn = "The Manager", Status = EmployeeStatus.Active });
        ScopeIs(DashboardScope.Everyone(DashboardScopeReason.Admin));
        DueInDaysIs(5);

        _tempFiles.When(t => t.SetFile(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call => _savedFiles[call.ArgAt<string>(0)] = call.ArgAt<byte[]>(1));
    }

    private void ScopeIs(DashboardScope scope) => _scopeService.GetScopeAsync().Returns(Task.FromResult(scope));

    private void DueInDaysIs(int days) =>
        _resolver.ResolveAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(Task.FromResult(new RequestMaster { DueInDays = days }));

    private void AddEmployee(int id, long unitId)
    {
        _employees.Add(new Employee { Id = id, TenantId = 1, Code = "E" + id, FullNameEn = "Employee " + id, Status = EmployeeStatus.Active });
        _assignments.Add(new EmployeeOrgAssignment
        {
            Id = id, TenantId = 1, EmployeeId = id, OrganizationUnitId = unitId,
            StartDate = new DateTime(2020, 1, 1), EndDate = DateTime.MaxValue.Date
        });
    }

    private WorkflowRequest AddRequest(int employeeId, int daysAgo, RequestStatus status, string type = "Leave", int? decidedAfterDays = null)
    {
        var created = _today.AddDays(-daysAgo).AddHours(9);
        var request = new WorkflowRequest
        {
            Id = _requests.Count + 1, TenantId = 1, EmployeeId = employeeId, RequestType = type, RequestNumber = "R" + (_requests.Count + 1),
            RequestStatus = status, WorkflowStep = 1, CreationTime = created
        };
        _requests.Add(request);
        _logs.Add(new RequestLog { Id = _logs.Count + 1, TenantId = 1, WorkflowRequestId = request.Id, ActionType = RequestActionType.Created, ActionDateTime = created });

        if (decidedAfterDays.HasValue)
        {
            _logs.Add(new RequestLog
            {
                Id = _logs.Count + 1, TenantId = 1, WorkflowRequestId = request.Id,
                ActionType = status == RequestStatus.Rejected ? RequestActionType.Rejected : RequestActionType.Approved,
                ActionDateTime = created.AddDays(decidedAfterDays.Value)
            });
        }

        if (status == RequestStatus.Open)
        {
            _approvers.Add(new WorkFlowApprover { Id = _approvers.Count + 1, TenantId = 1, WorkflowRequestId = request.Id, ApproverId = Manager, WorkflowStep = 1 });
        }

        return request;
    }

    private WorkflowDashboardAppService CreateService()
    {
        var unitRepository = Substitute.For<IRepository<OrganizationUnit, long>>();
        unitRepository.GetAll().Returns(_ => _units.AsQueryable());

        var service = new WorkflowDashboardAppService(
            Fakes.RepoOver(_employees),
            Fakes.RepoOver(_assignments),
            Fakes.RepoOver(_requests),
            Fakes.RepoOver(_logs),
            Fakes.RepoOver(_approvers),
            Fakes.RepoOver(_types),
            unitRepository,
            _resolver,
            _scopeService,
            new WorkflowDashboardExcelExporter(_tempFiles) { LocalizationManager = Fakes.LocalizationManager() });

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        ((ApplicationService)service).UnitOfWorkManager = Fakes.UnitOfWorkManager();
        return service;
    }

    private WorkflowDashboardInput LastSixtyDays(long? unitId = null, string type = null) => new()
    {
        FromDate = _today.AddDays(-60), ToDate = _today, OrganizationUnitId = unitId, RequestType = type
    };

    [Fact]
    public async Task GetDashboard_Should_Count_Requests_And_Leave_Out_Drafts()
    {
        AddRequest(1, 2, RequestStatus.Open);
        AddRequest(1, 20, RequestStatus.Approved, decidedAfterDays: 3);
        AddRequest(2, 10, RequestStatus.Rejected, decidedAfterDays: 1);
        AddRequest(2, 1, RequestStatus.Draft);

        var result = await CreateService().GetDashboard(LastSixtyDays());

        result.Kpis.Raised.ShouldBe(3);
        result.Kpis.Open.ShouldBe(1);
        result.Kpis.AverageDaysToDecide.ShouldBe(2);
        result.Types.Single().NameEn.ShouldBe("Leave request");
    }

    /// <summary>Legacy sent midnight as the end, so a request raised on the last day was missed.</summary>
    [Fact]
    public async Task GetDashboard_Should_Include_Requests_Raised_On_The_Last_Day()
    {
        AddRequest(1, 0, RequestStatus.Open);

        (await CreateService().GetDashboard(LastSixtyDays())).Kpis.Raised.ShouldBe(1);
    }

    [Fact]
    public async Task GetDashboard_Should_Mark_Open_Requests_Past_Their_Chains_Due_Days()
    {
        AddRequest(1, 10, RequestStatus.Open);
        AddRequest(1, 2, RequestStatus.Open);

        var result = await CreateService().GetDashboard(LastSixtyDays());

        result.Kpis.Overdue.ShouldBe(1);
        result.Waiting.Single().ApproverNameEn.ShouldBe("The Manager");
        result.Waiting.Single().Waiting.ShouldBe(2);
    }

    [Fact]
    public async Task GetDashboard_Should_Still_Show_A_Request_Whose_Chain_Cannot_Be_Found()
    {
        _resolver.ResolveAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<int?>())
            .ThrowsAsync(new InvalidOperationException("no chain"));
        AddRequest(1, 30, RequestStatus.Open);

        var result = await CreateService().GetDashboard(LastSixtyDays());

        result.Kpis.Open.ShouldBe(1);
        result.Kpis.Overdue.ShouldBe(0);
    }

    [Fact]
    public async Task GetDashboard_Should_Narrow_By_Unit_And_Type()
    {
        AddRequest(1, 5, RequestStatus.Open, type: "Leave");
        AddRequest(2, 5, RequestStatus.Open, type: "Loan");

        (await CreateService().GetDashboard(LastSixtyDays(unitId: 2))).Kpis.Raised.ShouldBe(1);
        (await CreateService().GetDashboard(LastSixtyDays(type: "Leave"))).Kpis.Raised.ShouldBe(1);
    }

    /// <summary>Legacy showed every request in the company to a user with no subordinates.</summary>
    [Fact]
    public async Task GetDashboard_Should_Show_Nothing_When_The_Scope_Is_Nobody()
    {
        ScopeIs(DashboardScope.Restricted(new List<int>(), new List<string>(), includesOwnTeam: false));
        AddRequest(1, 5, RequestStatus.Open);

        (await CreateService().GetDashboard(LastSixtyDays())).Kpis.Raised.ShouldBe(0);
    }

    [Fact]
    public async Task GetDashboard_Should_Count_Only_Requests_Of_Employees_In_Scope()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2 }, new List<string>(), includesOwnTeam: true));
        AddRequest(1, 5, RequestStatus.Open);
        AddRequest(2, 5, RequestStatus.Open);

        var result = await CreateService().GetDashboard(LastSixtyDays());

        result.Kpis.Raised.ShouldBe(1);
        result.Waiting.Single().ApproverNameEn.ShouldBe("The Manager"); // named though outside the scope
    }

    [Fact]
    public async Task GetRequests_Should_List_Overdue_And_Those_Waiting_With_An_Approver()
    {
        AddRequest(1, 10, RequestStatus.Open);
        AddRequest(2, 1, RequestStatus.Open);
        AddRequest(2, 3, RequestStatus.Approved, decidedAfterDays: 1);
        var service = CreateService();

        GetWorkflowRequestsInput List(WorkflowRequestListKind kind) => new()
        {
            FromDate = _today.AddDays(-60), ToDate = _today, Kind = kind, MaxResultCount = 10
        };

        (await service.GetRequests(List(WorkflowRequestListKind.Overdue))).Items.Single().EmployeeCode.ShouldBe("E1");

        var withManager = List(WorkflowRequestListKind.Approver);
        withManager.ApproverId = Manager;
        (await service.GetRequests(withManager)).TotalCount.ShouldBe(2);

        var approved = List(WorkflowRequestListKind.Status);
        approved.Status = RequestStatus.Approved;
        var row = (await service.GetRequests(approved)).Items.Single();
        row.Days.ShouldBe(1);
        row.WaitingWithEn.ShouldBeNull();
    }

    [Fact]
    public async Task ExportToExcel_Should_Put_The_Whole_Dashboard_On_A_Summary_Sheet_Plus_One_Sheet_Per_Tile()
    {
        AddRequest(1, 5, RequestStatus.Open);

        var file = await CreateService().ExportToExcel(new ExportWorkflowDashboardInput
        {
            FromDate = _today.AddDays(-60), ToDate = _today, Part = WorkflowDashboardExportPart.All
        });

        using var book = new XLWorkbook(new MemoryStream(_savedFiles[file.FileToken]));
        book.Worksheets.Count.ShouldBe(7);
        book.Worksheet(1).Name.ShouldBe("Summary");
        book.Worksheet(1).Cell(3, 2).GetString().ShouldBe("Dashboard_ScopeAllAdmin");
    }
}
