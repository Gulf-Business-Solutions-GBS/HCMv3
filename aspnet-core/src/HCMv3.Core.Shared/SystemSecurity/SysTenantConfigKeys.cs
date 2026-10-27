namespace HCMv3.SystemSecurity;

/// <summary>
/// Every <c>Key</c> the legacy application reads from the <c>SysTenantConfig</c> table (legacy
/// <c>SysService.GetSysTenantConfig(key)</c> and the direct <c>XPQuery&lt;SysTenantConfig&gt;</c> reads in
/// <c>SysService.Get*Settings</c>). The spelling is the legacy one, including its oddities
/// (<see cref="OvertimeRequestModificationStep"/>, <see cref="ShowHideIsTravelling"/>), so a tenant's
/// existing rows keep working when its data is moved across.
///
/// The Dynamics 365 keys live in <c>HCMv3.Integrations.D365FinanceSettingKeys</c>.
/// </summary>
public static class SysTenantConfigKeys
{
    // Appraisal
    public const string AppraisalCycle = "APPRAISAL_CYCLE";
    public const string EnablePaPeerReview = "ENABLE_PA_PEER_REVIEW";
    public const string SetPeerMinLimit = "SET_PEER_MIN_LIMIT";
    public const string SetPeerMaxLimit = "SET_PEER_MAX_LIMIT";

    // Document
    public const string EnableDocSaveInFileSystem = "ENABLE_DOC_SAVE_IN_FILE_SYSTEM";
    public const string FileSystemAttachmentPath = "FILE_SYSTEM_ATTACHMENT_PATH";

    // FI posting
    public const string SeparateProvisionFiPosting = "SEPARATE_PROVISION_FI_POSTING";
    public const string EnableAutoWageAccountEntry = "ENABLE_AUTO_WAGEACCOUNT_ENTRY";

    // General
    public const string DisableAutoEmpCode = "DISABLE_AUTO_EMPCODE";
    public const string DateEditMask = "DATE_EDIT_MASK";
    public const string DateDisplayFormat = "DATE_DISPLAY_FORMAT";
    public const string TimeEditMask = "TIME_EDIT_MASK";
    public const string TimeDisplayFormat = "TIME_DISPLAY_FORMAT";
    public const string StartupDashboard = "STARTUP_DASHBOARD";
    public const string NoticePeriodDaysExcludingSubmissionDate = "NOTICE_PERIOD_DAYS_EXCLUDING_SUBMISSIONDATE";
    public const string EnableGridDataAccessModeServer = "ENABLE_GRID_DATA_ACCESS_MODE_SERVER";

    // Organization
    public const string EnablePostingGroup = "ENABLE_POSTINGGROUP";
    public const string EnableBudgetingGroup = "ENABLE_BUDGETINGGROUP";
    public const string EnablePosition = "ENABLE_POSITION";

    // Payroll
    public const string ReportGroup = "REPORT_GROUP";
    public const string DefaultWorkingHours = "DEFAULT_WORKING_HOURS";
    public const string PayslipSignature = "PAYSLIP_SIGNATURE";
    public const string LeaveAdvPaymentDateBefore15 = "LEAVE_ADV_PAYMENTDATE_BEFORE_15";
    public const string LeaveAdvPaymentDateAfter15 = "LEAVE_ADV_PAYMENTDATE_AFTER_15";
    public const string EnablePeriodWiseProvision = "ENABLE_PERIODWISE_PROVISION";
    public const string EnableNotReturnFromLeaveLogic = "ENABLE_NOTRETURNFROMLEAVELOGIC";
    public const string EnableBasicPayMoreThenFixedDays = "ENABLE_BASICPAY_MORE_THEN_FIXEDDAYS";
    public const string EnableUnpaidDedSlabWise = "ENABLE_UNPAID_DED_SLAB_WISE";
    public const string GenerateRegisterOnNotReturnFromLeave = "GENERATE_REGISTER_ON_NOTRETURNFROMLEAVE";
    public const string OffCycleBankFileCutoff = "OFFCYCLE_BANKFILE_CUTOFF";
    public const string PayrollNetPayAmtRounding = "PAYROLL_NET_PAY_AMT_ROUNDING";
    public const string ApplyAnnualLeaveNoImpactLogic = "APPLY_ANNUALLEAVENOIMPACT_LOGIC";
    public const string EnablePayrollEngineAuto = "ENABLE_PAYROLLENGINE_AUTO";
    public const string EnableWageAccountByOrgUnit = "ENABLE_WAGEACCOUNT_BY_ORGUNIT";

