using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// The indemnity and annual leave provisions are worked out on every wage type marked to count for them -- and
/// every salary column of Load Employee Data is marked, so an allowance raises both. Made-up figures only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveProvisionBase_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime September = new(2026, 9, 30);

    private readonly IGoLiveSetupAppService _goLive;

    public GoLiveProvisionBase_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>The catalogue marks every grid salary wage type for indemnity and links it to annual leave.</summary>
    [Fact]
    public void Should_Count_Every_Grid_Salary_Wage_Type_For_Both_Provisions()
    {
        var pack = Resolve<IReferenceDataSeeder>().GetPack("payroll");
        var codes = GoLiveEmployeeLoader.SalaryColumns.Select(c => c.WageCode).ToList();

        pack.WageTypes.Where(w => codes.Contains(w.Code) && !w.IncludeInIndemnity).Select(w => w.Code).ShouldBeEmpty();
        codes.Where(c => !pack.WageTypeLeaves.Any(l => l.AbsenceTypeCode == "1" && l.WageTypeCode == c)).ShouldBeEmpty();
    }

    /// <summary>
    /// Basic salary 900 and other allowance 100: both provisions are worked out on 1,000, not on the basic salary
    /// alone.
    /// </summary>
    [Fact]
    public async Task Should_Work_Out_Both_Provisions_On_Basic_Salary_And_Allowances()
    {
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => Resolve<IReferenceDataSeeder>().SeedPackAsync(pack, TenantId));
        }

        await _goLive.LoadEmployees(new GoLiveLoadEmployeesInput
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = DefaultArea,
            Employees = new List<GoLiveEmployeeRowDto>
            {
                new()
                {
                    Code = "E001",
                    FullName = "Employee E001",
                    JoiningDate = new DateTime(2020, 1, 15),
                    CivilId = "290011501000",
                    BasicSalary = 900,
                    OtherAllowance = 100,
                },
            },
        });

        var run = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);

        var employeeId = UsingDbContext(context => context.Employees.Single(e => e.Code == "E001" && !e.IsDeleted).Id);
        var lines = (await _goLive.GetSalaryRegister(new GoLiveSalaryRegisterInput { PayDate = September, EmployeeId = employeeId })).Lines;

        var indemnity = lines.Where(l => l.WageCode is "3000" or "3003").ToList();
        indemnity.ShouldNotBeEmpty("no indemnity provision line was written");
        indemnity.ShouldAllBe(l => Math.Abs(l.Rate - 1000) < 0.001);

        lines.Single(l => l.WageCode == "3001").Rate.ShouldBe(1000, 0.001);
    }
}
