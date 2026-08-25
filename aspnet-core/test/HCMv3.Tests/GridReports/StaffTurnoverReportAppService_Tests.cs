using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class StaffTurnoverReportAppService_Tests : AppTestBase
{
    private readonly IStaffTurnoverReportAppService _appService;

    public StaffTurnoverReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IStaffTurnoverReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, DateTime joiningDate)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
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
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = joiningDate,
                ProbationEndDate = joiningDate.AddMonths(3),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    [Fact]
    public async Task Should_Only_Include_Employees_Who_Joined_In_The_Requested_Window()
    {
        CreateEmployee("STO-IN", new DateTime(2024, 6, 15));
        CreateEmployee("STO-OUT", new DateTime(2023, 1, 1));

        var result = await _appService.GetStaffTurnoverReport(new GetStaffTurnoverReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            MaxResultCount = 100
        });

        var codes = result.Items.Select(i => i.AvgNoOfEmp).ToList();
        codes.ShouldContain("STO-IN");
        codes.ShouldNotContain("STO-OUT");
    }

    /// <summary>
    /// LEGACY DEFECT REPRODUCED: legacy never computes a real turnover rate -- Period is literally the
    /// employee's surrogate id and AvgNoOfEmp/Turnover both just repeat the employee CODE. See
    /// StaffTurnoverReportRowDto backend remarks for the full explanation; this test pins the reproduction.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Meaningless_Placeholder_Columns()
    {
        var employee = CreateEmployee("STO-PLACEHOLDER", new DateTime(2024, 3, 1));

        var result = await _appService.GetStaffTurnoverReport(new GetStaffTurnoverReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            MaxResultCount = 100
        });

        var row = result.Items.Single(i => i.AvgNoOfEmp == "STO-PLACEHOLDER");
        row.Period.ShouldBe(employee.Id.ToString());
        row.AvgNoOfEmp.ShouldBe("STO-PLACEHOLDER");
        row.Turnover.ShouldBe("STO-PLACEHOLDER");
    }
}