    // Report
    public const string ShowSubordinatesPayslip = "SHOW_SUBORDINATES_PAYSLIP";
    public const string ShowPayslipOnDay = "SHOW_PAYSLIP_ON_DAY";
    public const string ShowPayslipFromDate = "SHOW_PAYSLIP_FROM_DATE";

    // Security
    public const string EnableRowLevelSecurity = "ENABLE_ROWLEVELSECURITY";

    // Self service
    public const string AllowPunchOutsideRange = "ALLOW_PUNCH_OUTSIDE_RANGE";
    public const string AutoLogoutAfterPunch = "AUTO_LOGOUT_AFTER_PUNCH";
    public const string EssAllowGeolocationSelfie = "ESS_ALLOW_GEOLOCATION_SELFIE";
    public const string HideGeolocationInfo = "HIDE_GEOLOCATION_INFO";
    public const string HideGeolocationPunchInfo = "HIDE_GEOLOCATION_PUNCH_INFO";
    public const string EmailNotification = "EMAIL_NOTIFICATION";
    public const string DefaultLoginPage = "DEFAULT_LOGIN_PAGE";

    // Time
    public const string TimeEvalEarliestDate = "TIME_EVAL_EARLIEST_DATE";
    public const string TeMaxLimitDays = "TE_MAX_LIMIT_DAYS";
    public const string EnableRosterDuty = "ENABLE_ROSTERDUTY";
    public const string ExcludePaidLeaveAccrualOnAbsent = "EXCLUDE_PAIDLEAVEACCRUAL_ON_ABSENT";
    public const string EnableTimeAttendanceIntegration = "ENABLE_TIMEATTENDANCE_INTEGRATION";
    public const string EnableAttendanceDataDumping = "ENABLE_ATTENDANCEDATA_DUMPING";
    public const string AutoAttendanceEnable = "AUTO_ATTENDANCE_ENABLE";
    public const string HourlyLeaveLogicEnable = "HOURLYLEAVELOGIC_ENABLE";
    public const string HourlyLeaveShouldSignIn = "HOURLYLEAVE_SHOULDSIGNIN";
    public const string HourlyLeaveShouldSignOut = "HOURLYLEAVE_SHOULDSIGNOUT";
    public const string EnableTimeEngineAutorun = "ENABLE_TIMEENGINE_AUTORUN";
    public const string LeaveDeductionStartMonth = "LEAVE_DEDUCTION_START_MONTH";
    public const string EnableOvertimeInTimeEngine = "ENABLE_OVERTIME_IN_TIME_ENGINE";
    public const string TimeEventImportPath = "TIME_EVENT_IMPORT_PATH";
    public const string CountWeekOffOnPh = "COUNT_WEEKOFF_ON_PH";
    public const string RestrictUnpaidLeaveIfAnnualLeaveAvailable = "RESTRICT_UNPAIDLEAVE_IF_ANNUALLEAVE_AVAILABLE";
    public const string ShowHideIsTravelling = "SHOW_HIDE_IsTravelling";
    public const string EnableAttendanceOnWeekOff = "ENABLE_ATTENDANCE_ON_WEEKOFF";
    public const string EnableAttendanceOnRestDay = "ENABLE_ATTENDANCE_ON_RESTDAY";
    public const string AllowPaidLeaveBackdateEntry = "ALLOW_PAID_LEAVE_BACKDATE_ENTRY";
    public const string SickLeaveAttMandatory = "SICK_LEAVE_ATT_MANDATORY";

    // Workflow
    public const string EnableUserWiseWorkflow = "ENABLE_USERWISE_WORKFLOW";
    public const string EnableOrgUnitTypeInWorkflow = "ENABLE_ORGUNITTYPE_IN_WORKFLOW";
    public const string HideLeavePayDetailsInWorkflow = "HIDE_LEAVE_PAY_DETAILS_IN_WORKFLOW";
    public const string EnableTravelCostOnWorkflowStep = "ENABLE_TRAVELCOST_ON_WORKFLOWSTEP";
    public const string OvertimeRequestModificationStep = "OVERTIME_REQEUST_MODIFICATION_STEP";
    public const string DynamicWorkflowSteps = "DYNAMIC_WORKFLOW_STEPS";

    // Seeded by legacy DefaultDataService but never read by legacy code; kept for data parity.
    public const string CutoverDate = "CUTOVER_DATE";
    public const string LeaveConflictCheck = "LEAVE_CONFLICT_CHECK";
}
