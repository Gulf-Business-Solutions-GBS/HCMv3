using Abp.Events.Bus.Entities;
using Abp.Events.Bus.Handlers;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Licensing;

/// <summary>
/// Applies the Max Active Employees limit to every NEW employee saved as active, whatever saved it: the Employee
/// screen, a bulk import, or a rejoin that creates a new employee record. Legacy checked in <c>Employee.OnSaving</c>.
///
/// <para>
/// Only creation is hooked. Turning an EXISTING employee active is checked where that happens
/// (<c>EmployeesAppService.UpdateAsync</c>, and any rejoin that reactivates the same record), because the entity
/// event carries no previous value: here an employee who was already active could not be told apart from one being
/// reactivated, and an over-limit tenant would be unable to save any active employee at all.
/// </para>
///
/// <para>
/// Registered by hand in <see cref="HCMv3.HcmNotifications.HCMv3HcmNotificationsModule"/>, not by convention: a
/// conventionally registered class is bound only to its name-matching interface, so the event bus would never
/// subscribe it (the trap described on <c>HCMv3CoreModule.RegisterBusinessRuleEntityHooks</c>).
/// </para>
/// </summary>
public class ActiveEmployeeLimitEntityHook : IEventHandler<EntityCreatingEventData<Employee>>
{
    private readonly IActiveEmployeeLimitChecker _limitChecker;

    public ActiveEmployeeLimitEntityHook(IActiveEmployeeLimitChecker limitChecker)
    {
        _limitChecker = limitChecker;
    }

    public void HandleEvent(EntityCreatingEventData<Employee> eventData)
    {
        var employee = eventData.Entity;
        if (employee.Status != EmployeeStatus.Active)
        {
            return;
        }

        _limitChecker.EnsureCanActivate(employee.TenantId, employee.Id);
    }
}
