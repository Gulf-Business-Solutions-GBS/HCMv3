using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Organizations;
using HCMv3.Authorization.Roles;
using HCMv3.Dashboards;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using NSubstitute;
using Shouldly;
using Xunit;

using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// The one "whose data may this user see" rule shared by every dashboard. The org-unit and manager halves come
/// from <see cref="IRlsService"/> (substituted here); what is tested is how they are combined, and the cases where
/// the answer must be everyone or nobody.
/// </summary>
public class DashboardScopeService_Tests
{
    private const long UserId = 1;
    private const int AdminRoleId = 10;
    private const int HrRoleId = 11;

    private readonly IRlsService _rlsService = Substitute.For<IRlsService>();
    private readonly IEmployeeSessionService _employeeSession = Substitute.For<IEmployeeSessionService>();

    private readonly List<UserRole> _userRoles = new();
    private readonly List<Role> _roles = new()
    {
        new Role(1, StaticRoleNames.Tenants.Admin, "Admin") { Id = AdminRoleId },
        new Role(1, "HR", "HR") { Id = HrRoleId }
    };
    private readonly List<SysUserOrgUnitData> _roleOrgUnits = new();

    public DashboardScopeService_Tests()
    {
        _rlsService.GetEmployeeIdsUnderOrgUnitForCurrentUserAsync().Returns(Task.FromResult(new List<int>()));
        _employeeSession.GetCurrentEmployeeAsync().Returns(Task.FromResult<Employee>(null));
    }

    private void GiveRole(int roleId) => _userRoles.Add(new UserRole(1, UserId, roleId) { Id = _userRoles.Count + 1 });

    private void GiveRoleOrgUnit(int roleId, string unitName, params int[] employeesInIt)
    {
        _roleOrgUnits.Add(new SysUserOrgUnitData
        {
            Id = _roleOrgUnits.Count + 1,
            RoleId = roleId,
            OrganizationUnit = new OrganizationUnit(1, unitName)
        });
        _rlsService.GetEmployeeIdsUnderOrgUnitForCurrentUserAsync().Returns(Task.FromResult(employeesInIt.ToList()));
    }

    private void LinkToEmployee(int employeeId, params int[] team)
    {
        _employeeSession.GetCurrentEmployeeAsync().Returns(Task.FromResult(new Employee { Id = employeeId }));
        _rlsService.GetEmployeeIdsUnderManagerAsync(employeeId).Returns(Task.FromResult(team.Append(employeeId).ToList()));
    }

    private DashboardScopeService CreateService()
    {
        var userRoles = Substitute.For<IRepository<UserRole, long>>();
        userRoles.GetAll().Returns(_ => _userRoles.AsQueryable());

        return new DashboardScopeService(
            _rlsService,
            _employeeSession,
            userRoles,
            Fakes.RepoOver(_roles),
            Fakes.RepoOver(_roleOrgUnits),
            Fakes.Session());
    }

    /// <summary>
    /// Nothing assigned means nothing shown -- whatever the row-level security switch in Security Settings says,
    /// which this service does not even read.
    /// </summary>
    [Fact]
    public async Task Should_Show_Nobody_To_A_User_With_No_Roles_And_No_Employee_Record()
    {
        var scope = await CreateService().GetScopeAsync();

        scope.IsEveryone.ShouldBeFalse();
        scope.Reason.ShouldBe(DashboardScopeReason.Nobody);
    }

    [Fact]
    public async Task Should_Show_Everyone_To_The_Admin_Role_Even_With_Row_Level_Security_On()
    {
        GiveRole(AdminRoleId);

        var scope = await CreateService().GetScopeAsync();

        scope.IsEveryone.ShouldBeTrue();
        scope.Reason.ShouldBe(DashboardScopeReason.Admin);
    }

    [Fact]
    public async Task Should_Show_The_Org_Units_Given_To_The_Users_Roles()
    {
        GiveRole(HrRoleId);
        GiveRoleOrgUnit(HrRoleId, "Finance", 5, 6);

        var scope = await CreateService().GetScopeAsync();

        scope.Reason.ShouldBe(DashboardScopeReason.Restricted);
        scope.EmployeeIds.ShouldBe(new[] { 5, 6 }, ignoreOrder: true);
        scope.OrgUnitNames.ShouldBe(new[] { "Finance" });
        scope.IncludesOwnTeam.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Show_A_Linked_Manager_Their_Team_Including_Themselves()
    {
        LinkToEmployee(7, 8, 9);

        var scope = await CreateService().GetScopeAsync();

        scope.Reason.ShouldBe(DashboardScopeReason.Restricted);
        scope.EmployeeIds.ShouldBe(new[] { 7, 8, 9 }, ignoreOrder: true);
        scope.IncludesOwnTeam.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Combine_The_Role_Org_Units_And_The_Own_Team()
    {
        GiveRole(HrRoleId);
        GiveRoleOrgUnit(HrRoleId, "Finance", 5, 6);
        LinkToEmployee(7, 6, 8);

        var scope = await CreateService().GetScopeAsync();

        scope.EmployeeIds.ShouldBe(new[] { 5, 6, 7, 8 }, ignoreOrder: true);
        scope.OrgUnitNames.ShouldBe(new[] { "Finance" });
        scope.IncludesOwnTeam.ShouldBeTrue();
    }

    /// <summary>
    /// The gap in <c>IRlsService.GetRlsEmployeeIdsAsync</c>, whose callers read an empty list as "no restriction":
    /// a dashboard must show nobody here, not everybody.
    /// </summary>
    [Fact]
    public async Task Should_Show_Nobody_When_Row_Level_Security_Resolves_To_Nobody()
    {
        GiveRole(HrRoleId);

        var scope = await CreateService().GetScopeAsync();

        scope.IsEveryone.ShouldBeFalse();
        scope.Reason.ShouldBe(DashboardScopeReason.Nobody);
        scope.EmployeeIds.ShouldBeEmpty();
        scope.Includes(5).ShouldBeFalse();
    }
}
