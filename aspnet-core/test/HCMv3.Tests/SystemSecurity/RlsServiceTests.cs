using System;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity.Services;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

public class RlsServiceTests : AppTestBase
{
    private readonly IRlsService _rlsService;

    public RlsServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _rlsService = Resolve<IRlsService>();
    }

    private Employee CreateEmployee(string code)
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
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    [Fact]
    public async Task Should_Return_Empty_RlsEmployeeIds_When_RowLevelSecurity_Disabled()
    {
        var ids = await _rlsService.GetRlsEmployeeIdsAsync();

        ids.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Empty_RlsEmployeeIds_When_Settings_Explicitly_Disable_It()
    {
        UsingDbContext(context =>
        {
            context.Set<ManageSecuritySettings>().Add(new ManageSecuritySettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EnableRowLevelSecurity = false
            });
        });

        var ids = await _rlsService.GetRlsEmployeeIdsAsync();

        ids.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Resolve_Employees_Under_Manager_Recursively()
    {
        var ceo = CreateEmployee("MGR100");
        var manager = CreateEmployee("MGR101");
        var staff = CreateEmployee("MGR102");

        UsingDbContext(context =>
        {
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = manager.Id,
                ManagerId = ceo.Id,
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(2099, 12, 31)
            });

            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = staff.Id,
                ManagerId = manager.Id,
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(2099, 12, 31)
            });
        });

        var result = await _rlsService.GetEmployeeIdsUnderManagerAsync(ceo.Id);

        result.ShouldContain(ceo.Id);
        result.ShouldContain(manager.Id);
        result.ShouldContain(staff.Id);
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Return_Only_Self_When_Employee_Has_No_Subordinates()
    {
        var employee = CreateEmployee("MGR200");

        var result = await _rlsService.GetEmployeeIdsUnderManagerAsync(employee.Id);

        result.ShouldBe(new[] { employee.Id });
    }

    [Fact]
    public async Task Should_Resolve_OrgUnit_Descendants_Via_Hierarchical_Code_Prefix()
    {
        long rootId = 0, childId = 0, grandChildId = 0;
        UsingDbContext(context =>
        {
            var root = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = "00001",
                DisplayName = "Root"
            }).Entity;
            context.SaveChanges();
            rootId = root.Id;

            var child = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = "00001.00002",
                DisplayName = "Child",
                ParentId = rootId
            }).Entity;
            context.SaveChanges();
            childId = child.Id;

            var grandChild = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = "00001.00002.00003",
                DisplayName = "GrandChild",
                ParentId = childId
            }).Entity;
            context.SaveChanges();
            grandChildId = grandChild.Id;
        });

        var descendants = await _rlsService.GetOrgUnitDescendantIdsAsync(rootId);

        descendants.ShouldContain(rootId);
        descendants.ShouldContain(childId);
        descendants.ShouldContain(grandChildId);
    }

    [Fact]
    public async Task Should_Resolve_Only_Self_And_Own_Subtree_For_Middle_Node()
    {
        long rootId = 0, childId = 0, grandChildId = 0;
        UsingDbContext(context =>
        {
            var root = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = "00010",
                DisplayName = "Root"
            }).Entity;
            context.SaveChanges();
            rootId = root.Id;

            var child = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = "00010.00011",
                DisplayName = "Child",
                ParentId = rootId
            }).Entity;
            context.SaveChanges();
            childId = child.Id;

            var grandChild = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = "00010.00011.00012",
                DisplayName = "GrandChild",
                ParentId = childId
            }).Entity;
            context.SaveChanges();
            grandChildId = grandChild.Id;
        });

        var descendants = await _rlsService.GetOrgUnitDescendantIdsAsync(childId);

        descendants.ShouldNotContain(rootId);
        descendants.ShouldContain(childId);
        descendants.ShouldContain(grandChildId);
    }
}
