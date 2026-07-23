using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AbsenceTypesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AbsenceTypesAppService CreateAppService()
    {
        return CreateService(() => new AbsenceTypesAppService(FakeRepository.For<AbsenceType>(Context)));
    }

    [Fact]
    public async Task Should_Create_AbsenceType_And_Default_ResetFrequency_To_Monthly()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceTypeDto
        {
            Code = "AT1",
            NameEn = "Annual Leave",
            NameAr = "إجازة سنوية"
        });

        var result = await service.GetAll(new GetAllAbsenceTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Annual Leave");
        result.Items[0].ResetFrequency.ShouldBe(ResetFrequency.Monthly);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceTypeDto
        {
            Code = "AT2",
            NameEn = "Sick Leave",
            NameAr = "إجازة مرضية"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceTypeDto
            {
                Code = "AT2",
                NameEn = "Hajj Leave",
                NameAr = "إجازة حج"
            }));
    }

    [Fact]
    public async Task Should_Throw_When_ReInitialize_Enabled_And_ReInitializeAfter_Not_Positive()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceTypeDto
            {
                Code = "AT3",
                NameEn = "Unpaid Leave",
                NameAr = "إجازة بدون راتب",
                ReInitialize = true,
                ReInitializeAfter = 0
            }));
    }

    [Fact]
    public async Task Should_Throw_When_AccrualPeriod_Custom_And_CustomStartMonth_None()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceTypeDto
            {
                Code = "AT4",
                NameEn = "Custom Leave",
                NameAr = "إجازة مخصصة",
                AccrualPeriod = AccrualPeriod.Custom,
                CustomStartMonth = CustomStartMonth.None
            }));
    }
}
