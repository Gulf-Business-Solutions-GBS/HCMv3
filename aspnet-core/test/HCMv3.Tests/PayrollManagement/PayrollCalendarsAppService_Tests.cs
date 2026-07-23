using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class PayrollCalendarsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private PayrollCalendarsAppService CreateAppService()
    {
        return CreateService(() => new PayrollCalendarsAppService(FakeRepository.For<PayrollCalendar>(Context)));
    }

    private int CreatePayrollArea(string code)
    {
        var period = new PayrollPeriod { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.PayrollPeriods.Add(period);
        Context.SaveChanges();

        var currency = new HCMv3.GeneralMasterInfo.Currency { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Currencies.Add(currency);
        Context.SaveChanges();

        var area = new PayrollArea
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            Name = code,
            PayrollPeriodId = period.Id,
            CurrencyId = currency.Id
        };
        Context.PayrollAreas.Add(area);
        Context.SaveChanges();
        return area.Id;
    }

    [Fact]
    public async Task Should_Create_PayrollCalendar()
    {
        var payrollAreaId = CreatePayrollArea("PC1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayrollCalendarDto
        {
            PayrollAreaId = payrollAreaId,
            PayrollMonth = 1,
            PayrollYear = 2025,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31)
        });

        var result = await service.GetAll(new GetAllPayrollCalendarsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Overlap()
    {
        var payrollAreaId = CreatePayrollArea("PC2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayrollCalendarDto
        {
            PayrollAreaId = payrollAreaId,
            PayrollMonth = 1,
            PayrollYear = 2025,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditPayrollCalendarDto
            {
                PayrollAreaId = payrollAreaId,
                PayrollMonth = 1,
                PayrollYear = 2025,
                StartDate = new DateTime(2025, 1, 15),
                EndDate = new DateTime(2025, 2, 15)
            }));
    }
}
