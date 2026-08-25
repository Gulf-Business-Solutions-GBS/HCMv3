using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
public class ProjectCostingReportAppService_Tests : AppTestBase
{
    private readonly IProjectCostingReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public ProjectCostingReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IProjectCostingReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Employee employee = null;
        UsingDbContext(context =>
        {
            var nationality = context.Set<HCMv3.GeneralMasterInfo.Nationality>().Add(new HCMv3.GeneralMasterInfo.Nationality
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

    private void AddDetail(Employee employee, Project project, DateTime period, string wageTypeCode, BillingType billingType, double amount)
    {
        UsingDbContext(context =>
        {
            var wageType = context.Set<WageType>().FirstOrDefault(w => w.Code == wageTypeCode && w.TenantId == TenantId);
            if (wageType == null)
            {
                wageType = context.Set<WageType>().Add(new WageType
                {
                    TenantId = TenantId,
                    Code = wageTypeCode,
                    NameEn = "WT-" + wageTypeCode,
                    NameAr = "WT-" + wageTypeCode,
                    BillingType = billingType,
                    PayrollOrder = 1
                }).Entity;
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
                Number = 0
            });
        });
    }

    [Fact]
    public async Task Should_Pivot_Earning_Wage_Types_Into_One_Column_Per_Type()
    {
        var employee = CreateEmployee("PC-1");
        var project = CreateProject("PROJ-PC1");
        var period = new DateTime(2024, 1, 31);

        AddDetail(employee, project, period, "1000", BillingType.Earning, 500);
        AddDetail(employee, project, period, "1003", BillingType.Earning, 100);

        var result = await _appService.GetProjectCostingReport(
            new GetProjectCostingReportInput { Month = 1, Year = 2024, EmployeeCode = "PC-1" });

        var row = result.Items.Single();
        row.WageTypeAmounts.Count.ShouldBe(2);
        row.WageTypeAmounts.Sum(w => w.Amount).ShouldBe(600);
        row.NetPay.ShouldBe(600);
    }

    [Fact]
    public async Task Should_Exclude_Deduction_Wage_Types_From_The_Base_Report_Pivot()
    {
        var employee = CreateEmployee("PC-2");
        var project = CreateProject("PROJ-PC2");
        var period = new DateTime(2024, 2, 29);

        AddDetail(employee, project, period, "1000", BillingType.Earning, 500);
        AddDetail(employee, project, period, "9999", BillingType.Deduction, 200);

        var result = await _appService.GetProjectCostingReport(
            new GetProjectCostingReportInput { Month = 2, Year = 2024, EmployeeCode = "PC-2" });

        var row = result.Items.Single();
        row.WageTypeAmounts.ShouldNotContain(w => w.WageTypeCode == "9999");
        row.NetPay.ShouldBe(500);
    }

    [Fact]
    public async Task Should_Export_With_A_Literal_Wage_Type_Name_Header_Per_Column()
    {
        var employee = CreateEmployee("PC-EXP");
        var project = CreateProject("PROJ-PC-EXP");
        var period = new DateTime(2024, 3, 31);

        AddDetail(employee, project, period, "1000", BillingType.Earning, 300);

        var file = await _appService.GetProjectCostingReportToExcel(
            new GetProjectCostingReportInput { Month = 3, Year = 2024, EmployeeCode = "PC-EXP" });

        file.FileName.ShouldBe("ProjectCosting.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(1);
        exported.First().Keys.ShouldContain("WT-1000");
        exported.First().Keys.ShouldContain("NetPay");
    }
}
