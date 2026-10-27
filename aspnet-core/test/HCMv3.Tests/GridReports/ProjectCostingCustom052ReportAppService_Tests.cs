using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.ProjectReport;
using HCMv3.GridReports.ProjectReport.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Per-customer variant of <c>ProjectCostingReportAppService_Tests</c> (legacy
/// <c>uscRptProjectCosting_Custom052.ascx</c>). Pins the two behaviours that actually differ from the base
/// report -- see <see cref="ProjectCostingCustom052ReportRowDto"/> remarks -- rather than re-covering the
/// shared pivot mechanics.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProjectCostingCustom052ReportAppService_Tests : AppTestBase
{
    private readonly IProjectCostingCustom052ReportAppService _appService;

    public ProjectCostingCustom052ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IProjectCostingCustom052ReportAppService>();
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

    private void AddDetail(Employee employee, Project project, DateTime period, string wageTypeCode, BillingType billingType, double amount, double number = 0)
    {
        UsingDbContext(context =>
        {
            var wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = wageTypeCode,
                NameEn = "WT-" + wageTypeCode,
                NameAr = "WT-" + wageTypeCode,
                BillingType = billingType,
                PayrollOrder = 1
            }).Entity;
            context.SaveChanges();

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
    public async Task Should_Include_Both_Earning_And_Deduction_Wage_Types_Unlike_The_Base_Report()
    {
        var employee = CreateEmployee("C52-1");
        var project = CreateProject("PROJ-C52-1");
        var period = new DateTime(2024, 1, 31);

        AddDetail(employee, project, period, "1000", BillingType.Earning, 500);
        AddDetail(employee, project, period, "2000", BillingType.Deduction, 50);

        var result = await _appService.GetProjectCostingCustom052Report(
            new GetProjectCostingCustom052ReportInput { Month = 1, Year = 2024, EmployeeCode = "C52-1" });

        var row = result.Items.Single();
        row.WageTypeAmounts.Select(w => w.WageTypeCode).ShouldContain("1000");
        row.WageTypeAmounts.Select(w => w.WageTypeCode).ShouldContain("2000");
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_TotalPay_Excluding_WageType_9000()
    {
        var employee = CreateEmployee("C52-2");
        var project = CreateProject("PROJ-C52-2");
        var period = new DateTime(2024, 2, 29);

        AddDetail(employee, project, period, "1000", BillingType.Earning, 500);
        AddDetail(employee, project, period, "9000", BillingType.Earning, 100);

        var result = await _appService.GetProjectCostingCustom052Report(
            new GetProjectCostingCustom052ReportInput { Month = 2, Year = 2024, EmployeeCode = "C52-2" });

        var row = result.Items.Single();
        row.TotalPay.ShouldBe(500);
    }
}
