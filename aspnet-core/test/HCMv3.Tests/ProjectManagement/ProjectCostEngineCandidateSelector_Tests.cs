using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Services;
using Shouldly;
using Xunit;

using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.ProjectManagement;

/// <summary>
/// Who the Project Cost Engine's employee picker lists -- legacy <c>uscProjectCosting.BuildEmployeePopupList</c>
/// (lines 1728-1958): employees in the payroll area on the period end, joined by then, ACTIVE; Processed when a
/// project register exists for that period, otherwise Pending.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProjectCostEngineCandidateSelector_Tests
{
    private const string Area = "PA";
    private static readonly DateTime January = new DateTime(2024, 1, 31);

    private readonly List<EmployeeOrgAssignment> _assignments = new List<EmployeeOrgAssignment>();
    private readonly List<Employee> _employees = new List<Employee>();
    private readonly List<ProjectRegister> _registers = new List<ProjectRegister>();

    private readonly PayrollArea _area = new PayrollArea { Id = 1, TenantId = 1, Code = Area, Name = Area };
    private readonly PayrollArea _otherArea = new PayrollArea { Id = 2, TenantId = 1, Code = "OTHER", Name = "OTHER" };

    private ProjectCostEngineCandidateSelector CreateSelector()
    {
        return new ProjectCostEngineCandidateSelector(
            Fakes.RepoOver(_assignments),
            Fakes.RepoOver(_employees),
            Fakes.RepoOver(_registers));
    }

    private void GivenEmployee(
        int id,
        string code,
        EmployeeStatus status = EmployeeStatus.Active,
        DateTime? joined = null,
        PayrollArea area = null,
        DateTime? assignmentStart = null,
        DateTime? assignmentEnd = null)
    {
        _employees.Add(new Employee
        {
            Id = id,
            TenantId = 1,
            Code = code,
            FullNameEn = code + " En",
            FullNameAr = code + " Ar",
            Status = status,
            JoiningDate = joined ?? new DateTime(2020, 1, 1)
        });

        var assignmentArea = area ?? _area;
        _assignments.Add(new EmployeeOrgAssignment
        {
            Id = _assignments.Count + 1,
            TenantId = 1,
            EmployeeId = id,
            PayrollAreaId = assignmentArea.Id,
            PayrollArea = assignmentArea,
            StartDate = assignmentStart ?? new DateTime(2020, 1, 1),
            EndDate = assignmentEnd ?? new DateTime(2030, 12, 31)
        });
    }

    [Fact]
    public async Task Should_List_Active_Employees_Of_The_Area_On_The_Period_End()
    {
        GivenEmployee(1, "E2");
        GivenEmployee(2, "E1");
        GivenEmployee(3, "X1", area: _otherArea);

        var candidates = await CreateSelector().GetCandidatesAsync(Area, January);

        candidates.Select(c => c.EmployeeCode).ShouldBe(new[] { "E1", "E2" });
        candidates[0].FullNameEn.ShouldBe("E1 En");
        candidates[0].FullNameAr.ShouldBe("E1 Ar");
    }

    /// <summary>Legacy line 1778 (20 July 2026): who had not joined by the period end is not offered.</summary>
    [Fact]
    public async Task Should_Leave_Out_Who_Joined_After_The_Period_End()
    {
        GivenEmployee(1, "E1");
        GivenEmployee(2, "E2", joined: new DateTime(2024, 2, 1));

        var candidates = await CreateSelector().GetCandidatesAsync(Area, January);

        candidates.Select(c => c.EmployeeCode).ShouldBe(new[] { "E1" });
    }

    /// <summary>Legacy lines 1812-1816: a regular run's eligible list is every ACTIVE employee.</summary>
    [Fact]
    public async Task Should_Leave_Out_Inactive_Employees()
    {
        GivenEmployee(1, "E1");
        GivenEmployee(2, "E2", status: EmployeeStatus.Inactive);

        var candidates = await CreateSelector().GetCandidatesAsync(Area, January);

        candidates.Select(c => c.EmployeeCode).ShouldBe(new[] { "E1" });
    }

    [Fact]
    public async Task Should_Leave_Out_Assignments_Not_Covering_The_Period_End()
    {
        GivenEmployee(1, "E1", assignmentEnd: new DateTime(2024, 1, 30));
        GivenEmployee(2, "E2", assignmentStart: new DateTime(2024, 2, 1));
        GivenEmployee(3, "E3", assignmentStart: January, assignmentEnd: January);

        var candidates = await CreateSelector().GetCandidatesAsync(Area, January);

        candidates.Select(c => c.EmployeeCode).ShouldBe(new[] { "E3" });
    }

    /// <summary>Legacy lines 1819-1826: Processed = a project register for this very period.</summary>
    [Fact]
    public async Task Should_Mark_Employees_With_A_Project_Register_For_The_Period_As_Processed()
    {
        GivenEmployee(1, "E1");
        GivenEmployee(2, "E2");
        _registers.Add(new ProjectRegister { Id = 1, TenantId = 1, EmployeeId = 1, Period = January });
        // A register for another period does not count.
        _registers.Add(new ProjectRegister { Id = 2, TenantId = 1, EmployeeId = 2, Period = new DateTime(2023, 12, 31) });

        var candidates = await CreateSelector().GetCandidatesAsync(Area, January);

        candidates.Single(c => c.EmployeeCode == "E1").IsProcessed.ShouldBeTrue();
        candidates.Single(c => c.EmployeeCode == "E2").IsProcessed.ShouldBeFalse();
    }

    /// <summary>Two assignments in the area covering the date still list the employee once.</summary>
    [Fact]
    public async Task Should_List_An_Employee_Once()
    {
        GivenEmployee(1, "E1");
        _assignments.Add(new EmployeeOrgAssignment
        {
            Id = 99, TenantId = 1, EmployeeId = 1, PayrollAreaId = 1, PayrollArea = _area,
            StartDate = new DateTime(2023, 1, 1), EndDate = new DateTime(2025, 1, 1)
        });

        var candidates = await CreateSelector().GetCandidatesAsync(Area, January);

        candidates.Count.ShouldBe(1);
    }

    /// <summary>The time of day on the period end is ignored, as legacy parsed txtEndDate as dd/MM/yyyy.</summary>
    [Fact]
    public async Task Should_Ignore_The_Time_Of_Day_On_The_Period_End()
    {
        GivenEmployee(1, "E1");
        _registers.Add(new ProjectRegister { Id = 1, TenantId = 1, EmployeeId = 1, Period = January });

        var candidates = await CreateSelector().GetCandidatesAsync(Area, January.AddHours(13));

        candidates.Single().IsProcessed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NO SUCH AREA")]
    public async Task Should_List_Nobody_Without_A_Known_Area(string area)
    {
        GivenEmployee(1, "E1");

        var candidates = await CreateSelector().GetCandidatesAsync(area, January);

        candidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_List_Nobody_Without_A_Period_End()
    {
        GivenEmployee(1, "E1");

        var candidates = await CreateSelector().GetCandidatesAsync(Area, default(DateTime));

        candidates.ShouldBeEmpty();
    }
}
