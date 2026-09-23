using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeesAppService_Tests : AppTestBase
{
    private readonly IEmployeesAppService _employeesAppService;

    public EmployeesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _employeesAppService = Resolve<IEmployeesAppService>();
    }

    private Nationality CreateTestNationality(string nameEn = "Test Nationality")
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
        });
        return nationality;
    }

    private CreateOrEditEmployeeDto ValidCreateDto(int nationalityId, string code = null)
    {
        return new CreateOrEditEmployeeDto
        {
            Code = code,
            FirstNameEn = "John",
            LastNameEn = "Doe",
            FirstNameAr = "جون",
            LastNameAr = "دو",
            BirthDate = new DateTime(1990, 1, 1),
            JoiningDate = new DateTime(2020, 1, 1),
            ProbationDays = 90,
            NationalityId = nationalityId,
            CivilID = 123456789012
        };
    }

    private void SetDisableAutoEmpCode(bool disabled)
    {
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var settings = context.Set<HCMv3.SystemSettings.ManageGeneralSettings>().FirstOrDefault(s => s.TenantId == tenantId)
                           ?? context.Set<HCMv3.SystemSettings.ManageGeneralSettings>()
                               .Add(new HCMv3.SystemSettings.ManageGeneralSettings { TenantId = tenantId }).Entity;
            settings.DisableAutoEmpcode = disabled;
        });
    }

    /// <summary>Legacy EmployeeProfileController: with DisableAutoEmpcode on the code is typed in, so it must be given.</summary>
    [Fact]
    public async Task With_Auto_Code_Disabled_A_Blank_Code_Is_Refused_And_A_Typed_Code_Is_Kept()
    {
        var nationality = CreateTestNationality();
        SetDisableAutoEmpCode(true);

        var exception = await Assert.ThrowsAsync<Abp.UI.UserFriendlyException>(() =>
            _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, code: null)));
        exception.Message.ShouldContain("Employee Code");

        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, code: "MAN-7001"));
        UsingDbContext(context => context.Set<Employee>().Any(e => e.Code == "MAN-7001")).ShouldBeTrue();
    }

    [Fact]
    public async Task With_Auto_Code_Enabled_A_Blank_Code_Is_Generated()
    {
        var nationality = CreateTestNationality();
        SetDisableAutoEmpCode(false);

        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, code: null));

        var generated = UsingDbContext(context => context.Set<Employee>()
            .OrderByDescending(e => e.Id).Select(e => e.Code).First());
        generated.ShouldNotBeNullOrWhiteSpace();
        int.TryParse(generated, out _).ShouldBeTrue();
    }

    /// <summary>
    /// Legacy Employee.AddAutoWageAccount: with AutoWageAssignOnHire on, the chosen employee's open-ended wage
    /// accounts are copied to the new hire from the first of the joining month; closed ones are not.
    /// </summary>
    [Fact]
    public async Task A_New_Hire_Should_Get_The_Chosen_Employees_Open_Ended_Wage_Accounts_When_The_Setting_Is_On()
    {
        var nationality = CreateTestNationality();
        var data = new HCMv3.Tests.GridReports.PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
        var source = data.CreateEmployee("AWH-SRC");
        var basic = data.CreateWageType("1000", HCMv3.PayrollManagement.BillingType.Earning);
        var allowance = data.CreateWageType("1001", HCMv3.PayrollManagement.BillingType.Earning);
        var account = data.CreateAccount("51000", "Salaries");
        data.CreateWageAccount(source, basic, account, endDate: new DateTime(9999, 12, 31));
        data.CreateWageAccount(source, allowance, account, endDate: new DateTime(2023, 12, 31));

        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var settings = context.Set<HCMv3.SystemSettings.ManagePersonnelSettings>().FirstOrDefault(s => s.TenantId == tenantId)
                           ?? context.Set<HCMv3.SystemSettings.ManagePersonnelSettings>()
                               .Add(new HCMv3.SystemSettings.ManagePersonnelSettings { TenantId = tenantId }).Entity;
            settings.AutoWageAssignOnHire = true;
        });

        var dto = ValidCreateDto(nationality.Id, "AWH-NEW");
        dto.JoiningDate = new DateTime(2024, 3, 18);
        dto.CopyWageAccountsFromEmployeeId = source.Id;
        await _employeesAppService.CreateOrEdit(dto);

        var copied = UsingDbContext(context =>
        {
            var newId = context.Set<Employee>().Single(e => e.Code == "AWH-NEW").Id;
            return context.Set<HCMv3.PayrollManagement.EmployeeWageAccount>().Where(w => w.EmployeeId == newId).ToList();
        });

        copied.Count.ShouldBe(1);
        copied[0].WageTypeId.ShouldBe(basic.Id);
        copied[0].StartDate.ShouldBe(new DateTime(2024, 3, 1));
        copied[0].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    // ---- GetEmployeeProfile ---------------------------------------------------------------------

    [Fact]
    public async Task GetEmployeeProfile_Should_Resolve_Lookups_To_Names()
    {
        // The whole reason this endpoint exists: a read screen has no dropdowns to translate ids with,
        // so the ids must arrive already resolved.
        var nationality = CreateTestNationality("Kuwaiti");
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "P100"));

        var id = UsingDbContext(context => context.Set<Employee>().Single(e => e.Code == "P100").Id);

        var profile = await _employeesAppService.GetEmployeeProfile(new EntityDto(id));

        profile.Code.ShouldBe("P100");
        profile.NationalityName.ShouldBe("Kuwaiti");
        profile.FullNameEn.ShouldBe("John Doe");
    }

    [Fact]
    public async Task GetEmployeeProfile_Should_Return_Blank_Names_For_Unset_Optional_Lookups()
    {
        // Eight of the nine lookups are nullable. An employee with no religion or birth place recorded
        // is an ordinary state -- it must render blank, not throw a null reference.
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "P101"));

        var id = UsingDbContext(context => context.Set<Employee>().Single(e => e.Code == "P101").Id);

        var profile = await _employeesAppService.GetEmployeeProfile(new EntityDto(id));

        profile.ReligionName.ShouldBeNull();
        profile.MaritalStatusName.ShouldBeNull();
        profile.QualificationName.ShouldBeNull();
        profile.BirthPlaceName.ShouldBeNull();
        profile.TitleName.ShouldBeNull();
        profile.GovernorateName.ShouldBeNull();
        profile.CountryName.ShouldBeNull();
        profile.RecruitmentTypeName.ShouldBeNull();
    }

    [Fact]
    public async Task GetEmployeeProfile_Should_Carry_The_Computed_Fields()
    {
        // FullName and ProbationEndDate are computed on save, and the profile screen is where anyone
        // would notice them being stale or absent.
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "P102"));

        var id = UsingDbContext(context => context.Set<Employee>().Single(e => e.Code == "P102").Id);

        var profile = await _employeesAppService.GetEmployeeProfile(new EntityDto(id));

        profile.FullNameAr.ShouldBe("جون دو");
        profile.ProbationEndDate.ShouldBe(new DateTime(2020, 1, 1).AddDays(90));
        profile.JoiningDate.ShouldBe(new DateTime(2020, 1, 1));
    }

    [Fact]
    public async Task GetEmployeeProfile_Should_Refuse_An_Unknown_Employee()
    {
        // Reachable by editing the URL. A friendly refusal beats an unhandled null reference.
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _employeesAppService.GetEmployeeProfile(new EntityDto(987654)));
    }

    [Fact]
    public async Task Should_Create_Employee_And_Compute_FullName_And_ProbationEndDate()
    {
        //Arrange
        var nationality = CreateTestNationality();

        //Act
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "E100"));

        //Assert
        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().Single(e => e.Code == "E100");
            employee.FullNameEn.ShouldBe("John Doe");
            employee.FullNameAr.ShouldBe("جون دو");
            employee.ProbationEndDate.ShouldBe(employee.JoiningDate.AddDays(90));
        });
    }

    [Fact]
    public async Task Should_Throw_When_BirthDate_Is_Future_Or_Current()
    {
        //Arrange
        var nationality = CreateTestNationality();
        var dto = ValidCreateDto(nationality.Id, "E101");
        dto.BirthDate = DateTime.Now.Date;

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeesAppService.CreateOrEdit(dto));
    }

    [Fact]
    public async Task Should_Throw_When_JoiningDate_Earlier_Than_BirthDate()
    {
        //Arrange
        var nationality = CreateTestNationality();
        var dto = ValidCreateDto(nationality.Id, "E102");
        dto.JoiningDate = new DateTime(1980, 1, 1);

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeesAppService.CreateOrEdit(dto));
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Active_CivilId()
    {
        //Arrange
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "E103"));

        var dto = ValidCreateDto(nationality.Id, "E104");
        dto.CivilID = 123456789012; // same as E103

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeesAppService.CreateOrEdit(dto));
    }

    [Fact]
    public async Task Should_Auto_Generate_Code_When_Blank()
    {
        //Arrange
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "5"));

        var dto = ValidCreateDto(nationality.Id);
        dto.CivilID = 999888777666;

        //Act
        await _employeesAppService.CreateOrEdit(dto);

        //Assert
        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().Single(e => e.CivilID == 999888777666);
            employee.Code.ShouldBe("6");
        });
    }

    [Fact]
    public async Task Should_Get_Employee_For_Edit()
    {
        //Arrange
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "E105"));
        int id = 0;
        UsingDbContext(context => id = context.Set<Employee>().Single(e => e.Code == "E105").Id);

        //Act
        var output = await _employeesAppService.GetEmployeeForEdit(new EntityDto(id));

        //Assert
        output.Employee.ShouldNotBeNull();
        output.Employee.Code.ShouldBe("E105");
    }

    [Fact]
    public async Task Should_Delete_Employee()
    {
        //Arrange
        var nationality = CreateTestNationality();
        await _employeesAppService.CreateOrEdit(ValidCreateDto(nationality.Id, "E106"));
        int id = 0;
        UsingDbContext(context => id = context.Set<Employee>().Single(e => e.Code == "E106").Id);

        //Act
        await _employeesAppService.Delete(new EntityDto(id));

        //Assert
        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().IgnoreQueryFilters().Single(e => e.Id == id);
            employee.IsDeleted.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_Throw_When_LoginID_Is_Already_Assigned_To_Another_Employee()
    {
        var nationality = CreateTestNationality();

        var first = ValidCreateDto(nationality.Id, "E900");
        first.LoginID = "shared.login";
        await _employeesAppService.CreateOrEdit(first);

        var second = ValidCreateDto(nationality.Id, "E901");
        second.CivilID = 223456789012;
        second.LoginID = "shared.login";

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeesAppService.CreateOrEdit(second));

        exception.Message.ShouldContain("shared.login");
        exception.Message.ShouldContain("E900");
    }

    [Fact]
    public async Task Should_Throw_When_LoginID_Duplicates_An_Inactive_Employee()
    {
        // Deliberately stricter than the CivilID/AccessID checks, which only look at active employees:
        // a leaver's row keeps its LoginID and the sign-in fallback does not filter by status, so reusing
        // it would make one login resolve to two people.
        var nationality = CreateTestNationality();

        var leaver = ValidCreateDto(nationality.Id, "E910");
        leaver.LoginID = "reused.login";
        await _employeesAppService.CreateOrEdit(leaver);

        var leaverId = UsingDbContext(context => context.Set<Employee>().Single(e => e.Code == "E910").Id);
        UsingDbContext(context =>
        {
            context.Set<Employee>().Single(e => e.Id == leaverId).Status = EmployeeStatus.Inactive;
        });

        // Confirm the leaver really is inactive, so a pass here cannot be explained by the status update
        // having silently failed.
        UsingDbContext(context =>
            context.Set<Employee>().Single(e => e.Id == leaverId).Status.ShouldBe(EmployeeStatus.Inactive));

        var joiner = ValidCreateDto(nationality.Id, "E911");
        joiner.CivilID = 323456789012;
        joiner.LoginID = "reused.login";

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeesAppService.CreateOrEdit(joiner));

        // Assert on the message, not just the exception type: every other validation in ValidateAsync also
        // throws UserFriendlyException, so a bare type assertion would pass even if the LoginID rule were
        // deleted and something unrelated tripped instead.
        exception.Message.ShouldContain("reused.login");
        exception.Message.ShouldContain("E910");

        UsingDbContext(context =>
            context.Set<Employee>().Any(e => e.Code == "E911").ShouldBeFalse());
    }

    [Fact]
    public async Task Should_Throw_When_An_Active_Employee_Takes_An_Inactive_Employees_LoginID_By_Edit()
    {
        // The create path is not the only way in: an existing employee can be edited onto a leaver's login.
        var nationality = CreateTestNationality();

        var leaver = ValidCreateDto(nationality.Id, "E940");
        leaver.LoginID = "leaver.login";
        await _employeesAppService.CreateOrEdit(leaver);

        var leaverId = UsingDbContext(context => context.Set<Employee>().Single(e => e.Code == "E940").Id);
        UsingDbContext(context =>
        {
            context.Set<Employee>().Single(e => e.Id == leaverId).Status = EmployeeStatus.Inactive;
        });

        var joiner = ValidCreateDto(nationality.Id, "E941");
        joiner.CivilID = 523456789012;
        await _employeesAppService.CreateOrEdit(joiner);

        var joinerId = UsingDbContext(context => context.Set<Employee>().Single(e => e.Code == "E941").Id);
        var forEdit = await _employeesAppService.GetEmployeeForEdit(new EntityDto(joinerId));
        forEdit.Employee.LoginID = "leaver.login";

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeesAppService.CreateOrEdit(forEdit.Employee));

        exception.Message.ShouldContain("leaver.login");
        exception.Message.ShouldContain("E940");

        UsingDbContext(context =>
            context.Set<Employee>().Single(e => e.Id == joinerId).LoginID.ShouldBeNull());
    }

    [Fact]
    public async Task Should_Allow_Saving_An_Employee_Keeping_Its_Own_LoginID()
    {
        var nationality = CreateTestNationality();

        var dto = ValidCreateDto(nationality.Id, "E920");
        dto.LoginID = "own.login";
        await _employeesAppService.CreateOrEdit(dto);

        var forEdit = await _employeesAppService.GetEmployeeForEdit(
            new EntityDto(UsingDbContext(context => context.Set<Employee>().Single(e => e.Code == "E920").Id)));

        // The duplicate check must exclude the row being saved, or every no-op edit would reject itself.
        forEdit.Employee.FirstNameEn = "Johnny";
        await _employeesAppService.CreateOrEdit(forEdit.Employee);

        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().Single(e => e.Code == "E920");
            employee.FirstNameEn.ShouldBe("Johnny");
            employee.LoginID.ShouldBe("own.login");
        });
    }

    [Fact]
    public async Task Should_Allow_Many_Employees_With_No_LoginID()
    {
        // Most employees have no login at all -- blank must never count as a duplicate.
        var nationality = CreateTestNationality();

        var first = ValidCreateDto(nationality.Id, "E930");
        await _employeesAppService.CreateOrEdit(first);

        var second = ValidCreateDto(nationality.Id, "E931");
        second.CivilID = 423456789012;
        await _employeesAppService.CreateOrEdit(second);

        UsingDbContext(context =>
        {
            context.Set<Employee>().Count(e => e.Code == "E930" || e.Code == "E931").ShouldBe(2);
        });
    }
}
