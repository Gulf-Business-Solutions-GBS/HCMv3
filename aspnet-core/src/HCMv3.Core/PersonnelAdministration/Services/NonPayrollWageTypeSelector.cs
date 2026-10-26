using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Which wage types a Non Payroll Transaction may use.
/// </summary>
public interface INonPayrollWageTypeSelector
{
    /// <summary>
    /// The wage types a user holding <paramref name="roleIds"/> may pick on a non-payroll transaction.
    /// </summary>
    IQueryable<WageType> Query(IReadOnlyCollection<int> roleIds);
}

/// <inheritdoc cref="INonPayrollWageTypeSelector"/>
/// <remarks>
/// Port of legacy <c>EmployeeNonPayrollTrans.NonPayrollWageType</c> (HCM10.Module, EmployeeNonPayrollTrans.cs 39-75):
/// active Earning or Deduction wage types that are calculated pay and neither basic nor arrear pay, excluding the
/// net-pay wage type 9000 and every wage type already owned by a leave, overtime, loan, ticket or violation type.
/// A secured wage type is offered only to a user holding one of its roles. Legacy had no administrator exception,
/// so neither does this.
/// </remarks>
public class NonPayrollWageTypeSelector : INonPayrollWageTypeSelector, ITransientDependency
{
    private const string NetPayWageTypeCode = "9000";

    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<WageTypeSecurityRole> _wageTypeSecurityRoleRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<LoanType> _loanTypeRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IRepository<DisciplinaryType> _disciplinaryTypeRepository;

    public NonPayrollWageTypeSelector(
        IRepository<WageType> wageTypeRepository,
        IRepository<WageTypeSecurityRole> wageTypeSecurityRoleRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<LoanType> loanTypeRepository,
        IRepository<TicketType> ticketTypeRepository,
        IRepository<DisciplinaryType> disciplinaryTypeRepository)
    {
        _wageTypeRepository = wageTypeRepository;
        _wageTypeSecurityRoleRepository = wageTypeSecurityRoleRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _overtimeTypeRepository = overtimeTypeRepository;
        _loanTypeRepository = loanTypeRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _disciplinaryTypeRepository = disciplinaryTypeRepository;
    }

    public IQueryable<WageType> Query(IReadOnlyCollection<int> roleIds)
    {
        var roles = (roleIds ?? new List<int>()).ToList();

        var owned = _absenceTypeRepository.GetAll().Where(t => t.WageTypeId != null).Select(t => t.WageTypeId.Value)
            .Concat(_overtimeTypeRepository.GetAll().Where(t => t.WageTypeId != null).Select(t => t.WageTypeId.Value))
            .Concat(_loanTypeRepository.GetAll().Where(t => t.WageTypeId != null).Select(t => t.WageTypeId.Value))
            .Concat(_ticketTypeRepository.GetAll().Where(t => t.WageTypeId != null).Select(t => t.WageTypeId.Value))
            .Concat(_disciplinaryTypeRepository.GetAll().Where(t => t.WageTypeId != null).Select(t => t.WageTypeId.Value))
            .ToList();

        var securedForUser = _wageTypeSecurityRoleRepository.GetAll()
            .Where(s => roles.Contains(s.RoleId))
            .Select(s => s.WageTypeId);

        return _wageTypeRepository.GetAll()
            .Where(w => w.Status
                        && (w.BillingType == BillingType.Earning || w.BillingType == BillingType.Deduction)
                        && !w.IncludeInBasicPay
                        && w.IncludeInCalculatedPay
                        && !w.IncludeInArrearPay
                        && !owned.Contains(w.Id)
                        && w.Code != NetPayWageTypeCode
                        && (!w.IsSecured || securedForUser.Contains(w.Id)));
    }
}
