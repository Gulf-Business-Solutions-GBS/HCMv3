using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class WageTypesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private WageTypesAppService CreateAppService()
    {
        return CreateService(() => new WageTypesAppService(FakeRepository.For<WageType>(Context)));
    }

    [Fact]
    public async Task Should_Create_WageType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeDto
        {
            Code = "WT1",
            NameEn = "Basic Salary",
            NameAr = "الراتب الأساسي",
            BillingType = BillingType.Earning,
            PayrollOrder = 1
        });

        var result = await service.GetAll(new GetAllWageTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Basic Salary");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeDto
        {
            Code = "WT2",
            NameEn = "Housing Allowance",
            NameAr = "بدل السكن",
            BillingType = BillingType.Earning,
            PayrollOrder = 2
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditWageTypeDto
            {
                Code = "WT2",
                NameEn = "Transport Allowance",
                NameAr = "بدل النقل",
                BillingType = BillingType.Earning,
                PayrollOrder = 3
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Creating_With_Reserved_BillingType_TimeType()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditWageTypeDto
            {
                Code = "WT3",
                NameEn = "Reserved Type",
                NameAr = "نوع محجوز",
                BillingType = BillingType.TimeType,
                PayrollOrder = 4
            }));
    }

    [Fact]
    public async Task Should_Throw_When_PayrollOrder_Is_Zero()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditWageTypeDto
            {
                Code = "WT4",
                NameEn = "No Order",
                NameAr = "بدون ترتيب",
                BillingType = BillingType.Earning,
                PayrollOrder = 0
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Deleting_Technical_WageType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeDto
        {
            Code = "WT5",
            NameEn = "System Wage Type",
            NameAr = "نوع أجر النظام",
            BillingType = BillingType.Earning,
            Category = Category.Technical,
            PayrollOrder = 5
        });

        var created = await service.GetAll(new GetAllWageTypesInput());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.Delete(new Abp.Application.Services.Dto.EntityDto(id)));
    }
}
