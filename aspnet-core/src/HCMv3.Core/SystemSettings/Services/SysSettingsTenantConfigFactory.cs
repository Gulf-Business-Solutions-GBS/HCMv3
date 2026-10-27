using System;
using System.Collections.Generic;
using System.Globalization;
using HCMv3.SystemSecurity;

namespace HCMv3.SystemSettings.Services;

/// <summary>
/// A tenant's <see cref="SysTenantConfig"/> rows, keyed for the factory. A key that has no row reads as
/// <c>""</c> (legacy <c>SysService.GetSysTenantConfig</c> returned <c>""</c> for a missing key), while
/// <see cref="Has"/> tells the two apart for the settings legacy only assigned when the row existed.
/// </summary>
public sealed class SysTenantConfigLookup
{
    private readonly IReadOnlyDictionary<string, string> _values;

    public SysTenantConfigLookup(IReadOnlyDictionary<string, string> values)
    {
        _values = values ?? new Dictionary<string, string>();
    }

    public static SysTenantConfigLookup Empty { get; } = new(new Dictionary<string, string>());

    public bool Has(string key) => _values.ContainsKey(key);

    public string Get(string key) => _values.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;

    /// <summary>Legacy <c>GetSysTenantConfig(key) == "1"</c>.</summary>
    public bool IsOn(string key) => Get(key) == "1";

    /// <summary>
    /// Legacy <c>GetSysTenantConfig(key) == "0" ? false : true</c>: on unless the key is explicitly "0".
    /// </summary>
    public bool IsOnUnlessZero(string key) => Get(key) != "0";

    /// <summary>
    /// Legacy <c>if (row != null) x = int.Parse(row.KeyValue)</c>. Legacy threw on a row whose value was
    /// not a number; this port leaves the setting at its default instead, since a bad config row should
    /// not stop every screen that reads settings.
    /// </summary>
    public int? GetInt(string key) =>
        Has(key) && int.TryParse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    public double? GetDouble(string key) =>
        Has(key) && double.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
}

/// <summary>
/// Builds a tenant's first <c>Manage*Settings</c> row from its <see cref="SysTenantConfig"/> keys. This is
/// the "row missing" branch of each legacy <c>SysService.Get*Settings(Session)</c> (HCM10 <c>SysService.cs</c>
/// lines 300-1050), key for key, including the two flags legacy defaulted to ON
/// (<see cref="SysTenantConfigKeys.EnableBasicPayMoreThenFixedDays"/>,
/// <see cref="SysTenantConfigKeys.EnableOvertimeInTimeEngine"/>) and the <c>REPORT_GROUP</c> name to
/// client-id mapping.
///
/// Pure: no repository access, so every mapping is unit-tested without a database. <c>TenantId</c> is set by
/// the caller.
/// </summary>
public static class SysSettingsTenantConfigFactory
{
    public static ManageAppraisalSettings CreateAppraisalSettings(SysTenantConfigLookup config)
    {
        var settings = new ManageAppraisalSettings();

        switch (config.Get(SysTenantConfigKeys.AppraisalCycle).ToUpperInvariant())
        {
            case "QUARTERLY":
                settings.AppraisalCycle = AppraisalCycle.Quarterly;
                break;
            case "HALFYEARLY":
                settings.AppraisalCycle = AppraisalCycle.HalfYearly;
                break;
            case "ANNUALLY":
                settings.AppraisalCycle = AppraisalCycle.Annually;
                break;
        }

        settings.EnablePAPeerReview = config.IsOn(SysTenantConfigKeys.EnablePaPeerReview);
        settings.SetPeerMinLimit = config.GetInt(SysTenantConfigKeys.SetPeerMinLimit) ?? 0;
        settings.SetPeerMaxLimit = config.GetInt(SysTenantConfigKeys.SetPeerMaxLimit) ?? 0;

        return settings;
    }

