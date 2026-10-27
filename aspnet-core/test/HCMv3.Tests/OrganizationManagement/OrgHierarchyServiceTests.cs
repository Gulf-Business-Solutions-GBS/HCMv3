using System;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Services;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.OrganizationManagement;

public class OrgHierarchyServiceTests : AppTestBase
{
    private readonly IOrgHierarchyService _orgHierarchyService;

    public OrgHierarchyServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _orgHierarchyService = Resolve<IOrgHierarchyService>();
    }

    [Fact]
    public void Should_Return_Parent_Level_Code_For_Given_Level()
    {
        _orgHierarchyService.GetParentOrgUnitTypeCode(OrganizationUnitType.Level3Code)
            .ShouldBe(OrganizationUnitType.Level2Code);

        _orgHierarchyService.GetParentOrgUnitTypeCode(OrganizationUnitType.Level1Code)
            .ShouldBe(OrganizationUnitType.Level0Code);
    }

    [Fact]
    public void Should_Return_Empty_Parent_For_Root_Level()
    {
        _orgHierarchyService.GetParentOrgUnitTypeCode(OrganizationUnitType.Level0Code).ShouldBe(string.Empty);
    }

    [Fact]
    public void Should_Return_Empty_Parent_For_Unrecognized_Code()
    {
        _orgHierarchyService.GetParentOrgUnitTypeCode("NotALevel").ShouldBe(string.Empty);
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
    public async Task Should_Resolve_Ancestor_OrgUnit_At_Requested_Level_For_Employee()
    {
        var employee = CreateEmployee("ORG100");

        long companyOuId = 0, departmentOuId = 0;
        UsingDbContext(context =>
        {
            var company = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = "00001",
                DisplayName = "Company"
            }).Entity;
            context.SaveChanges();
            companyOuId = company.Id;

            var department = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = "00001.00002",
                DisplayName = "Department",
                ParentId = companyOuId
            }).Entity;
            context.SaveChanges();
            departmentOuId = department.Id;
        });

        int companyTypeId = 0, departmentTypeId = 0;
        UsingDbContext(context =>
        {
            var companyType = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = OrganizationUnitType.Level1Code,
                NameEn = "Company",
                NameAr = "Company"
            }).Entity;
            context.SaveChanges();
            companyTypeId = companyType.Id;

            var departmentType = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = OrganizationUnitType.Level2Code,
                NameEn = "Department",
                NameAr = "Department"
            }).Entity;
            context.SaveChanges();
            departmentTypeId = departmentType.Id;
        });

        UsingDbContext(context =>
        {
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = AbpSession.TenantId ?? 1,
                OrganizationUnitId = companyOuId,
                NameAr = "Company",
                OrganizationUnitTypeId = companyTypeId
            });

            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = AbpSession.TenantId ?? 1,
                OrganizationUnitId = departmentOuId,
                NameAr = "Department",
                OrganizationUnitTypeId = departmentTypeId
            });
        });

        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employee.Id,
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(2099, 12, 31),
                OrganizationUnitId = departmentOuId
            });
        });

        var currentOrgUnitId = await _orgHierarchyService.GetCurrentOrgUnitIdForEmployeeAsync(employee.Id);
        currentOrgUnitId.ShouldBe(departmentOuId);

        var companyLevelOrgUnitId = await _orgHierarchyService.GetOrgUnitAtLevelForEmployeeAsync(employee.Id, OrganizationUnitType.Level1Code);
        companyLevelOrgUnitId.ShouldBe(companyOuId);

        var departmentLevelOrgUnitId = await _orgHierarchyService.GetOrgUnitAtLevelForEmployeeAsync(employee.Id, OrganizationUnitType.Level2Code);
        departmentLevelOrgUnitId.ShouldBe(departmentOuId);
    }

    [Fact]
    public async Task Should_Return_Null_When_Employee_Has_No_Org_Assignment()
    {
        var employee = CreateEmployee("ORG200");

        var result = await _orgHierarchyService.GetCurrentOrgUnitIdForEmployeeAsync(employee.Id);

        result.ShouldBeNull();
    }
}
