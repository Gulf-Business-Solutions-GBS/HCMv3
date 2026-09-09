using System.Linq;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.BusinessRules.Hooks;
using HCMv3.BusinessRules.Services;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.BusinessRules;

/// <summary>
/// Runs leave rules on every leave save, and resolves the two companion parameters a leave rule needs.
///
/// <para>
/// The extra work over <see cref="EmployeeBusinessRuleHook"/> is the point of the class: a leave row knows
/// only ids, while a rule wants to ask about the person and the type. Resolving them HERE, once per save,
/// means a rule author writes <c>Employee.Gender</c> without needing to know it cost a query -- and means
/// the query happens exactly once no matter how many rules are armed on the event.
/// </para>
/// </summary>
public class EmployeeAbsenceBusinessRuleHook : BusinessRuleEntityHookBase<EmployeeAbsence>
{
    private readonly IRuleEmployeeSnapshotBuilder _employeeSnapshotBuilder;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;

    public EmployeeAbsenceBusinessRuleHook(
        IBusinessRuleRunner runner,
        IRuleBaseObjectRegistry registry,
        IAbpSession session,
        IUnitOfWorkManager unitOfWorkManager,
        IRuleEmployeeSnapshotBuilder employeeSnapshotBuilder,
        IRepository<AbsenceType> absenceTypeRepository)
        : base(runner, registry, session, unitOfWorkManager)
    {
        _employeeSnapshotBuilder = employeeSnapshotBuilder;
        _absenceTypeRepository = absenceTypeRepository;
    }

    protected override string BaseObjectCode => EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode;

    protected override void PopulateParameters(RuleExecutionContext context, EmployeeAbsence entity)
    {
        context.WithParameter(EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter, entity);

        // The snapshot is resolved as of the leave's START date, not today. An eligibility rule about a
        // backdated request must see the org assignment that was in force then, or it answers about a
        // person who did not exist in that role at the time.
        var asOf = entity.StartDate == default ? context.ExecutionDate : entity.StartDate;

        // GetAwaiter().GetResult() rather than AsyncHelper: the builder's work is synchronous underneath
        // (it returns Task.FromResult over IQueryable), so there is no continuation to marshal.
        var employee = _employeeSnapshotBuilder.BuildAsync(entity.EmployeeId, asOf).GetAwaiter().GetResult();

        context.WithParameter(EmployeeAbsenceRuleBaseObjectProvider.EmployeeParameter, employee);

        // Null when the type is not set yet. Rules read null fields off it without failing, so an OnInit
        // rule on a blank leave form still works.
        var absenceType = entity.AbsenceTypeId == 0
            ? null
            : _absenceTypeRepository.GetAll().FirstOrDefault(t => t.Id == entity.AbsenceTypeId);

        context.WithParameter(EmployeeAbsenceRuleBaseObjectProvider.LeaveTypeParameter, absenceType);
    }
}
