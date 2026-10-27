using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class PayrollDataServiceTests : AppTestBase
{
    private readonly IPayrollDataService _payrollDataService;

    public PayrollDataServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _payrollDataService = Resolve<IPayrollDataService>();
    }

    private int CreatePayrollArea()
    {
        return UsingDbContext(context =>
        {
            var currency = new Currency
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "KWD",
                NameEn = "Kuwaiti Dinar",
                NameAr = "دينار كويتي"
            };
            context.Set<Currency>().Add(currency);
            context.SaveChanges();

            var payrollPeriod = new PayrollPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "Monthly",
                NameAr = "شهري"
            };
            context.Set<PayrollPeriod>().Add(payrollPeriod);
            context.SaveChanges();

            var payrollArea = new PayrollArea
            {
                TenantId = AbpSession.TenantId ?? 1,
                Name = "Main",
                PayrollPeriodId = payrollPeriod.Id,
                CurrencyId = currency.Id
            };
            context.Set<PayrollArea>().Add(payrollArea);
            context.SaveChanges();
            return payrollArea.Id;
        });
    }

    private void EnableDisableRetroCalculation()
    {
        UsingDbContext(context =>
        {
            context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                DisableRetroCalculation = true
            });
            context.SaveChanges();
        });
    }

    private void CreatePayrollControlRecord(int payrollAreaId, DateTime monthStart, DateTime monthEnd, bool periodLocked, bool periodClosed)
    {
        UsingDbContext(context =>
        {
            context.Set<PayrollControlRecord>().Add(new PayrollControlRecord
            {
                TenantId = AbpSession.TenantId ?? 1,
                PayrollAreaId = payrollAreaId,
                PayrollMonth = monthStart.Month,
                PayrollYear = monthStart.Year,
                CalendarMonthStart = monthStart,
                CalendarMonthEnd = monthEnd,
                PeriodLocked = periodLocked,
                PeriodClosed = periodClosed
            });
            context.SaveChanges();
        });
    }

    [Fact]
    public async Task ValidateRetroCalculationAsync_Should_NoOp_When_PayrollArea_Is_Null()
    {
        await Should.NotThrowAsync(() => _payrollDataService.ValidateRetroCalculationAsync(null, new DateTime(2020, 1, 1), typeof(object)));
    }

    [Fact]
    public async Task ValidateRetroCalculationAsync_Should_NoOp_When_DisableRetroCalculation_Is_Off()
    {
        var payrollAreaId = CreatePayrollArea();
        var payrollArea = UsingDbContext(context => context.Set<PayrollArea>().First(r => r.Id == payrollAreaId));

        CreatePayrollControlRecord(payrollAreaId, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), periodLocked: false, periodClosed: true);

        await Should.NotThrowAsync(() => _payrollDataService.ValidateRetroCalculationAsync(payrollArea, new DateTime(2024, 1, 1), typeof(object)));
    }

    [Fact]
    public async Task ValidateRetroCalculationAsync_Should_Throw_When_TransDate_Before_ClosedPeriod_MonthEnd_Plus_One()
    {
        EnableDisableRetroCalculation();
        var payrollAreaId = CreatePayrollArea();
        var payrollArea = UsingDbContext(context => context.Set<PayrollArea>().First(r => r.Id == payrollAreaId));

        // PeriodLocked = No, PeriodClosed = Yes => minDate = CalendarMonthEnd.AddDays(1)
        CreatePayrollControlRecord(payrollAreaId, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), periodLocked: false, periodClosed: true);

        var transDate = new DateTime(2024, 6, 15); // before minDate (2024-07-01)

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            _payrollDataService.ValidateRetroCalculationAsync(payrollArea, transDate, typeof(object)));

        exception.Message.ShouldContain("Retro calculation is disabled");
        exception.Message.ShouldContain(payrollArea.Name);
    }

    [Fact]
    public async Task ValidateRetroCalculationAsync_Should_NotThrow_When_TransDate_After_MinDate()
    {
        EnableDisableRetroCalculation();
        var payrollAreaId = CreatePayrollArea();
        var payrollArea = UsingDbContext(context => context.Set<PayrollArea>().First(r => r.Id == payrollAreaId));

        // PeriodLocked = No, PeriodClosed = No => minDate = CalendarMonthStart
        CreatePayrollControlRecord(payrollAreaId, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), periodLocked: false, periodClosed: false);

        var transDate = new DateTime(2024, 6, 15); // after CalendarMonthStart (2024-06-01)

        await Should.NotThrowAsync(() => _payrollDataService.ValidateRetroCalculationAsync(payrollArea, transDate, typeof(object)));
    }

    [Fact]
    public async Task ValidateRetroCalculationAsync_Should_NotThrow_When_EntityType_Is_PayrollSensitive_And_MustRetroRun()
    {
        EnableDisableRetroCalculation();
        var payrollAreaId = CreatePayrollArea();
        var payrollArea = UsingDbContext(context => context.Set<PayrollArea>().First(r => r.Id == payrollAreaId));

        CreatePayrollControlRecord(payrollAreaId, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), periodLocked: false, periodClosed: true);

        UsingDbContext(context =>
        {
            context.Set<SysObject>().Add(new SysObject
            {
                TenantId = AbpSession.TenantId ?? 1,
                Name = nameof(PayrollArea),
                FullName = typeof(PayrollArea).FullName,
                IsPayrollSensitive = true,
                MustRetroRun = true
            });
            context.SaveChanges();
        });

        var transDate = new DateTime(2024, 6, 15); // would normally be before minDate (2024-07-01)

        await Should.NotThrowAsync(() => _payrollDataService.ValidateRetroCalculationAsync(payrollArea, transDate, typeof(PayrollArea)));
    }
}
