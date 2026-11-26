using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeAbsencesAppService_Tests : AppTestBase
{
    private readonly IEmployeeAbsencesAppService _appService;

    public EmployeeAbsencesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAbsencesAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        Employee employee = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

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
                NationalityId = nationality.Id
            }).Entity;
        });

        // Leave days are counted against the employee's working week (legacy ActualLeaveDays), so a test
        // employee needs one -- see EssLeaveTestDuty.
        UsingDbContext(context => HCMv3.Tests.Workflow.EssLeaveTestDuty.AssignAllWorkingWeek(context, AbpSession.TenantId ?? 1, employee.Id));

        return employee;
    }

    private AbsenceType CreateTestAbsenceType(string code)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,

                // As the seeded leave types are: a bare AbsenceType is "once only, no backdating".
                CanApplyAgain = true,
                AllowDuringProbation = true,
                AllowBackDatedDays = 3650
            }).Entity;
        });
        return absenceType;
    }

    [Fact]
    public async Task Should_Create_Absence()
    {
        //Arrange
        var employee = CreateTestEmployee("EA100");
        var absenceType = CreateTestAbsenceType("AT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 5)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAbsencesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CalendarDays.ShouldBe(5);
        result.Items[0].TotalLeaveDays.ShouldBe(5);
    }

    // ---- Employee Leaves list (every employee's leave on one screen) ----

    private async Task CreateLeave(Employee employee, AbsenceType absenceType, DateTime start)
    {
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            StartDate = start,
            EndDate = start.AddDays(1)
        });
    }

    private void RenameEmployee(Employee employee, string nameEn, string nameAr)
    {
        UsingDbContext(context =>
        {
            var row = context.Set<Employee>().Single(e => e.Id == employee.Id);
            row.FullNameEn = nameEn;
            row.FullNameAr = nameAr;
        });
    }

    [Fact]
    public async Task Should_List_Leaves_Of_Every_Employee_When_No_Employee_Is_Given()
    {
        // Arrange
        var first = CreateTestEmployee("ELL01");
        var second = CreateTestEmployee("ELL02");
        var type = CreateTestAbsenceType("ELLT1");
        await CreateLeave(first, type, new DateTime(2024, 2, 1));
        await CreateLeave(second, type, new DateTime(2024, 2, 1));

        // Act
        var result = await _appService.GetAll(new GetAllEmployeeAbsencesInput { Filter = "ELL0", MaxResultCount = 100 });

        // Assert
        result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ShouldBe(new[] { "ELL01", "ELL02" });
    }

    [Theory]
    [InlineData("ELF77")]
    [InlineData("Zainab")]
    [InlineData("زينب")]
    [InlineData("ELFTYPE")]
    public async Task Should_Filter_Leaves_By_Employee_Code_Name_Or_Leave_Type(string filter)
    {
        // Arrange
        var wanted = CreateTestEmployee("ELF77");
        RenameEmployee(wanted, "Zainab Hadi", "زينب هادي");
        var other = CreateTestEmployee("ELF78");
        await CreateLeave(wanted, CreateTestAbsenceType("ELFTYPE"), new DateTime(2024, 3, 1));
        await CreateLeave(other, CreateTestAbsenceType("ELFOTHER"), new DateTime(2024, 3, 1));

        // Act
        var result = await _appService.GetAll(new GetAllEmployeeAbsencesInput { Filter = filter, MaxResultCount = 100 });

        // Assert
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeCode.ShouldBe("ELF77");
    }

    [Theory]
    [InlineData("ELK55")]
    [InlineData("Omar")]
    [InlineData("عمر")]
    public async Task Should_Find_Employees_For_Lookup_By_Code_Or_Name(string filter)
    {
        // Arrange
        var wanted = CreateTestEmployee("ELK55");
        RenameEmployee(wanted, "Omar Fahd", "عمر فهد");
        CreateTestEmployee("ELK56");

        // Act
        var result = await _appService.GetEmployeesForLookup(new GetEmployeeAbsenceEmployeesInput { Filter = filter });

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].Code.ShouldBe("ELK55");
        result.Items[0].NameEn.ShouldBe("Omar Fahd");
    }

    [Fact]
    public async Task Should_Order_Lookup_Numeric_Codes_In_Number_Order()
    {
        // Arrange
        CreateTestEmployee("ELQ9102");
        CreateTestEmployee("ELQ91");
        CreateTestEmployee("ELQ910");

        // Act
        var result = await _appService.GetEmployeesForLookup(new GetEmployeeAbsenceEmployeesInput { Filter = "ELQ91" });

        // Assert
        result.Items.Select(i => i.Code).ShouldBe(new[] { "ELQ91", "ELQ910", "ELQ9102" });
    }

    [Fact]
    public async Task Should_Return_Employee_Code_And_Name_For_Edit()
    {
        // Arrange
        var employee = CreateTestEmployee("ELE01");
        RenameEmployee(employee, "Huda Saleh", "هدى صالح");
        await CreateLeave(employee, CreateTestAbsenceType("ELET1"), new DateTime(2024, 4, 1));
        var id = (await _appService.GetAll(new GetAllEmployeeAbsencesInput { EmployeeId = employee.Id })).Items[0].Id;

        // Act
        var result = await _appService.GetEmployeeAbsenceForEdit(new Abp.Application.Services.Dto.EntityDto(id));

        // Assert
        result.EmployeeCode.ShouldBe("ELE01");
        result.EmployeeFullNameEn.ShouldBe("Huda Saleh");
    }

    private void SetAutoHalfDayLeave(bool enabled)
    {
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var settings = context.Set<HCMv3.SystemSettings.ManageTimeSettings>().FirstOrDefault(s => s.TenantId == tenantId)
                           ?? context.Set<HCMv3.SystemSettings.ManageTimeSettings>()
                               .Add(new HCMv3.SystemSettings.ManageTimeSettings { TenantId = tenantId }).Entity;
            settings.EnableAutoHalfDayLeave = enabled;
        });
    }

    /// <summary>Legacy EmployeeAbsenceController: the half-day choice exists only with EnableAutoHalfDayLeave on.</summary>
    [Fact]
    public async Task Should_Refuse_A_Half_Day_Leave_Unless_Time_Settings_Enable_It()
    {
        var employee = CreateTestEmployee("EA700");
        var absenceType = CreateTestAbsenceType("AT700");
        UsingDbContext(context =>
        {
            context.Set<AbsenceType>().Single(t => t.Id == absenceType.Id).AllowPartialLeave = true;
        });

        CreateOrEditEmployeeAbsenceDto HalfDay() => new()
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            StartDate = new DateTime(2024, 3, 4),
            EndDate = new DateTime(2024, 3, 4),
            HalfdayType = HalfdayType.StartOfDuty
        };

        SetAutoHalfDayLeave(false);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => _appService.CreateOrEdit(HalfDay()));
        exception.Message.ShouldContain("Half-day");

        SetAutoHalfDayLeave(true);
        await _appService.CreateOrEdit(HalfDay());

        var result = await _appService.GetAll(new GetAllEmployeeAbsencesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("EA200");
        var absenceType = CreateTestAbsenceType("AT200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
            {
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceType.Id,
                StartDate = new DateTime(2024, 1, 10),
                EndDate = new DateTime(2024, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Absence_Period()
    {
        //Arrange
        var employee = CreateTestEmployee("EA300");
        var absenceType = CreateTestAbsenceType("AT300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 2, 10)
        });

        //Act & Assert - overlapping period for the same employee
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
            {
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceType.Id,
                StartDate = new DateTime(2024, 2, 5),
                EndDate = new DateTime(2024, 2, 15)
            }));
    }
}
