using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class PayrollDriverAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly PayrollDriverAppService _appService;

    public PayrollDriverAppService_Tests()
    {
        _appService = CreateService(() => new PayrollDriverAppService(
            FakeRepository.For<PayrollDriver>(Context),
            FakeRepository.For<Country>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PayrollDriver()
    {
        var countryId = CreateCountry("Qatar");

        await _appService.CreateOrEditPayrollDriver(new PayrollDriverEditDto
        {
            Id = 0,
            CountryId = countryId,
            LeaveDeductionStartMonth = true
        });

        var result = await _appService.GetPayrollDrivers(new GetPayrollDriversInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].CountryId.ShouldBe(countryId);
        result.Items[0].CountryName.ShouldBe("Qatar");
        result.Items[0].LeaveDeductionStartMonth.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_Country_Blank()
    {
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.CreateOrEditPayrollDriver(new PayrollDriverEditDto { Id = 0, CountryId = 0 }));
    }

    [Fact]
    public async Task Should_Throw_When_Country_Duplicate()
    {
        var countryId = CreateCountry("UAE");

        await _appService.CreateOrEditPayrollDriver(new PayrollDriverEditDto { Id = 0, CountryId = countryId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.CreateOrEditPayrollDriver(new PayrollDriverEditDto { Id = 0, CountryId = countryId }));
    }

    [Fact]
    public async Task Should_Get_ForEdit_And_Update()
    {
        var countryId1 = CreateCountry("KSA");
        var countryId2 = CreateCountry("Oman");

        await _appService.CreateOrEditPayrollDriver(new PayrollDriverEditDto { Id = 0, CountryId = countryId1 });

        var result = await _appService.GetPayrollDrivers(new GetPayrollDriversInput());
        var id = result.Items[0].Id;

        var editDto = await _appService.GetPayrollDriverForEdit(new EntityDto(id));
        editDto.CountryId.ShouldBe(countryId1);

        editDto.CountryId = countryId2;
        editDto.LeaveDeductionStartMonth = true;
        await _appService.CreateOrEditPayrollDriver(editDto);

        var updated = await _appService.GetPayrollDriverForEdit(new EntityDto(id));
        updated.CountryId.ShouldBe(countryId2);
        updated.LeaveDeductionStartMonth.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Delete_PayrollDriver()
    {
        var countryId = CreateCountry("Bahrain");
        await _appService.CreateOrEditPayrollDriver(new PayrollDriverEditDto { Id = 0, CountryId = countryId });

        var result = await _appService.GetPayrollDrivers(new GetPayrollDriversInput());
        var id = result.Items[0].Id;

        await _appService.DeletePayrollDriver(new EntityDto(id));

        var afterDelete = await _appService.GetPayrollDrivers(new GetPayrollDriversInput());
        afterDelete.TotalCount.ShouldBe(0);
    }
}
