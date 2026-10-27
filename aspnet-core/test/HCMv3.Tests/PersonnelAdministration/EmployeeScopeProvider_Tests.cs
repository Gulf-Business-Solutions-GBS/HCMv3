using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeScopeProvider_Tests : AppTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IEmployeeScopeProvider _employeeScopeProvider;

    public EmployeeScopeProvider_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _employeeScopeProvider = Resolve<IEmployeeScopeProvider>();
    }

    private int CreateNationality()
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        return nationality.Id;
    }

    private Employee CreateEmployee(string code, long? userId = null)
    {
        var nationalityId = CreateNationality();

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,
                NationalityId = nationalityId
            }).Entity;
        });

        return employee;
    }

    /// <summary>Creates the employee record for the currently logged-in tenant admin, linked by UserId.</summary>
    private Employee CreateCurrentUserEmployee(string code)
    {
        return CreateEmployee(code, AbpSession.UserId);
    }

    private void CreateManagerAssignment(int employeeId, int managerId, DateTime? startDate = null, DateTime? endDate = null)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                ManagerId = managerId,
                StartDate = startDate ?? new DateTime(2020, 1, 1),
                EndDate = endDate ?? InfiniteEndDate
            });
        });
    }

    private void CreateAbsence(int employeeId)
    {
        UsingDbContext(context =>
        {
            var absenceType = context.Set<AbsenceType>().FirstOrDefault();
            if (absenceType == null)
            {
                absenceType = context.Set<AbsenceType>().Add(new AbsenceType
                {
                    TenantId = AbpSession.TenantId ?? 1,
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                    NameEn = "Annual",
                    NameAr = "سنوي"
                }).Entity;

                context.SaveChanges();
            }

            context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceType.Id,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 5)
            });
        });
    }

    [Fact]
    public async Task Should_Resolve_Current_Employee_Id_From_The_UserId_Link()
    {
        var me = CreateCurrentUserEmployee("SCOPE100");

        var employeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();

        employeeId.ShouldBe(me.Id);
    }

    [Fact]
    public async Task Should_Return_Null_Current_Employee_Id_When_The_User_Is_Not_Linked()
    {
        var employeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();

        employeeId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Throw_A_Friendly_Error_When_A_Required_Employee_Link_Is_Missing()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync());
    }

    [Fact]
    public async Task Self_Scope_Should_Contain_Only_The_Current_Employee()
    {
        var me = CreateCurrentUserEmployee("SCOPE200");
        CreateEmployee("SCOPE200OTHER");

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.Self);

        filter.IsUnrestricted.ShouldBeFalse();
        filter.EmployeeIds.ShouldBe(new[] { me.Id });
    }

    [Fact]
    public async Task DirectReports_Scope_Should_Exclude_The_Manager_And_Indirect_Reports()
    {
        var me = CreateCurrentUserEmployee("SCOPE300");
        var report = CreateEmployee("SCOPE300REPORT");
        var grandReport = CreateEmployee("SCOPE300GRAND");

        CreateManagerAssignment(report.Id, me.Id);
        CreateManagerAssignment(grandReport.Id, report.Id);

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.DirectReports);

        filter.Includes(report.Id).ShouldBeTrue();
        filter.Includes(me.Id).ShouldBeFalse();
        filter.Includes(grandReport.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Team_Scope_Should_Contain_Self_Plus_Direct_Reports()
    {
        var me = CreateCurrentUserEmployee("SCOPE400");
        var report = CreateEmployee("SCOPE400REPORT");
        var stranger = CreateEmployee("SCOPE400STRANGER");

        CreateManagerAssignment(report.Id, me.Id);

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.Team);

        filter.Includes(me.Id).ShouldBeTrue();
        filter.Includes(report.Id).ShouldBeTrue();
        filter.Includes(stranger.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Subordinates_Scope_Should_Walk_The_Whole_Tree()
    {
        var me = CreateCurrentUserEmployee("SCOPE500");
        var report = CreateEmployee("SCOPE500REPORT");
        var grandReport = CreateEmployee("SCOPE500GRAND");
        var stranger = CreateEmployee("SCOPE500STRANGER");

        CreateManagerAssignment(report.Id, me.Id);
        CreateManagerAssignment(grandReport.Id, report.Id);

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.Subordinates);

        filter.Includes(me.Id).ShouldBeTrue();
        filter.Includes(report.Id).ShouldBeTrue();
        filter.Includes(grandReport.Id).ShouldBeTrue();
        filter.Includes(stranger.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Ignore_Manager_Assignments_That_Are_Not_Current()
    {
        var me = CreateCurrentUserEmployee("SCOPE600");
        var formerReport = CreateEmployee("SCOPE600FORMER");

        CreateManagerAssignment(formerReport.Id, me.Id,
            startDate: new DateTime(2020, 1, 1),
            endDate: new DateTime(2021, 1, 1));

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.DirectReports);

        filter.Includes(formerReport.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Honour_An_Explicit_AsOf_Date()
    {
        var me = CreateCurrentUserEmployee("SCOPE650");
        var formerReport = CreateEmployee("SCOPE650FORMER");

        CreateManagerAssignment(formerReport.Id, me.Id,
            startDate: new DateTime(2020, 1, 1),
            endDate: new DateTime(2021, 1, 1));

        var filter = await _employeeScopeProvider.GetScopeAsync(
            EmployeeScope.DirectReports, new DateTime(2020, 6, 1));

        filter.Includes(formerReport.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Resolve_To_Empty_Rather_Than_Unrestricted_When_The_User_Is_Not_Linked()
    {
        var someoneElse = CreateEmployee("SCOPE700");

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.Team);

        filter.IsUnrestricted.ShouldBeFalse();
        filter.IsEmpty.ShouldBeTrue();
        filter.Includes(someoneElse.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task All_Scope_Should_Be_Unrestricted_Even_Without_A_Linked_Employee()
    {
        var someoneElse = CreateEmployee("SCOPE800");

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.All);

        filter.IsUnrestricted.ShouldBeTrue();
        filter.Includes(someoneElse.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Filter_An_Employee_Owned_Query_Down_To_The_Scope()
    {
        var me = CreateCurrentUserEmployee("SCOPE900");
        var stranger = CreateEmployee("SCOPE900STRANGER");

        CreateAbsence(me.Id);
        CreateAbsence(stranger.Id);

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.Self);

        var employeeIds = UsingDbContext(context =>
            filter.Apply(context.Set<EmployeeAbsence>().AsQueryable())
                .Select(a => a.EmployeeId)
                .ToList());

        employeeIds.ShouldAllBe(id => id == me.Id);
        employeeIds.ShouldNotContain(stranger.Id);
    }

    [Fact]
    public async Task Unrestricted_Filter_Should_Leave_A_Query_Untouched()
    {
        var me = CreateCurrentUserEmployee("SCOPE950");
        var stranger = CreateEmployee("SCOPE950STRANGER");

        CreateAbsence(me.Id);
        CreateAbsence(stranger.Id);

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.All);

        var employeeIds = UsingDbContext(context =>
            filter.Apply(context.Set<EmployeeAbsence>().AsQueryable())
                .Select(a => a.EmployeeId)
                .ToList());

        employeeIds.ShouldContain(me.Id);
        employeeIds.ShouldContain(stranger.Id);
    }

    [Fact]
    public async Task EnsureInScope_Should_Throw_For_An_Employee_Outside_The_Scope()
    {
        CreateCurrentUserEmployee("SCOPE1000");
        var stranger = CreateEmployee("SCOPE1000STRANGER");

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _employeeScopeProvider.EnsureInScopeAsync(stranger.Id, EmployeeScope.Self));
    }

    [Fact]
    public async Task EnsureInScope_Should_Pass_For_An_Employee_Inside_The_Scope()
    {
        var me = CreateCurrentUserEmployee("SCOPE1100");

        await _employeeScopeProvider.EnsureInScopeAsync(me.Id, EmployeeScope.Self);

        (await _employeeScopeProvider.IsInScopeAsync(me.Id, EmployeeScope.Self)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Treat_A_Self_Referencing_Manager_Row_As_A_Direct_Report()
    {
        var me = CreateCurrentUserEmployee("SCOPE1200");

        // Legacy data sets contain rows where an employee is recorded as their own manager.
        CreateManagerAssignment(me.Id, me.Id);

        var filter = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.DirectReports);

        filter.IsEmpty.ShouldBeTrue();
    }
}
