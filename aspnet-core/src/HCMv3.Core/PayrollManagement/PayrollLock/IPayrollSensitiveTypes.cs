using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using HCMv3.SystemSecurity;

namespace HCMv3.PayrollManagement.PayrollLock;

/// <summary>
/// Which record types a company has flagged payroll sensitive and time sensitive, from the System Objects screen.
/// Legacy kept the same two lists in a static cache (<c>MyBaseObject.PayrollSensitiveList</c> /
/// <c>TimeSensitiveList</c>); here they are cached per company and dropped when System Objects change.
/// </summary>
public interface IPayrollSensitiveTypes : IDomainService
{
    /// <summary>Payroll sensitive: the payroll lock covers it, and changing it triggers a recalculation.</summary>
    bool IsPayrollSensitive(int tenantId, Type type);

    /// <summary>Time sensitive: changing it also moves the employee's next time evaluation date back.</summary>
    bool IsTimeSensitive(int tenantId, Type type);
}

/// <inheritdoc cref="IPayrollSensitiveTypes"/>
public class PayrollSensitiveTypes : HCMv3DomainServiceBase, IPayrollSensitiveTypes
{
    public const string CacheName = "PayrollSensitiveTypes";

    private readonly IRepository<SysObject> _sysObjectRepository;
    private readonly ICacheManager _cacheManager;

    public PayrollSensitiveTypes(IRepository<SysObject> sysObjectRepository, ICacheManager cacheManager)
    {
        _sysObjectRepository = sysObjectRepository;
        _cacheManager = cacheManager;
    }

    public bool IsPayrollSensitive(int tenantId, Type type) =>
        type?.FullName != null && Flags(tenantId).PayrollSensitive.Contains(type.FullName);

    public bool IsTimeSensitive(int tenantId, Type type) =>
        type?.FullName != null && Flags(tenantId).TimeSensitive.Contains(type.FullName);

    private SensitiveTypeNames Flags(int tenantId)
    {
        return _cacheManager
            .GetCache<int, SensitiveTypeNames>(CacheName)
            .Get(tenantId, () =>
            {
                using (UnitOfWorkManager.Current.SetTenantId(tenantId))
                using (UnitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    var rows = _sysObjectRepository.GetAll()
                        .Where(r => r.FullName != null && (r.IsPayrollSensitive || r.IsTimeSensitive))
                        .Select(r => new { r.FullName, r.IsPayrollSensitive, r.IsTimeSensitive })
                        .ToList();

                    return new SensitiveTypeNames(
                        rows.Where(r => r.IsPayrollSensitive).Select(r => r.FullName).ToHashSet(StringComparer.Ordinal),
                        rows.Where(r => r.IsTimeSensitive).Select(r => r.FullName).ToHashSet(StringComparer.Ordinal));
                }
            });
    }

    /// <summary>Both lists in one cache entry: every save reads them together.</summary>
    public class SensitiveTypeNames
    {
        public SensitiveTypeNames(HashSet<string> payrollSensitive, HashSet<string> timeSensitive)
        {
            PayrollSensitive = payrollSensitive;
            TimeSensitive = timeSensitive;
        }

        public HashSet<string> PayrollSensitive { get; }

        public HashSet<string> TimeSensitive { get; }
    }
}
