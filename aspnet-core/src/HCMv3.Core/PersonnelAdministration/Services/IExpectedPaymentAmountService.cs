using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The "Expected Amount" of an <see cref="EmployeePayment"/> keyed in as Days or Hours: what that many days
/// of the employee's partial-pay salary are worth. Ported from the legacy XAF non-persistent
/// <c>EmployeePayment.ExpectedAmount</c> getter and its private <c>GetExpectedAmount</c>
/// (<c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeePayment.cs</c>).
///
/// <para>Read-only: nothing is stored. It is the on-screen preview figure.</para>
/// </summary>
public interface IExpectedPaymentAmountService : IDomainService
{
    /// <summary>
    /// Returns 0 when the unit type is Amount, the number is 0, or the employee, wage type or payment date is
    /// missing -- exactly the legacy getter's gate.
    /// </summary>
    Task<double> GetExpectedAmountAsync(int employeeId, int wageTypeId, DateTime paymentDate, UnitType unitType, double number);
}
