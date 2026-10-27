using System;
using System.Threading.Tasks;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.PayrollDataService</c> (a static-methods-only
/// class operating on an XPO <c>Session</c>).
/// </summary>
public interface IPayrollDataService
{
    /// <summary>
    /// Ported from legacy <c>PayrollDataService.ValidateRetroCalculation</c>. Throws
    /// <see cref="Abp.UI.UserFriendlyException"/> with the same message shape as legacy when retro
    /// calculation is disabled globally (<c>ManagePayrollSettings.DisableRetroCalculation</c>) and
    /// <paramref name="transDate"/> falls before the current period's lock/close boundary for
    /// <paramref name="payrollArea"/>'s <see cref="PayrollControlRecord"/>, unless <paramref name="entityType"/>
    /// is registered in <c>HCMv3.SystemSecurity.SysObject</c> as payroll-sensitive AND allowed to retro-run
    /// (<c>SysObject.IsPayrollSensitive</c> + <c>SysObject.MustRetroRun</c>, matched by <c>SysObject.FullName</c>
    /// == <c>entityType.FullName</c> -- see implementation remarks for the exact adaptation from legacy's
    /// in-memory <c>MyBaseObject.PayrollSensitiveList</c> cache).
    /// </summary>
    /// <param name="payrollArea">The transaction's PayrollArea. A null value is a no-op (matches legacy).</param>
    /// <param name="transDate">The date of the record being added/modified/deleted.</param>
    /// <param name="entityType">
    /// The CLR type of the entity being validated (legacy passed the XPO business object's <c>Type</c>).
    /// </param>
    Task ValidateRetroCalculationAsync(PayrollArea payrollArea, DateTime transDate, Type entityType);
}
