using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PerformanceReport;
using HCMv3.GridReports.PerformanceReport.Dto;
using HCMv3.PerformanceManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Performance Appraisal Reviewer Summary grid report (legacy customer variant
/// <c>uscRptPerformanceAppraisal_Custom012.ascx</c>, <c>Table1()</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PerformanceAppraisalReviewerSummaryReportAppService_Tests : AppTestBase
{
    private readonly IPerformanceAppraisalReviewerSummaryReportAppService _appService;

    public PerformanceAppraisalReviewerSummaryReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPerformanceAppraisalReviewerSummaryReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
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
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private PAEmployee CreatePAEmployee(Employee employee, string cycleCode)
    {
        FormType formType = null;
        PACycle cycle = null;
        UsingDbContext(context =>
        {
            formType = context.Set<FormType>().Add(new FormType
            {
                TenantId = TenantId,
                Code = cycleCode,
                NameEn = cycleCode + "-Form",
                NameAr = cycleCode + "-Form-Ar"
            }).Entity;

            cycle = context.Set<PACycle>().Add(new PACycle
            {
                TenantId = TenantId,
                Code = cycleCode,
                NameEn = cycleCode + "-Cycle",
                NameAr = cycleCode + "-Cycle-Ar"
            }).Entity;
        });

        PAEmployee paEmployee = null;
        UsingDbContext(context =>
        {
            paEmployee = context.Set<PAEmployee>().Add(new PAEmployee
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                FormTypeId = formType.Id,
                PACycleId = cycle.Id
            }).Entity;
        });

        return paEmployee;
    }

    private void CreateSelfReview(PAEmployee paEmployee, Employee employee, double score)
    {
        UsingDbContext(context =>
        {
            context.Set<PAEmployeeReviewer>().Add(new PAEmployeeReviewer
            {
                TenantId = TenantId,
                PAEmployeeId = paEmployee.Id,
                EmployeeId = employee.Id,
                IsPeer = false,
                AverageScore = score
            });
        });
    }

    private static GetPerformanceAppraisalReviewerSummaryReportInput Input(string employeeCode = null, string cycleName = null)
    {
        return new GetPerformanceAppraisalReviewerSummaryReportInput
        {
            EmployeeCode = employeeCode,
            CycleName = cycleName,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Code()
    {
        var included = CreateEmployee("RS-INC");
        var excluded = CreateEmployee("RS-EXC");
        var paInc = CreatePAEmployee(included, "RS-INC");
        var paExc = CreatePAEmployee(excluded, "RS-EXC");
        CreateSelfReview(paInc, included, 4.0);
        CreateSelfReview(paExc, excluded, 4.0);

        var result = await _appService.GetPerformanceAppraisalReviewerSummaryReport(Input(employeeCode: "RS-INC"));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("RS-INC");
        codes.ShouldNotContain("RS-EXC");
    }

    [Fact]
    public async Task Should_Project_Self_Assessment_Score_From_The_Employees_Own_Review()
    {
        var employee = CreateEmployee("RS-SELF");
        var paEmployee = CreatePAEmployee(employee, "RS-SELF");
        CreateSelfReview(paEmployee, employee, 3.7);

        var result = await _appService.GetPerformanceAppraisalReviewerSummaryReport(Input(employeeCode: "RS-SELF"));

        result.Items.Single().SelfAssessmentScore.ShouldBe(3.7);
    }
}
