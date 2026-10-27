using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings.Services;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IPayrollDataService"/>
/// <remarks>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.PayrollDataService</c>. Every
/// <c>XPQuery&lt;T&gt;(session)</c> call is replaced with an injected <c>IRepository&lt;T&gt;</c>, and the
/// legacy <c>SysService.GetPayrollSettings(Session)</c> singleton lookup is replaced with the shared
/// <see cref="ISysSettingsAccessor.GetPayrollSettingsAsync"/> (Wave 1).
///
/// <para><b>MyBaseObject.PayrollSensitiveList / SysObject.MustRetroRun adaptation:</b> legacy kept an
/// in-memory, statically-cached list of "payroll sensitive" business object descriptors
/// (<c>MyBaseObject.PayrollSensitiveList</c>, presumably populated from the same underlying <c>SysObject</c>
/// table this port already has). This port has no such cache (and no XAF <c>Type</c>-registry
/// infrastructure to build one from) -- instead, this queries the ported <see cref="SysObject"/> table
/// directly by <see cref="SysObject.FullName"/> == <paramref name="PayrollDataService.ValidateRetroCalculationAsync"/>'s
/// <c>entityType.FullName</c>, filtered to rows flagged <see cref="SysObject.IsPayrollSensitive"/>, and reads
/// <see cref="SysObject.MustRetroRun"/> off the match (defaulting to <c>false</c> if no matching, payroll-
/// sensitive <c>SysObject</c> row exists) -- functionally equivalent to legacy's cache-then-lookup, just
/// backed by a live query instead of a static in-memory list.
/// </para>
///
/// <see cref="UnitOfWorkAttribute"/> at the class level, matching <c>SysSettingsAccessor</c>/<c>AccrualService</c>
/// (this domain service is independently callable, opening its own unit of work when not already inside one).
/// </remarks>
[UnitOfWork]
public class PayrollDataService : HCMv3DomainServiceBase, IPayrollDataService
{
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<SysObject> _sysObjectRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public PayrollDataService(
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<SysObject> sysObjectRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _sysObjectRepository = sysObjectRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task ValidateRetroCalculationAsync(PayrollArea payrollArea, DateTime transDate, Type entityType)
    {
        if (payrollArea == null)
        {
            return;
        }

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        if (payrollSettings == null || !payrollSettings.DisableRetroCalculation)
        {
            return;
        }

        // NOTE: HCMv3.Core has no reference to EntityFrameworkCore (per architecture rules), so the usual
        // Microsoft.EntityFrameworkCore async IQueryable extensions are not available here -- same
        // documented constraint as SysSettingsAccessor/AccrualService. Plain synchronous System.Linq over
        // IRepository<T>.GetAll() is used instead.
        var mustRetroRun = entityType != null && _sysObjectRepository.GetAll()
            .Where(r => r.FullName == entityType.FullName && r.IsPayrollSensitive)
            .Select(r => r.MustRetroRun)
            .FirstOrDefault();

        if (mustRetroRun)
        {
            return;
        }

        var payrollControlRecord = _payrollControlRecordRepository.GetAll()
            .FirstOrDefault(r => r.PayrollAreaId == payrollArea.Id);

        if (payrollControlRecord == null)
        {
            return;
        }

        var minDate = default(DateTime);
        if (!payrollControlRecord.PeriodLocked && !payrollControlRecord.PeriodClosed)
        {
            minDate = payrollControlRecord.CalendarMonthStart;
        }
        else if (payrollControlRecord.PeriodLocked && !payrollControlRecord.PeriodClosed)
        {
            minDate = payrollControlRecord.CalendarMonthStart;
        }
        else if (!payrollControlRecord.PeriodLocked && payrollControlRecord.PeriodClosed)
        {
            minDate = payrollControlRecord.CalendarMonthEnd.AddDays(1);
        }
        // (PeriodLocked && PeriodClosed) is not covered by any branch, leaving minDate at its default value --
        // faithful to legacy, which has no corresponding "else if" for that combination either.

        if (minDate > transDate)
        {
            throw new UserFriendlyException(string.Format(
                "Retro calculation is disabled. You cannot add, modify or delete records before date : {0}, Payroll Area : {1}",
                minDate.ToString("dd/MM/yyyy"), payrollArea.Name));
        }
    }
}
