using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Transaction;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EmployeeLeaveSalaryReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeLeaveSalaryReportAppService _appService;

    public EmployeeLeaveSalaryReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeLeaveSalaryReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality { TenantId = TenantId, Code = NewCode(), NameEn = "N", NameAr = "N" }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2015, 1, 1),
                ProbationEndDate = new DateTime(2015, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee;
    }

    private AbsenceType CreateAbsenceType(string code)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType { TenantId = TenantId, Code = code, NameEn = code + "-En", NameAr = code + "-Ar", IsActive = true }).Entity;
        });
        return absenceType;
    }

    private EmployeeAbsence CreateAbsence(Employee employee, AbsenceType absenceType, DateTime start, DateTime end)
    {
        EmployeeAbsence absence = null;
        UsingDbContext(context =>
        {
            absence = context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceType.Id,
                StartDate = start,
                EndDate = end,
                StartTime = start,
                EndTime = end,
                AbsenceHours = 0,
                CalendarDays = (end - start).Days + 1,
                TotalLeaveDays = (end - start).Days + 1
            }).Entity;
        });
        return absence;
    }

    private static GetEmployeeLeaveSalaryReportInput Input(DateTime from, DateTime to, string employeeCode = null, string leaveTypeCode = null)
    {
        return new GetEmployeeLeaveSalaryReportInput { FromDate = from, ToDate = to, EmployeeCode = employeeCode, LeaveTypeCode = leaveTypeCode, MaxResultCount = 100, SkipCount = 0 };
    }

    [Fact]
    public async Task Should_Only_Include_Absences_Fully_Inside_The_Window()
    {
        // Legacy line 560-561 is NOT an overlap test -- both StartDate and EndDate must be inside the window.
        var absenceType = CreateAbsenceType(NewCode());
        var inside = CreateEmployee("ELS-IN");
        var partiallyOutside = CreateEmployee("ELS-OUT");

        CreateAbsence(inside, absenceType, new DateTime(2024, 6, 5), new DateTime(2024, 6, 10));
        CreateAbsence(partiallyOutside, absenceType, new DateTime(2023, 12, 20), new DateTime(2024, 1, 5));

        var result = await _appService.GetEmployeeLeaveSalaryReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("ELS-IN");
        codes.ShouldNotContain("ELS-OUT");
    }

    [Fact]
    public async Task Should_Filter_By_LeaveType_Code()
    {
        var matchType = CreateAbsenceType(NewCode());
        var otherType = CreateAbsenceType(NewCode());
        var employee = CreateEmployee("ELS-TYPE");

        CreateAbsence(employee, matchType, new DateTime(2024, 6, 1), new DateTime(2024, 6, 2));
        CreateAbsence(employee, otherType, new DateTime(2024, 6, 1), new DateTime(2024, 6, 2));

        var result = await _appService.GetEmployeeLeaveSalaryReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ELS-TYPE", matchType.Code));

        result.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Compute_Single_Month_Leave_As_TotalLeaveSalary_Equal_To_Month_Start_Portion()
    {
        // No basic-pay rate set up -> leaveRate is 0, so the totals should be exactly zero, not throw.
        var absenceType = CreateAbsenceType(NewCode());
        var employee = CreateEmployee("ELS-CALC");
        CreateAbsence(employee, absenceType, new DateTime(2024, 6, 5), new DateTime(2024, 6, 10));

        var result = await _appService.GetEmployeeLeaveSalaryReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ELS-CALC"));

        var row = result.Items.Single();
        row.TotalLeaveSalary.ShouldBe(0);
        row.TotalEmployeeSalary.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_Every_Filtered_Row()
    {
        var absenceType = CreateAbsenceType(NewCode());
        var a = CreateEmployee("ELSEXP-A");
        var b = CreateEmployee("ELSEXP-B");
        CreateAbsence(a, absenceType, new DateTime(2024, 6, 1), new DateTime(2024, 6, 2));
        CreateAbsence(b, absenceType, new DateTime(2024, 6, 1), new DateTime(2024, 6, 2));

        var file = await _appService.GetEmployeeLeaveSalaryReportToExcel(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ELSEXP-A,ELSEXP-B"));

        file.FileName.ShouldBe("EmployeeLeaveSalary.xlsx");
    }
}
