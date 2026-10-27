using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P5. In-memory <see cref="IPayrollRegisterStore"/> for the wage-rule tests.
///
/// <para>Reproduces the two store behaviours the rules actually depend on: the legacy "zero means delete"
/// convention (<see cref="IPayrollRegisterStore.SaveLineAsync"/> remarks) and Id assignment on insert.
/// Register-target handling is deliberately NOT reproduced -- that is
/// <c>PayrollRegisterStore_Tests</c>'s job.</para>
///
/// <para>Kept in its own file rather than added to <c>PayrollEngineFakes</c>: sibling porting waves are
/// editing this folder concurrently and the shared helper is a merge hazard.</para>
/// </summary>
internal sealed class FakePayrollRegisterStore : IPayrollRegisterStore
{
    private int _nextId = 1;

    public List<PayrollRegisterLine> Lines { get; } = new List<PayrollRegisterLine>();

    public Dictionary<int, List<SalaryRegisterDayCount>> DayDetails { get; } = new Dictionary<int, List<SalaryRegisterDayCount>>();

    public List<int> DeletedLineIds { get; } = new List<int>();

    public Task<List<PayrollRegisterLine>> GetLinesAsync(PayrollRegisterRef register)
        => Task.FromResult(Lines.ToList());

    public Task<PayrollRegisterLine> GetLineAsync(PayrollRegisterRef register, string wageCode)
        => Task.FromResult(Lines.FirstOrDefault(l => l.WageCode == wageCode));

    public Task<PayrollRegisterLine> GetLineByWageTypeIdAsync(PayrollRegisterRef register, int wageTypeId)
        => Task.FromResult(Lines.FirstOrDefault(l => l.WageTypeId == wageTypeId));

    public Task<PayrollRegisterLine> SaveLineAsync(PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero = true)
    {
        var existing = Lines.FirstOrDefault(l => l.WageTypeId == line.WageTypeId && l.WageTypeId != 0)
                       ?? Lines.FirstOrDefault(l => l.WageCode == line.WageCode && line.WageCode != null);

        if (deleteWhenAmountIsZero && line.Amount == 0)
        {
            if (existing != null)
            {
                DeletedLineIds.Add(existing.Id);
                Lines.Remove(existing);
            }

            return Task.FromResult<PayrollRegisterLine>(null);
        }

        if (existing == null)
        {
            existing = new PayrollRegisterLine
            {
                Id = _nextId++,
                WageTypeId = line.WageTypeId,
                WageCode = line.WageCode
            };
            Lines.Add(existing);
        }

        existing.Rate = line.Rate;
        existing.Number = line.Number;
        existing.Amount = line.Amount;

        line.Id = existing.Id;
        return Task.FromResult(line);
    }

    public Task DeleteLineAsync(PayrollRegisterRef register, int wageTypeId)
    {
        var existing = Lines.FirstOrDefault(l => l.WageTypeId == wageTypeId);
        if (existing != null)
        {
            DeletedLineIds.Add(existing.Id);
            Lines.Remove(existing);
        }

        return Task.CompletedTask;
    }

    public Task ClearLinesAsync(PayrollRegisterRef register)
    {
        Lines.Clear();
        return Task.CompletedTask;
    }

    public Task SaveDayDetailsAsync(PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days)
    {
        DayDetails[lineId] = days?.ToList() ?? new List<SalaryRegisterDayCount>();
        return Task.CompletedTask;
    }
}