    public static ManageDocumentSettings CreateDocumentSettings(SysTenantConfigLookup config)
    {
        return new ManageDocumentSettings
        {
            EnableDocSaveInFileSystem = config.IsOn(SysTenantConfigKeys.EnableDocSaveInFileSystem),
            FileSystemAttachmentPath = config.Get(SysTenantConfigKeys.FileSystemAttachmentPath)
        };
    }

    public static ManageFIPostingSettings CreateFIPostingSettings(SysTenantConfigLookup config)
    {
        return new ManageFIPostingSettings
        {
            SeparateProvisionPosting = config.IsOn(SysTenantConfigKeys.SeparateProvisionFiPosting),
            EnableAutoWageAccountEntry = config.IsOn(SysTenantConfigKeys.EnableAutoWageAccountEntry),
            AllowDuplicateCodeInCostElement = false
        };
    }

    public static ManageGeneralSettings CreateGeneralSettings(SysTenantConfigLookup config)
    {
        return new ManageGeneralSettings
        {
            DisableAutoEmpcode = config.IsOn(SysTenantConfigKeys.DisableAutoEmpCode),
            DateEditMask = config.Get(SysTenantConfigKeys.DateEditMask),
            DateDisplayFormat = config.Get(SysTenantConfigKeys.DateDisplayFormat),
            TimeEditMask = config.Get(SysTenantConfigKeys.TimeEditMask),
            TimeDisplayFormat = config.Get(SysTenantConfigKeys.TimeDisplayFormat),
            StartupDashboard = config.Get(SysTenantConfigKeys.StartupDashboard),
            NoticePeriodDaysExcludingSubmissionDate = config.IsOn(SysTenantConfigKeys.NoticePeriodDaysExcludingSubmissionDate),
            EnableGridDataAccessModeServer = config.IsOn(SysTenantConfigKeys.EnableGridDataAccessModeServer)
        };
    }

    public static ManageNotificationSettings CreateNotificationSettings(SysTenantConfigLookup config)
    {
        return new ManageNotificationSettings
        {
            DisableNotificationWindow = false
        };
    }

    public static ManageOrgSettings CreateOrgSettings(SysTenantConfigLookup config)
    {
        return new ManageOrgSettings
        {
            EnablePostingGroup = config.IsOn(SysTenantConfigKeys.EnablePostingGroup),
            EnableBudgetingGroup = config.IsOn(SysTenantConfigKeys.EnableBudgetingGroup),
            EnablePosition = config.IsOn(SysTenantConfigKeys.EnablePosition)
        };
    }

    /// <summary>Legacy <c>REPORT_GROUP</c> names that stand for a client id; any other value is used as is.</summary>
    public static string MapReportGroupToClientId(string reportGroup)
    {
        return reportGroup switch
        {
            "IMILL" => "004",
            "LND" => "007",
            "MASHAAN" => "008",
            "CUSTOM19" => "019",
            _ => reportGroup
        };
    }

    public static ManagePayrollSettings CreatePayrollSettings(SysTenantConfigLookup config)
    {
        return new ManagePayrollSettings
        {
            ClientId = MapReportGroupToClientId(config.Get(SysTenantConfigKeys.ReportGroup)),
            DefaultWorkingHours = config.GetDouble(SysTenantConfigKeys.DefaultWorkingHours) ?? 0,
            PayslipSignature = config.IsOn(SysTenantConfigKeys.PayslipSignature),
            LeaveAdvPaymentdateBefore15 = config.IsOn(SysTenantConfigKeys.LeaveAdvPaymentDateBefore15),
            LeaveAdvPaymentdateAfter15 = config.IsOn(SysTenantConfigKeys.LeaveAdvPaymentDateAfter15),
            LeaveProvisionAsPerCurrentSalary = config.IsOn(SysTenantConfigKeys.EnablePeriodWiseProvision),
            EnableNotReturnFromLeaveLogic = config.IsOn(SysTenantConfigKeys.EnableNotReturnFromLeaveLogic),
            EnableBasicpayMoreThenFixedDays = config.IsOnUnlessZero(SysTenantConfigKeys.EnableBasicPayMoreThenFixedDays),
            EnableUnpaidDedSlabWise = config.IsOn(SysTenantConfigKeys.EnableUnpaidDedSlabWise),
            GenerateRegisterOnNotReturnFromLeave = config.IsOn(SysTenantConfigKeys.GenerateRegisterOnNotReturnFromLeave),
            OffcycleBankFileCutoff = config.GetInt(SysTenantConfigKeys.OffCycleBankFileCutoff) ?? 0,
            RoundNetPay = config.IsOn(SysTenantConfigKeys.PayrollNetPayAmtRounding),
            ApplyAnnualleaveNoImpactLogic = config.IsOn(SysTenantConfigKeys.ApplyAnnualLeaveNoImpactLogic),
            EnablePayrollEngineAutorun = config.IsOn(SysTenantConfigKeys.EnablePayrollEngineAuto)
        };
    }

