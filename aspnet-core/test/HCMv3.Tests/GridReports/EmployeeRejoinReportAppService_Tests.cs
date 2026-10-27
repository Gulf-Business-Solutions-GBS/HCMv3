using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Rejoin grid-report port (legacy <c>uscRptEmployeeRejoin.ascx</c>,
/// <c>Table1()</c>). Note this report CONTAINS rather than OVERLAPS: legacy filters on
/// <c>StartDate &gt;= dtFrom &amp;&amp; StartDate &lt;= dtTo</c>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeRejoinReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeRejoinReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeRejoinReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeRejoinReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(
        string code,
        EmployeeStatus status = EmployeeStatus.Active,
        string previousEmpCode = null)
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
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                PreviousEmpCode = previousEmpCode,
                Status = status
            }).Entity;
        });

        return employee;
    }

    private void CreateRejoin(Employee employee, DateTime rejoinDate, bool carryForwardLeave = false)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeRejoin>().Add(new EmployeeRejoin
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                RejoinDate = rejoinDate,
                CarryForwardLeaveBalance = carryForwardLeave,
                CarryForwardIndemnityBalance = !carryForwardLeave,
                CarryForwardTicketBalance = carryForwardLeave,
                CarryForwardLoanBalance = !carryForwardLeave
            });
        });
    }

    private static GetEmployeeRejoinReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeRejoinReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Rejoins_Inside_The_Requested_Window()
    {
        var inRange = CreateEmployee("RJ-IN");
        var before = CreateEmployee("RJ-BEFORE");
        var after = CreateEmployee("RJ-AFTER");

        CreateRejoin(inRange, new DateTime(2024, 6, 1));
        CreateRejoin(before, new DateTime(2023, 12, 31));
        CreateRejoin(after, new DateTime(2025, 1, 1));

        var result = await _appService.GetEmployeeRejoinReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("RJ-IN");
        codes.ShouldNotContain("RJ-BEFORE");
        codes.ShouldNotContain("RJ-AFTER");
    }

    [Fact]
    public async Task Should_Include_Rejoins_Landing_Exactly_On_The_Window_Boundaries()
    {
        var onFrom = CreateEmployee("RJEDGE-FROM");
        var onTo = CreateEmployee("RJEDGE-TO");

        // Legacy predicate is StartDate >= dtFrom && StartDate <= dtTo -- both ends inclusive.
        CreateRejoin(onFrom, new DateTime(2024, 1, 1));
        CreateRejoin(onTo, new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeRejoinReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("RJEDGE-FROM");
        codes.ShouldContain("RJEDGE-TO");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status_And_Codes()
    {
        var active = CreateEmployee("RJST-ACTIVE");
        var inactive = CreateEmployee("RJST-INACTIVE", EmployeeStatus.Inactive);

        CreateRejoin(active, new DateTime(2024, 5, 1));
        CreateRejoin(inactive, new DateTime(2024, 5, 1));

        var byStatus = await _appService.GetEmployeeRejoinReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        byStatus.Items.Select(i => i.EmployeeCode).ShouldContain("RJST-INACTIVE");
        byStatus.Items.Select(i => i.EmployeeCode).ShouldNotContain("RJST-ACTIVE");

        var byCode = await _appService.GetEmployeeRejoinReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "RJST-ACTIVE"));

        byCode.Items.Select(i => i.EmployeeCode).ShouldBe(new List<string> { "RJST-ACTIVE" });
    }

    [Fact]
    public async Task Should_Map_CarryForward_Flags_To_The_Legacy_Rejoin_Columns()
    {
        var employee = CreateEmployee("RJFLAG-1");
        CreateRejoin(employee, new DateTime(2024, 3, 1), carryForwardLeave: true);

        var result = await _appService.GetEmployeeRejoinReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "RJFLAG-1"));

        var row = result.Items.Single();
        row.StartDate.ShouldBe(new DateTime(2024, 3, 1));
        row.RejoinWithPrevLeaveAccrual.ShouldBeTrue();
        row.RejoinWithPrevTicketAccrual.ShouldBeTrue();
        row.RejoinWithPrevIndemnityAccrual.ShouldBeFalse();
        row.RejoinWithPrevLoan.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Resolve_NewEmployeeCode_From_The_Record_Pointing_Back_Through_PreviousEmpCode()
    {
        var rejoined = CreateEmployee("RJNEW-OLD");
        CreateEmployee("RJNEW-NEW", previousEmpCode: "RJNEW-OLD");
        CreateRejoin(rejoined, new DateTime(2024, 3, 1));

        var result = await _appService.GetEmployeeRejoinReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "RJNEW-OLD"));

        result.Items.Single().NewEmployeeCode.ShouldBe("RJNEW-NEW");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var first = CreateEmployee("RJEXP-A");
        var second = CreateEmployee("RJEXP-B");
        var third = CreateEmployee("RJEXP-C");
        var excluded = CreateEmployee("RJEXP-OUT");

        CreateRejoin(first, new DateTime(2024, 2, 1));
        CreateRejoin(second, new DateTime(2024, 3, 1));
        CreateRejoin(third, new DateTime(2024, 4, 1));
        CreateRejoin(excluded, new DateTime(2024, 5, 1));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
            employeeCode: "RJEXP-A,RJEXP-B,RJEXP-C", maxResultCount: 1);

        var file = await _appService.GetEmployeeRejoinReportToExcel(input);

        file.FileName.ShouldBe("EmployeeRejoin.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c)
            .ShouldBe(new List<string> { "RJEXP-A", "RJEXP-B", "RJEXP-C" });
    }
}
