using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Administration.Tools.Dto;

namespace HCMv3.Administration.Tools;

/// <summary>
/// Ported from legacy <c>HCM10.Web/UserControls/Administration/uscPayrollConfigPanel.ascx.cs</c>.
///
/// IMPORTANT — this control was NOT a settings screen despite its name. It is a panel of destructive
/// payroll maintenance utilities. None of its function is covered by the 14 ported
/// <c>Manage*Settings</c> singletons.
/// </summary>
public interface IPayrollAdminToolsAppService : IApplicationService
{
    /// <summary>
    /// Legacy line 98. Creates a <c>PayrollCalendar</c> row for every payroll month between
    /// <c>StartDate</c> and <c>EndDate</c> for the chosen payroll area that does not already have one.
    /// Existing rows are left untouched (legacy never updated them).
    /// </summary>
    Task<AdminToolResultDto> GeneratePayrollCalendar(GeneratePayrollCalendarInput input);

    /// <summary>
    /// Legacy line 1253. Deletes the matching <c>SalaryRegister</c> rows (and their details) for a given
    /// period/run-type. Aborts the whole operation if ANY matched employee has a later register, since that
    /// later register's carried-forward balances would silently become wrong.
    /// </summary>
    Task<AdminToolResultDto> DeleteSalaryRegisters(SalaryRegisterMaintenanceInput input);

    /// <summary>
    /// Legacy line 1397. Clears <c>SalaryRegister.Locked</c> for the matching rows, with the same
    /// "no later register may exist" guard as the delete tool.
    /// </summary>
    Task<AdminToolResultDto> UnlockSalaryRegisters(SalaryRegisterMaintenanceInput input);

    /// <summary>
    /// Legacy line 1171. Re-runs annual-leave forfeit calculation. Legacy ran this ONLY when
    /// <c>ManageTimeSettings.ForfeitRule == Monthly</c>; that guard is preserved.
    /// </summary>
    Task<AdminToolResultDto> FixForfeitQuota(FixForfeitQuotaInput input);
}
