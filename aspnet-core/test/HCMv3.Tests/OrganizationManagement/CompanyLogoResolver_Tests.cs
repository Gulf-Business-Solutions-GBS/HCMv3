using System;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Services;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.OrganizationManagement;

/// <summary>
/// <see cref="ICompanyLogoResolver"/>: which unit's logo a printed report shows, decided as the legacy report SQL
/// decided it (<c>CASE WHEN Company_1.OID IS NOT NULL THEN Company_1.logo WHEN Company_2.OID ... END</c>, OrgLevel1
/// being the top of the tree), with the logo read from the tenant file store.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CompanyLogoResolver_Tests : AppTestBase
{
    private readonly ICompanyLogoResolver _resolver;
    private readonly IEntityImageStore _images;

    public CompanyLogoResolver_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _resolver = Resolve<ICompanyLogoResolver>();
        _images = Resolve<IEntityImageStore>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateUnitType(string nameEn, string code = OrganizationUnitType.Level2Code)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var type = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
            context.SaveChanges();
            id = type.Id;
        });

        return id;
    }

    /// <summary>A unit with an explicit Zero tree code, its HCMv3 profile, type and (optional) logo path.</summary>
    private long CreateUnit(string code, int unitTypeId, string logoPath = null)
    {
        long id = 0;
        UsingDbContext(context =>
        {
            var unit = context.OrganizationUnits.Add(new OrganizationUnit(TenantId, "OU " + code) { Code = code }).Entity;
            context.SaveChanges();
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = TenantId,
                OrganizationUnitId = unit.Id,
                Code = "P" + unit.Id,
                NameAr = "OU " + code,
                OrganizationUnitTypeId = unitTypeId,
                LogoPath = logoPath
            });
            context.SaveChanges();
            id = unit.Id;
        });

        return id;
    }

    /// <summary>A root segment no seeded unit uses, so each test owns its own little tree.</summary>
    private static string NewRoot() => Random.Shared.Next(60000, 99999).ToString();

    [Fact]
    public void Should_List_A_Units_Own_Code_And_Every_Ancestor_Code()
    {
        CompanyLogoResolver.AncestorOrSelfCodes("00001.00002.00003")
            .ShouldBe(new[] { "00001", "00001.00002", "00001.00002.00003" });
    }

    [Fact]
    public async Task Should_Return_The_Logo_Of_The_Company_Above_The_Employees_Unit()
    {
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var department = CreateUnitType("Department");
        var root = NewRoot();
        CreateUnit(root, company, "tenant-1/org-unit-logos/company.img");
        var employeeUnit = CreateUnit(root + ".00001", department, "tenant-1/org-unit-logos/department.img");

        (await _resolver.GetEmployeeCompanyLogoPathAsync(employeeUnit)).ShouldBe("tenant-1/org-unit-logos/company.img");
    }

    [Fact]
    public async Task Should_Return_The_Units_Own_Logo_When_The_Employee_Sits_In_The_Company_Unit()
    {
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var unit = CreateUnit(NewRoot(), company, "tenant-1/org-unit-logos/own.img");

        (await _resolver.GetEmployeeCompanyLogoPathAsync(unit)).ShouldBe("tenant-1/org-unit-logos/own.img");
    }

    [Fact]
    public async Task Should_Take_The_Top_Most_Company_When_Two_Companies_Are_On_The_Path()
    {
        // Legacy tested Company_1 (OrgLevel1, the top) before Company_2 and stopped at the first that existed.
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var root = NewRoot();
        CreateUnit(root, company, "tenant-1/org-unit-logos/group.img");
        var subsidiary = CreateUnit(root + ".00001", company, "tenant-1/org-unit-logos/subsidiary.img");

        (await _resolver.GetEmployeeCompanyLogoPathAsync(subsidiary)).ShouldBe("tenant-1/org-unit-logos/group.img");
    }

    [Fact]
    public async Task Should_Not_Fall_Through_To_A_Lower_Company_When_The_Top_Company_Has_No_Logo()
    {
        // Legacy parity: the CASE picks Company_1 because it EXISTS, and prints its (empty) logo.
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var root = NewRoot();
        CreateUnit(root, company);
        var subsidiary = CreateUnit(root + ".00001", company, "tenant-1/org-unit-logos/subsidiary.img");

        (await _resolver.GetEmployeeCompanyLogoPathAsync(subsidiary)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Ignore_Units_Above_That_Are_Not_Companies()
    {
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var holding = CreateUnitType("Holding");
        var root = NewRoot();
        CreateUnit(root, holding, "tenant-1/org-unit-logos/holding.img");
        var employeeUnit = CreateUnit(root + ".00001", company, "tenant-1/org-unit-logos/company.img");

        (await _resolver.GetEmployeeCompanyLogoPathAsync(employeeUnit)).ShouldBe("tenant-1/org-unit-logos/company.img");
    }

    [Fact]
    public async Task Should_Ignore_A_Company_Elsewhere_In_The_Tree()
    {
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var department = CreateUnitType("Department");
        CreateUnit("70001", company, "tenant-1/org-unit-logos/other-company.img");
        var employeeUnit = CreateUnit("70002", department);

        (await _resolver.GetEmployeeCompanyLogoPathAsync(employeeUnit)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_Null_For_No_Unit_Or_An_Unknown_Unit()
    {
        (await _resolver.GetEmployeeCompanyLogoPathAsync(null)).ShouldBeNull();
        (await _resolver.GetEmployeeCompanyLogoPathAsync(987654321)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Read_The_Employees_Company_Logo_From_The_File_Store()
    {
        var logo = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };
        var path = await _images.ReplaceAsync(EntityImageKinds.OrgUnitLogo, TenantId, 4242, null, logo);
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var department = CreateUnitType("Department");
        var root = NewRoot();
        CreateUnit(root, company, path);
        var employeeUnit = CreateUnit(root + ".00001", department);

        (await _resolver.ReadEmployeeCompanyLogoAsync(TenantId, employeeUnit)).ShouldBe(logo);
    }

    [Fact]
    public async Task Should_Read_Nothing_When_The_Logo_File_Is_Missing()
    {
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var unit = CreateUnit(NewRoot(), company, $"tenant-{TenantId}/org-unit-logos/gone-{Guid.NewGuid():N}.img");

        (await _resolver.ReadEmployeeCompanyLogoAsync(TenantId, unit)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Read_Another_Tenants_Unit()
    {
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var path = await _images.ReplaceAsync(EntityImageKinds.OrgUnitLogo, TenantId, 4343, null, new byte[] { 9, 9, 9 });
        var unit = CreateUnit(NewRoot(), company, path);
        var otherTenantId = UsingDbContext(null, context =>
        {
            var created = context.Tenants.Add(new HCMv3.MultiTenancy.Tenant("logoother", "Logo Other") { IsActive = true }).Entity;
            context.SaveChanges();
            return created.Id;
        });

        (await _resolver.ReadEmployeeCompanyLogoAsync(otherTenantId, unit)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Read_No_Logo_For_A_Tenant_That_Does_Not_Exist()
    {
        // A report opened for a tenant that has since been removed prints without a logo instead of failing.
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        var unit = CreateUnit(NewRoot(), company, "tenant-1/org-unit-logos/x.img");

        (await _resolver.ReadEmployeeCompanyLogoAsync(987654, unit)).ShouldBeNull();
        (await _resolver.ReadFirstUnitLogoAsync(987654, CompanyLogoResolver.CompanyUnitTypeName, null)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Take_The_First_Company_In_Tree_Order_For_The_Report_Wide_Logo()
    {
        // rptPayslipAr / rptSubordPayslipEn/Ar: "SELECT logo FROM OrganizationUnit WHERE type = Company", first row.
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName);
        CreateUnit("00000", company, "tenant-1/org-unit-logos/first.img");
        CreateUnit("00000.00001", company, "tenant-1/org-unit-logos/second.img");

        (await _resolver.GetFirstUnitLogoPathAsync(CompanyLogoResolver.CompanyUnitTypeName, null))
            .ShouldBe("tenant-1/org-unit-logos/first.img");
    }

    [Fact]
    public async Task Should_Take_The_First_Level1_Unit_For_The_Payroll_Sheet_Logo()
    {
        // rptPayrollSheet: "... WHERE OrganizationUnitType.Code LIKE '%OrgLevel1%'" -- by level code, not by name.
        var company = CreateUnitType(CompanyLogoResolver.CompanyUnitTypeName, OrganizationUnitType.Level0Code);
        var level1 = CreateUnitType("Division", OrganizationUnitType.Level1Code);
        CreateUnit("00000", company, "tenant-1/org-unit-logos/root.img");
        CreateUnit("00000.00001", level1, "tenant-1/org-unit-logos/level1.img");

        (await _resolver.GetFirstUnitLogoPathAsync(null, OrganizationUnitType.Level1Code))
            .ShouldBe("tenant-1/org-unit-logos/level1.img");
    }

    [Fact]
    public async Task Should_Return_Null_For_The_Report_Wide_Logo_Without_A_Filter()
    {
        (await _resolver.GetFirstUnitLogoPathAsync(null, null)).ShouldBeNull();
    }
}
