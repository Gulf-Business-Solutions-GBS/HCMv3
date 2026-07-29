using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IPayrollRegisterStore"/>
/// <remarks>
/// Wave P1 implements <see cref="PayrollRegisterTarget.Live"/> only. Every method throws a loud
/// <see cref="NotImplementedException"/> for <see cref="PayrollRegisterTarget.Simulation"/> naming the exact
/// legacy file that must be folded in (wave P7) -- deliberately NOT falling back to the live tables, which
/// would silently corrupt real payroll data during a simulation run.
///
/// <para>NOTE: <c>HCMv3.Core</c> has no reference to EntityFrameworkCore (per architecture rules), so queries
/// use the plain synchronous <c>IQueryable</c> from <c>IRepository&lt;T&gt;.GetAll()</c>, exactly like
/// <c>AccrualService</c> and <c>CurrencyService</c>. Inserts/updates/deletes DO use the repositories' genuine
/// async methods.</para>
///
/// <para>TenantId on new child rows is copied from the owning <c>SalaryRegister</c> (the same approach
/// <c>AccrualService</c> uses for <c>EmployeeAbsQuotaForfeit</c>), not read from <c>AbpSession</c> -- a payroll
/// run may legitimately execute under a background-job identity in a later wave.</para>
/// </remarks>
[UnitOfWork]
public class PayrollRegisterStore : HCMv3DomainServiceBase, IPayrollRegisterStore
{
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<SalaryRegisterDaysDetail> _salaryRegisterDaysDetailRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public PayrollRegisterStore(
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<SalaryRegisterDaysDetail> salaryRegisterDaysDetailRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _salaryRegisterDaysDetailRepository = salaryRegisterDaysDetailRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    public Task<List<PayrollRegisterLine>> GetLinesAsync(PayrollRegisterRef register)
    {
        GuardTarget(register);

        var lines = QueryLines(register.RegisterId).ToList();
        return Task.FromResult(lines);
    }

    public Task<PayrollRegisterLine> GetLineAsync(PayrollRegisterRef register, string wageCode)
    {
        GuardTarget(register);

        var line = QueryLines(register.RegisterId).FirstOrDefault(l => l.WageCode == wageCode);
        return Task.FromResult(line);
    }

    public Task<PayrollRegisterLine> GetLineByWageTypeIdAsync(PayrollRegisterRef register, int wageTypeId)
    {
        GuardTarget(register);

        var line = QueryLines(register.RegisterId).FirstOrDefault(l => l.WageTypeId == wageTypeId);
        return Task.FromResult(line);
    }

    public async Task<PayrollRegisterLine> SaveLineAsync(PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero = true)
    {
        GuardTarget(register);

        if (line == null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        var wageTypeId = ResolveWageTypeId(line);

        var existing = _salaryRegisterDetailRepository.GetAll()
            .FirstOrDefault(d => d.SalaryRegisterId == register.RegisterId && d.WageTypeId == wageTypeId);

        // Legacy "zero means delete" -- see IPayrollRegisterStore.SaveLineAsync remarks.
        if (deleteWhenAmountIsZero && line.Amount == 0)
        {
            if (existing != null)
            {
                await DeleteDetailWithDaysAsync(existing.Id);
            }

            return null;
        }

        if (existing == null)
        {
            var salaryRegister = _salaryRegisterRepository.GetAll().FirstOrDefault(r => r.Id == register.RegisterId);
            if (salaryRegister == null)
            {
                throw new InvalidOperationException($"SalaryRegister {register.RegisterId} not found.");
            }

            existing = new SalaryRegisterDetail
            {
                TenantId = salaryRegister.TenantId,
                SalaryRegisterId = register.RegisterId,
                WageTypeId = wageTypeId
            };
        }

        existing.Rate = line.Rate;
        existing.Number = line.Number;
        existing.Amount = line.Amount;

        // Legacy stamps every engine-written register line (e.g. Program.SetRoundoff:
        // `rec.IsSystemGenerated = true; rec.Save();`). This store is only ever reached from the engine.
        existing.IsSystemGenerated = true;

        var saved = await _salaryRegisterDetailRepository.InsertOrUpdateAndGetIdAsync(existing);

        line.Id = saved;
        line.WageTypeId = wageTypeId;
        line.IsSystemGenerated = true;
        return line;
    }

    public async Task DeleteLineAsync(PayrollRegisterRef register, int wageTypeId)
    {
        GuardTarget(register);

        var existing = _salaryRegisterDetailRepository.GetAll()
            .FirstOrDefault(d => d.SalaryRegisterId == register.RegisterId && d.WageTypeId == wageTypeId);

        if (existing != null)
        {
            await DeleteDetailWithDaysAsync(existing.Id);
        }
    }

    public async Task ClearLinesAsync(PayrollRegisterRef register)
    {
        GuardTarget(register);

        var detailIds = _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegisterId == register.RegisterId)
            .Select(d => d.Id)
            .ToList();

        foreach (var detailId in detailIds)
        {
            await DeleteDetailWithDaysAsync(detailId);
        }
    }

    public async Task SaveDayDetailsAsync(PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days)
    {
        GuardTarget(register);

        var detail = _salaryRegisterDetailRepository.GetAll().FirstOrDefault(d => d.Id == lineId);
        if (detail == null)
        {
            throw new InvalidOperationException($"SalaryRegisterDetail {lineId} not found.");
        }

        var existingIds = _salaryRegisterDaysDetailRepository.GetAll()
            .Where(d => d.SalaryRegisterDetailId == lineId)
            .Select(d => d.Id)
            .ToList();

        foreach (var id in existingIds)
        {
            await _salaryRegisterDaysDetailRepository.DeleteAsync(id);
        }

        if (days == null)
        {
            return;
        }

        foreach (var day in days)
        {
            await _salaryRegisterDaysDetailRepository.InsertAsync(new SalaryRegisterDaysDetail
            {
                TenantId = detail.TenantId,
                SalaryRegisterDetailId = lineId,
                Period = day.Period,
                Rate = day.Rate,
                Number = day.Number,
                Amount = day.Amount
            });
        }
    }

    private IQueryable<PayrollRegisterLine> QueryLines(int registerId)
    {
        return from detail in _salaryRegisterDetailRepository.GetAll()
               join wageType in _wageTypeRepository.GetAll() on detail.WageTypeId equals wageType.Id
               where detail.SalaryRegisterId == registerId
               select new PayrollRegisterLine
               {
                   Id = detail.Id,
                   WageTypeId = detail.WageTypeId,
                   WageCode = wageType.Code,
                   Rate = detail.Rate,
                   Number = detail.Number,
                   Amount = detail.Amount,
                   IsSystemGenerated = detail.IsSystemGenerated
               };
    }

    private int ResolveWageTypeId(PayrollRegisterLine line)
    {
        if (line.WageTypeId != 0)
        {
            return line.WageTypeId;
        }

        if (string.IsNullOrEmpty(line.WageCode))
        {
            throw new ArgumentException("PayrollRegisterLine must carry either WageTypeId or WageCode.", nameof(line));
        }

        var wageType = _wageTypeRepository.GetAll().FirstOrDefault(w => w.Code == line.WageCode);
        if (wageType == null)
        {
            // Legacy Program.CalculateTimeType, line 2728: "WageType {code} : {message} not found".
            throw new InvalidOperationException($"WageType {line.WageCode} not found");
        }

        return wageType.Id;
    }

    private async Task DeleteDetailWithDaysAsync(int detailId)
    {
        var dayIds = _salaryRegisterDaysDetailRepository.GetAll()
            .Where(d => d.SalaryRegisterDetailId == detailId)
            .Select(d => d.Id)
            .ToList();

        foreach (var dayId in dayIds)
        {
            await _salaryRegisterDaysDetailRepository.DeleteAsync(dayId);
        }

        await _salaryRegisterDetailRepository.DeleteAsync(detailId);
    }

    private static void GuardTarget(PayrollRegisterRef register)
    {
        if (register == null)
        {
            throw new ArgumentNullException(nameof(register));
        }

        if (register.Target != PayrollRegisterTarget.Live)
        {
            throw new NotImplementedException(
                "PayrollRegisterTarget.Simulation is not implemented in wave P1. Wave P7 folds legacy "
                + "HCM10.PayrollEngine/SimulationPayrollHelper.cs into this store by adding a branch here that "
                + "writes SimulationSalaryRegisterDetail/SimulationSalaryRegisterDaysDetail instead of "
                + "SalaryRegisterDetail/SalaryRegisterDaysDetail. Falling back to the live tables is NOT an "
                + "option -- a simulation run must never touch real payroll results.");
        }
    }
}
