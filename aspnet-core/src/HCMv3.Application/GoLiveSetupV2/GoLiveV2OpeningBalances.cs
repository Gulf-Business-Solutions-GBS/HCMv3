using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.TimeManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// The annual leave each employee already has at go-live, kept as the time transfer the engines already read:
/// type "1", Annual Leave Opening Balance.
///
/// <para><b>Why its date matters.</b> The time engine adds a transfer's days to the annual leave accrued on the
/// transfer's own date, so a transfer dated inside the payroll month lands in that month's accrual (4010),
/// closing balance (4022) and leave provision (3001, 3004). Dated before the month it would count nowhere.</para>
///
/// <para>Load Employee Data comes before the payroll month is chosen, so a load dates the transfer as best it
/// can -- the month the payroll is set to, or this month -- and Run payroll moves it onto the first day of the
/// month it runs, for every employee not yet paid for an earlier month. A run of the same month again leaves
/// it where it is.</para>
///
/// <para>Only the transfer this setup made is touched, found by its comment: one entered on the Time Transfer
/// screen stays as it was.</para>
/// </summary>
public class GoLiveV2OpeningBalances : ITransientDependency
{
    /// <summary>Marks the transfer as the one Load Employee Data keeps. Also what the Time Transfer screen shows.</summary>
    public const string Comment = "Go-Live Setup V2: annual leave opening balance";

    public const string TransferTypeCode = "1";

    private readonly IRepository<TimeTransfer> _transfers;
    private readonly IRepository<TimeTransferType> _transferTypes;
    private readonly IRepository<PayrollControlRecord> _controlRecords;
    private readonly IRepository<SalaryRegister> _registers;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public GoLiveV2OpeningBalances(
        IRepository<TimeTransfer> transfers,
        IRepository<TimeTransferType> transferTypes,
        IRepository<PayrollControlRecord> controlRecords,
        IRepository<SalaryRegister> registers,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _transfers = transfers;
        _transferTypes = transferTypes;
        _controlRecords = controlRecords;
        _registers = registers;
        _payrollAreaResolver = payrollAreaResolver;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<int?> TransferTypeIdAsync() =>
        await _transferTypes.GetAll()
            .Where(t => t.Code == TransferTypeCode)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

    /// <summary>The employee's opening balance transfer, if this setup made one.</summary>
    public Task<TimeTransfer> FindAsync(int employeeId) =>
        _transfers.GetAll()
            .Where(t => t.EmployeeId == employeeId && t.Comments == Comment)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync();

    /// <summary>
    /// The date a load gives a new opening balance: the first day of the month the payroll area's control
    /// record is on, or of this month when it has none yet. Run payroll corrects it to the month it runs.
    /// </summary>
    public async Task<DateTime> LoadDateAsync(int payrollAreaId)
    {
        var monthEnd = await _controlRecords.GetAll()
            .Where(r => r.PayrollAreaId == payrollAreaId)
            .Select(r => (DateTime?)r.CalendarMonthEnd)
            .FirstOrDefaultAsync();

        var inMonth = monthEnd ?? Clock.Now;
        return new DateTime(inMonth.Year, inMonth.Month, 1);
    }

    /// <summary>
    /// Moves the opening balances of everyone in the payroll area who has not been paid for an earlier month onto
    /// the first day of the month being run. Returns how many moved.
    ///
    /// <para>Saved on its own at once: the engines run their batches in units of work of their own, which would
    /// not see a date still waiting on this request to finish.</para>
    /// </summary>
    public async Task<int> DateForRunAsync(string payrollAreaName, DateTime payInDate)
    {
        var monthStart = new DateTime(payInDate.Year, payInDate.Month, 1);
        using var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);

        var ids = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName, payInDate);
        if (ids.Count == 0)
        {
            await uow.CompleteAsync();
            return 0;
        }

        var paidBefore = await _registers.GetAll()
            .Where(r => ids.Contains(r.EmployeeId)
                        && r.PayrollRunType == PayrollRunType.Regular
                        && r.InPeriod < monthStart)
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync();

        var toMove = await _transfers.GetAll()
            .Where(t => ids.Contains(t.EmployeeId)
                        && !paidBefore.Contains(t.EmployeeId)
                        && t.Comments == Comment
                        && t.TransferDate != monthStart)
            .ToListAsync();

        foreach (var transfer in toMove)
        {
            transfer.TransferDate = monthStart;
        }

        await uow.CompleteAsync();
        return toMove.Count;
    }
}
