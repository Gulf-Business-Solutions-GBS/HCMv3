using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.Navigation;
using HCMv3.Navigation.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Navigation;

/// <summary>
/// Covers the people half of the header search. The behaviour worth pinning down is what it refuses to do:
/// answer on one or two characters, return more than the cap, or drop somebody who has no org assignment.
/// </summary>
public class GlobalSearchAppService_Tests : AppTestBase
{
    private readonly IGlobalSearchAppService _globalSearchAppService;

    public GlobalSearchAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _globalSearchAppService = Resolve<IGlobalSearchAppService>();
    }

    [Fact]
    public async Task Should_Find_People_By_English_Name_Arabic_Name_And_Code()
    {
        var employee = CreateEmployee("GS-001", "Mian Zeeshan Mahmood", "ميان زيشان");

        var byEnglishName = await _globalSearchAppService.SearchPeople(
            new SearchPeopleInput { Filter = "Zeeshan" });
        byEnglishName.People.Select(x => x.Id).ShouldBe(new[] { employee.Id });

        var byArabicName = await _globalSearchAppService.SearchPeople(
            new SearchPeopleInput { Filter = "زيشان" });
        byArabicName.People.Select(x => x.Id).ShouldBe(new[] { employee.Id });

        var byCode = await _globalSearchAppService.SearchPeople(
            new SearchPeopleInput { Filter = "GS-001" });
        byCode.People.Select(x => x.Id).ShouldBe(new[] { employee.Id });
    }

    [Fact]
    public async Task Should_Return_Nothing_For_A_Filter_Below_The_Minimum_Length()
    {
        CreateEmployee("GS-002", "Aaron Short", "آرون");

        // One character would match most of the company: slow to answer and useless to read.
        var result = await _globalSearchAppService.SearchPeople(new SearchPeopleInput { Filter = "A" });

        result.People.ShouldBeEmpty();
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Ignore_Surrounding_Whitespace_In_The_Filter()
    {
        var employee = CreateEmployee("GS-003", "Padded Name", "محاط");

        var result = await _globalSearchAppService.SearchPeople(new SearchPeopleInput { Filter = "  Padded  " });

        result.People.Select(x => x.Id).ShouldBe(new[] { employee.Id });
    }

    [Fact]
    public async Task Should_Cap_Results_And_Flag_That_More_Exist()
    {
        for (var index = 0; index < 6; index++)
        {
            CreateEmployee($"GS-CAP{index}", $"Capped Person {index}", "مكرر");
        }

        var result = await _globalSearchAppService.SearchPeople(new SearchPeopleInput
        {
            Filter = "Capped",
            MaxResultCount = 4
        });

        result.People.Count.ShouldBe(4);
        result.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Flag_More_When_The_Results_Fit()
    {
        CreateEmployee("GS-FIT1", "Fitting Person One", "واحد");
        CreateEmployee("GS-FIT2", "Fitting Person Two", "اثنان");

        var result = await _globalSearchAppService.SearchPeople(new SearchPeopleInput
        {
            Filter = "Fitting",
            MaxResultCount = 4
        });

        result.People.Count.ShouldBe(2);
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Clamp_An_Oversized_Request_To_The_Allowed_Maximum()
    {
        CreateEmployee("GS-004", "Clamped Person", "مقيد");

        var result = await _globalSearchAppService.SearchPeople(new SearchPeopleInput
        {
            Filter = "Clamped",
            MaxResultCount = int.MaxValue
        });

        result.People.Count.ShouldBeLessThanOrEqualTo(SearchPeopleInput.MaxAllowedResultCount);
    }

    [Fact]
    public async Task Should_Carry_Job_Title_And_Department_From_The_Latest_Assignment()
    {
        var employee = CreateEmployee("GS-005", "Placed Person", "موظف");
        var oldTitleId = AddJobTitle("JT-OLD", "Officer", "مسؤول");
        var newTitleId = AddJobTitle("JT-NEW", "Senior Manager", "مدير أول");
        var unitId = AddOrganizationUnit("00042", "Supply Network", "شبكة التوريد");

        AddOrgAssignment(employee.Id, oldTitleId, null, new DateTime(2020, 1, 1));
        AddOrgAssignment(employee.Id, newTitleId, unitId, new DateTime(2025, 1, 1));

        var result = await _globalSearchAppService.SearchPeople(new SearchPeopleInput { Filter = "Placed" });

        var person = result.People.Single();
        person.JobTitleEn.ShouldBe("Senior Manager");
        person.JobTitleAr.ShouldBe("مدير أول");
        person.DepartmentEn.ShouldBe("Supply Network");
        person.DepartmentAr.ShouldBe("شبكة التوريد");
    }

    [Fact]
    public async Task Should_Still_Return_Someone_Who_Has_No_Org_Assignment()
    {
        var employee = CreateEmployee("GS-006", "Unassigned Person", "غير معين");

        var result = await _globalSearchAppService.SearchPeople(new SearchPeopleInput { Filter = "Unassigned" });

        var person = result.People.Single();
        person.Id.ShouldBe(employee.Id);
        person.JobTitleEn.ShouldBeNull();
        person.DepartmentEn.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Report_Whether_A_Photo_Exists_Without_Returning_It()
    {
        CreateEmployee("GS-007", "Pictured Person", "مصور", photo: new byte[] { 1, 2, 3 });
        CreateEmployee("GS-008", "Pictureless Person", "بدون صورة");

        var withPhoto = await _globalSearchAppService.SearchPeople(new SearchPeopleInput { Filter = "Pictured" });
        withPhoto.People.Single().HasPhoto.ShouldBeTrue();

        var withoutPhoto = await _globalSearchAppService.SearchPeople(new SearchPeopleInput { Filter = "Pictureless" });
        withoutPhoto.People.Single().HasPhoto.ShouldBeFalse();
    }

    private int AddJobTitle(string code, string nameEn, string nameAr)
    {
        var jobTitleId = 0;
        UsingDbContext(context =>
        {
            var jobTitle = context.Set<JobTitle>().Add(new JobTitle
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = nameEn,
                NameAr = nameAr
            }).Entity;
            context.SaveChanges();
            jobTitleId = jobTitle.Id;
        });
        return jobTitleId;
    }

    private long AddOrganizationUnit(string treeCode, string displayName, string nameAr)
    {
        long organizationUnitId = 0;
        UsingDbContext(context =>
        {
            var unit = context.Set<OrganizationUnit>().Add(new OrganizationUnit
            {
                TenantId = AbpSession.TenantId,
                Code = treeCode,
                DisplayName = displayName
            }).Entity;
            context.SaveChanges();
            organizationUnitId = unit.Id;
        });

        var organizationUnitTypeId = 0;
        UsingDbContext(context =>
        {
            var type = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = OrganizationUnitType.Level2Code,
                NameEn = "Department",
                NameAr = "إدارة"
            }).Entity;
            context.SaveChanges();
            organizationUnitTypeId = type.Id;
        });

        UsingDbContext(context =>
        {
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = AbpSession.TenantId ?? 1,
                OrganizationUnitId = organizationUnitId,
                NameAr = nameAr,
                OrganizationUnitTypeId = organizationUnitTypeId
            });
        });

        return organizationUnitId;
    }

    private void AddOrgAssignment(int employeeId, int jobTitleId, long? organizationUnitId, DateTime startDate)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                JobTitleId = jobTitleId,
                OrganizationUnitId = organizationUnitId,
                StartDate = startDate,
                EndDate = new DateTime(2099, 12, 31)
            });
        });
    }

    private Employee CreateEmployee(string code, string fullNameEn, string fullNameAr, byte[] photo = null)
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
                FullNameEn = fullNameEn,
                FullNameAr = fullNameAr,
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                NationalityId = nationality.Id,
                Photo = photo
            }).Entity;
        });

        return employee;
    }
}
