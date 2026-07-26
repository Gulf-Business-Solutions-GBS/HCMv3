using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManagePayrollSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManagePayrollSettingsAppService _appService;

    public ManagePayrollSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManagePayrollSettingsAppService(
            FakeRepository.For<ManagePayrollSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_PayrollSettings()
    {
        await _appService.CreateOrEditPayrollSettings(new PayrollSettingsEditDto
        {
            Id = 0,
            ClientId = "CL-001",
            DefaultWorkingHours = 8,
            PayslipSignature = true,
            OvertimeHoursBasis = OvertimeHoursBasis.DutyHours,
            SocialSecurityRateRule = SocialSecurityRateRule.ContractSalary,
            PickOffcyclePayment = PickOffcyclePayment.CurrentMonth,
            DefaultLeavePaymentType = LeavePaymentType.PayInAdvance,
            Decimal6RoundingFrom = new DateTime(2024, 1, 1),
            IncludeSalaryIncProvDiffInSalRegEffDt = new DateTime(2024, 6, 1)
        });

        var list = await _appService.GetPayrollSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].ClientId.ShouldBe("CL-001");
        list.Items[0].DefaultWorkingHours.ShouldBe(8);
        list.Items[0].PayslipSignature.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Get_PayrollSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditPayrollSettings(new PayrollSettingsEditDto
        {
            Id = 0,
            ClientId = "CL-100",
            OvertimeHoursBasis = OvertimeHoursBasis.FixedHours
        });

        var created = await _appService.GetPayrollSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetPayrollSettingsForEdit(new EntityDto(id));
        editDto.ClientId.ShouldBe("CL-100");
        editDto.OvertimeHoursBasis.ShouldBe(OvertimeHoursBasis.FixedHours);

        editDto.ClientId = "CL-200";
        editDto.OvertimeHoursBasis = OvertimeHoursBasis.DutyHours;
        await _appService.CreateOrEditPayrollSettings(editDto);

        var updated = await _appService.GetPayrollSettingsForEdit(new EntityDto(id));
        updated.ClientId.ShouldBe("CL-200");
        updated.OvertimeHoursBasis.ShouldBe(OvertimeHoursBasis.DutyHours);
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_PayrollSettings()
    {
        await _appService.CreateOrEditPayrollSettings(new PayrollSettingsEditDto { Id = 0 });
        var created = await _appService.GetPayrollSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeletePayrollSettings(new EntityDto(id)));
    }
}
