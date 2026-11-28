using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Legacy XAF <c>Employee.OnSaving</c> region "Update LeaveQuota EntitlmentFrom and DeductionFrom": when the joining
/// date changes, the employee's earliest Leave Quota row of every active accrual leave type is moved to start from it.
/// </summary>
public interface IEmployeeLeaveQuotaRealignService : IDomainService
{
    Task RealignAsync(int employeeId, DateTime joiningDate);

    /// <summary>Legacy EntitlmentFrom rule for one leave type, given the joining date.</summary>
    DateTime GetEntitlementFrom(HCMv3.PayrollManagement.AbsenceType leaveType, DateTime joiningDate);
}
