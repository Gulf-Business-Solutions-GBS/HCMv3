using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IPayrollRegisterStore"/>
/// <remarks>
/// <para><b>Two targets.</b> <see cref="PayrollRegisterTarget.Live"/> reads and writes
/// <c>SalaryRegisterDetail</c>/<c>SalaryRegisterDaysDetail</c>. <see cref="PayrollRegisterTarget.Simulation"/>
/// (wave P7, legacy <c>HCM10.PayrollEngine/SimulationPayrollHelper.cs</c>) reads and writes
/// <c>SimulationSalaryRegisterDetail</c>/<c>SimulationSalaryRegisterDaysDetail</c> through
/// <see cref="ISimulationRegisterRows"/> -- and NEVER falls back to the live tables: a simulation must not touch
/// real payroll results. If the simulation rows service is not available the store refuses loudly rather than
/// write anywhere.</para>
///
/// <para>NOTE: <c>HCMv3.Core</c> has no reference to EntityFrameworkCore (per architecture rules), so queries
/// use the plain synchronous <c>IQueryable</c> from <c>IRepository&lt;T&gt;.GetAll()</c>, exactly like
/// <c>AccrualService</c> and <c>CurrencyService</c>. Inserts/updates/deletes DO use the repositories' genuine
/// async methods.</para>
///
/// <para>TenantId on new child rows is copied from the owning register (the same approach
/// <c>AccrualService</c> uses for <c>EmployeeAbsQuotaForfeit</c>), not read from <c>AbpSession</c> -- a payroll
/// run executes under a background-job identity.</para>
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

    /// <summary>
    /// The simulation tables. Property-injected (ABP/Castle) so the existing hand-built test stores keep
    /// compiling; a simulation call without it is refused (see <see cref="Simulation"/>).
    /// </summary>
    public ISimulationRegisterRows SimulationRows { get; set; }

    public Task<List<PayrollRegisterLine>> GetLinesAsync(PayrollRegisterRef register)
    {
        GuardTarget(register);

        var lines = (IsSimulation(register) ? QuerySimulationLines(register.RegisterId) : QueryLines(register.RegisterId)).ToList();
        return Task.FromResult(lines);
    }

    public Task<PayrollRegisterLine> GetLineAsync(PayrollRegisterRef register, string wageCode)
    {
        GuardTarget(register);

        var query = IsSimulation(register) ? QuerySimulationLines(register.RegisterId) : QueryLines(register.RegisterId);
        var line = query.FirstOrDefault(l => l.WageCode == wageCode);
        return Task.FromResult(line);
    }

    public Task<PayrollRegisterLine> GetLineByWageTypeIdAsync(PayrollRegisterRef register, int wageTypeId)
    {
        GuardTarget(register);

        var query = IsSimulation(register) ? QuerySimulationLines(register.RegisterId) : QueryLines(register.RegisterId);
        var line = query.FirstOrDefault(l => l.WageTypeId == wageTypeId);
        return Task.FromResult(line);
    }

    public async Task<PayrollRegisterLine> SaveLineAsync(PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero = true)
    {
        GuardTarget(register);

        if (line == null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        if (IsSimulation(register))
        {
            return await SaveSimulationLineAsync(register, line, deleteWhenAmountIsZero);
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
        await FlushAsync();

        line.Id = saved;
        line.WageTypeId = wageTypeId;
        line.IsSystemGenerated = true;
        return line;
    }

    public async Task DeleteLineAsync(PayrollRegisterRef register, int wageTypeId)
    {
        GuardTarget(register);

        if (IsSimulation(register))
        {
            var simulationLineId = Simulation.Details
                .Where(d => d.SimulationSalaryRegisterId == register.RegisterId && d.WageTypeId == wageTypeId)
                .Select(d => (int?)d.Id)
                .FirstOrDefault();

            if (simulationLineId != null)
            {
                await Simulation.DeleteDetailWithDaysAsync(simulationLineId.Value);
                await FlushAsync();
            }

            return;
        }

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

        if (IsSimulation(register))
        {
            // Legacy SimulationPayrollHelper lines 920-929 / 1069-1077: day rows first, then the lines.
            var simulationLineIds = Simulation.Details
                .Where(d => d.SimulationSalaryRegisterId == register.RegisterId)
                .Select(d => d.Id)
                .ToList();

            foreach (var simulationLineId in simulationLineIds)
            {
                await Simulation.DeleteDetailWithDaysAsync(simulationLineId);
                await FlushAsync();
            }

            return;
        }

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

        if (IsSimulation(register))
        {
            await SaveSimulationDayDetailsAsync(lineId, days);
            return;
        }

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

        await FlushAsync();
    }

    #region << Simulation target (wave P7) >>

    /// <summary>The simulation tables, or a loud refusal when they are not wired.</summary>
    private ISimulationRegisterRows Simulation =>
        SimulationRows ?? throw new InvalidOperationException(
            "A simulation payroll run needs the simulation register tables (ISimulationRegisterRows), which are "
            + "not available. Nothing was written: a simulation must never fall back to the live registers.");

    private static bool IsSimulation(PayrollRegisterRef register) => register.Target == PayrollRegisterTarget.Simulation;

    /// <summary>Simulation twin of <see cref="QueryLines"/>. <c>IsSystemGenerated</c> has no column there; every simulation line is engine-written.</summary>
    private IQueryable<PayrollRegisterLine> QuerySimulationLines(int registerId)
    {
        return from detail in Simulation.Details
               join wageType in _wageTypeRepository.GetAll() on detail.WageTypeId equals wageType.Id
               where detail.SimulationSalaryRegisterId == registerId
               select new PayrollRegisterLine
               {
                   Id = detail.Id,
                   WageTypeId = detail.WageTypeId,
                   WageCode = wageType.Code,
                   Rate = detail.Rate,
                   Number = detail.Number,
                   Amount = detail.Amount,
                   IsSystemGenerated = true
               };
    }

    /// <summary>Simulation twin of the live <see cref="SaveLineAsync"/> body -- same "zero means delete" rule.</summary>
    private async Task<PayrollRegisterLine> SaveSimulationLineAsync(PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero)
    {
        var wageTypeId = ResolveWageTypeId(line);

        var existingId = Simulation.Details
            .Where(d => d.SimulationSalaryRegisterId == register.RegisterId && d.WageTypeId == wageTypeId)
            .Select(d => (int?)d.Id)
            .FirstOrDefault();

        if (deleteWhenAmountIsZero && line.Amount == 0)
        {
            if (existingId != null)
            {
                await Simulation.DeleteDetailWithDaysAsync(existingId.Value);
                await FlushAsync();
            }

            return null;
        }

        int savedId;

        if (existingId == null)
        {
            var header = Simulation.GetHeaderCopy(register.RegisterId);
            if (header == null)
            {
                throw new InvalidOperationException($"SimulationSalaryRegister {register.RegisterId} not found.");
            }

            savedId = await Simulation.InsertDetailAsync(new SalaryRegisterDetail
            {
                TenantId = header.TenantId,
                SalaryRegisterId = register.RegisterId,
                WageTypeId = wageTypeId,
                Rate = line.Rate,
                Number = line.Number,
                Amount = line.Amount
            });
        }
        else
        {
            savedId = existingId.Value;
            await Simulation.UpdateDetailAsync(new SalaryRegisterDetail
            {
                Id = savedId,
                SalaryRegisterId = register.RegisterId,
                WageTypeId = wageTypeId,
                Rate = line.Rate,
                Number = line.Number,
                Amount = line.Amount
            });
        }

        await FlushAsync();

        line.Id = savedId;
        line.WageTypeId = wageTypeId;
        line.IsSystemGenerated = true;
        return line;
    }

    /// <summary>
    /// Simulation twin of the live <see cref="SaveDayDetailsAsync"/> body. Legacy
    /// <c>SimulationPayrollHelper.AddSalaryRegisterDaysDetail_Simu</c> (line 4602).
    /// </summary>
    private async Task SaveSimulationDayDetailsAsync(int lineId, IEnumerable<SalaryRegisterDayCount> days)
    {
        var detail = Simulation.GetDetailCopy(lineId);
        if (detail == null)
        {
            throw new InvalidOperationException($"SimulationSalaryRegisterDetail {lineId} not found.");
        }

        await Simulation.DeleteDayDetailsOfAsync(lineId);

        if (days == null)
        {
            return;
        }

        foreach (var day in days)
        {
            await Simulation.InsertDayDetailAsync(new SalaryRegisterDaysDetail
            {
                TenantId = detail.TenantId,
                SalaryRegisterDetailId = lineId,
                Period = day.Period,
                Rate = day.Rate,
                Number = day.Number,
                Amount = day.Amount
            });
        }

        await FlushAsync();
    }

    #endregion

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
        await FlushAsync();
    }

    /// <summary>
    /// Writes pending changes before the next read. Every read here is a database query, and an UPDATE of a line
    /// that already exists is otherwise held until the unit of work ends -- so on a re-run, a line worked out from
    /// another one (4022 from 4010, the leave provisions from 4010/4022, ...) read the PREVIOUS run's value.
    /// Inserts were unaffected (getting the id saves them), which is why a first run was always right.
    /// </summary>
    private async Task FlushAsync()
    {
        var unitOfWork = UnitOfWorkManager?.Current;
        if (unitOfWork != null)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }

    private static void GuardTarget(PayrollRegisterRef register)
    {
        if (register == null)
        {
            throw new ArgumentNullException(nameof(register));
        }

        if (register.Target != PayrollRegisterTarget.Live && register.Target != PayrollRegisterTarget.Simulation)
        {
            throw new NotSupportedException($"Unknown PayrollRegisterTarget {register.Target}.");
        }
    }
}
