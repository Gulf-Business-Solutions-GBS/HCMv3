using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 5 -- Social Security Allowance.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 527-568 (branch) and 3211-3252
/// (<c>GetSocialSecurityRulePay</c>).
/// </summary>
public class SocialSecurityAllowanceWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<EmployeeSocialSecurity> _employeeSocialSecurityRepository;
    private readonly IRepository<SocialSecurity> _socialSecurityRepository;
    private readonly IRepository<SocialSecurityRule> _socialSecurityRuleRepository;
    private readonly IRepository<EmployeeDependant> _employeeDependantRepository;

    public SocialSecurityAllowanceWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeSocialSecurity> employeeSocialSecurityRepository,
        IRepository<SocialSecurity> socialSecurityRepository,
        IRepository<SocialSecurityRule> socialSecurityRuleRepository,
        IRepository<EmployeeDependant> employeeDependantRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _employeeSocialSecurityRepository = employeeSocialSecurityRepository;
        _socialSecurityRepository = socialSecurityRepository;
        _socialSecurityRuleRepository = socialSecurityRuleRepository;
        _employeeDependantRepository = employeeDependantRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "5";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 530-532.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var amount = GetSocialSecurityRulePay(context);

        // Legacy line 538.
        if (amount == 0)
        {
            return;
        }

        await PayrollRegisterLineEditor.SetAmountAsync(
            _registerStore, context.Register, context.WageType, amount);

        // Legacy lines 559-563. NOTE the sign: wage code 5 posts the amount POSITIVE, unlike codes
        // 11/13/23/25/27/29/31/33/35 which post (-1) * amount. Preserved exactly.
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId,
                PayrollRunGates.MonthStart(context.PayDate),
                context.OffCyclePaymentDate,
                amount,
                context.PayrollRunType,
                context.OffCycleReason,
                context.AdvancePayComment,
                context.Settings);
        }
    }

    /// <summary>Legacy <c>GetSocialSecurityRulePay</c> (legacy lines 3211-3252).</summary>
    private double GetSocialSecurityRulePay(PayrollCalculationContext context)
    {
        var payDate = context.PayDate;

        // Legacy lines 3221-3223.
        var hasEmployeeSocialSecurity = _employeeSocialSecurityRepository.GetAll()
            .Any(r => r.EmployeeId == context.EmployeeId
                      && r.StartDate <= payDate
                      && r.EndDate >= payDate
                      && r.SocialSecurityContrib);

        if (!hasEmployeeSocialSecurity)
        {
            return 0;
        }

        // Legacy lines 3227-3230.
        var socialSecurity = _socialSecurityRepository.GetAll()
            .FirstOrDefault(r => r.StartDate <= payDate
                                 && r.EndDate >= payDate
                                 && r.NationalityId == context.Employee.NationalityId);

        if (socialSecurity == null)
        {
            return 0;
        }

        // Legacy line 3233: emp.Dependant.Count() -- ALL dependants, unfiltered.
        var dependantCount = _employeeDependantRepository.GetAll()
            .Count(r => r.EmployeeId == context.EmployeeId);

        var qualificationId = context.Employee.QualificationId;
        var maritalStatusId = context.Employee.MaritalStatusId;
        var gender = context.Employee.Gender;

        // Legacy lines 3234-3240. Legacy compares the navigation objects; null-vs-null therefore matched,
        // which the nullable-id comparisons below reproduce.
        var rule = _socialSecurityRuleRepository.GetAll()
            .Where(r => r.SocialSecurityId == socialSecurity.Id
                        && r.StartDate <= payDate
                        && r.EndDate >= payDate
                        && r.QualificationId == qualificationId
                        && r.Gender == gender
                        && r.MaritalStatusId == maritalStatusId
                        && r.Dependant <= dependantCount)
            .OrderByDescending(r => r.Dependant)
            .FirstOrDefault();

        return rule != null ? rule.Amount : 0;
    }
}
