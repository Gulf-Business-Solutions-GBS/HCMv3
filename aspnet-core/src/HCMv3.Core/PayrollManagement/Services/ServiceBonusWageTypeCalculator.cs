using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 35 -- Service Bonus.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 1013-1055 (branch) and 4081-4127
/// (<c>GetServiceBonus</c>).
///
/// <para><b>Anniversary-month only.</b> The bonus is paid only when the pay month equals the employee's
/// joining MONTH (legacy line 4110 -- month number only, the year is deliberately not compared).</para>
///
/// <para><b>Completed-years calculation, verbatim.</b> Legacy builds a <c>TimeSpan</c> from joining date to
/// period end and reads years/months off <c>DateTime(1,1,1) + span</c> (legacy lines 4098-4108), then
/// <c>currentYears = Math.Floor(years + months / 12)</c>. That is a peculiar way to count service years and it
/// is reproduced exactly rather than replaced with a calendar-correct difference -- the slab lookup
/// (<c>EmployeeSubGroupBonus.ChangeYear == currentYears</c>) is an EQUALITY match, so any change of formula
/// silently stops paying the bonus.</para>
///
/// <para><b>Base</b> is the sum of <c>Rate</c> (not Amount) over register lines whose wage type has
/// <c>IncludeInServiceBonus</c> (legacy line 4115). No rounding.</para>
///
/// <para><b>Slab rows are NOT date-filtered</b>: legacy line 4112 reads
/// <c>empSubGroup.Bonus.Where(r =&gt; r.ChangeYear == currentYears)</c> and ignores the rows'
/// StartDate/EndDate. Preserved.</para>
///
/// <para><b>PORT GAP.</b> Requires <see cref="IEmployeeSubGroupResolver"/>, which currently throws.</para>
/// </summary>
public class ServiceBonusWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IEmployeeSubGroupResolver _subGroupResolver;
    private readonly IRepository<EmployeeSubGroupBonus> _subGroupBonusRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public ServiceBonusWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IEmployeeSubGroupResolver subGroupResolver,
        IRepository<EmployeeSubGroupBonus> subGroupBonusRepository,
        IRepository<Employee> employeeRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _subGroupResolver = subGroupResolver;
        _subGroupBonusRepository = subGroupBonusRepository;
        _employeeRepository = employeeRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "35";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 1016-1018.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var amount = await GetServiceBonusAsync(context);

        // Legacy line 1023.
        if (amount == 0)
        {
            return;
        }

        await PayrollRegisterLineEditor.SetAmountAsync(
            _registerStore, context.Register, context.WageType, amount);

        // Legacy lines 1044-1050: posted NEGATED.
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, PayrollRunGates.MonthStart(context.PayDate), (-1) * amount,
                context.PayrollRunType, context.OffCycleReason, context.AdvancePayComment, context.Settings);
        }
    }

    /// <summary>Legacy <c>GetServiceBonus</c> (legacy lines 4081-4127).</summary>
    private async Task<double> GetServiceBonusAsync(PayrollCalculationContext context)
    {
        var payDate = context.PayDate;
        var payEndDate = payDate;

        // Legacy lines 4090-4095.
        var subGroupId = await _subGroupResolver.GetEmployeeSubGroupIdAsync(context.EmployeeId, payDate);
        if (subGroupId == null)
        {
            throw new InvalidOperationException("EmployeeSubGroup Not found.");
        }

        var joiningDate = context.Employee != null
            ? context.Employee.JoiningDate
            : _employeeRepository.GetAll().Where(e => e.Id == context.EmployeeId)
                .Select(e => e.JoiningDate).FirstOrDefault();

        // Legacy lines 4098-4108, verbatim.
        var zeroTime = new DateTime(1, 1, 1);
        var span = payEndDate - joiningDate;

        double years = (zeroTime + span).Year - 1;
        double months = (zeroTime + span).Month - 1;

        var currentYears = Math.Floor(years + (months / 12));

        // Legacy line 4110: joining MONTH equals the period-end month.
        if (joiningDate.Month != payEndDate.Month)
        {
            return 0;
        }

        var percentage = _subGroupBonusRepository.GetAll()
            .Where(r => r.EmployeeSubGroupId == subGroupId.Value && r.ChangeYear == currentYears)
            .Select(r => r.Percentage)
            .FirstOrDefault();

        if (percentage == 0)
        {
            return 0;
        }

        // Legacy line 4115.
        var lines = await _registerStore.GetLinesAsync(context.Register);
        var includedIds = WageTypeFlagLookup.WageTypeIdsWith(_wageTypeRepository, w => w.IncludeInServiceBonus);
        var totalServiceBonus = lines.Where(l => includedIds.Contains(l.WageTypeId)).Sum(l => l.Rate);

        return totalServiceBonus * percentage / 100;
    }
}
