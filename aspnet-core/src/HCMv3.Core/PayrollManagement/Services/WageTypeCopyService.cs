using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Port of legacy <c>ObjectCopyController.GetWageType</c>. Differences from legacy, both deliberate:
/// <list type="bullet">
/// <item><description>Legacy's <c>Max()</c> threw when no custom wage type of that billing type existed yet; here
/// the source wage type's own code is the starting point.</description></item>
/// <item><description>Legacy gave the arrear copy <c>nextFreeCode + 1</c> without checking that code was free;
/// here it gets the next free code.</description></item>
/// </list>
/// </summary>
public class WageTypeCopyService : HCMv3DomainServiceBase, IWageTypeCopyService, ITransientDependency
{
    /// <summary>Net Pay is never a peer when numbering or ordering (legacy <c>r.Code != "9000"</c>).</summary>
    private const string NetPayWageTypeCode = "9000";

    /// <summary>Payroll orders from here up are reserved for Net Pay and loan deductions.</summary>
    private const int ReservedPayrollOrderStart = 50000;

    private const int PayrollOrderStep = 10;

    private const string ArrearSuffix = " Arrear";

    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<WageTypeException> _wageTypeExceptionRepository;
    private readonly IRepository<WageTypeSecurityRole> _wageTypeSecurityRoleRepository;
    private readonly IRepository<LoanType> _loanTypeRepository;

    public WageTypeCopyService(
        IRepository<WageType> wageTypeRepository,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<WageTypeException> wageTypeExceptionRepository,
        IRepository<WageTypeSecurityRole> wageTypeSecurityRoleRepository,
        IRepository<LoanType> loanTypeRepository)
    {
        _wageTypeRepository = wageTypeRepository;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _wageTypeExceptionRepository = wageTypeExceptionRepository;
        _wageTypeSecurityRoleRepository = wageTypeSecurityRoleRepository;
        _loanTypeRepository = loanTypeRepository;
    }

    public async Task<int> CopyAsync(int sourceWageTypeId, string name)
    {
        var source = await _wageTypeRepository.GetAsync(sourceWageTypeId);
        var usedCodes = new HashSet<string>(await _wageTypeRepository.GetAll().Select(w => w.Code).ToListAsync());

        var copy = CloneFields(source, name);
        copy.Code = await NextFreeCodeAsync(source, isArrear: false, usedCodes);
        copy.PayrollOrder = await LastPayrollOrderAsync(source) + PayrollOrderStep;
        copy.ArrearSourceWageTypeId = null;
        await _wageTypeRepository.InsertAsync(copy);
        await CurrentUnitOfWork.SaveChangesAsync();
        await CopyChildRowsAsync(source.Id, copy.Id, copy.TenantId);

        var sourceArrear = await _wageTypeRepository.FirstOrDefaultAsync(w => w.ArrearSourceWageTypeId == source.Id);
        if (sourceArrear != null)
        {
            var arrearCopy = CloneFields(sourceArrear, name + ArrearSuffix);
            arrearCopy.Code = await NextFreeCodeAsync(source, isArrear: true, usedCodes);
            arrearCopy.PayrollOrder = copy.PayrollOrder + PayrollOrderStep;
            arrearCopy.ArrearSourceWageTypeId = copy.Id;
            await _wageTypeRepository.InsertAsync(arrearCopy);
            await CurrentUnitOfWork.SaveChangesAsync();
            await CopyChildRowsAsync(sourceArrear.Id, arrearCopy.Id, arrearCopy.TenantId);
        }

        return copy.Id;
    }

