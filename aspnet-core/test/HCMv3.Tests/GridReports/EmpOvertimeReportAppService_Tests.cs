using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Transaction;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EmpOvertimeReportAppService_Tests : AppTestBase
{
    private readonly IEmpOvertimeReportAppService _appService;

    public EmpOvertimeReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmpOvertimeReportAppService>();
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

    private OvertimeType CreateOvertimeType(string code, string nameEn)
    {
        OvertimeType overtimeType = null;
        UsingDbContext(context =>
        {
            overtimeType = context.Set<OvertimeType>().Add(new OvertimeType { TenantId = TenantId, Code = code, NameEn = nameEn, NameAr = nameEn }).Entity;
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

    private static GetEmpOvertimeReportInput Input(DateTime from, DateTime to, string employeeCode = null, string overtimeTypeCode = null)
    {
        return new GetEmpOvertimeReportInput { FromDate = from, ToDate = to, EmployeeCode = employeeCode, OvertimeTypeCode = overtimeTypeCode, MaxResultCount = 100, SkipCount = 0 };
    }

    [Fact]
    public async Task Should_Bucket_Hours_By_The_Four_Legacy_Overtime_Type_Names()
    {
        var employee = CreateEmployee("EOT-1");
        var normal = CreateOvertimeType(NewCode(), "Normal Day OT");
        var weekend = CreateOvertimeType(NewCode(), "Weekend OT");

        CreateOvertime(employee, normal, new DateTime(2024, 6, 1), 3);
        CreateOvertime(employee, weekend, new DateTime(2024, 6, 2), 5);

        var result = await _appService.GetEmpOvertimeReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EOT-1"));

        var row = result.Items.Single();
        row.NormalHours.ShouldBe(3);
        row.WeekendHours.ShouldBe(5);
        row.TotalHours.ShouldBe(8);
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_Overtime_Type_Bucketing_By_English_Name_Behavior()
    {
        // An overtime type not named one of the four legacy literals falls into NO bucket -- reproduced
        // exactly, this is not a data-loss defect: the row still exists, just with zeroed buckets.
        var employee = CreateEmployee("EOT-UNBUCKETED");
        var customType = CreateOvertimeType(NewCode(), "Custom Overtime");
        CreateOvertime(employee, customType, new DateTime(2024, 6, 1), 4);

        var result = await _appService.GetEmpOvertimeReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EOT-UNBUCKETED"));

        var row = result.Items.Single();
        row.NormalHours.ShouldBe(0);
        row.WeekendHours.ShouldBe(0);
        row.PublicHolidayHours.ShouldBe(0);
        row.SpecialHours.ShouldBe(0);
        row.TotalHours.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Resolve_OrgLevel_Columns_Without_Crashing()
    {
        var employee = CreateEmployee("EOT-ORG");
        var type = CreateOvertimeType(NewCode(), "Normal Day OT");
        CreateOvertime(employee, type, new DateTime(2024, 6, 1), 1);

        var result = await _appService.GetEmpOvertimeReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EOT-ORG"));

        result.Items.Single().OrgLevel1.ShouldBe(string.Empty);
    }

    /// <summary>
    /// A payable overtime type: pay wage type plus a Fixed rate for 2024, with FixedHours payroll settings so
    /// the legacy hours-basis gate passes (legacy <c>EmployeeOvertime.cs</c> lines 208, 217-219, 235, 250).
    /// </summary>
    private OvertimeType CreatePayableOvertimeType(string nameEn, double overtimeRate, double fixedRate, bool seedSettings)
    {
        OvertimeType overtimeType = null;
        UsingDbContext(context =>
        {
            if (seedSettings)
            {
                context.Set<HCMv3.SystemSettings.ManagePayrollSettings>().Add(new HCMv3.SystemSettings.ManagePayrollSettings
                {
                    TenantId = TenantId,
                    OvertimeHoursBasis = HCMv3.SystemSettings.OvertimeHoursBasis.FixedHours,
                    DefaultWorkingHours = 8
                });
            }

            var wageType = context.Set<HCMv3.PayrollManagement.WageType>().Add(new HCMv3.PayrollManagement.WageType
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = "OT Pay",
                NameAr = "OT Pay",
                PayrollOrder = 1
            }).Entity;
            context.SaveChanges();

            overtimeType = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = TenantId,
                Code = NewCode(),
                NameEn = nameEn,
                NameAr = nameEn,
                OvertimeRate = overtimeRate,
                WageTypeId = wageType.Id
            }).Entity;
            context.SaveChanges();

            context.Set<OvertimeTypeDetail>().Add(new OvertimeTypeDetail
            {
                TenantId = TenantId,
                OvertimeTypeId = overtimeType.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                OvertimeRateType = OvertimeRateType.Fixed,
                Rate = fixedRate
            });
        });
        return overtimeType;
    }

    [Fact]
    public async Task Should_Sum_The_Expected_Overtime_Amount_Of_Every_Row_Of_The_Employee()
    {
        // Legacy uscRptEmpOvertime.ascx.cs line 618 (per row) and line 634 (g.Sum per employee). The sum
        // covers every overtime row in the window, including a type outside the four hour buckets.
        // Per row (legacy EmployeeOvertime.cs lines 206, 250, 294):
        //   Normal Day OT: 3 h x 1.5 x 2   = 9
        //   Custom OT:     2 h x 1.0 x 0.5 = 1
        var employee = CreateEmployee("EOT-AMT");
        var normal = CreatePayableOvertimeType("Normal Day OT", 1.5, 2, seedSettings: true);
        var custom = CreatePayableOvertimeType("Custom OT", 1.0, 0.5, seedSettings: false);
        CreateOvertime(employee, normal, new DateTime(2024, 6, 1), 3);
        CreateOvertime(employee, custom, new DateTime(2024, 6, 2), 2);

        var result = await _appService.GetEmpOvertimeReport(Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "EOT-AMT"));

        result.Items.Single().ExpectedOvertimeAmount.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Leave_Out_Rows_Outside_The_Window_From_The_Expected_Amount()
    {
        var employee = CreateEmployee("EOT-WIN");
        var normal = CreatePayableOvertimeType("Normal Day OT", 1.0, 2, seedSettings: true);
        CreateOvertime(employee, normal, new DateTime(2024, 6, 1), 1);
        CreateOvertime(employee, normal, new DateTime(2024, 9, 1), 5);

        var result = await _appService.GetEmpOvertimeReport(Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), "EOT-WIN"));

        // Only the June row: 1 h x 1.0 x 2 = 2.
        result.Items.Single().ExpectedOvertimeAmount.ShouldBe(2);
    }
}
