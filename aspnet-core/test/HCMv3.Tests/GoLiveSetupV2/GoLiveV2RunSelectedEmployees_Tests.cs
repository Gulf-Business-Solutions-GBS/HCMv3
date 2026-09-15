using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.Threading;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetupV2;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetupV2;

/// <summary>
/// Run payroll for everyone, or only for the employees whose codes were picked. Made-up figures only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2RunSelectedEmployees_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime September = new(2026, 9, 30);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2RunSelectedEmployees_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>With codes picked, only those employees are run; the others get no salary register.</summary>
    [Fact]
    public async Task Should_Run_Only_The_Selected_Employees()
    {
        await GivenEmployees("E001", "E002");

        var run = await Run(new List<string> { " e002 " });

        run.Succeeded.ShouldBeTrue(run.Message);
        RegisteredCodes().ShouldBe(new[] { "E002" });
    }

    /// <summary>No codes picked runs everyone.</summary>
    [Fact]
    public async Task Should_Run_Everyone_When_No_Codes_Are_Picked()
    {
        await GivenEmployees("E001", "E002");

        var run = await Run(new List<string>());

        run.Succeeded.ShouldBeTrue(run.Message);
        RegisteredCodes().ShouldBe(new[] { "E001", "E002" });
    }

    /// <summary>A code the payroll area does not have stops the run before either engine starts, and is named.</summary>
    [Fact]
    public async Task Should_Refuse_Codes_The_Payroll_Area_Does_Not_Have()
    {
        await GivenEmployees("E001");

        var run = await Run(new List<string> { "E001", "X999" });

        run.Succeeded.ShouldBeFalse();
        run.StoppedAt.ShouldBe(GoLiveV2StoppedAt.Input);
        run.Message.ShouldBe(string.Format(
            Resolve<ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName, "GoLiveV2RunCodesNotInArea"), 1, "X999"));
        RegisteredCodes().ShouldBeEmpty();
    }

    // ---------------------------------------------------------------- arrange

    private Task<GoLiveV2RunPayrollOutput> Run(List<string> codes) =>
        _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea, EmployeeCodes = codes });

    private string[] RegisteredCodes() =>
        UsingDbContext(context => context.SalaryRegisters
            .Where(r => r.TenantId == TenantId && !r.IsDeleted && r.InPeriod == September
                        && r.PayrollResultType == PayrollResultType.Current)
            .Select(r => r.Employee.Code)
            .Distinct()
            .OrderBy(c => c)
            .ToArray());

    private async Task GivenEmployees(params string[] codes)
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }

        await _goLive.LoadEmployees(new GoLiveV2LoadEmployeesInput
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = DefaultArea,
            Employees = codes
                .Select((code, i) => new GoLiveV2EmployeeRowDto
                {
                    Code = code,
                    FullName = "Employee " + code,
                    JoiningDate = new DateTime(2020, 1, 15),
                    CivilId = "29001150" + (1000 + i),
                    BasicSalary = 500,
                })
                .ToList(),
        });
    }
}
