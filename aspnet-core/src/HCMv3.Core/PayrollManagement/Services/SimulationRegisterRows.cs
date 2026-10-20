using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The one place that reads and writes the three SIMULATION register tables
/// (<see cref="SimulationSalaryRegister"/>, <see cref="SimulationSalaryRegisterDetail"/>,
/// <see cref="SimulationSalaryRegisterDaysDetail"/>) for a payroll run.
///
/// <para><b>Copies in the live shape.</b> The services the engine reaches were written against
/// <see cref="SalaryRegister"/>/<see cref="SalaryRegisterDetail"/>/<see cref="SalaryRegisterDaysDetail"/>. For a
/// simulation run they get an UNTRACKED copy of the simulation row in that shape (<see cref="GetHeaderCopy"/>,
/// <see cref="GetDetailCopy"/>, <see cref="GetDayDetailCopies"/>) whose ids are the simulation ids, change it as
/// they always have, and hand it back through the matching write here, which copies the values onto the
/// simulation row. Nothing is ever written to a live register table this way: the copies are not tracked, and
/// every write lands on a simulation entity.</para>
///
/// <para><b>When it saves.</b> Exactly when the live repository calls it replaces do: an update or a delete is
/// only tracked (the caller -- <see cref="PayrollRegisterStore"/> -- saves where its live branch saves), an insert
/// whose id is needed is saved to get the id. So a simulation reads back, at every point, what a live run reads
/// back there, and its figures cannot drift from the live run's for a reason as dull as when a row was
/// saved. Only the clearing of earlier simulations at the start of a run saves at once.</para>
///
/// <para>Legacy: <c>HCM10.PayrollEngine/SimulationPayrollHelper.cs</c> and the <c>*_Simu</c> methods of the
/// category services, which create <c>SimulationSalaryRegister</c>/<c>SimulationSalaryRegisterDetail</c>/
/// <c>SimulationSalaryRegisterDaysDetail</c> objects where <c>Program</c> creates the live ones.</para>
/// </summary>
public interface ISimulationRegisterRows : IDomainService
{
    IQueryable<SimulationSalaryRegister> Headers { get; }

    IQueryable<SimulationSalaryRegisterDetail> Details { get; }

    IQueryable<SimulationSalaryRegisterDaysDetail> DayDetails { get; }

    /// <summary>An untracked <see cref="SalaryRegister"/>-shaped copy of a simulation header, or null.</summary>
    SalaryRegister GetHeaderCopy(int simulationRegisterId);

    /// <summary>Untracked copies of the simulation headers that match <paramref name="predicate"/>.</summary>
    List<SalaryRegister> GetHeaderCopies(Func<IQueryable<SimulationSalaryRegister>, IQueryable<SimulationSalaryRegister>> predicate);

    Task<int> InsertHeaderAsync(SimulationSalaryRegister header);

    Task UpdateHeaderAsync(SimulationSalaryRegister header);

    Task DeleteHeaderWithLinesAsync(int simulationRegisterId);

    /// <summary>
    /// Legacy <c>SimulationPayrollHelper.DeleteAllPreviousSimulationResiters</c>: every simulation register of
    /// each employee -- any period, any run type -- with all its lines and day rows.
    /// </summary>
    Task<int> DeleteAllForEmployeesAsync(IReadOnlyCollection<int> employeeIds);

    /// <summary>An untracked <see cref="SalaryRegisterDetail"/>-shaped copy of a simulation line, or null.</summary>
    SalaryRegisterDetail GetDetailCopy(int simulationDetailId);

    /// <summary>Writes Rate/Number/Amount of <paramref name="copy"/> onto the simulation line with its id.</summary>
    Task UpdateDetailAsync(SalaryRegisterDetail copy);

    /// <summary>Inserts a simulation line from <paramref name="copy"/> (its <c>SalaryRegisterId</c> is the simulation register id) and returns the new id.</summary>
    Task<int> InsertDetailAsync(SalaryRegisterDetail copy);

    Task DeleteDetailWithDaysAsync(int simulationDetailId);

    /// <summary>Deletes every day row of one simulation line; returns how many.</summary>
    Task<int> DeleteDayDetailsOfAsync(int simulationDetailId);

