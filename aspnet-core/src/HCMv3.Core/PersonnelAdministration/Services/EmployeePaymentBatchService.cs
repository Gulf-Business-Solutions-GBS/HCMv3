using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeePaymentBatchService"/>
public class EmployeePaymentBatchService : HCMv3DomainServiceBase, IEmployeePaymentBatchService, ITransientDependency
{
    /// <summary>
    /// The one wage type legacy excluded by literal code (<c>PaymentRequest.PaymentWageType</c>:
    /// <c>r.Code != "9000"</c>). Kept as a named constant rather than a magic string so the exclusion is at
    /// least visible; legacy left no comment saying what 9000 is.
    /// </summary>
    public const string ExcludedWageTypeCode = "9000";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<LoanType> _loanTypeRepository;

    public EmployeePaymentBatchService(
        IRepository<Employee> employeeRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<LoanType> loanTypeRepository)
    {
        _employeeRepository = employeeRepository;
        _wageTypeRepository = wageTypeRepository;
        _overtimeTypeRepository = overtimeTypeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _loanTypeRepository = loanTypeRepository;
    }

    /// <inheritdoc/>
    public async Task EnsureLinesArePayableAsync(IReadOnlyCollection<EmployeePaymentBatchDetail> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            throw new UserFriendlyException(L("EmployeePaymentBatchHasNoLines"));
        }

        // Zero first, because it is the cheaper answer and the more common mistake -- a row added to the grid
        // and never filled in. Legacy's rule, finally invoked.
        if (lines.Any(l => l.Amount == 0))
        {
            throw new UserFriendlyException(L("AmountCannotBeZero"));
        }

        var duplicateEmployeeId = lines
            .GroupBy(l => l.EmployeeId)
            .Where(g => g.Count() > 1)
            .Select(g => (int?)g.Key)
            .FirstOrDefault();

        if (duplicateEmployeeId.HasValue)
        {
            // Named, not counted. "An employee appears twice" in a hundred-line batch is not actionable;
            // legacy's message got this right even though the check never ran.
            var employeeName = await Task.FromResult(_employeeRepository.GetAll()
                .Where(e => e.Id == duplicateEmployeeId.Value)
                .Select(e => e.Code + " - " + e.FullNameEn)
                .FirstOrDefault());

            throw new UserFriendlyException(
                L("DuplicateEmployeeInEmployeePaymentBatch", employeeName ?? duplicateEmployeeId.Value.ToString()));
        }
    }

    /// <inheritdoc/>
    public async Task<List<int>> GetSelectableWageTypeIdsAsync()
    {
        // Materialised separately and compared in memory, exactly as legacy did with its three XPQuery id
        // lists. The sets are small (type masters), and a single query with three NOT EXISTS subqueries reads
        // far worse for no measurable gain.
        var claimedWageTypeIds = new HashSet<int>();

        claimedWageTypeIds.UnionWith(_overtimeTypeRepository.GetAll()
            .Where(t => t.WageTypeId.HasValue)
            .Select(t => t.WageTypeId.Value)
            .ToList());

        claimedWageTypeIds.UnionWith(_absenceTypeRepository.GetAll()
            .Where(t => t.WageTypeId.HasValue)
            .Select(t => t.WageTypeId.Value)
            .ToList());

        claimedWageTypeIds.UnionWith(_loanTypeRepository.GetAll()
            .Where(t => t.WageTypeId.HasValue)
            .Select(t => t.WageTypeId.Value)
            .ToList());

        // Legacy's predicate, unchanged and in the same order:
        //   active; an Earning or a Deduction (never a TimeType or a Provision, which are engine-internal);
        //   NOT part of basic pay (basic pay is not something you hand out ad hoc);
        //   IS part of calculated pay (otherwise the payment would never reach a payslip);
        //   NOT an arrear wage type (arrears are generated from their source, not keyed in);
        //   not the reserved code 9000; and not already owned by an overtime, absence or loan type, because
        //   those wage types are written by their own engines and a manual payment would collide with them.
        var selectable = await Task.FromResult(_wageTypeRepository.GetAll()
            .Where(w => w.Status
                        && (w.BillingType == BillingType.Earning || w.BillingType == BillingType.Deduction)
                        && !w.IncludeInBasicPay
                        && w.IncludeInCalculatedPay
                        && !w.IncludeInArrearPay
                        && w.Code != ExcludedWageTypeCode)
            .Select(w => w.Id)
            .ToList());

        return selectable.Where(id => !claimedWageTypeIds.Contains(id)).ToList();
    }
}
