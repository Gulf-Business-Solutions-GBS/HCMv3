using System;
using System.Linq;
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

    /// <summary>
    /// Legacy <c>SysService.GetGeneralSettings</c> created the row on first read; a tenant never sees a
    /// missing settings row, and the settings screen then edits the same row the engines read.
    /// </summary>
    [Fact]
    public async Task Should_Create_GeneralSettings_On_First_Read_And_Reuse_It_After()
    {
        var first = await _sysSettingsAccessor.GetGeneralSettingsAsync();

        first.ShouldNotBeNull();
        first.Id.ShouldBeGreaterThan(0);
        first.TenantId.ShouldBe(AbpSession.TenantId ?? 1);

        var second = await _sysSettingsAccessor.GetGeneralSettingsAsync();
        second.Id.ShouldBe(first.Id);

        UsingDbContext(context =>
        {
            context.Set<ManageGeneralSettings>().Count(s => s.TenantId == (AbpSession.TenantId ?? 1)).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Build_The_First_PayrollSettings_From_The_Tenant_Config_Keys()
    {
        AddTenantConfig(SysTenantConfigKeys.ReportGroup, "IMILL");
        AddTenantConfig(SysTenantConfigKeys.DefaultWorkingHours, "8");
        AddTenantConfig(SysTenantConfigKeys.PayslipSignature, "1");
        AddTenantConfig(SysTenantConfigKeys.EnableUnpaidDedSlabWise, "1");

        var settings = await _sysSettingsAccessor.GetPayrollSettingsAsync();

        settings.ClientId.ShouldBe("004");
        settings.DefaultWorkingHours.ShouldBe(8);
        settings.PayslipSignature.ShouldBeTrue();
        settings.EnableUnpaidDedSlabWise.ShouldBeTrue();
        settings.RoundNetPay.ShouldBeFalse();
        // Legacy: on unless the key is explicitly "0".
        settings.EnableBasicpayMoreThenFixedDays.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Build_The_First_TimeSettings_From_The_Tenant_Config_Keys()
    {
        AddTenantConfig(SysTenantConfigKeys.TimeEvalEarliestDate, "01/04/2021");
        AddTenantConfig(SysTenantConfigKeys.TeMaxLimitDays, "300");
        AddTenantConfig(SysTenantConfigKeys.EnableTimeAttendanceIntegration, "1");
        AddTenantConfig(SysTenantConfigKeys.EnableOvertimeInTimeEngine, "0");

        var settings = await _sysSettingsAccessor.GetTimeSettingsAsync();

        settings.EarliestEvaluationDate.ShouldBe(new DateTime(2021, 4, 1));
        settings.FutureEvaluationInDays.ShouldBe(300);
        settings.EnableTimeAttendanceIntegration.ShouldBeTrue();
        settings.EnableOvertimeInTimeEngine.ShouldBeFalse();

        // The row the accessor built is now what IsDateAfterTimeEvalEarliestDate reads.
        (await _sysSettingsAccessor.IsDateAfterTimeEvalEarliestDateAsync(new DateTime(2021, 3, 31))).ShouldBeFalse();
        (await _sysSettingsAccessor.IsDateAfterTimeEvalEarliestDateAsync(new DateTime(2021, 4, 1))).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Replace_A_Settings_Row_The_Tenant_Already_Has()
    {
        AddTenantConfig(SysTenantConfigKeys.EnableRowLevelSecurity, "1");
        UsingDbContext(context =>
        {
            context.Set<ManageSecuritySettings>().Add(new ManageSecuritySettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EnableRowLevelSecurity = false
            });
        });

        var settings = await _sysSettingsAccessor.GetSecuritySettingsAsync();

        settings.EnableRowLevelSecurity.ShouldBeFalse();
    }

    /// <summary>Sets a key's value, replacing the row the test database may already seed for it.</summary>
    private void AddTenantConfig(string key, string value)
    {
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var existing = context.Set<SysTenantConfig>().FirstOrDefault(r => r.TenantId == tenantId && r.Key == key);
            if (existing != null)
            {
                existing.KeyValue = value;
                return;
            }

            context.Set<SysTenantConfig>().Add(new SysTenantConfig
            {
                TenantId = tenantId,
                Key = key,
                KeyValue = value,
                StartDate = new DateTime(1800, 1, 1),
                EndDate = new DateTime(9999, 12, 31)
            });
        });
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
