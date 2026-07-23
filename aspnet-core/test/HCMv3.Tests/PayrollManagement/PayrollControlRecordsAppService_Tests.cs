using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class PayrollControlRecordsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private PayrollControlRecordsAppService CreateAppService()
    {
        return CreateService(() => new PayrollControlRecordsAppService(
            FakeRepository.For<PayrollControlRecord>(Context),
            FakeRepository.For<PayrollArea>(Context)));
    }

    private int CreatePayrollArea(string payrollPeriodCode, string areaCode)
    {
        var period = new PayrollPeriod { TenantId = AbpSession.TenantId ?? 1, Code = payrollPeriodCode, NameEn = payrollPeriodCode, NameAr = payrollPeriodCode };
        Context.PayrollPeriods.Add(period);
        Context.SaveChanges();

        var currency = new HCMv3.GeneralMasterInfo.Currency { TenantId = AbpSession.TenantId ?? 1, Code = areaCode, NameEn = areaCode, NameAr = areaCode };
        Context.Currencies.Add(currency);
        Context.SaveChanges();

        var area = new PayrollArea
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = areaCode,
            Name = areaCode,
            PayrollPeriodId = period.Id,
            CurrencyId = currency.Id
        };
        Context.PayrollAreas.Add(area);
        Context.SaveChanges();
        return area.Id;
    }

    [Fact]
    public async Task Should_Create_PayrollControlRecord_And_Compute_CalendarRange_For_JanDec()
    {
        var payrollAreaId = CreatePayrollArea("Jan-Dec", "PCR1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayrollControlRecordDto
        {
            PayrollAreaId = payrollAreaId,
            PayrollMonth = 3,
            PayrollYear = 2025
        });

        var result = await service.GetAll(new GetAllPayrollControlRecordsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].CalendarMonthStart.ShouldBe(new DateTime(2025, 3, 1));
        result.Items[0].CalendarMonthEnd.ShouldBe(new DateTime(2025, 3, 31));
    }

    [Fact]
    public async Task Should_Create_PayrollControlRecord_And_Compute_CalendarRange_For_JulJun()
    {
        var payrollAreaId = CreatePayrollArea("Jul-Jun", "PCR2");
        var service = CreateAppService();

        // PayrollMonth 1 (< wrap threshold 7) maps to July of the same PayrollYear, no year wrap.
        await service.CreateOrEdit(new CreateOrEditPayrollControlRecordDto
        {
            PayrollAreaId = payrollAreaId,
            PayrollMonth = 1,
            PayrollYear = 2025
        });

        var result = await service.GetAll(new GetAllPayrollControlRecordsInput());
        result.Items[0].CalendarMonthStart.ShouldBe(new DateTime(2025, 7, 1));
        result.Items[0].CalendarMonthEnd.ShouldBe(new DateTime(2025, 7, 31));
    }

    [Fact]
    public async Task Should_Throw_When_Second_ControlRecord_Created_For_Same_PayrollArea()
    {
        var payrollAreaId = CreatePayrollArea("Jan-Dec", "PCR3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayrollControlRecordDto
        {
            PayrollAreaId = payrollAreaId,
            PayrollMonth = 1,
            PayrollYear = 2025
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditPayrollControlRecordDto
            {
                PayrollAreaId = payrollAreaId,
                PayrollMonth = 2,
                PayrollYear = 2025
            }));
    }
}