    Task InsertDayDetailAsync(SalaryRegisterDaysDetail copy);

    List<SalaryRegisterDaysDetail> GetDayDetailCopies(int simulationDetailId);

    Task UpdateDayDetailAsync(SalaryRegisterDaysDetail copy);
}

/// <inheritdoc cref="ISimulationRegisterRows"/>
[UnitOfWork]
public class SimulationRegisterRows : HCMv3DomainServiceBase, ISimulationRegisterRows
{
    private readonly IRepository<SimulationSalaryRegister> _headerRepository;
    private readonly IRepository<SimulationSalaryRegisterDetail> _detailRepository;
    private readonly IRepository<SimulationSalaryRegisterDaysDetail> _dayDetailRepository;

    public SimulationRegisterRows(
        IRepository<SimulationSalaryRegister> headerRepository,
        IRepository<SimulationSalaryRegisterDetail> detailRepository,
        IRepository<SimulationSalaryRegisterDaysDetail> dayDetailRepository)
    {
        _headerRepository = headerRepository;
        _detailRepository = detailRepository;
        _dayDetailRepository = dayDetailRepository;
    }

    public IQueryable<SimulationSalaryRegister> Headers => _headerRepository.GetAll();

    public IQueryable<SimulationSalaryRegisterDetail> Details => _detailRepository.GetAll();

    public IQueryable<SimulationSalaryRegisterDaysDetail> DayDetails => _dayDetailRepository.GetAll();

    public SalaryRegister GetHeaderCopy(int simulationRegisterId)
    {
        var header = _headerRepository.GetAll().FirstOrDefault(r => r.Id == simulationRegisterId);
        return header == null ? null : ToCopy(header);
    }

    public List<SalaryRegister> GetHeaderCopies(Func<IQueryable<SimulationSalaryRegister>, IQueryable<SimulationSalaryRegister>> predicate)
    {
        return predicate(_headerRepository.GetAll()).ToList().Select(ToCopy).ToList();
    }

    public async Task<int> InsertHeaderAsync(SimulationSalaryRegister header)
    {
        // InsertAndGetIdAsync: the id is the FK of every line written next, and ABP assigns it on flush only.
        return await _headerRepository.InsertAndGetIdAsync(header);
    }

    public async Task UpdateHeaderAsync(SimulationSalaryRegister header)
    {
        await _headerRepository.UpdateAsync(header);
    }

    public async Task DeleteHeaderWithLinesAsync(int simulationRegisterId)
    {
        var detailIds = _detailRepository.GetAll()
            .Where(d => d.SimulationSalaryRegisterId == simulationRegisterId)
            .Select(d => d.Id)
            .ToList();

        foreach (var detailId in detailIds)
        {
            await DeleteDetailWithDaysAsync(detailId);
        }

        await _headerRepository.DeleteAsync(simulationRegisterId);
        await FlushAsync();
    }

    public async Task<int> DeleteAllForEmployeesAsync(IReadOnlyCollection<int> employeeIds)
    {
        if (employeeIds == null || employeeIds.Count == 0)
        {
            return 0;
        }

        var headerIds = _headerRepository.GetAll()
            .Where(r => employeeIds.Contains(r.EmployeeId))
            .Select(r => r.Id)
            .ToList();

        foreach (var headerId in headerIds)
        {
            await DeleteHeaderWithLinesAsync(headerId);
        }

        return headerIds.Count;
    }

    public SalaryRegisterDetail GetDetailCopy(int simulationDetailId)
    {
        var detail = _detailRepository.GetAll().FirstOrDefault(d => d.Id == simulationDetailId);
        if (detail == null)
        {
            return null;
        }

        return new SalaryRegisterDetail
        {
            Id = detail.Id,
            TenantId = detail.TenantId,
            SalaryRegisterId = detail.SimulationSalaryRegisterId,
            WageTypeId = detail.WageTypeId,
            Rate = detail.Rate,
            Number = detail.Number,
            Amount = detail.Amount,
            IsSystemGenerated = true
        };
    }

