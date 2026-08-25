using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.ProjectReport;
using HCMv3.GridReports.ProjectReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class ProjectMonthwiseSummaryReportAppService_Tests : AppTestBase
{
    private readonly IProjectMonthwiseSummaryReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public ProjectMonthwiseSummaryReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IProjectMonthwiseSummaryReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Employee employee = null;
        UsingDbContext(context =>
        {
            var nationality = context.Set<Nationality>().Add(new Nationality
            { TenantId = TenantId, Code = Guid.NewGuid().ToString("N").Substring(0, 8), NameEn = "N", NameAr = "N" }).Entity;
            context.SaveChanges();

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
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private Project CreateProject(string code)
    {
        Project project = null;
        UsingDbContext(context =>
        {
            var type = context.Set<ProjectType>().Add(new ProjectType
            { TenantId = TenantId, Code = "PT-" + code, NameEn = "T", NameAr = "T" }).Entity;
            context.SaveChanges();

            project = context.Set<Project>().Add(new Project
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "Project " + code,
                NameAr = "مشروع " + code,
                ProjectTypeId = type.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31)
            }).Entity;
        });

        return project;
    }

    private WageType CreateWageType(string code, BillingType billingType, bool dontImpactNetPay = false)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "WT-" + code,
                NameAr = "WT-" + code,
                BillingType = billingType,
                DontImpactNetPay = dontImpactNetPay,
                PayrollOrder = 1
            }).Entity;
        });

        return wageType;
    }

    private void AddDetail(Employee employee, Project project, DateTime period, WageType wageType, double amount, double number = 0)
    {
        UsingDbContext(context =>
        {
            var register = context.Set<ProjectRegister>().Add(new ProjectRegister
            { TenantId = TenantId, EmployeeId = employee.Id, Period = period }).Entity;
            context.SaveChanges();

            context.Set<ProjectRegisterDetail>().Add(new ProjectRegisterDetail
            {
                TenantId = TenantId,
                ProjectRegisterId = register.Id,
                ProjectId = project.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                Number = number
            });
        });
    }

    [Fact]
    public async Task Should_Pivot_WorkingDays_Per_Project_From_WageType_1000_Number()
    {
        var employee = CreateEmployee("PMS-1");
        var projectA = CreateProject("PROJ-PMS-A");
        var projectB = CreateProject("PROJ-PMS-B");
        var period = new DateTime(2024, 1, 31);
        var basicPay = CreateWageType("1000", BillingType.Earning);

        AddDetail(employee, projectA, period, basicPay, amount: 100, number: 20);
        AddDetail(employee, projectB, period, basicPay, amount: 200, number: 10);

        var result = await _appService.GetProjectMonthwiseSummaryReport(
            new GetProjectMonthwiseSummaryReportInput { Month = 1, Year = 2024, EmployeeCode = "PMS-1" });

        var row = result.Items.Single();
        row.ProjectAmounts.Count.ShouldBe(2);
        row.TotalWorkingDays.ShouldBe(30);
        row.ProjectAmounts.Single(p => p.ProjectCode == "PROJ-PMS-A").WorkingDays.ShouldBe(20);
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_Overtime_Hours_Via_OvertimeType_Linked_WageType()
    {
        var employee = CreateEmployee("PMS-2");
        var project = CreateProject("PROJ-PMS-OT");
        var period = new DateTime(2024, 2, 29);
        var otWageType = CreateWageType("1007", BillingType.Earning);

        UsingDbContext(context =>
        {
            context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = TenantId,
                Code = "OT1",
                NameEn = "Overtime",
                NameAr = "Overtime",
                WageTypeId = otWageType.Id
            });
        });

        AddDetail(employee, project, period, otWageType, amount: 75, number: 5);

        var result = await _appService.GetProjectMonthwiseSummaryReport(
            new GetProjectMonthwiseSummaryReportInput { Month = 2, Year = 2024, EmployeeCode = "PMS-2" });

        var row = result.Items.Single();
        row.TotalOTHours.ShouldBe(5);
        row.OTAmount.ShouldBe(75);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var first = CreateEmployee("PMS-EXP-A");
        var second = CreateEmployee("PMS-EXP-B");
        var project = CreateProject("PROJ-PMS-EXP");
        var period = new DateTime(2024, 3, 31);
        var basicPay = CreateWageType("1000", BillingType.Earning);

        AddDetail(first, project, period, basicPay, amount: 100, number: 20);
        AddDetail(second, project, period, basicPay, amount: 200, number: 15);

        var input = new GetProjectMonthwiseSummaryReportInput
        {
            Month = 3,
            Year = 2024,
            EmployeeCode = "PMS-EXP-A,PMS-EXP-B",
            MaxResultCount = 1
        };

        var file = await _appService.GetProjectMonthwiseSummaryReportToExcel(input);

        file.FileName.ShouldBe("ProjectMonthwiseSummary.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
        exported.First().Keys.ShouldContain("TotalWorkingDays");
    }
}
