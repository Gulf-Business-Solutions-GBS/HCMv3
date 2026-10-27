using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Transaction;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EmpOvertimeByDayReportAppService_Tests : AppTestBase
{
    private readonly IEmpOvertimeByDayReportAppService _appService;

    public EmpOvertimeByDayReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmpOvertimeByDayReportAppService>();
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
                NationalityId = nationality.Id,
                AccessID = "A-" + code,
                ReferenceCode = "R-" + code
            }).Entity;
        });

        return employee;
    }

    private OvertimeType CreateOvertimeType(string code)
    {
        OvertimeType overtimeType = null;
        UsingDbContext(context =>
        {
            overtimeType = context.Set<OvertimeType>().Add(new OvertimeType { TenantId = TenantId, Code = code, NameEn = "OT-" + code, NameAr = "OT-" + code }).Entity;
        });
        return overtimeType;
    }

    private void CreateOvertime(Employee employee, OvertimeType overtimeType, DateTime effectiveDate, double approvedHours)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOvertime>().Add(new EmployeeOvertime
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                OvertimeTypeId = overtimeType.Id,
                EffectiveDate = effectiveDate,
                ApprovedOvertimeHours = approvedHours
            });
        });
    }

    private void CreateTimeRegister(Employee employee, DateTime dayDate, double actualWorkingMins, double absentDay = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DayDate = dayDate,
                ActualWorkingMins = actualWorkingMins,
                AbsentDay = absentDay
            });
        });
    }

    private static GetEmpOvertimeByDayReportInput Input(DateTime from, DateTime to, string employeeCode = null)
    {
        return new GetEmpOvertimeByDayReportInput { FromDate = from, ToDate = to, EmployeeCode = employeeCode, MaxResultCount = 100, SkipCount = 0 };
    }

    [Fact]
    public async Task Should_Only_Include_Employees_Who_Have_An_Overtime_Row_Ever()
    {
        var withOvertime = CreateEmployee("EOD-HAS-OT");
        var withoutOvertime = CreateEmployee("EOD-NO-OT");
        var type = CreateOvertimeType(NewCode());
        CreateOvertime(withOvertime, type, new DateTime(2020, 1, 1), 2);

        var result = await _appService.GetEmpOvertimeByDayReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("EOD-HAS-OT");
        codes.ShouldNotContain("EOD-NO-OT");
    }

    [Fact]
    public async Task Should_Sum_ActualWorkingHours_And_TotalOTHours_Within_The_Window()
    {
        var employee = CreateEmployee("EOD-SUM");
        var type = CreateOvertimeType(NewCode());
        CreateOvertime(employee, type, new DateTime(2020, 1, 1), 1); // establishes universe membership
        CreateOvertime(employee, type, new DateTime(2024, 6, 5), 4);
        CreateTimeRegister(employee, new DateTime(2024, 6, 5), actualWorkingMins: 480);

        var result = await _appService.GetEmpOvertimeByDayReport(Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), "EOD-SUM"));

        var row = result.Items.Single();
        row.ActualWorkingHours.ShouldBe(8);
        row.TotalOTHours.ShouldBe(4);
        row.TotalHours.ShouldBe(12);
        row.AccessId.ShouldBe("A-EOD-SUM");
    }

    [Fact]
    public async Task Should_Resolve_OrgLevel_Columns_Without_Crashing()
    {
        var employee = CreateEmployee("EOD-ORG");
        var type = CreateOvertimeType(NewCode());
        CreateOvertime(employee, type, new DateTime(2024, 6, 1), 1);

        var result = await _appService.GetEmpOvertimeByDayReport(Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), "EOD-ORG"));

        result.Items.Single().OrgLevel1.ShouldBe(string.Empty);
    }
}
