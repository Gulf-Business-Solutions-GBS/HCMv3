using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeSessionServiceTests : AppTestBase
{
    private readonly IEmployeeSessionService _employeeSessionService;

    public EmployeeSessionServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _employeeSessionService = Resolve<IEmployeeSessionService>();
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

    [Fact]
    public async Task Should_Resolve_Current_Employee_By_Matching_LoginID_To_Current_UserName()
    {
        var currentUser = GetCurrentUser();
        var nationalityId = CreateNationality();

        UsingDbContext(context =>
        {
            context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "SESS100",
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                LoginID = currentUser.UserName,
                NationalityId = nationalityId
            });
        });

        var employee = await _employeeSessionService.GetCurrentEmployeeAsync();

        employee.ShouldNotBeNull();
        employee.Code.ShouldBe("SESS100");
    }

    [Fact]
    public async Task Should_Return_Null_When_No_Employee_Matches_Current_User()
    {
        var employee = await _employeeSessionService.GetCurrentEmployeeAsync();

        employee.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Resolve_Current_Employee_By_UserId_Link()
    {
        var nationalityId = CreateNationality();

        UsingDbContext(context =>
        {
            context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "SESS110",
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                UserId = AbpSession.UserId,
                NationalityId = nationalityId
            });
        });

        var employee = await _employeeSessionService.GetCurrentEmployeeAsync();

        employee.ShouldNotBeNull();
        employee.Code.ShouldBe("SESS110");
    }

    [Fact]
    public async Task Should_Prefer_The_UserId_Link_Over_The_Legacy_LoginID_Match()
    {
        var currentUser = GetCurrentUser();
        var nationalityId = CreateNationality();

        // Exactly the drift the UserId link exists to remove: one employee still carries the login string,
        // a different one holds the real link. The link must win.
        UsingDbContext(context =>
        {
            context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "SESS120LOGIN",
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                LoginID = currentUser.UserName,
                NationalityId = nationalityId
            });

            context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "SESS120LINK",
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                UserId = AbpSession.UserId,
                NationalityId = nationalityId
            });
        });

        var employee = await _employeeSessionService.GetCurrentEmployeeAsync();

        employee.ShouldNotBeNull();
        employee.Code.ShouldBe("SESS120LINK");
    }

    [Fact]
    public async Task Should_Return_Current_Employee_Id_Without_Loading_The_Whole_Row()
    {
        var nationalityId = CreateNationality();

        Employee created = null;
        UsingDbContext(context =>
        {
            created = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "SESS130",
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                UserId = AbpSession.UserId,
                NationalityId = nationalityId
            }).Entity;
        });

        var employeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();

        employeeId.ShouldBe(created.Id);
    }

    [Fact]
    public async Task Should_Fall_Back_To_LoginID_For_GetCurrentEmployeeId_When_Not_Linked_Yet()
    {
        var currentUser = GetCurrentUser();
        var nationalityId = CreateNationality();

        Employee created = null;
        UsingDbContext(context =>
        {
            created = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "SESS140",
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                LoginID = currentUser.UserName,
                NationalityId = nationalityId
            }).Entity;
        });

        var employeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();

        employeeId.ShouldBe(created.Id);
    }

    [Fact]
    public async Task Should_Resolve_Active_Employee_Code_For_GetSysCreatedBy()
    {
        var nationalityId = CreateNationality();

        UsingDbContext(context =>
        {
            context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "SESS200",
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                LoginID = "sysCreator1",
                Status = EmployeeStatus.Active,
                NationalityId = nationalityId
            });
        });

        var code = await _employeeSessionService.GetSysCreatedByAsync("sysCreator1");

        code.ShouldBe("SESS200");
    }

    [Fact]
    public async Task Should_Fallback_To_Input_When_No_Employee_Matches_GetSysCreatedBy()
    {
        var code = await _employeeSessionService.GetSysCreatedByAsync("unknownLogin");

        code.ShouldBe("unknownLogin");
    }

    [Fact]
    public async Task Should_Return_Empty_For_GetSysCreatedBy_With_Blank_Input()
    {
        var code = await _employeeSessionService.GetSysCreatedByAsync("   ");

        code.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Recognize_Current_TenantAdmin_As_Admin_Role()
    {
        var isAdmin = await _employeeSessionService.IsCurrentUserInAdminRoleAsync();

        isAdmin.ShouldBeTrue();
    }
}
