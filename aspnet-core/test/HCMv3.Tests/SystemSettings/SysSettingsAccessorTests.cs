using System;
using System.Threading.Tasks;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class SysSettingsAccessorTests : AppTestBase
{
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public SysSettingsAccessorTests()
    {
        LoginAsDefaultTenantAdmin();
        _sysSettingsAccessor = Resolve<ISysSettingsAccessor>();
    }

    [Fact]
    public async Task Should_Return_Null_GeneralSettings_When_Not_Configured()
    {
        var settings = await _sysSettingsAccessor.GetGeneralSettingsAsync();

        settings.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_GeneralSettings_Once_Configured()
    {
        UsingDbContext(context =>
        {
            context.Set<ManageGeneralSettings>().Add(new ManageGeneralSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                DateEditMask = "yyyy-MM-dd",
                TimeEditMask = "hh:mm tt"
            });
        });

        var settings = await _sysSettingsAccessor.GetGeneralSettingsAsync();

        settings.ShouldNotBeNull();
        settings.DateEditMask.ShouldBe("yyyy-MM-dd");
    }

    [Fact]
    public async Task Should_Default_SysDateFormat_When_No_Settings_Configured()
    {
        (await _sysSettingsAccessor.GetSysDateFormatAsync()).ShouldBe("dd/MM/yyyy");
        (await _sysSettingsAccessor.GetSysTimeFormatAsync()).ShouldBe("HH:mm");
        (await _sysSettingsAccessor.GetSysDateTimeFormatAsync()).ShouldBe("dd/MM/yyyy HH:mm");
    }

    [Fact]
    public async Task Should_Use_Configured_Date_And_Time_Masks()
    {
        UsingDbContext(context =>
        {
            context.Set<ManageGeneralSettings>().Add(new ManageGeneralSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                DateEditMask = "yyyy-MM-dd",
                TimeEditMask = "hh:mm tt"
            });
        });

        (await _sysSettingsAccessor.GetSysDateFormatAsync()).ShouldBe("yyyy-MM-dd");
        (await _sysSettingsAccessor.GetSysTimeFormatAsync()).ShouldBe("hh:mm tt");
        (await _sysSettingsAccessor.GetSysDateTimeFormatAsync()).ShouldBe("yyyy-MM-dd hh:mm tt");
    }

    [Fact]
    public async Task Should_Return_True_For_IsDateAfterTimeEvalEarliestDate_When_No_TimeSettings_Configured()
    {
        (await _sysSettingsAccessor.IsDateAfterTimeEvalEarliestDateAsync(new DateTime(2020, 1, 1))).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Return_False_When_TransDate_Before_EarliestEvaluationDate()
    {
        UsingDbContext(context =>
        {
            context.Set<ManageTimeSettings>().Add(new ManageTimeSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EarliestEvaluationDate = new DateTime(2022, 1, 1)
            });
        });

        (await _sysSettingsAccessor.IsDateAfterTimeEvalEarliestDateAsync(new DateTime(2020, 1, 1))).ShouldBeFalse();
        (await _sysSettingsAccessor.IsDateAfterTimeEvalEarliestDateAsync(new DateTime(2023, 1, 1))).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Return_Empty_String_When_SysTenantConfig_Key_Not_Found()
    {
        (await _sysSettingsAccessor.GetSysTenantConfigAsync("NON_EXISTENT_KEY")).ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Return_KeyValue_When_SysTenantConfig_Key_Found()
    {
        UsingDbContext(context =>
        {
            context.Set<SysTenantConfig>().Add(new SysTenantConfig
            {
                TenantId = AbpSession.TenantId ?? 1,
                Key = "ENABLE_ROWLEVELSECURITY",
                KeyValue = "1",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2020, 1, 1)
            });
        });

        (await _sysSettingsAccessor.GetSysTenantConfigAsync("ENABLE_ROWLEVELSECURITY")).ShouldBe("1");
    }

    [Fact]
    public async Task Should_Return_Value_Regardless_Of_StartDate_EndDate_Range_Matching_Legacy_Behavior()
    {
        // Legacy SysService.GetSysTenantConfig does NOT filter by StartDate/EndDate at all -- only by Key --
        // so a row whose date range does not cover "today" (or any date) must still be returned.
        UsingDbContext(context =>
        {
            context.Set<SysTenantConfig>().Add(new SysTenantConfig
            {
                TenantId = AbpSession.TenantId ?? 1,
                Key = "SOME_OLD_CONFIG",
                KeyValue = "OldValue",
                StartDate = new DateTime(2000, 1, 1),
                EndDate = new DateTime(2000, 12, 31)
            });
        });

        (await _sysSettingsAccessor.GetSysTenantConfigAsync("SOME_OLD_CONFIG")).ShouldBe("OldValue");
    }
}
