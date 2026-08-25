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
}