    public async Task UpdateDetailAsync(SalaryRegisterDetail copy)
    {
        var detail = _detailRepository.GetAll().FirstOrDefault(d => d.Id == copy.Id);
        if (detail == null)
        {
            throw new InvalidOperationException($"SimulationSalaryRegisterDetail {copy.Id} not found.");
        }

        detail.Rate = copy.Rate;
        detail.Number = copy.Number;
        detail.Amount = copy.Amount;
        await _detailRepository.UpdateAsync(detail);
    }

    public async Task<int> InsertDetailAsync(SalaryRegisterDetail copy)
    {
        var tenantId = copy.TenantId != 0
            ? copy.TenantId
            : _headerRepository.GetAll().Where(r => r.Id == copy.SalaryRegisterId).Select(r => r.TenantId).FirstOrDefault();

        var id = await _detailRepository.InsertAndGetIdAsync(new SimulationSalaryRegisterDetail
        {
            TenantId = tenantId,
            SimulationSalaryRegisterId = copy.SalaryRegisterId,
            WageTypeId = copy.WageTypeId,
            Rate = copy.Rate,
            Number = copy.Number,
            Amount = copy.Amount
        });

        copy.Id = id;
        return id;
    }

    public async Task DeleteDetailWithDaysAsync(int simulationDetailId)
    {
        await DeleteDayDetailsOfAsync(simulationDetailId);
        await _detailRepository.DeleteAsync(simulationDetailId);
    }

    public async Task<int> DeleteDayDetailsOfAsync(int simulationDetailId)
    {
        var dayIds = _dayDetailRepository.GetAll()
            .Where(d => d.SimulationSalaryRegisterDetailId == simulationDetailId)
            .Select(d => d.Id)
            .ToList();

        foreach (var dayId in dayIds)
        {
            await _dayDetailRepository.DeleteAsync(dayId);
        }

        return dayIds.Count;
    }

    public async Task InsertDayDetailAsync(SalaryRegisterDaysDetail copy)
    {
        var tenantId = copy.TenantId != 0
            ? copy.TenantId
            : _detailRepository.GetAll().Where(d => d.Id == copy.SalaryRegisterDetailId).Select(d => d.TenantId).FirstOrDefault();

        await _dayDetailRepository.InsertAsync(new SimulationSalaryRegisterDaysDetail
        {
            TenantId = tenantId,
            SimulationSalaryRegisterDetailId = copy.SalaryRegisterDetailId,
            Period = copy.Period,
            Rate = copy.Rate,
            Number = copy.Number,
            Amount = copy.Amount
        });
    }

    public List<SalaryRegisterDaysDetail> GetDayDetailCopies(int simulationDetailId)
    {
        return _dayDetailRepository.GetAll()
            .Where(d => d.SimulationSalaryRegisterDetailId == simulationDetailId)
            .ToList()
            .Select(d => new SalaryRegisterDaysDetail
            {
                Id = d.Id,
                TenantId = d.TenantId,
                SalaryRegisterDetailId = d.SimulationSalaryRegisterDetailId,
                Period = d.Period,
                Rate = d.Rate,
                Number = d.Number,
                Amount = d.Amount
            })
            .ToList();
    }

    public async Task UpdateDayDetailAsync(SalaryRegisterDaysDetail copy)
    {
        var day = _dayDetailRepository.GetAll().FirstOrDefault(d => d.Id == copy.Id);
        if (day == null)
        {
            return;
        }

        day.Rate = copy.Rate;
        day.Number = copy.Number;
        day.Amount = copy.Amount;
        await _dayDetailRepository.UpdateAsync(day);
    }

    private static SalaryRegister ToCopy(SimulationSalaryRegister header)
    {
        return new SalaryRegister
        {
            Id = header.Id,
            TenantId = header.TenantId,
            EmployeeId = header.EmployeeId,
            ForPeriod = header.ForPeriod,
            InPeriod = header.InPeriod,
            PayrollRunType = header.PayrollRunType,
            PayrollResultType = header.PayrollResultType,
            PayrollReasonTypeId = header.PayrollReasonTypeId
        };
    }

    /// <summary>Saves the clearing of earlier simulations (see the interface remarks on when this class saves).</summary>
    private async Task FlushAsync()
    {
        var unitOfWork = UnitOfWorkManager?.Current;
        if (unitOfWork != null)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }
}
