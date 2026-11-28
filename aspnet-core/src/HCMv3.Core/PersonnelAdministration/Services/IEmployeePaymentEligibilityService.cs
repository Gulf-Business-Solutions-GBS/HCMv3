using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Which employees a one-off payment may be keyed in for. Ported from legacy XAF
/// <c>EmployeePayment.GetAllowedEmployeesForPayment</c> (the data source of the Payment screen's Employee
/// lookup): every active employee, plus an inactive (separated, not revoked, not rejoined since) employee
/// who either has no final settlement yet, or whose final settlement falls in the payroll period currently
/// being processed for their payroll area.
/// </summary>
public interface IEmployeePaymentEligibilityService : IDomainService
{
    Task<List<int>> GetAllowedEmployeeIdsAsync();
}
