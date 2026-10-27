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
/// Tests for the Performance Appraisal Manager Evaluation grid report (legacy customer variant
/// <c>uscRptPerformanceAppraisal_Custom035.ascx</c>, <c>Table1()</c>). Only the current MANAGER's
/// (non-peer) review counts -- employees with no manager-authored review are absent from the result,
/// exactly like legacy's inner join.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PerformanceAppraisalManagerEvaluationReportAppService_Tests : AppTestBase
{
    private readonly IPerformanceAppraisalManagerEvaluationReportAppService _appService;

    public PerformanceAppraisalManagerEvaluationReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPerformanceAppraisalManagerEvaluationReportAppService>();
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

    private void CreateManager(Employee employee, Employee manager, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ManagerId = manager.Id,
                StartDate = start,
                EndDate = end
            });
        });
    }

    private void CreateReviewer(PAEmployee paEmployee, Employee reviewer, bool isPeer, double averageScore)
    {
        UsingDbContext(context =>
        {
            context.Set<PAEmployeeReviewer>().Add(new PAEmployeeReviewer
            {
                TenantId = TenantId,
                PAEmployeeId = paEmployee.Id,
                EmployeeId = reviewer.Id,
                IsPeer = isPeer,
                AverageScore = averageScore
            });
        });
    }

    private static GetPerformanceAppraisalManagerEvaluationReportInput Input(string employeeCode = null, string cycleName = null)
    {
        return new GetPerformanceAppraisalManagerEvaluationReportInput
        {
            EmployeeCode = employeeCode,
            CycleName = cycleName,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Include_Only_The_Current_Managers_Review()
    {
        var employee = CreateEmployee("MGR-EMP");
        var manager = CreateEmployee("MGR-MANAGER");
        var peer = CreateEmployee("MGR-PEER");
        var paEmployee = CreatePAEmployee(employee, "MGR-CYCLE");

        CreateManager(employee, manager, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));
        CreateReviewer(paEmployee, manager, isPeer: false, averageScore: 4.2);
        CreateReviewer(paEmployee, peer, isPeer: true, averageScore: 5.0);

        var result = await _appService.GetPerformanceAppraisalManagerEvaluationReport(Input(employeeCode: "MGR-EMP"));

        var row = result.Items.Single();
        row.AverageMark.ShouldBe(4.2);
    }

    [Fact]
    public async Task Should_Omit_Employees_With_No_Manager_Authored_Review()
    {
        var employee = CreateEmployee("MGR-NOMGR");
        var paEmployee = CreatePAEmployee(employee, "MGR-NOMGR-CYCLE");
        var peer = CreateEmployee("MGR-NOMGR-PEER");
        // Only a peer review exists -- no manager relationship recorded, so legacy's inner join drops it.
        CreateReviewer(paEmployee, peer, isPeer: true, averageScore: 5.0);

        var result = await _appService.GetPerformanceAppraisalManagerEvaluationReport(Input(employeeCode: "MGR-NOMGR"));

        result.Items.ShouldBeEmpty();
    }
}
