using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Timing;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSettings;
using HCMv3.Tests.GridReports;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy <c>ManageOrgSettings.EnablePosition</c>: an org assignment must name a position, and each one keeps a
/// holder row on that position (<c>UpdatedPositionRelation</c>, <c>UpdatedAllPositionRelation</c>,
/// <c>DeletePositionRelation</c>), which in turn keeps the position's Vacant flag.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PositionOccupancy_Tests : AppTestBase
{
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IEmployeeOrgAssignmentsAppService _appService;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PositionOccupancy_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeOrgAssignmentsAppService>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, TenantId);
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static DateTime Today => Clock.Now.Date;

    private void SetEnablePosition(bool enabled)
    {
        UsingDbContext(context =>
        {
            var org = context.Set<ManageOrgSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                      ?? context.Set<ManageOrgSettings>().Add(new ManageOrgSettings { TenantId = TenantId }).Entity;
            org.EnablePosition = enabled;
        });
    }

    private Position CreatePosition(string code, int fteLimit = 1, int? jobTitleId = null)
    {
        Position position = null;
        UsingDbContext(context =>
        {
            position = context.Set<Position>().Add(new Position
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "Position " + code,
                NameAr = "وظيفة " + code,
                StartDate = new DateTime(2000, 1, 1),
                EndDate = OpenEnded,
                Active = true,
                Vacant = true,
                FTELimit = fteLimit,
                JobTitleId = jobTitleId
            }).Entity;
        });
        return position;
    }

    private PositionRelationship[] Holders(int employeeId) =>
        UsingDbContext(context => context.Set<PositionRelationship>()
            .Where(r => r.EmployeeId == employeeId && !r.IsDeleted)
            .OrderBy(r => r.StartDate)
            .ToArray());

    private bool IsVacant(int positionId) =>
        UsingDbContext(context => context.Set<Position>().Single(p => p.Id == positionId).Vacant);

    private int AssignmentId(int employeeId, DateTime startDate) =>
        UsingDbContext(context => context.Set<EmployeeOrgAssignment>()
            .Single(a => a.EmployeeId == employeeId && a.StartDate == startDate).Id);

    private Task AssignAsync(int employeeId, DateTime startDate, int? positionId) =>
        _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employeeId,
            StartDate = startDate,
            PositionId = positionId
        });

    [Fact]
    public async Task Should_Require_A_Position_When_Positions_Are_On()
    {
        SetEnablePosition(true);
        var employee = _data.CreateEmployee("POS001");

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => AssignAsync(employee.Id, Today.AddDays(-10), null));

        ex.Message.ShouldBe("Position cannot be blank.");
    }

    [Fact]
    public async Task Should_Not_Require_Or_Track_Positions_When_Positions_Are_Off()
    {
        SetEnablePosition(false);
        var employee = _data.CreateEmployee("POS002");
        var position = CreatePosition("P002");

        await AssignAsync(employee.Id, Today.AddDays(-10), null);
        await AssignAsync(employee.Id, Today.AddDays(-5), position.Id);

        Holders(employee.Id).ShouldBeEmpty();
        IsVacant(position.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Create_A_Holder_Row_And_Fill_The_Position()
    {
        SetEnablePosition(true);
        var employee = _data.CreateEmployee("POS003");
        var position = CreatePosition("P003");

        await AssignAsync(employee.Id, Today.AddDays(-10), position.Id);

        var holder = Holders(employee.Id).ShouldHaveSingleItem();
        holder.PositionId.ShouldBe(position.Id);
        holder.RelationType.ShouldBe(PositionRelationType.Holder);
        holder.StartDate.ShouldBe(Today.AddDays(-10));
        holder.EndDate.ShouldBe(OpenEnded);
        IsVacant(position.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task A_Move_Should_End_The_Old_Holder_Row_The_Day_Before()
    {
        SetEnablePosition(true);
        var employee = _data.CreateEmployee("POS004");
        var oldPosition = CreatePosition("P004A");
        var newPosition = CreatePosition("P004B");

        await AssignAsync(employee.Id, Today.AddDays(-30), oldPosition.Id);
        await AssignAsync(employee.Id, Today.AddDays(-5), newPosition.Id);

        var holders = Holders(employee.Id);
        holders.Length.ShouldBe(2);
        holders[0].PositionId.ShouldBe(oldPosition.Id);
        holders[0].EndDate.ShouldBe(Today.AddDays(-6));
        holders[1].PositionId.ShouldBe(newPosition.Id);
        holders[1].EndDate.ShouldBe(OpenEnded);
        IsVacant(oldPosition.Id).ShouldBeTrue();
        IsVacant(newPosition.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Deleting_An_Assignment_Should_Remove_Its_Holder_Row_And_Reopen_The_Previous_One()
    {
        SetEnablePosition(true);
        var employee = _data.CreateEmployee("POS005");
        var oldPosition = CreatePosition("P005A");
        var newPosition = CreatePosition("P005B");
        await AssignAsync(employee.Id, Today.AddDays(-30), oldPosition.Id);
        await AssignAsync(employee.Id, Today.AddDays(-5), newPosition.Id);

        await _appService.Delete(new EntityDto(AssignmentId(employee.Id, Today.AddDays(-5))));

        var holder = Holders(employee.Id).ShouldHaveSingleItem();
        holder.PositionId.ShouldBe(oldPosition.Id);
        holder.EndDate.ShouldBe(OpenEnded);
        IsVacant(newPosition.Id).ShouldBeTrue();
        IsVacant(oldPosition.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Changing_The_Position_On_An_Assignment_Should_Move_The_Holder_Row()
    {
        SetEnablePosition(true);
        var employee = _data.CreateEmployee("POS006");
        var first = CreatePosition("P006A");
        var second = CreatePosition("P006B");
        await AssignAsync(employee.Id, Today.AddDays(-10), first.Id);

        await _appService.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            Id = AssignmentId(employee.Id, Today.AddDays(-10)),
            EmployeeId = employee.Id,
            StartDate = Today.AddDays(-10),
            PositionId = second.Id
        });

        Holders(employee.Id).ShouldHaveSingleItem().PositionId.ShouldBe(second.Id);
        IsVacant(first.Id).ShouldBeTrue();
        IsVacant(second.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_A_Holder_Beyond_The_Positions_FTE_Limit()
    {
        SetEnablePosition(true);
        var position = CreatePosition("P007", fteLimit: 1);
        var first = _data.CreateEmployee("POS007A");
        var second = _data.CreateEmployee("POS007B");
        await AssignAsync(first.Id, Today.AddDays(-10), position.Id);

        await Should.ThrowAsync<UserFriendlyException>(() => AssignAsync(second.Id, Today.AddDays(-3), position.Id));
    }

    [Fact]
    public async Task An_Employee_Who_Is_Leaving_Should_Hold_The_Position_Until_The_Last_Day_In_Service()
    {
        SetEnablePosition(true);
        var employee = _data.CreateEmployee("POS008");
        var position = CreatePosition("P008");
        var lastDay = Today.AddDays(20);
        UsingDbContext(context =>
        {
            var reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = TenantId, Code = "SR-POS8", NameEn = "Resigned", NameAr = "استقالة"
            }).Entity;
            context.SaveChanges();
            context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SubmissionDate = Today,
                LastDayInService = lastDay,
                PlannedLastDayInService = lastDay,
                SeparationType = SeparationType.Resignation,
                SeparationReasonId = reason.Id
            });
        });

        await AssignAsync(employee.Id, Today.AddDays(-10), position.Id);

        Holders(employee.Id).ShouldHaveSingleItem().EndDate.ShouldBe(lastDay);
    }

    [Fact]
    public async Task Choosing_A_Position_Should_Fill_The_Assignment_From_It()
    {
        SetEnablePosition(true);
        var employee = _data.CreateEmployee("POS009");
        JobTitle jobTitle = null;
        UsingDbContext(context =>
        {
            jobTitle = context.Set<JobTitle>().Add(new JobTitle
            {
                TenantId = TenantId, Code = "JT-POS9", NameEn = "Analyst", NameAr = "محلل"
            }).Entity;
        });
        var position = CreatePosition("P009", jobTitleId: jobTitle.Id);

        await AssignAsync(employee.Id, Today.AddDays(-10), position.Id);

        UsingDbContext(context => context.Set<EmployeeOrgAssignment>()
            .Single(a => a.EmployeeId == employee.Id).JobTitleId.ShouldBe(jobTitle.Id));
    }

    [Fact]
    public async Task The_Picker_Should_List_Vacant_Active_Positions_And_The_One_Already_Held()
    {
        SetEnablePosition(true);
        var employee = _data.CreateEmployee("POS010");
        var held = CreatePosition("P010A");
        var vacant = CreatePosition("P010B");
        await AssignAsync(employee.Id, Today.AddDays(-10), held.Id);

        var withoutCurrent = await _appService.GetPositionsForOrgAssignment(new GetPositionsForOrgAssignmentInput());
        var withCurrent = await _appService.GetPositionsForOrgAssignment(
            new GetPositionsForOrgAssignmentInput { CurrentPositionId = held.Id });

        withoutCurrent.Items.Select(p => p.Id).ShouldContain(vacant.Id);
        withoutCurrent.Items.Select(p => p.Id).ShouldNotContain(held.Id);
        withCurrent.Items.Select(p => p.Id).ShouldContain(held.Id);
    }
}
