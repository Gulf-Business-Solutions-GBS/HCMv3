using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// HR's side of leave plans: the list (also the employee record's section) and the Leave Planner (legacy
/// <c>uscLeavePlanner</c>). Run as the tenant admin, whom the dashboard scope rule gives every employee.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeavePlansAppService_Tests : AppTestBase
{
    private readonly ILeavePlansAppService _plans;
    private readonly ILeavePlannerAppService _planner;

    public LeavePlansAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _plans = Resolve<ILeavePlansAppService>();
        _planner = Resolve<ILeavePlannerAppService>();
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
                Status = EmployeeStatus.Active,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int AddPlan(int employeeId, DateTime start, DateTime end, RequestStatus? status, int? requestId = null)
    {
        LeavePlan plan = null;
        UsingDbContext(context =>
        {
            plan = context.Set<LeavePlan>().Add(new LeavePlan
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = start,
                EndDate = end,
                PlannedDays = (end - start).Days + 1,
                WorkflowStatus = status,
                WorkflowRequestId = requestId
            }).Entity;
        });

        return plan.Id;
    }

    private int AddRequest(int employeeId, RequestStatus status)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = "LeavePlan",
                RequestNumber = Guid.NewGuid().ToString("N").Substring(0, 8),
                RequestStatus = status,
                WorkflowStep = 1
            }).Entity;
        });

        return request.Id;
    }

    // ---------------------------------------------------------------- the list

    [Fact]
    public async Task The_List_Should_Show_Pending_And_Approved_Plans_But_Not_Unsent_Drafts()
    {
        var employeeId = CreateEmployee("LPA100");
        var start = new DateTime(2031, 5, 4);

        var recorded = AddPlan(employeeId, start, start.AddDays(1), null);
        var pending = AddPlan(employeeId, start, start.AddDays(2), RequestStatus.Open, AddRequest(employeeId, RequestStatus.Open));
        var draft = AddPlan(employeeId, start, start.AddDays(3), RequestStatus.Draft);

        var result = await _plans.GetAll(new GetAllLeavePlansInput { EmployeeId = employeeId, MaxResultCount = 50 });

        var ids = result.Items.Select(i => i.Id).ToList();
        ids.ShouldContain(recorded);
        ids.ShouldContain(pending);
        ids.ShouldNotContain(draft);
        result.Items.Single(i => i.Id == pending).RequestNumber.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Turning_Pending_Off_Should_Leave_Only_Settled_Plans()
    {
        var employeeId = CreateEmployee("LPA110");
        var start = new DateTime(2031, 6, 1);

        var approved = AddPlan(employeeId, start, start, RequestStatus.Approved);
        var pending = AddPlan(employeeId, start, start, RequestStatus.Open);

        var result = await _plans.GetAll(new GetAllLeavePlansInput { EmployeeId = employeeId, IncludePending = false, MaxResultCount = 50 });

        result.Items.Select(i => i.Id).ShouldBe(new[] { approved });
        result.Items.Select(i => i.Id).ShouldNotContain(pending);
    }

    [Fact]
    public async Task Deleting_A_Pending_Plan_Should_Close_Its_Request()
    {
        // Otherwise an approver would be left with a request pointing at nothing.
        var employeeId = CreateEmployee("LPA120");
        var requestId = AddRequest(employeeId, RequestStatus.Open);
        var planId = AddPlan(employeeId, new DateTime(2031, 7, 1), new DateTime(2031, 7, 2), RequestStatus.Open, requestId);

        await _plans.Delete(new EntityDto(planId));

        UsingDbContext(c => c.Set<LeavePlan>().IgnoreQueryFilters().Single(p => p.Id == planId).IsDeleted).ShouldBeTrue();
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId).RequestStatus).ShouldBe(RequestStatus.Closed);
    }

    // ---------------------------------------------------------------- the planner

    [Fact]
    public async Task The_Planner_Should_Show_Approved_Plans_By_Default_And_Pending_Ones_Only_When_Asked()
    {
        // Legacy's four check boxes: "Show Leave Planner Approved" and "... Unapproved" are separate switches.
        var approvedOwner = CreateEmployee("LPA200");
        var pendingOwner = CreateEmployee("LPA201");
        var month = new DateTime(2031, 9, 1);

        AddPlan(approvedOwner, month.AddDays(2), month.AddDays(4), RequestStatus.Approved);
        AddPlan(pendingOwner, month.AddDays(2), month.AddDays(4), RequestStatus.Open);

        var byDefault = await _planner.GetLeavePlanner(new GetLeavePlannerInput { Month = month, Filter = "LPA20" });
        var shown = byDefault.Groups.SelectMany(g => g.Rows).Select(r => r.EmployeeId).ToList();
        shown.ShouldContain(approvedOwner);
        shown.ShouldNotContain(pendingOwner);

        var withPending = await _planner.GetLeavePlanner(new GetLeavePlannerInput
        {
            Month = month,
            Filter = "LPA20",
            IncludePlannedPending = true
        });

        var pendingRow = withPending.Groups.SelectMany(g => g.Rows).Single(r => r.EmployeeId == pendingOwner);
        pendingRow.Days[2].ShouldBe(LeavePlannerDayFlags.PlannedPending);
    }

    [Fact]
    public async Task The_Planner_Should_Never_Show_A_Rejected_Plan()
    {
        // Legacy's "unapproved" was RequestStatus != Approved, which swept in refused requests too. A refused
        // plan is nobody being away.
        var employeeId = CreateEmployee("LPA210");
        var month = new DateTime(2031, 10, 1);

        AddPlan(employeeId, month, month.AddDays(1), RequestStatus.Rejected);

        var planner = await _planner.GetLeavePlanner(new GetLeavePlannerInput
        {
            Month = month,
            Filter = "LPA210",
            IncludePlanned = true,
            IncludePlannedPending = true
        });

        planner.Groups.SelectMany(g => g.Rows).ShouldBeEmpty();
    }

    [Fact]
    public async Task The_Planner_Should_Cover_The_Whole_Month_Of_The_Day_Given()
    {
        var planner = await _planner.GetLeavePlanner(new GetLeavePlannerInput { Month = new DateTime(2031, 2, 14), Filter = "nobody-at-all" });

        planner.FromDate.ShouldBe(new DateTime(2031, 2, 1));
        planner.ToDate.ShouldBe(new DateTime(2031, 2, 28));
        planner.Scope.ShouldNotBeNull();
    }
}
