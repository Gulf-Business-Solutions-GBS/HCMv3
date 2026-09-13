using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.BusinessRules.AfterSave;
using HCMv3.BusinessRules.Hooks;
using HCMv3.BusinessRules.Services;

namespace HCMv3.PersonnelAdministration.BusinessRules;

/// <summary>
/// Runs employee rules on every employee save.
///
/// <para>
/// The entire opt-in for the Employee entity: this class plus one registration line. Everything else --
/// which fields are reachable, which rules are armed, what they do -- is data.
/// </para>
/// </summary>
public class EmployeeBusinessRuleHook : BusinessRuleEntityHookBase<Employee>
{
    public EmployeeBusinessRuleHook(
        IBusinessRuleRunner runner,
        IRuleBaseObjectRegistry registry,
        IAbpSession session,
        IUnitOfWorkManager unitOfWorkManager,
        IBusinessRuleAfterSaveDispatcher afterSaveDispatcher)
        : base(runner, registry, session, unitOfWorkManager, afterSaveDispatcher)
    {
    }

    protected override string BaseObjectCode => EmployeeRuleBaseObjectProvider.BaseObjectCode;

    protected override void PopulateParameters(RuleExecutionContext context, Employee entity)
    {
        context.WithParameter(EmployeeRuleBaseObjectProvider.EmployeeParameter, entity);
    }
}
