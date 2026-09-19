using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using Abp.Threading;
using Abp.Timing;
using Abp.UI;
using HCMv3.PayrollManagement.Services;
using HCMv3.SystemSecurity;

namespace HCMv3.PayrollManagement.PayrollLock;

/// <inheritdoc cref="IPayrollLockGuard"/>
public class PayrollLockGuard : HCMv3DomainServiceBase, IPayrollLockGuard
{
    /// <summary>The company's payroll-sensitive type names, cached per tenant. Cleared when System Objects change.</summary>
    public const string SensitiveTypesCacheName = "PayrollSensitiveTypes";

    /// <summary>Units of work a system process has exempted. Weak, so a finished unit of work is not kept alive.</summary>
    private static readonly ConditionalWeakTable<IActiveUnitOfWork, object> ExemptUnitsOfWork = new();

    /// <summary>The <c>EmployeeId</c> property per entity type, looked up once. Null when the type has none.</summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo> EmployeeIdProperties = new();

    private readonly IRepository<SysObject> _sysObjectRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly ICacheManager _cacheManager;

    public PayrollLockGuard(
        IRepository<SysObject> sysObjectRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        ICacheManager cacheManager)
    {
        _sysObjectRepository = sysObjectRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollAreaResolver = payrollAreaResolver;
        _cacheManager = cacheManager;
    }

    public void ExemptCurrentUnitOfWork()
    {
        var unitOfWork = UnitOfWorkManager.Current;
        if (unitOfWork != null)
        {
            ExemptUnitsOfWork.AddOrUpdate(unitOfWork, null);
        }
    }

    public void EnsureChangeAllowed(object entity)
    {
        if (entity is not IMustHaveTenant tenantEntity)
        {
            return;
        }

        var unitOfWork = UnitOfWorkManager.Current;
        if (unitOfWork == null || ExemptUnitsOfWork.TryGetValue(unitOfWork, out _))
        {
            return;
        }

        // Cheapest test first: this runs on every save in the system, and almost none are payroll-sensitive.
        var type = entity.GetType();
        if (!GetSensitiveTypeNames(tenantEntity.TenantId).Contains(type.FullName))
        {
            return;
        }

        var employeeId = GetEmployeeId(entity);
        if (employeeId is not > 0)
        {
            return;
        }

        using (unitOfWork.SetTenantId(tenantEntity.TenantId))
        {
            // Today, not the record's own date -- legacy reads the payroll area the employee is in now.
            var payrollArea = AsyncHelper.RunSync(() =>
                _payrollAreaResolver.GetEmployeePayrollAreaAsync(employeeId.Value, Clock.Now.Date));

            if (payrollArea == null)
            {
                return;
            }

            // The latest period only, as legacy's CheckIfPayrollAreaLocked: an old period left locked does not
            // count once a later one exists.
            var latestPeriodLocked = _payrollControlRecordRepository.GetAll()
                .Where(r => r.PayrollAreaId == payrollArea.Id)
                .OrderByDescending(r => r.PayrollYear)
                .ThenByDescending(r => r.PayrollMonth)
                .Select(r => (bool?)r.PeriodLocked)
                .FirstOrDefault();

            if (latestPeriodLocked == true)
            {
                throw new UserFriendlyException(L("PayrollAreaLockedForEditing", payrollArea.Name));
            }
        }
    }

    private HashSet<string> GetSensitiveTypeNames(int tenantId)
    {
        return _cacheManager
            .GetCache<int, HashSet<string>>(SensitiveTypesCacheName)
            .Get(tenantId, () =>
            {
                using (UnitOfWorkManager.Current.SetTenantId(tenantId))
                {
                    return _sysObjectRepository.GetAll()
                        .Where(r => r.IsPayrollSensitive && r.FullName != null)
                        .Select(r => r.FullName)
                        .ToList()
                        .ToHashSet(StringComparer.Ordinal);
                }
            });
    }

    private static int? GetEmployeeId(object entity)
    {
        var property = EmployeeIdProperties.GetOrAdd(entity.GetType(), type => type.GetProperty("EmployeeId"));
        return property?.GetValue(entity) switch
        {
            int id => id,
            _ => null,
        };
    }
}
