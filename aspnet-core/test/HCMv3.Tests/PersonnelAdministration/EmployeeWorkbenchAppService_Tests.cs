using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Timing;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeWorkbenchAppService_Tests : AppTestBase
{
    private readonly IEmployeeWorkbenchAppService _appService;
    private readonly DateTime _today;

    public EmployeeWorkbenchAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeWorkbenchAppService>();
        _today = Clock.Now.Date;
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, string fullNameEn, EmployeeStatus status = EmployeeStatus.Active, long civilId = 0)
    {
        Employee employee = null;
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = fullNameEn,
                FullNameAr = "ف ل",
                Status = status,
                CivilID = civilId,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    private AbsenceType CreateAbsenceType(string code, bool isReturnDateRequired)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                IsReturnDateRequired = isReturnDateRequired
            }).Entity;
        });
        return absenceType;
    }

    private EmployeeAbsence AddLeave(int employeeId, int absenceTypeId, DateTime start, DateTime end, Action<EmployeeAbsence> customize = null)
    {
        var leave = new EmployeeAbsence
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            StartDate = start,
            EndDate = end,
            InitialEndDate = end
        };
        customize?.Invoke(leave);
        UsingDbContext(context => context.Set<EmployeeAbsence>().Add(leave));
        return leave;
    }

    private void AddTimeRegister(int employeeId, DateTime day, DateTime actualIn, DateTime actualOut, Action<TimeRegister> customize = null)
    {
        var register = new TimeRegister
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            DayDate = day,
            ActualIn = actualIn,
            ActualOut = actualOut,
            PlannedWorkingDay = 1
        };
        customize?.Invoke(register);
        UsingDbContext(context => context.Set<TimeRegister>().Add(register));
    }

    #region SearchEmployees

    [Fact]
    public async Task Should_Find_Employee_By_Code_Or_Name()
    {
        CreateEmployee("WB100", "Ahmad Khalid");
        CreateEmployee("WB101", "Sara Ali");

        var byCode = await _appService.SearchEmployees(new SearchWorkbenchEmployeesInput { Filter = "WB100" });
        var byName = await _appService.SearchEmployees(new SearchWorkbenchEmployeesInput { Filter = "Sara" });

        byCode.Items.Select(e => e.Code).ShouldBe(new[] { "WB100" });
        byName.Items.Select(e => e.Code).ShouldBe(new[] { "WB101" });
    }

    [Fact]
    public async Task Should_Find_Employee_By_Civil_Id()
    {
        CreateEmployee("WB110", "Civil Person", civilId: 289010112345);

        var result = await _appService.SearchEmployees(new SearchWorkbenchEmployeesInput { Filter = "289010112345" });

        result.Items.Select(e => e.Code).ShouldBe(new[] { "WB110" });
    }

    [Fact]
    public async Task Should_Leave_Out_Ex_Employees_Unless_Asked()
    {
        CreateEmployee("WB120", "Yousef Current");
        CreateEmployee("WB121", "Yousef Leaver", EmployeeStatus.Inactive);

        var withoutLeavers = await _appService.SearchEmployees(new SearchWorkbenchEmployeesInput { Filter = "Yousef" });
        var withLeavers = await _appService.SearchEmployees(new SearchWorkbenchEmployeesInput { Filter = "Yousef", IncludeExEmployees = true });

        withoutLeavers.Items.Select(e => e.Code).ShouldBe(new[] { "WB120" });
        withLeavers.Items.Select(e => e.Code).ShouldBe(new[] { "WB120", "WB121" });
    }

    [Fact]
    public async Task Should_List_Current_Employees_Before_Ex_Employees()
    {
        CreateEmployee("WB130", "Omar Leaver", EmployeeStatus.Inactive);
        CreateEmployee("WB131", "Omar Current");

        var result = await _appService.SearchEmployees(new SearchWorkbenchEmployeesInput { Filter = "Omar", IncludeExEmployees = true });

        result.Items.Select(e => e.Code).ShouldBe(new[] { "WB131", "WB130" });
        result.Items[1].Status.ShouldBe(EmployeeStatus.Inactive);
    }

    [Fact]
    public async Task Should_Return_No_More_Than_Max_Result_Count()
    {
        for (var i = 0; i < 4; i++)
        {
            CreateEmployee("WB14" + i, "Many Match");
        }

        var result = await _appService.SearchEmployees(new SearchWorkbenchEmployeesInput { Filter = "Many Match", MaxResultCount = 3 });

        result.Items.Count.ShouldBe(3);
    }

    #endregion

    #region GetHeader

    [Fact]
    public async Task Should_Return_Current_Assignment_And_Manager_Not_Future_Ones()
    {
        var employee = CreateEmployee("WB200", "Header Person");
        var oldManager = CreateEmployee("WB201", "Old Manager");
        var futureManager = CreateEmployee("WB202", "Future Manager");

        UsingDbContext(context =>
        {
            var current = context.Set<JobTitle>().Add(new JobTitle { TenantId = TenantId, Code = "JT1", NameEn = "Accountant", NameAr = "محاسب" }).Entity;
            var future = context.Set<JobTitle>().Add(new JobTitle { TenantId = TenantId, Code = "JT2", NameEn = "Chief Accountant", NameAr = "رئيس" }).Entity;
            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId, EmployeeId = employee.Id, JobTitleId = current.Id,
                StartDate = _today.AddYears(-1), EndDate = _today.AddDays(9)
            });
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId, EmployeeId = employee.Id, JobTitleId = future.Id,
                StartDate = _today.AddDays(10), EndDate = new DateTime(9999, 12, 31)
            });
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = TenantId, EmployeeId = employee.Id, ManagerId = oldManager.Id,
                StartDate = _today.AddYears(-1), EndDate = _today.AddDays(9)
            });
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = TenantId, EmployeeId = employee.Id, ManagerId = futureManager.Id,
                StartDate = _today.AddDays(10), EndDate = new DateTime(9999, 12, 31)
            });
        });

        var header = await _appService.GetHeader(new EntityDto(employee.Id));

        header.Code.ShouldBe("WB200");
        header.JobTitleNameEn.ShouldBe("Accountant");
        header.ManagerFullNameEn.ShouldBe("Old Manager");
    }

    [Fact]
    public async Task Should_Fall_Back_To_Latest_Assignment_When_None_Has_Started()
    {
        var employee = CreateEmployee("WB210", "New Joiner");
        UsingDbContext(context =>
        {
            var title = context.Set<JobTitle>().Add(new JobTitle { TenantId = TenantId, Code = "JT3", NameEn = "Clerk", NameAr = "كاتب" }).Entity;
            context.SaveChanges();
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId, EmployeeId = employee.Id, JobTitleId = title.Id,
                StartDate = _today.AddDays(5), EndDate = new DateTime(9999, 12, 31)
            });
        });

        var header = await _appService.GetHeader(new EntityDto(employee.Id));

        header.JobTitleNameEn.ShouldBe("Clerk");
    }

    [Fact]
    public async Task Should_Return_Last_Day_In_Service_Ignoring_Revoked_Separations()
    {
        var employee = CreateEmployee("WB220", "Leaver", EmployeeStatus.Inactive);
        UsingDbContext(context =>
        {
            var reason = context.Set<SeparationReason>().Add(new SeparationReason { TenantId = TenantId, Code = "R1", NameEn = "Resigned", NameAr = "استقالة" }).Entity;
            context.SaveChanges();

            context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId, EmployeeId = employee.Id, SeparationReasonId = reason.Id,
                SubmissionDate = new DateTime(2026, 5, 1), LastDayInService = new DateTime(2026, 6, 30),
                PlannedLastDayInService = new DateTime(2026, 6, 30)
            });
            context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId, EmployeeId = employee.Id, SeparationReasonId = reason.Id,
                SubmissionDate = new DateTime(2026, 7, 1), LastDayInService = new DateTime(2026, 8, 31),
                PlannedLastDayInService = new DateTime(2026, 8, 31), RevokeDate = new DateTime(2026, 7, 15)
            });
        });

        var header = await _appService.GetHeader(new EntityDto(employee.Id));

        header.Status.ShouldBe(EmployeeStatus.Inactive);
        header.LastDayInService.ShouldBe(new DateTime(2026, 6, 30));
    }

    [Fact]
    public async Task Should_Have_No_Last_Day_In_Service_For_A_Current_Employee()
    {
        var employee = CreateEmployee("WB230", "Stayer");

        var header = await _appService.GetHeader(new EntityDto(employee.Id));

        header.LastDayInService.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_An_Unknown_Employee()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => _appService.GetHeader(new EntityDto(int.MaxValue)));
    }

    #endregion

    #region GetNotReturnedLeaves

    [Fact]
    public async Task Should_List_An_Ended_Leave_With_No_Return_As_Not_Returned()
    {
        var employee = CreateEmployee("WB300", "On Leave");
        var annual = CreateAbsenceType("WBAL1", isReturnDateRequired: true);
        var leave = AddLeave(employee.Id, annual.Id, _today.AddDays(-20), _today.AddDays(-4));

        var result = await _appService.GetNotReturnedLeaves(new EntityDto(employee.Id));

        result.Items.Count.ShouldBe(1);
        result.Items[0].Id.ShouldBe(leave.Id);
        result.Items[0].AbsenceTypeNameEn.ShouldBe("WBAL1");
        result.Items[0].DaysOverdue.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Not_List_A_Leave_That_Is_Returned_Hourly_Still_Running_Or_Needs_No_Return()
    {
        var employee = CreateEmployee("WB310", "Not Overdue");
        var annual = CreateAbsenceType("WBAL2", isReturnDateRequired: true);
        var sick = CreateAbsenceType("WBSL2", isReturnDateRequired: false);

        AddLeave(employee.Id, annual.Id, _today.AddDays(-30), _today.AddDays(-20), l => { l.IsReturned = true; l.ReturnDate = _today.AddDays(-19); });
        AddLeave(employee.Id, annual.Id, _today.AddDays(-15), _today.AddDays(-15), l => l.AbsenceHours = 2);
        AddLeave(employee.Id, annual.Id, _today.AddDays(-2), _today.AddDays(3));
        AddLeave(employee.Id, sick.Id, _today.AddDays(-10), _today.AddDays(-8));

        var result = await _appService.GetNotReturnedLeaves(new EntityDto(employee.Id));

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Not_List_Another_Employees_Leave()
    {
        var employee = CreateEmployee("WB320", "Me");
        var other = CreateEmployee("WB321", "Someone Else");
        var annual = CreateAbsenceType("WBAL3", isReturnDateRequired: true);
        AddLeave(other.Id, annual.Id, _today.AddDays(-20), _today.AddDays(-4));

        var result = await _appService.GetNotReturnedLeaves(new EntityDto(employee.Id));

        result.Items.ShouldBeEmpty();
    }

    #endregion

    #region GetRecentAttendance

    private DateTime At(DateTime day, int hour) => day.AddHours(hour);

    [Fact]
    public async Task Should_Flag_Missing_In_And_Missing_Out_On_Working_Days()
    {
        var employee = CreateEmployee("WB400", "Puncher");
        var twoDaysAgo = _today.AddDays(-2);
        var yesterday = _today.AddDays(-1);
        AddTimeRegister(employee.Id, twoDaysAgo, default, At(twoDaysAgo, 15));
        AddTimeRegister(employee.Id, yesterday, At(yesterday, 7), default);

        var result = await _appService.GetRecentAttendance(new EntityDto(employee.Id));

        var missingIn = result.Items.Single(d => d.DayDate == twoDaysAgo);
        missingIn.IsMissingIn.ShouldBeTrue();
        missingIn.IsMissingOut.ShouldBeFalse();
        missingIn.ActualIn.ShouldBeNull();

        var missingOut = result.Items.Single(d => d.DayDate == yesterday);
        missingOut.IsMissingOut.ShouldBeTrue();
        missingOut.IsMissingIn.ShouldBeFalse();
        missingOut.ActualOut.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Flag_Missing_Out_For_Today()
    {
        var employee = CreateEmployee("WB410", "Still At Work");
        AddTimeRegister(employee.Id, _today, At(_today, 7), default);

        var result = await _appService.GetRecentAttendance(new EntityDto(employee.Id));

        result.Items.Single().IsMissingOut.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Flag_A_Missing_Punch_On_Leave_Or_Off_Days()
    {
        var employee = CreateEmployee("WB420", "Resting");
        var d1 = _today.AddDays(-1);
        var d2 = _today.AddDays(-2);
        var d3 = _today.AddDays(-3);
        AddTimeRegister(employee.Id, d1, At(d1, 7), default, r => r.LeaveDay = 1);
        AddTimeRegister(employee.Id, d2, At(d2, 7), default, r => r.WeeklyOff = 1);
        AddTimeRegister(employee.Id, d3, At(d3, 7), default, r => r.PublicHoliday = 1);

        var result = await _appService.GetRecentAttendance(new EntityDto(employee.Id));

        result.Items.Count.ShouldBe(3);
        result.Items.ShouldAllBe(d => !d.IsMissingIn && !d.IsMissingOut && !d.IsWorkingDay);
        result.Items.Single(d => d.DayDate == d1).IsOnLeave.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Treat_The_Legacy_1800_Date_As_No_Punch()
    {
        var employee = CreateEmployee("WB430", "Migrated");
        var yesterday = _today.AddDays(-1);
        AddTimeRegister(employee.Id, yesterday, At(yesterday, 7), new DateTime(1800, 1, 1));

        var result = await _appService.GetRecentAttendance(new EntityDto(employee.Id));

        var day = result.Items.Single();
        day.ActualOut.ShouldBeNull();
        day.IsMissingOut.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Return_Only_The_Last_Seven_Days_Newest_First()
    {
        var employee = CreateEmployee("WB440", "Regular");
        for (var i = 0; i < 10; i++)
        {
            var day = _today.AddDays(-i);
            AddTimeRegister(employee.Id, day, At(day, 7), At(day, 15));
        }

        var result = await _appService.GetRecentAttendance(new EntityDto(employee.Id));

        result.Items.Count.ShouldBe(EmployeeWorkbenchAppService.RecentAttendanceDays);
        result.Items.First().DayDate.ShouldBe(_today);
        result.Items.Last().DayDate.ShouldBe(_today.AddDays(-6));
        result.Items.ShouldAllBe(d => !d.IsMissingIn && !d.IsMissingOut && d.IsWorkingDay);
    }

    #endregion
}
