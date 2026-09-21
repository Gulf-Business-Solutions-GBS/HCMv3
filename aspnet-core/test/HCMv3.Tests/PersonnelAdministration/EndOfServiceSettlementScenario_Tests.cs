using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// End of service, end to end, for the eight cases: four lengths of service x resignation / termination, each
/// leaving on 30/06/2024 on a 260 KD salary, under the Kuwait defaults (see <see cref="FullAndFinalTestBase"/>).
///
/// <para>The chain, as legacy runs it:</para>
/// <list type="number">
/// <item><description>the Full &amp; Final screen suggests the days to pay -- accrued days x the rule's
/// percentage -- and, with auto-fill on, pre-fills them;</description></item>
/// <item><description>saving it writes an <c>EmployeeGratuity</c> row with those days on the compensation
/// date;</description></item>
/// <item><description>the end-of-service off-cycle's wage code 4005 sums those rows for the month, and wage
/// code 1010 pays them at a day's pay: 260 / 26 = 10 KD a day.</description></item>
/// </list>
///
/// <para>Steps 1-2 run through the real app service; step 3 through the real
/// <see cref="IIndemnityCalculationService.GetIndemnityPayAsync"/> on an end-of-service off-cycle register.
/// The 4005 line itself is filled here with legacy's one-line sum (<c>Program.GetIndemnityPaidDays</c>, the
/// month's gratuity days), because producing it needs the whole payroll run around it.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class EndOfServiceSettlementScenario_Tests : FullAndFinalTestBase
{
    private static readonly DateTime LastDay = new DateTime(2024, 6, 30);

    private readonly IEmployeeFnFPaymentsAppService _fnfAppService;
    private readonly IIndemnityCalculationService _indemnityService;

    public EndOfServiceSettlementScenario_Tests()
    {
        _fnfAppService = Resolve<IEmployeeFnFPaymentsAppService>();
        _indemnityService = Resolve<IIndemnityCalculationService>();
    }

    /// <param name="joining">2, 4, 7, 12 and 21 years before the last day.</param>
    /// <param name="accruedDays">The provision: the same whichever way the employee left.</param>
    /// <param name="paidDays">What Full &amp; Final pays: accrued x the rule's percentage.</param>
    /// <param name="paidAmount">What the end-of-service payroll pays: paid days x 10 KD.</param>
    [Theory]
    [InlineData("2022-07-01", SeparationType.Termination, 30, 30, 300)]
    [InlineData("2022-07-01", SeparationType.Resignation, 30, 0, 0)]
    [InlineData("2020-07-01", SeparationType.Termination, 60, 60, 600)]
    [InlineData("2020-07-01", SeparationType.Resignation, 60, 30, 300)]
    [InlineData("2017-07-01", SeparationType.Termination, 127, 127, 1270)]
    [InlineData("2017-07-01", SeparationType.Resignation, 127, 84.671, 846.71)]
    [InlineData("2012-07-01", SeparationType.Termination, 257, 257, 2570)]
    [InlineData("2012-07-01", SeparationType.Resignation, 257, 257, 2570)]
    // 21 years: 75 + 16 x 26 = 491 days earned, over the 18-month cap of 260 x 18 = 4,680 KD = 468 days.
    [InlineData("2003-07-01", SeparationType.Termination, 468, 468, 4680)]
    [InlineData("2003-07-01", SeparationType.Resignation, 468, 468, 4680)]
    public async Task Should_Pay_End_Of_Service_Indemnity_By_Service_Band_And_Separation_Type(
        string joining, SeparationType separationType, double accruedDays, double paidDays, double paidAmount)
    {
        // Arrange: a leaver whose indemnity quota holds everything accrued, auto-fill on.
        SetAutoFill(true);
        var leaver = SeedLeaver(DateTime.Parse(joining), LastDay, separationType);
        SeedGratuityBalance(leaver.EmployeeId, accruedDays);

        // Act 1: the Full & Final screen -- suggestion, auto-fill, save.
        var balances = await _fnfAppService.GetFullAndFinalBalances(new EntityDto(leaver.EmployeeId));
        await _fnfAppService.CreateOrEdit(new CreateOrEditEmployeeFnFPaymentDto
        {
            EmployeeId = leaver.EmployeeId,
            CompensationDate = LastDay,
            GratuityCompensateDays = balances.AutoFillGratuityCompensateDays
        });

        // Act 2: the end-of-service off-cycle's indemnity pay.
        var registerId = SeedEndOfServiceRegister(leaver.EmployeeId, IndemnityPaidDaysInMonth(leaver.EmployeeId));
        var pay = await _indemnityService.GetIndemnityPayAsync(
            leaver.EmployeeId, registerId, WageTypeId("1010"), LastDay, PayrollRunType.OffCycle, "End Of Service",
            SubGroupOf(leaver.EmployeeId), indemnitySlabs: null);

        // Assert
        balances.GratuityAccrualDays.ShouldBe(accruedDays);
        balances.SuggestedIndemnityDays.ShouldBe(paidDays);
        balances.AutoFillGratuityCompensateDays.ShouldBe(paidDays);
        IndemnityPaidDaysInMonth(leaver.EmployeeId).ShouldBe(paidDays);
        pay.Amount.ShouldBe(paidAmount, 0.0005);

        UsingDbContext(context => context.Set<Employee>().Find(leaver.EmployeeId).Status.ShouldBe(EmployeeStatus.Inactive));
    }

    /// <summary>Legacy <c>Program.GetIndemnityPaidDays</c>: this month's live gratuity rows.</summary>
    private double IndemnityPaidDaysInMonth(int employeeId)
    {
        var monthStart = new DateTime(LastDay.Year, LastDay.Month, 1);

        return UsingDbContext(context => context.Set<EmployeeGratuity>()
            .Where(g => g.EmployeeId == employeeId && !g.IsDeleted && g.GratuityPaidDate >= monthStart && g.GratuityPaidDate <= LastDay)
            .Sum(g => g.GratuityPaidDays));
    }

    /// <summary>An end-of-service off-cycle register carrying the 260 KD basic and the month's 4005 days.</summary>
    private int SeedEndOfServiceRegister(int employeeId, double indemnityPaidDays)
    {
        var paidDaysWageTypeId = EnsureWageType("4005");

        return UsingDbContext(context =>
        {
            var reason = context.Set<PayrollReasonType>().FirstOrDefault(r => r.NameEn == "End Of Service")
                         ?? context.Set<PayrollReasonType>().Add(new PayrollReasonType { TenantId = TenantId, NameEn = "End Of Service", NameAr = "نهاية الخدمة" }).Entity;
            context.SaveChanges();

            var register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                ForPeriod = LastDay,
                InPeriod = LastDay,
                PayrollRunType = PayrollRunType.OffCycle,
                PayrollReasonTypeId = reason.Id,
                OffCyclePaymentDate = LastDay
            }).Entity;
            context.SaveChanges();

            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId, SalaryRegisterId = register.Id, WageTypeId = WageTypeId("1000"),
                Rate = MonthlyBasic, Number = 26, Amount = MonthlyBasic
            });
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId, SalaryRegisterId = register.Id, WageTypeId = paidDaysWageTypeId,
                Number = indemnityPaidDays
            });
            context.SaveChanges();

            return register.Id;
        });
    }

    private int EnsureWageType(string code)
    {
        return UsingDbContext(context =>
        {
            var existing = context.Set<WageType>().FirstOrDefault(w => w.Code == code);
            if (existing != null)
            {
                return existing.Id;
            }

            var wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId, Code = code, NameEn = "WT " + code, NameAr = "بند " + code, PayrollOrder = 1, Status = true
            }).Entity;
            context.SaveChanges();
            return wageType.Id;
        });
    }

    private int WageTypeId(string code) => UsingDbContext(context => context.Set<WageType>().Single(w => w.Code == code).Id);

    private int? SubGroupOf(int employeeId) =>
        UsingDbContext(context => context.Set<EmployeeOrgAssignment>().First(a => a.EmployeeId == employeeId).EmployeeSubGroupId);
}
