using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Tests for <see cref="RosterScheduleViewerAppService"/> — the read-only port of legacy
/// <c>uscRosterDutySchedule.ascx.cs</c> (<c>FillEmployeeGrid</c> + <c>BindSchedular</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class RosterScheduleViewerAppService_Tests : AppTestBase
{
    private readonly IRosterScheduleViewerAppService _appService;
    private readonly int _tenantId;

    public RosterScheduleViewerAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _tenantId = AbpSession.TenantId ?? 1;
        _appService = Resolve<IRosterScheduleViewerAppService>();
    }

    #region Fixtures

    private int CreateEmployee(string code, string name)
    {
        return UsingDbContext(context =>
        {
            var nationality = new Nationality { TenantId = _tenantId, NameEn = "Local", NameAr = "محلي" };
            context.Set<Nationality>().Add(nationality);
            context.SaveChanges();

            var employee = new Employee
            {
                TenantId = _tenantId,
                Code = code,
                FirstNameEn = name,
                LastNameEn = "Doe",
                FirstNameAr = name,
                LastNameAr = "Doe",
                FullNameEn = name,
                FullNameAr = name,
                NationalityId = nationality.Id
            };
            context.Set<Employee>().Add(employee);
            context.SaveChanges();
            return employee.Id;
        });
    }

    private int CreateDuty(string code, TimeSpan start, TimeSpan end, bool isWeeklyOff = false)
    {
        return UsingDbContext(context =>
        {
            var duty = new Duty
            {
                TenantId = _tenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                StartTime = start,
                EndTime = end,
                IsWeeklyOff = isWeeklyOff
            };
            context.Set<Duty>().Add(duty);
            context.SaveChanges();
            return duty.Id;
        });
    }

    /// <summary>Classic duty period with the same duty on every day of week.</summary>
    private int CreateDutyPeriod(string name, int dutyId)
    {
        return UsingDbContext(context =>
        {
            var dp = new DutyPeriod
            {
                TenantId = _tenantId,
                Code = name,
                NameEn = name,
                NameAr = name,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                SundayDutyId = dutyId,
                MondayDutyId = dutyId,
                TuesdayDutyId = dutyId,
                WednesdayDutyId = dutyId,
                ThursdayDutyId = dutyId,
                FridayDutyId = dutyId,
                SaturdayDutyId = dutyId
            };
            context.Set<DutyPeriod>().Add(dp);
            context.SaveChanges();
            return dp.Id;
        });
    }

    private void AssignEmployeeDutyPeriod(int employeeId, int dutyPeriodId, DateTime from, DateTime to, int startPoint = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = _tenantId,
                EmployeeId = employeeId,
                DutyPeriodId = dutyPeriodId,
                StartDate = from,
                EndDate = to,
                StartPoint = startPoint
            });
            context.SaveChanges();
        });
    }

    private int CreateRosterGroupWithMember(string groupName, int employeeId, int dutyPeriodId, DateTime from, DateTime to)
    {
        return UsingDbContext(context =>
        {
            var group = new RosterGroup
            {
                TenantId = _tenantId,
                Code = groupName,
                NameEn = groupName,
                NameAr = groupName
            };
            context.Set<RosterGroup>().Add(group);
            context.SaveChanges();

            context.Set<RosterGroupDetail>().Add(new RosterGroupDetail
            {
                TenantId = _tenantId,
                RosterGroupId = group.Id,
                EmployeeId = employeeId,
                StartDate = from,
                EndDate = to
            });

            context.Set<RosterGroupSchedule>().Add(new RosterGroupSchedule
            {
                TenantId = _tenantId,
                RosterGroupId = group.Id,
                DutyPeriodId = dutyPeriodId,
                StartDate = from,
                EndDate = to,
                StartPoint = 1
            });

            context.SaveChanges();
            return group.Id;
        });
    }

    #endregion

    #region GetRosterDutyScheduleEmployees

    [Fact]
    public async Task GetRosterDutyScheduleEmployees_Should_Return_Directly_Assigned_Employees()
    {
        var dutyId = CreateDuty("D1", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
        var dutyPeriodId = CreateDutyPeriod("DP1", dutyId);
        var employeeId = CreateEmployee("E001", "Alice");
        AssignEmployeeDutyPeriod(employeeId, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetRosterDutyScheduleEmployees(new GetRosterDutyScheduleEmployeesInput
        {
            DutyPeriodId = dutyPeriodId,
            AsOfDate = new DateTime(2024, 6, 1)
        });

        result.TotalCount.ShouldBe(1);
        var row = result.Items.Single();
        row.Id.ShouldBe(employeeId);
        row.Code.ShouldBe("E001");
        row.Name.ShouldBe("Alice");
        row.AssignmentSource.ShouldBe("EmployeeDutyPeriod");
        row.RosterGroupName.ShouldBeNull();
    }

    /// <summary>
    /// Legacy's roster-group branch was dead (unsatisfiable predicate) and its result was discarded by
    /// <c>ds1.Union(ds2);</c>. This asserts the corrected behaviour.
    /// </summary>
    [Fact]
    public async Task GetRosterDutyScheduleEmployees_Should_Return_RosterGroup_Members()
    {
        var dutyId = CreateDuty("D2", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
        var dutyPeriodId = CreateDutyPeriod("DP2", dutyId);
        var employeeId = CreateEmployee("E002", "Bob");
        CreateRosterGroupWithMember("GRP-A", employeeId, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetRosterDutyScheduleEmployees(new GetRosterDutyScheduleEmployeesInput
        {
            DutyPeriodId = dutyPeriodId,
            AsOfDate = new DateTime(2024, 6, 1)
        });

        result.TotalCount.ShouldBe(1);
        var row = result.Items.Single();
        row.Id.ShouldBe(employeeId);
        row.AssignmentSource.ShouldBe("RosterGroup");
        row.RosterGroupName.ShouldBe("GRP-A");
    }

    [Fact]
    public async Task GetRosterDutyScheduleEmployees_Should_Union_Both_Branches_Without_Duplicates()
    {
        var dutyId = CreateDuty("D3", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
        var dutyPeriodId = CreateDutyPeriod("DP3", dutyId);

        var directOnly = CreateEmployee("E010", "Direct");
        var groupOnly = CreateEmployee("E011", "Grouped");
        var both = CreateEmployee("E012", "Both");

        AssignEmployeeDutyPeriod(directOnly, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        AssignEmployeeDutyPeriod(both, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateRosterGroupWithMember("GRP-B", groupOnly, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateRosterGroupWithMember("GRP-C", both, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetRosterDutyScheduleEmployees(new GetRosterDutyScheduleEmployeesInput
        {
            DutyPeriodId = dutyPeriodId,
            AsOfDate = new DateTime(2024, 6, 1)
        });

        result.TotalCount.ShouldBe(3);
        result.Items.Select(i => i.Id).OrderBy(i => i)
            .ShouldBe(new[] { directOnly, groupOnly, both }.OrderBy(i => i));

        // Direct assignment wins the provenance label when an employee is reachable both ways.
        result.Items.Single(i => i.Id == both).AssignmentSource.ShouldBe("EmployeeDutyPeriod");
    }

    [Fact]
    public async Task GetRosterDutyScheduleEmployees_Should_Exclude_Assignments_Outside_AsOfDate()
    {
        var dutyId = CreateDuty("D4", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
        var dutyPeriodId = CreateDutyPeriod("DP4", dutyId);
        var employeeId = CreateEmployee("E020", "Expired");
        AssignEmployeeDutyPeriod(employeeId, dutyPeriodId, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31));

        var result = await _appService.GetRosterDutyScheduleEmployees(new GetRosterDutyScheduleEmployeesInput
        {
            DutyPeriodId = dutyPeriodId,
            AsOfDate = new DateTime(2024, 6, 1)
        });

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRosterDutyScheduleEmployees_Should_Apply_Filter()
    {
        var dutyId = CreateDuty("D5", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
        var dutyPeriodId = CreateDutyPeriod("DP5", dutyId);
        var alice = CreateEmployee("E030", "Alice");
        var bob = CreateEmployee("E031", "Bob");
        AssignEmployeeDutyPeriod(alice, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        AssignEmployeeDutyPeriod(bob, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetRosterDutyScheduleEmployees(new GetRosterDutyScheduleEmployeesInput
        {
            DutyPeriodId = dutyPeriodId,
            AsOfDate = new DateTime(2024, 6, 1),
            Filter = "Alice"
        });

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Id.ShouldBe(alice);
    }

    [Fact]
    public async Task GetRosterDutyScheduleEmployees_Should_Resolve_Location_From_OrgAssignment()
    {
        var dutyId = CreateDuty("D6", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
        var dutyPeriodId = CreateDutyPeriod("DP6", dutyId);
        var employeeId = CreateEmployee("E040", "Located");
        AssignEmployeeDutyPeriod(employeeId, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        UsingDbContext(context =>
        {
            var location = new WorkLocation
            {
                TenantId = _tenantId,
                Code = "HQ",
                NameEn = "Head Office",
                NameAr = "Head Office"
            };
            context.Set<WorkLocation>().Add(location);
            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = _tenantId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                WorkLocationId = location.Id
            });
            context.SaveChanges();
        });

        var result = await _appService.GetRosterDutyScheduleEmployees(new GetRosterDutyScheduleEmployeesInput
        {
            DutyPeriodId = dutyPeriodId,
            AsOfDate = new DateTime(2024, 6, 1)
        });

        result.Items.Single().Location.ShouldBe("Head Office");
    }

    #endregion

    #region GetRosterDutySchedule

    [Fact]
    public async Task GetRosterDutySchedule_Should_Return_Empty_When_No_Employees_Selected()
    {
        var result = await _appService.GetRosterDutySchedule(new GetRosterDutyScheduleInput
        {
            EmployeeIds = new List<int>(),
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 7)
        });

        result.Resources.ShouldBeEmpty();
        result.Days.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRosterDutySchedule_Should_Resolve_A_Cell_For_Every_Day_In_Range()
    {
        var dutyId = CreateDuty("DAY", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
        var dutyPeriodId = CreateDutyPeriod("DP7", dutyId);
        var employeeId = CreateEmployee("E050", "Scheduled");
        AssignEmployeeDutyPeriod(employeeId, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetRosterDutySchedule(new GetRosterDutyScheduleInput
        {
            EmployeeIds = new List<int> { employeeId },
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 7)
        });

        result.Resources.Single().Id.ShouldBe(employeeId);
        result.Days.Count.ShouldBe(7);
        result.Days.Select(d => d.DayDate).ShouldBeUnique();

        foreach (var day in result.Days)
        {
            day.EmployeeId.ShouldBe(employeeId);
            day.DutyId.ShouldBe(dutyId);
            day.DutyName.ShouldBe("DAY");
            day.StartTime.ShouldBe(new TimeSpan(8, 0, 0));
            day.EndTime.ShouldBe(new TimeSpan(17, 0, 0));
            day.ErrorMessage.ShouldBeNull();
        }
    }

    /// <summary>
    /// Regression guard for the legacy infinite loop: legacy advanced the day cursor only inside
    /// <c>if (duty != null)</c>, so an unresolvable day hung the request. Here the cursor always advances
    /// and the unresolved day is simply returned with a null DutyId.
    /// </summary>
    [Fact]
    public async Task GetRosterDutySchedule_Should_Return_Empty_Cells_When_No_Duty_Resolves()
    {
        var employeeId = CreateEmployee("E060", "Unassigned");

        var result = await _appService.GetRosterDutySchedule(new GetRosterDutyScheduleInput
        {
            EmployeeIds = new List<int> { employeeId },
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 5)
        });

        result.Days.Count.ShouldBe(5);
        result.Days.ShouldAllBe(d => d.DutyId == null);
        result.Days.ShouldAllBe(d => d.DutyName == null);
    }

    [Fact]
    public async Task GetRosterDutySchedule_Should_Carry_Duty_Flags()
    {
        var offDutyId = CreateDuty("OFF", TimeSpan.Zero, TimeSpan.Zero, isWeeklyOff: true);
        var dutyPeriodId = CreateDutyPeriod("DP8", offDutyId);
        var employeeId = CreateEmployee("E070", "OffDuty");
        AssignEmployeeDutyPeriod(employeeId, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetRosterDutySchedule(new GetRosterDutyScheduleInput
        {
            EmployeeIds = new List<int> { employeeId },
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 1)
        });

        result.Days.Single().IsWeeklyOff.ShouldBeTrue();
    }

    [Fact]
    public async Task GetRosterDutySchedule_Should_Resolve_Duty_Via_RosterGroup_Rotation()
    {
        var rotationDutyId = CreateDuty("ROT", new TimeSpan(20, 0, 0), new TimeSpan(4, 0, 0));
        var employeeId = CreateEmployee("E080", "Rotating");

        var dutyPeriodId = UsingDbContext(context =>
        {
            var dp = new DutyPeriod
            {
                TenantId = _tenantId,
                Code = "ROT-DP",
                NameEn = "ROT-DP",
                NameAr = "ROT-DP",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            };
            context.Set<DutyPeriod>().Add(dp);
            context.SaveChanges();

            context.Set<DutyPeriodDetail>().Add(new DutyPeriodDetail
            {
                TenantId = _tenantId,
                DutyPeriodId = dp.Id,
                Week = 1,
                Day1DutyId = rotationDutyId,
                Day2DutyId = rotationDutyId,
                Day3DutyId = rotationDutyId,
                Day4DutyId = rotationDutyId,
                Day5DutyId = rotationDutyId,
                Day6DutyId = rotationDutyId,
                Day7DutyId = rotationDutyId
            });
            context.SaveChanges();
            return dp.Id;
        });

        CreateRosterGroupWithMember("GRP-ROT", employeeId, dutyPeriodId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetRosterDutySchedule(new GetRosterDutyScheduleInput
        {
            EmployeeIds = new List<int> { employeeId },
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 3)
        });

        result.Days.Count.ShouldBe(3);
        result.Days.ShouldAllBe(d => d.DutyId == rotationDutyId);
    }

    [Fact]
    public async Task GetRosterDutySchedule_Should_Reject_Inverted_Date_Range()
    {
        var employeeId = CreateEmployee("E090", "Inverted");

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.GetRosterDutySchedule(new GetRosterDutyScheduleInput
            {
                EmployeeIds = new List<int> { employeeId },
                StartDate = new DateTime(2024, 6, 10),
                EndDate = new DateTime(2024, 6, 1)
            }));
    }

    [Fact]
    public async Task GetRosterDutySchedule_Should_Reject_Oversized_Date_Range()
    {
        var employeeId = CreateEmployee("E091", "TooLong");

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.GetRosterDutySchedule(new GetRosterDutyScheduleInput
            {
                EmployeeIds = new List<int> { employeeId },
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }

    #endregion
}
