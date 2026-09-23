namespace HCMv3.SystemSettings.Dto;

/// <summary>
/// The tenant settings that only shape screens, gathered for the client in one call. Legacy read each of
/// these from its <c>Manage*Settings</c> row inside a view controller to show, hide or enable a field; here
/// the screens read them from this DTO. Everything with a server-side rule has its own service; this is the
/// presentation-only remainder.
/// </summary>
public class TenantUiSettingsDto
{
    /// <summary>Legacy <c>ManageOrgSettings.EnablePostingGroup</c>: the posting group is shown on org unit and action screens.</summary>
    public bool EnablePostingGroup { get; set; }

    /// <summary>Legacy <c>ManageOrgSettings.EnableBudgetingGroup</c>: the budgeting group is shown on org unit and action screens.</summary>
    public bool EnableBudgetingGroup { get; set; }

    /// <summary>Legacy <c>ManageOrgSettings.EnablePosition</c>: the position is shown (and expected) on org assignment screens.</summary>
    public bool EnablePosition { get; set; }

    /// <summary>Legacy <c>ManageOrgSettings.EnableOrgChart3Levels</c>: the org chart opens showing three levels.</summary>
    public bool EnableOrgChart3Levels { get; set; }

    /// <summary>Legacy <c>ManagePayrollSettings.ShowBillingTypeInPaymentScreen</c>: the billing type column on employee payments.</summary>
    public bool ShowBillingTypeInPaymentScreen { get; set; }

    /// <summary>Legacy <c>ManagePayrollSettings.AutoFillFnFCompensation</c>: F&amp;F compensation days are pre-filled.</summary>
    public bool AutoFillFnFCompensation { get; set; }

    /// <summary>Legacy <c>ManagePayrollSettings.EditableAutoFillFnFCompensation</c>: pre-filled compensation days stay editable.</summary>
    public bool EditableAutoFillFnFCompensation { get; set; }

    /// <summary>Legacy <c>ManagePayrollSettings.AllowSimulationRun</c>: the payroll run screen offers a simulation run.</summary>
    public bool AllowSimulationRun { get; set; }

    /// <summary>Legacy <c>ManageReportSettings.DecimalInReports</c>: decimals shown in grid reports (3 when unset).</summary>
    public int DecimalInReports { get; set; }

    /// <summary>Legacy <c>ManageGeneralSettings.DisableAutoEmpcode</c>: the employee code is entered by hand.</summary>
    public bool DisableAutoEmpcode { get; set; }

    /// <summary>Legacy <c>ManageTimeSettings.EnableIsTravelling</c>: the travelling tick box on leave screens.</summary>
    public bool EnableIsTravelling { get; set; }

    /// <summary>Legacy <c>ManageTimeSettings.EnableAutoHalfDayLeave</c>: the half-day choice on leave screens.</summary>
    public bool EnableAutoHalfDayLeave { get; set; }

    /// <summary>Legacy <c>ManagePersonnelSettings.AutoWageAssignOnHire</c>: the new-employee form offers "copy wage accounts from".</summary>
    public bool AutoWageAssignOnHire { get; set; }
}
