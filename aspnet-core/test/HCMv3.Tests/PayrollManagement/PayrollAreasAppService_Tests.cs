using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class PayrollAreasAppService_Tests : PayrollManagementAppServiceTestBase
{
    private PayrollAreasAppService CreateAppService()
    {
        return CreateService(() => new PayrollAreasAppService(FakeRepository.For<PayrollArea>(Context)));
    }

    private int CreatePayrollPeriod(string code)
    {
        var entity = new PayrollPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.PayrollPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateCurrency(string code)
    {
        var entity = new HCMv3.GeneralMasterInfo.Currency
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.Currencies.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_PayrollArea()
    {
        var payrollPeriodId = CreatePayrollPeriod("Jan-Dec");
        var currencyId = CreateCurrency("USD");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayrollAreaDto
        {
            Code = "PA1",
            Name = "Head Office",
            PayrollPeriodId = payrollPeriodId,
            CurrencyId = currencyId,
            CutOffDay = 25
        });

        var result = await service.GetAll(new GetAllPayrollAreasInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Head Office");
    }

    private int CreateNotificationConfig(string name)
    {
        var entity = new HCMv3.GeneralMasterInfo.NotificationConfig { TenantId = AbpSession.TenantId ?? 1, Name = name };
        Context.Set<HCMv3.GeneralMasterInfo.NotificationConfig>().Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Store_The_Three_Period_Notification_Configurations()
    {
        var payrollPeriodId = CreatePayrollPeriod("Jan-Dec");
        var currencyId = CreateCurrency("KWD");
        var releaseId = CreateNotificationConfig("Release");
        var exitId = CreateNotificationConfig("Exit");
        var correctionId = CreateNotificationConfig("Correction");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPayrollAreaDto
        {
            Code = "PA1",
            Name = "Head Office",
            PayrollPeriodId = payrollPeriodId,
            CurrencyId = currencyId,
            PAReleaseNotificationId = releaseId,
            PAExitNotificationId = exitId,
            PACorrectionNotificationId = correctionId
        });

        var item = (await service.GetAll(new GetAllPayrollAreasInput())).Items[0];
        item.PAReleaseNotificationId.ShouldBe(releaseId);
        item.PAReleaseNotificationName.ShouldBe("Release");
        item.PAExitNotificationId.ShouldBe(exitId);
        item.PAExitNotificationName.ShouldBe("Exit");
        item.PACorrectionNotificationId.ShouldBe(correctionId);
        item.PACorrectionNotificationName.ShouldBe("Correction");

        var forEdit = await service.GetPayrollAreaForEdit(new Abp.Application.Services.Dto.EntityDto(item.Id));
        forEdit.PayrollArea.PAReleaseNotificationId.ShouldBe(releaseId);
        forEdit.PayrollArea.PAExitNotificationId.ShouldBe(exitId);
        forEdit.PayrollArea.PACorrectionNotificationId.ShouldBe(correctionId);
    }

    [Fact]
    public async Task Should_Clear_A_Period_Notification_Configuration_On_Update()
    {
        var payrollPeriodId = CreatePayrollPeriod("Jan-Dec");
        var currencyId = CreateCurrency("KWD");
        var releaseId = CreateNotificationConfig("Release");
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPayrollAreaDto
        {
            Code = "PA1", Name = "Head Office", PayrollPeriodId = payrollPeriodId, CurrencyId = currencyId,
            PAReleaseNotificationId = releaseId
        });
        var id = (await service.GetAll(new GetAllPayrollAreasInput())).Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditPayrollAreaDto
        {
            Id = id, Code = "PA1", Name = "Head Office", PayrollPeriodId = payrollPeriodId, CurrencyId = currencyId,
            PAReleaseNotificationId = null
        });

        (await service.GetAll(new GetAllPayrollAreasInput())).Items[0].PAReleaseNotificationId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Throw_When_PayrollPeriod_Missing()
    {
        var currencyId = CreateCurrency("USD2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditPayrollAreaDto
            {
                Code = "PA2",
                Name = "Branch Office",
                PayrollPeriodId = 0,
                CurrencyId = currencyId
            }));
    }
}