    private static WageType CloneFields(WageType source, string name)
    {
        return new WageType
        {
            TenantId = source.TenantId,
            NameEn = name,
            NameAr = name,
            Status = true,
            BillingType = source.BillingType,
            BillingMethod = source.BillingMethod,
            ProrateMethod = source.ProrateMethod,
            FixedDays = source.FixedDays,
            Rate = source.Rate,
            Category = source.Category,
            IsSecured = source.IsSecured,
            DontImpactNetPay = source.DontImpactNetPay,
            IncludeInAdvancePay = source.IncludeInAdvancePay,
            IncludeInArrearPay = source.IncludeInArrearPay,
            IncludeInBasicPay = source.IncludeInBasicPay,
            IncludeInCalculatedPay = source.IncludeInCalculatedPay,
            IncludeInCutoff = source.IncludeInCutoff,
            IncludeInDisciplinary = source.IncludeInDisciplinary,
            IncludeInFIPosting = source.IncludeInFIPosting,
            IncludeInIndemnity = source.IncludeInIndemnity,
            IncludeInOvertimePay = source.IncludeInOvertimePay,
            IncludeInPaidLeave = source.IncludeInPaidLeave,
            IncludeInVariablePay = source.IncludeInVariablePay,
            IncludeInSocialSecurity = source.IncludeInSocialSecurity,
            IncludeInPartialPay = source.IncludeInPartialPay,
            IncludeInLateEarlyDed = source.IncludeInLateEarlyDed,
            IncludeInServiceBonus = source.IncludeInServiceBonus,
            IncludeInFund = source.IncludeInFund
        };
    }

    /// <summary>
    /// One past the highest numeric code among custom wage types of the source's billing type (arrear or
    /// non-arrear to match), skipping codes already taken. Claims the code in <paramref name="usedCodes"/>.
    /// </summary>
    private async Task<string> NextFreeCodeAsync(WageType source, bool isArrear, HashSet<string> usedCodes)
    {
        var peerCodes = await _wageTypeRepository.GetAll()
            .Where(w => w.BillingType == source.BillingType
                        && w.Code != NetPayWageTypeCode
                        && w.IncludeInArrearPay == isArrear
                        && w.Category == Category.Custom)
            .Select(w => w.Code)
            .ToListAsync();

        var next = peerCodes
            .Append(source.Code)
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .Max() + 1;

        while (usedCodes.Contains(next.ToString()))
        {
            next++;
        }

        var code = next.ToString();
        usedCodes.Add(code);
        return code;
    }

    /// <summary>
    /// Legacy ordering: a copy of a loan deduction goes after the other loan deductions; anything else goes
    /// after the last ordinary wage type of its billing type, below the reserved range.
    /// </summary>
    private async Task<int> LastPayrollOrderAsync(WageType source)
    {
        var loanWageTypeIds = _loanTypeRepository.GetAll()
            .Where(t => t.WageTypeId != null)
            .Select(t => t.WageTypeId.Value);

        var peers = _wageTypeRepository.GetAll()
            .Where(w => w.BillingType == source.BillingType && w.Code != NetPayWageTypeCode);

        var sourceIsLoanDeduction = await loanWageTypeIds.AnyAsync(id => id == source.Id);
        peers = sourceIsLoanDeduction
            ? peers.Where(w => loanWageTypeIds.Contains(w.Id))
            : peers.Where(w => w.PayrollOrder < ReservedPayrollOrderStart);

        return await peers.Select(w => (int?)w.PayrollOrder).MaxAsync() ?? 0;
    }

    private async Task CopyChildRowsAsync(int sourceId, int copyId, int tenantId)
    {
        foreach (var leave in await _wageTypeLeaveRepository.GetAllListAsync(r => r.WageTypeId == sourceId))
        {
            await _wageTypeLeaveRepository.InsertAsync(new WageTypeLeave
            {
                TenantId = tenantId,
                WageTypeId = copyId,
                AbsenceTypeId = leave.AbsenceTypeId
            });
        }

        foreach (var exception in await _wageTypeExceptionRepository.GetAllListAsync(r => r.WageTypeId == sourceId))
        {
            await _wageTypeExceptionRepository.InsertAsync(new WageTypeException
            {
                TenantId = tenantId,
                WageTypeId = copyId,
                ExceptWageTypeId = exception.ExceptWageTypeId
            });
        }

        foreach (var role in await _wageTypeSecurityRoleRepository.GetAllListAsync(r => r.WageTypeId == sourceId))
        {
            await _wageTypeSecurityRoleRepository.InsertAsync(new WageTypeSecurityRole
            {
                TenantId = tenantId,
                WageTypeId = copyId,
                RoleId = role.RoleId
            });
        }
    }
}
