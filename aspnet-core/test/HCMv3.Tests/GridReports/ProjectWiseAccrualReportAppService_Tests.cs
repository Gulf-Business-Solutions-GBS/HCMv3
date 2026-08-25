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
public class ProjectWiseAccrualReportAppService_Tests : AppTestBase
{
    private readonly IProjectWiseAccrualReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public ProjectWiseAccrualReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IProjectWiseAccrualReportAppService>();
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

    /// <summary>
    /// Creates the PayrollCalendar row the report resolves Month/Year through -- without one, the report
    /// falls back to legacy's 1900 sentinel period and finds nothing.
    /// </summary>
    private void CreatePayrollCalendar(int month, int year, DateTime periodEnd)
    {
        UsingDbContext(context =>
        {
            var currency = context.Set<HCMv3.GeneralMasterInfo.Currency>().Add(new HCMv3.GeneralMasterInfo.Currency
            { TenantId = TenantId, Code = Guid.NewGuid().ToString("N").Substring(0, 3), NameEn = "Cur", NameAr = "Cur" }).Entity;
            var payrollPeriod = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            { TenantId = TenantId, Code = Guid.NewGuid().ToString("N").Substring(0, 8), NameEn = "P", NameAr = "P" }).Entity;
            context.SaveChanges();

            var payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = "PA",
                CurrencyId = currency.Id,
                PayrollPeriodId = payrollPeriod.Id
            }).Entity;
            context.SaveChanges();

            context.Set<PayrollCalendar>().Add(new PayrollCalendar
            {
                TenantId = TenantId,
                PayrollAreaId = payrollArea.Id,
                PayrollMonth = month,
                PayrollYear = year,
                StartDate = new DateTime(periodEnd.Year, periodEnd.Month, 1),
                EndDate = periodEnd
            });
        });
    }

    private void AddDetailWithCode(Employee employee, Project project, DateTime period, string wageTypeCode, double amount, double number = 0)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().FirstOrDefault(w => w.Code == wageTypeCode);
            if (wageType == null)
            {
                wageType = context.Set<WageType>().Add(new WageType
                { TenantId = TenantId, Code = wageTypeCode, NameEn = "WT-" + wageTypeCode, NameAr = "WT-" + wageTypeCode, PayrollOrder = 1 }).Entity;
                context.SaveChanges();
            }

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

    private static GetProjectWiseAccrualReportInput Input(int month, int year, string employeeCode = null, int maxResultCount = 100)
    {
        return new GetProjectWiseAccrualReportInput { Month = month, Year = year, EmployeeCode = employeeCode, MaxResultCount = maxResultCount };
    }

    [Fact]
    public async Task Should_Return_Rows_Only_For_The_Requested_Period()
    {
        var employee = CreateEmployee("PWA-1");
        var project = CreateProject("PROJ-PWA1");
        var period = new DateTime(2024, 1, 31);
        var otherPeriod = new DateTime(2024, 2, 29);
        CreatePayrollCalendar(1, 2024, period);

        AddDetailWithCode(employee, project, period, "1000", 1000);
        AddDetailWithCode(employee, project, otherPeriod, "1000", 2000);

        var result = await _appService.GetProjectWiseAccrualReport(Input(1, 2024, "PWA-1"));

        result.Items.Count.ShouldBe(1);
        result.Items.Single().BasicPay.ShouldBe(1000);
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_NetPay_As_Sum_Of_The_Eight_Pay_Columns()
    {
        var employee = CreateEmployee("PWA-2");
        var project = CreateProject("PROJ-PWA2");
        var period = new DateTime(2024, 3, 31);
        CreatePayrollCalendar(3, 2024, period);

        AddDetailWithCode(employee, project, period, "1000", 500); // BasicPay
        AddDetailWithCode(employee, project, period, "1003", 100); // AnnualLeavePay
        AddDetailWithCode(employee, project, period, "1013", 50);  // SickLeavePay

        var result = await _appService.GetProjectWiseAccrualReport(Input(3, 2024, "PWA-2"));

        var row = result.Items.Single();
        row.NetPay.ShouldBe(650);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var first = CreateEmployee("PWA-EXP-A");
        var second = CreateEmployee("PWA-EXP-B");
        var project = CreateProject("PROJ-PWA-EXP");
        var period = new DateTime(2024, 4, 30);
        CreatePayrollCalendar(4, 2024, period);

        AddDetailWithCode(first, project, period, "1000", 100);
        AddDetailWithCode(second, project, period, "1000", 200);

        var input = Input(4, 2024, "PWA-EXP-A,PWA-EXP-B", maxResultCount: 1);

        var file = await _appService.GetProjectWiseAccrualReportToExcel(input);

        file.FileName.ShouldBe("ProjectWiseAccrual.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
        exported.First().Keys.ShouldContain("Net Pay");
    }
}
