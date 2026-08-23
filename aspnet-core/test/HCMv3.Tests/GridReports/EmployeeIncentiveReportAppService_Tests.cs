using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Incentive grid-report port (legacy <c>uscRptEmployeeIncentive.ascx</c>,
/// <c>Table1()</c> + <c>Table2()</c>). This is the only report in its batch with a real DRILL-DOWN and the
/// only one that uses the date range, so both are pinned here.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeIncentiveReportAppService_Tests : AppTestBase
{
    private const string IncentiveWageTypeCode = "1012";

    /// <summary>Source of unique <c>OrganizationUnit.Code</c> values across the whole test class.</summary>
    private static int _nextOrgUnitCodeNumber = 5000;

    private readonly IEmployeeIncentiveReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeIncentiveReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeIncentiveReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
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
                Status = status
            }).Entity;
        });

        return employee;
    }

    private WageType CreateIncentiveWageType()
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().FirstOrDefault(w => w.Code == IncentiveWageTypeCode);
            if (wageType == null)
            {
                wageType = context.Set<WageType>().Add(new WageType
                {
                    TenantId = TenantId,
                    Code = IncentiveWageTypeCode,
                    NameEn = "Incentive",
                    NameAr = "Incentive Ar"
                }).Entity;
            }
        });

        return wageType;
    }

    /// <summary>Creates an org unit with its HR profile, i.e. the "department" the master grid groups on.</summary>
    private long CreateOrgUnit(string displayName)
    {
        long organizationUnitId = 0;

        UsingDbContext(context =>
        {
            // OrganizationUnit.Code is normally assigned by OrganizationUnitManager; created directly here,
            // so a unique 5-digit tree code is stamped explicitly (the resolver prefix-matches on it).
            var unit = context.Set<OrganizationUnit>().Add(new OrganizationUnit(TenantId, displayName)
            {
                Code = OrganizationUnit.CreateCode(++_nextOrgUnitCodeNumber)
            }).Entity;
            context.SaveChanges();

            var unitType = context.Set<OrganizationUnitType>()
                .FirstOrDefault(t => t.Code == OrganizationUnitType.Level1Code);

            if (unitType == null)
            {
                unitType = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
                {
                    TenantId = TenantId,
                    Code = OrganizationUnitType.Level1Code,
                    NameEn = "Level 1",
                    NameAr = "Level 1 Ar"
                }).Entity;
                context.SaveChanges();
            }

            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = TenantId,
                OrganizationUnitId = unit.Id,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameAr = displayName + "-Ar",
                OrganizationUnitTypeId = unitType.Id
            });

            context.SaveChanges();
            organizationUnitId = unit.Id;
        });

        return organizationUnitId;
    }

    private void CreateOrgAssignment(Employee employee, long organizationUnitId, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                OrganizationUnitId = organizationUnitId
            });
        });
    }

    private void CreateIncentive(Employee employee, WageType wageType, DateTime paymentDate)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeRemuneration>().Add(new EmployeeRemuneration
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                WageTypeId = wageType.Id,
                PaymentDate = paymentDate,
                PaymentNumber = 1
            });
        });
    }

    private static GetEmployeeIncentiveReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeIncentiveReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    private static GetEmployeeIncentiveDetailReportInput DetailInput(
        long organizationUnitId,
        DateTime from,
        DateTime to,
        int maxResultCount = 100)
    {
        return new GetEmployeeIncentiveDetailReportInput
        {
            OrganizationUnitId = organizationUnitId,
            FromDate = from,
            ToDate = to,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Group_Master_Rows_By_Organization_Unit_And_Count_Transactions()
    {
        var wageType = CreateIncentiveWageType();
        var unitA = CreateOrgUnit("INC-UNIT-A");
        var unitB = CreateOrgUnit("INC-UNIT-B");

        var first = CreateEmployee("INC-G1");
        var second = CreateEmployee("INC-G2");
        var third = CreateEmployee("INC-G3");

        var today = DateTime.Now.Date;
        CreateOrgAssignment(first, unitA, today.AddYears(-2), today.AddYears(2));
        CreateOrgAssignment(second, unitA, today.AddYears(-2), today.AddYears(2));
        CreateOrgAssignment(third, unitB, today.AddYears(-2), today.AddYears(2));

        // Legacy counted TRANSACTIONS, not employees, despite the "Employee" caption.
        CreateIncentive(first, wageType, new DateTime(2024, 3, 1));
        CreateIncentive(first, wageType, new DateTime(2024, 4, 1));
        CreateIncentive(second, wageType, new DateTime(2024, 5, 1));
        CreateIncentive(third, wageType, new DateTime(2024, 6, 1));

        var result = await _appService.GetEmployeeIncentiveReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
                employeeCode: "INC-G1,INC-G2,INC-G3"));

        var rowA = result.Items.Single(i => i.OrganizationUnitId == unitA);
        rowA.EmployeeCount.ShouldBe(3);

        var rowB = result.Items.Single(i => i.OrganizationUnitId == unitB);
        rowB.EmployeeCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Include_Transactions_Touching_The_Window_Boundary()
    {
        var wageType = CreateIncentiveWageType();
        var unit = CreateOrgUnit("INC-EDGE-UNIT");

        var onFrom = CreateEmployee("INC-EDGE-FROM");
        var onTo = CreateEmployee("INC-EDGE-TO");
        var outside = CreateEmployee("INC-EDGE-OUT");

        var today = DateTime.Now.Date;
        CreateOrgAssignment(onFrom, unit, today.AddYears(-2), today.AddYears(2));
        CreateOrgAssignment(onTo, unit, today.AddYears(-2), today.AddYears(2));
        CreateOrgAssignment(outside, unit, today.AddYears(-2), today.AddYears(2));

        // Legacy predicate is PaymentDate >= From && PaymentDate <= To -- both ends INCLUSIVE.
        CreateIncentive(onFrom, wageType, new DateTime(2024, 1, 1));
        CreateIncentive(onTo, wageType, new DateTime(2024, 12, 31));
        CreateIncentive(outside, wageType, new DateTime(2025, 1, 1));

        var result = await _appService.GetEmployeeIncentiveDetailReport(
            DetailInput(unit, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("INC-EDGE-FROM");
        codes.ShouldContain("INC-EDGE-TO");
        codes.ShouldNotContain("INC-EDGE-OUT");
    }

    [Fact]
    public async Task Should_Restrict_Detail_Rows_To_The_Drilled_Into_Organization_Unit()
    {
        var wageType = CreateIncentiveWageType();
        var unitA = CreateOrgUnit("INC-DRILL-A");
        var unitB = CreateOrgUnit("INC-DRILL-B");

        var inA = CreateEmployee("INC-DRILL-1");
        var inB = CreateEmployee("INC-DRILL-2");

        var today = DateTime.Now.Date;
        CreateOrgAssignment(inA, unitA, today.AddYears(-2), today.AddYears(2));
        CreateOrgAssignment(inB, unitB, today.AddYears(-2), today.AddYears(2));

        CreateIncentive(inA, wageType, new DateTime(2024, 3, 1));
        CreateIncentive(inB, wageType, new DateTime(2024, 3, 1));

        var result = await _appService.GetEmployeeIncentiveDetailReport(
            DetailInput(unitA, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("INC-DRILL-1");
        codes.ShouldNotContain("INC-DRILL-2");

        // The org level chain must be stamped on (it is resolved after the query, not in SQL).
        result.Items.Single().OrgLevel1.ShouldBe("INC-DRILL-A");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Detail_Rows_Not_Just_The_Current_Page()
    {
        var wageType = CreateIncentiveWageType();
        var unit = CreateOrgUnit("INC-EXP-UNIT");

        var today = DateTime.Now.Date;
        for (var i = 1; i <= 3; i++)
        {
            var employee = CreateEmployee("INC-EXP-" + i);
            CreateOrgAssignment(employee, unit, today.AddYears(-2), today.AddYears(2));
            CreateIncentive(employee, wageType, new DateTime(2024, 3, 1));
        }

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var input = DetailInput(unit, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), maxResultCount: 1);

        var file = await _appService.GetEmployeeIncentiveDetailReportToExcel(input);

        file.FileName.ShouldBe("EmployeeIncentiveDetail.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.First().Keys.ShouldContain("Wage Type");
    }
}