    public static ManagePersonnelSettings CreatePersonnelSettings(SysTenantConfigLookup config)
    {
        return new ManagePersonnelSettings
        {
            EnableSponsorGovtJobTitleQuota = false
        };
    }

    public static ManageProjectCostingSettings CreateProjectCostingSettings(SysTenantConfigLookup config)
    {
        return new ManageProjectCostingSettings
        {
            CalculateNetPayProjectWise = false
        };
    }

    public static ManageReportSettings CreateReportSettings(SysTenantConfigLookup config)
    {
        var settings = new ManageReportSettings
        {
            ShowSubordinatesPayslip = config.IsOn(SysTenantConfigKeys.ShowSubordinatesPayslip),
            ShowPayslipOnDay = config.GetInt(SysTenantConfigKeys.ShowPayslipOnDay) ?? 0
        };

        var showPayslipFromDate = config.Get(SysTenantConfigKeys.ShowPayslipFromDate);
        if (!string.IsNullOrEmpty(showPayslipFromDate) && TryParseLegacyDate(showPayslipFromDate, out var date))
        {
            settings.ShowPayslipFromDate = date;
        }

        return settings;
    }

    public static ManageSecuritySettings CreateSecuritySettings(SysTenantConfigLookup config)
    {
        return new ManageSecuritySettings
        {
            EnableRowLevelSecurity = config.IsOn(SysTenantConfigKeys.EnableRowLevelSecurity)
        };
    }

    public static ManageSelfServiceSettings CreateSelfServiceSettings(SysTenantConfigLookup config)
    {
        return new ManageSelfServiceSettings
        {
            AllowPunchOutsideRange = config.IsOn(SysTenantConfigKeys.AllowPunchOutsideRange),
            AutoLogoutAfterPunch = config.IsOn(SysTenantConfigKeys.AutoLogoutAfterPunch),
            AllowGeolocationSelfie = config.IsOn(SysTenantConfigKeys.EssAllowGeolocationSelfie),
            HideGeolocationInfo = config.IsOn(SysTenantConfigKeys.HideGeolocationInfo),
            HideGeolocationPunchInfo = config.IsOn(SysTenantConfigKeys.HideGeolocationPunchInfo),
            EmailNotification = config.IsOn(SysTenantConfigKeys.EmailNotification),
            DefaultLoginPage = config.Get(SysTenantConfigKeys.DefaultLoginPage)
        };
    }

