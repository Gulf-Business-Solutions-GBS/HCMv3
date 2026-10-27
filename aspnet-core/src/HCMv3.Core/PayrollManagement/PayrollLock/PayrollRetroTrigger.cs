using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Repositories;
using Abp.Threading;
using Abp.Timing;
using Abp.UI;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;

namespace HCMv3.PayrollManagement.PayrollLock;

/// <inheritdoc cref="IPayrollRetroTrigger"/>
public class PayrollRetroTrigger : HCMv3DomainServiceBase, IPayrollRetroTrigger
{
    /// <summary>Legacy's "never set" date for these fields: <c>new DateTime(1, 1, 1)</c>, i.e. DateTime.MinValue.</summary>
    private static readonly DateTime NeverSet = DateTime.MinValue;

    /// <summary>The date properties legacy reads, in legacy's order. The first one set wins.</summary>
    private static readonly string[] DateProperties = { "StartDate", "EffectiveDate", "PaymentDate", "CompensationDate" };

    /// <summary>
    /// Legacy's dd/MM/yyyy, pinned to the invariant culture: a "/" in a .NET date format is replaced by whatever
    /// the machine's language settings use as a date separator, which would leave these messages in two formats.
    /// </summary>
    private static string Day(DateTime date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static readonly ConcurrentDictionary<Type, PropertyInfo> EmployeeIdProperties = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> DateProperties_ = new();

    /// <summary>Rows marked for this save only. Weak, so nothing is kept alive by being marked.</summary>
    private static readonly ConditionalWeakTable<object, object> EndDateAutoChanged = new();
    private static readonly ConditionalWeakTable<object, object> StopRetroEffect = new();

    private readonly IRepository<EmployeePayrollStatus> _payrollStatusRepository;
    private readonly IRepository<RetroReasonLog> _retroReasonLogRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IPayrollSensitiveTypes _sensitiveTypes;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IPayrollDataService _payrollDataService;

    public PayrollRetroTrigger(
        IRepository<EmployeePayrollStatus> payrollStatusRepository,
        IRepository<RetroReasonLog> retroReasonLogRepository,
        IRepository<Employee> employeeRepository,
        IPayrollSensitiveTypes sensitiveTypes,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IPayrollDataService payrollDataService)
    {
        _payrollStatusRepository = payrollStatusRepository;
        _retroReasonLogRepository = retroReasonLogRepository;
        _employeeRepository = employeeRepository;
        _sensitiveTypes = sensitiveTypes;
        _payrollAreaResolver = payrollAreaResolver;
        _payrollDataService = payrollDataService;
    }

    /// <summary>Open <see cref="SuppressEarliestMasterDataChange"/> scopes on this async flow. Static because the
    /// trigger is resolved per use; AsyncLocal so a scope on one request never leaks into another.</summary>
    private static readonly System.Threading.AsyncLocal<int> SuppressMasterDataChangeDepth = new();

    public IDisposable SuppressEarliestMasterDataChange()
    {
        SuppressMasterDataChangeDepth.Value++;
        return new Abp.DisposeAction(() => SuppressMasterDataChangeDepth.Value--);
    }

    public void MarkEndDateAutoChange(object entity)
    {
        if (entity != null)
        {
            EndDateAutoChanged.AddOrUpdate(entity, null);
        }
    }

    public void MarkStopRetroEffect(object entity)
    {
        if (entity != null)
        {
            StopRetroEffect.AddOrUpdate(entity, null);
        }
    }

    public void RecordChange(object entity, bool deleted)
    {
        if (entity is not IMustHaveTenant tenantEntity || UnitOfWorkManager.Current == null)
        {
            return;
        }

        // A row re-closed by end-date chaining is not a change of its own -- legacy's IsEndDateAutoChange.
        if (EndDateAutoChanged.TryGetValue(entity, out _))
        {
            return;
        }

        // Legacy ignores StopRetroEffect when deleting; deleting a record always affects pay.
        if (!deleted && StopRetroEffect.TryGetValue(entity, out _))
        {
            return;
        }

        var type = entity.GetType();
        if (!_sensitiveTypes.IsPayrollSensitive(tenantEntity.TenantId, type))
        {
            return;
        }

        var employeeId = GetEmployeeId(entity);
        if (employeeId is not > 0)
        {
            return;
        }

        var (transDate, dateFieldName) = GetTransDate(entity);
        if (transDate == default)
        {
            return;
        }

        var id = entity is IEntity<int> withId ? withId.Id : 0;

        // ABP raises this event after the row has been written, so the id is already there; what tells a new row
        // from a changed one is that nothing has modified it yet.
        var created = entity is IHasModificationTime { LastModificationTime: null };
        var what = deleted ? "deleted." : created ? "created." : "modified.";
        var reason = string.Format(
            "{0} {1} Id : {2} | {3} : {4}",
            type.Name,
            what,
            id,
            dateFieldName,
            Day(transDate));

        Apply(
            tenantEntity.TenantId,
            type,
            employeeId.Value,
            transDate,
            reason,
            id);
    }

    public void SetRetroDate(Type entityType, int employeeId, DateTime retroFromDate, int entityObjectId = 0)
    {
        if (entityType == null || employeeId <= 0 || retroFromDate == default || UnitOfWorkManager.Current == null)
        {
            return;
        }

        var tenantId = UnitOfWorkManager.Current.GetTenantId() ?? 0;
        if (!_sensitiveTypes.IsPayrollSensitive(tenantId, entityType))
        {
            return;
        }

        var reason = string.Format("{0} SetRetroDate called. Date : {1}", entityType.Name, Day(retroFromDate));
        Apply(tenantId, entityType, employeeId, retroFromDate, reason, entityObjectId);
    }

    /// <summary>
    /// The body legacy shares between saving, deleting and SetRetroDate: validate, then lower the two dates and log.
    /// </summary>
    private void Apply(int tenantId, Type type, int employeeId, DateTime transDate, string reason, int entityObjectId)
    {
        using (UnitOfWorkManager.Current.SetTenantId(tenantId))
        {
            var payrollArea = AsyncHelper.RunSync(() =>
                _payrollAreaResolver.GetEmployeePayrollAreaAsync(employeeId, Clock.Now.Date));

            // "Retro calculation is disabled. You cannot add, modify or delete records before date ..."
            AsyncHelper.RunSync(() => _payrollDataService.ValidateRetroCalculationAsync(payrollArea, transDate, type));

            var status = _payrollStatusRepository.GetAll().FirstOrDefault(s => s.EmployeeId == employeeId);
            if (status == null)
            {
                status = NewStatusFor(tenantId, employeeId, transDate);
            }

            if (transDate < status.EarliestRetroAccountingDate)
            {
                throw new UserFriendlyException(L(
                    "CannotModifyBeforeEarliestRetroAccountingDate",
                    Day(status.EarliestRetroAccountingDate)));
            }

            // Down only. A change dated later than a pending one must not shorten the window the run will redo.
            if (SuppressMasterDataChangeDepth.Value == 0
                && (status.EarliestMasterDataChange == NeverSet || status.EarliestMasterDataChange > transDate))
            {
                status.EarliestMasterDataChange = transDate;
                _retroReasonLogRepository.Insert(new RetroReasonLog
                {
                    TenantId = tenantId,
                    EmployeeId = employeeId,
                    ChangedOn = Clock.Now,
                    RetroDate = transDate,
                    EntityObjectName = type.FullName,
                    EntityObjectId = entityObjectId,
                    RetroReason = reason,
                });
            }

            if (_sensitiveTypes.IsTimeSensitive(tenantId, type)
                && (status.NextTimeEvaluationFrom == NeverSet || status.NextTimeEvaluationFrom > transDate))
            {
                status.NextTimeEvaluationFrom = transDate;
            }

            _payrollStatusRepository.InsertOrUpdate(status);
        }
    }

    /// <summary>
    /// Legacy SetRetroDate creates the row when the employee has none, with the joining date as the floor.
    ///
    /// <para>The two dates start unset rather than at the change's own date, so the caller's normal "lower it and say
    /// why" step runs and the change is logged. Legacy filled them in here and then skipped its own log line -- the
    /// very first back-dated change on an employee went unrecorded.</para>
    /// </summary>
    private EmployeePayrollStatus NewStatusFor(int tenantId, int employeeId, DateTime transDate)
    {
        var joiningDate = _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => e.JoiningDate)
            .FirstOrDefault();

        return new EmployeePayrollStatus
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            EarliestRetroAccountingDate = joiningDate,
            EarliestMasterDataChange = NeverSet,
            NextTimeEvaluationFrom = NeverSet,
        };
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

    /// <summary>
    /// Legacy's transDate: StartDate, else EffectiveDate, else PaymentDate, else CompensationDate -- in that order,
    /// taking the first that HOLDS a date. A record with an empty StartDate falls through to the next, as in legacy.
    /// </summary>
    private static (DateTime Date, string FieldName) GetTransDate(object entity)
    {
        var properties = DateProperties_.GetOrAdd(entity.GetType(), type => DateProperties
            .Select(type.GetProperty)
            .Where(p => p != null && (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?)))
            .ToArray());

        foreach (var property in properties)
        {
            if (property.GetValue(entity) is DateTime value && value != default)
            {
                return (value.Date, property.Name);
            }
        }

        return (default, null);
    }
}