    public static ManageTimeSettings CreateTimeSettings(SysTenantConfigLookup config)
    {
        var settings = new ManageTimeSettings
        {
            FutureEvaluationInDays = config.GetInt(SysTenantConfigKeys.TeMaxLimitDays) ?? 0,
            EnableRosterDuty = config.IsOn(SysTenantConfigKeys.EnableRosterDuty),
            ExcludePaidLeaveAccrualOnAbsent = config.IsOn(SysTenantConfigKeys.ExcludePaidLeaveAccrualOnAbsent),
            EnableTimeAttendanceIntegration = config.IsOn(SysTenantConfigKeys.EnableTimeAttendanceIntegration),
            EnableMiddleTierTimeEvents = config.IsOn(SysTenantConfigKeys.EnableAttendanceDataDumping),
            EnableAttendanceOnHoliday = config.IsOn(SysTenantConfigKeys.AutoAttendanceEnable),
            HourlyLeavelogicEnable = config.IsOn(SysTenantConfigKeys.HourlyLeaveLogicEnable),
            HourlyLeaveShouldSignIn = config.IsOn(SysTenantConfigKeys.HourlyLeaveShouldSignIn),
            HourlyLeaveShouldSignOut = config.IsOn(SysTenantConfigKeys.HourlyLeaveShouldSignOut),
            EnableTimeEngineAutorun = config.IsOn(SysTenantConfigKeys.EnableTimeEngineAutorun),
            LeaveDeductionStartMonth = config.GetInt(SysTenantConfigKeys.LeaveDeductionStartMonth) ?? 0,
            EnableOvertimeInTimeEngine = config.IsOnUnlessZero(SysTenantConfigKeys.EnableOvertimeInTimeEngine),
            TimeEventImportPath = config.Get(SysTenantConfigKeys.TimeEventImportPath),
            CountWeekoffOnPH = config.IsOn(SysTenantConfigKeys.CountWeekOffOnPh),
            RestrictUnpaidleaveIfAnnualleaveAvailable = config.IsOn(SysTenantConfigKeys.RestrictUnpaidLeaveIfAnnualLeaveAvailable),
            EnableIsTravelling = config.IsOn(SysTenantConfigKeys.ShowHideIsTravelling),
            EnableAttendanceOnWeekOff = config.IsOn(SysTenantConfigKeys.EnableAttendanceOnWeekOff),
            EnableAttendanceOnRestDay = config.IsOn(SysTenantConfigKeys.EnableAttendanceOnRestDay),
            AllowPaidleaveBackdateEntry = config.IsOn(SysTenantConfigKeys.AllowPaidLeaveBackdateEntry),
            SickLeaveAttachMandatory = config.IsOn(SysTenantConfigKeys.SickLeaveAttMandatory)
        };

        // Legacy: DateTime.ParseExact(value, "dd/MM/yyyy", InvariantCulture) when the value is not "".
        var earliest = config.Get(SysTenantConfigKeys.TimeEvalEarliestDate);
        if (earliest != string.Empty && TryParseLegacyDate(earliest, out var earliestDate))
        {
            settings.EarliestEvaluationDate = earliestDate;
        }

        return settings;
    }

    public static ManageWorkflowSettings CreateWorkflowSettings(SysTenantConfigLookup config)
    {
        return new ManageWorkflowSettings
        {
            EnableWorkflowGroup = config.IsOn(SysTenantConfigKeys.EnableUserWiseWorkflow),
            EnableOrgUnitTypeInWorkflow = config.IsOn(SysTenantConfigKeys.EnableOrgUnitTypeInWorkflow),
            HideLeavePayDetailsInWorkflow = config.IsOn(SysTenantConfigKeys.HideLeavePayDetailsInWorkflow),
            EnableTravelCostOnWorkflowstep = config.GetInt(SysTenantConfigKeys.EnableTravelCostOnWorkflowStep) ?? 0,
            OvertimeRequestModificationStep = config.GetInt(SysTenantConfigKeys.OvertimeRequestModificationStep) ?? 0,
            DynamicWorkflowSteps = config.IsOn(SysTenantConfigKeys.DynamicWorkflowSteps)
        };
    }

    /// <summary>
    /// The dd/MM/yyyy layout legacy stored dates in, then anything the invariant culture accepts (legacy
    /// used the server culture for <c>SHOW_PAYSLIP_FROM_DATE</c>, which on the Kuwait servers was dd/MM/yyyy).
    /// </summary>
    private static bool TryParseLegacyDate(string value, out DateTime date)
    {
        if (DateTime.TryParseExact(value, SysTenantConfigDefaults.LegacyDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
        {
            return true;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
