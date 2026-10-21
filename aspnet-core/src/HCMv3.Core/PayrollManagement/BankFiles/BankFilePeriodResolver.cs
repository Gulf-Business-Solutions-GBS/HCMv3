using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Timing;
using Abp.UI;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement.BankFiles;

/// <summary>The period a bank file is made for: the payroll month it names and the calendar dates it covers.</summary>
public class BankFilePeriod
{
    public BankFilePeriodType PeriodType { get; set; }

    /// <summary>Payroll month (legacy <c>txtMonth</c>) -- NOT necessarily the calendar month of <see cref="EndDate"/>.</summary>
    public int PayrollMonth { get; set; }

    /// <summary>Payroll year (legacy <c>txtyear</c>).</summary>
    public int PayrollYear { get; set; }

    /// <summary>Legacy <c>txtStartDate</c>. Date only.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Legacy <c>txtEndDate</c> -- the date every register, bank and sponsor lookup is made on. Date only.</summary>
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Works out the Bank File screen's start and end dates on the SERVER, from the payroll area's own records,
/// instead of trusting dates a browser sends.
///
/// <para><b>Why on the server.</b> A date picked in a browser in Kuwait travels as local midnight with a
/// +03:00 offset; a server whose clock runs on UTC reads it as 21:00 the DAY BEFORE, and the bank file then
/// looks for registers dated the 30th of a month that ends on the 31st -- and finds none. Legacy never had the
/// problem because its text boxes were filled and read on the server. Resolving the period here, from the
/// payroll month and year, keeps it that way.</para>
///
/// <para><b>Current</b> = legacy <c>SetCurrentDates</c> (<c>uscBankFile.ascx.cs:2551-2564</c>): the payroll
/// area's control record supplies the payroll month, year, start and end, exactly as stored (a period running
/// 26th to 25th stays so). Without a payroll area legacy left the boxes blank and the generate then failed;
/// here that is a plain message.</para>
///
/// <para><b>Other</b> = legacy <c>SetDates</c> (<c>:2528-2550</c>): the month and year typed (today's when
/// blank) go through the payroll area's payroll period -- legacy
/// <c>PayrollControlRecord.GetStartEndDateFromPayrollPeriod</c>, which HCMv3 already ports as
/// <see cref="PayrollPeriodShiftCalculator"/> -- so a fiscal period such as Apr-Mar maps payroll month 1 to
/// April. Without a payroll area it is simply that calendar month.</para>
/// </summary>
public interface IBankFilePeriodResolver : IDomainService
{
    /// <summary>
    /// Throws <see cref="UserFriendlyException"/> when no period can be worked out: Current with no payroll
    /// area or no control record, or an unknown payroll period code / impossible month.
    /// </summary>
    Task<BankFilePeriod> ResolveAsync(BankFilePeriodType periodType, int? payrollAreaId, int? payrollMonth, int? payrollYear);
}

/// <inheritdoc cref="IBankFilePeriodResolver"/>
public class BankFilePeriodResolver : HCMv3DomainServiceBase, IBankFilePeriodResolver
{
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;

    public BankFilePeriodResolver(
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<PayrollArea> payrollAreaRepository)
    {
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollAreaRepository = payrollAreaRepository;
    }

    public async Task<BankFilePeriod> ResolveAsync(
        BankFilePeriodType periodType, int? payrollAreaId, int? payrollMonth, int? payrollYear)
    {
        return periodType == BankFilePeriodType.Current
            ? await ResolveCurrentAsync(payrollAreaId)
            : await ResolveOtherAsync(payrollAreaId, payrollMonth, payrollYear);
    }

    private async Task<BankFilePeriod> ResolveCurrentAsync(int? payrollAreaId)
    {
        if (!payrollAreaId.HasValue)
        {
            throw new UserFriendlyException(L("PayrollRunPleaseSelectPayrollArea"));
        }

        // Legacy: PayrollControlRecord.Where(r => r.PayrollArea.Name == cmbPayrollArea.Text).FirstOrDefault().
        var record = await _payrollControlRecordRepository.GetAll()
            .Where(r => r.PayrollAreaId == payrollAreaId.Value)
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync();

        if (record == null)
        {
            throw new UserFriendlyException(L("BankFilePayrollControlRecordNotFound"));
        }

        return new BankFilePeriod
        {
            PeriodType = BankFilePeriodType.Current,
            PayrollMonth = record.PayrollMonth,
            PayrollYear = record.PayrollYear,
            StartDate = record.CalendarMonthStart.Date,
            EndDate = record.CalendarMonthEnd.Date
        };
    }

    private async Task<BankFilePeriod> ResolveOtherAsync(int? payrollAreaId, int? payrollMonth, int? payrollYear)
    {
        // Legacy SetDates :2530-2531 -- a blank box takes today's month / year.
        var month = payrollMonth ?? Clock.Now.Month;
        var year = payrollYear ?? Clock.Now.Year;

        if (month < 1 || month > 12 || year < 1900 || year > 2999)
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }

        DateTime start;
        DateTime end;

        if (payrollAreaId.HasValue)
        {
            var payrollPeriodCode = await _payrollAreaRepository.GetAll()
                .Where(a => a.Id == payrollAreaId.Value && a.PayrollPeriod != null)
                .Select(a => a.PayrollPeriod.Code)
                .FirstOrDefaultAsync();

            try
            {
                (start, end) = PayrollPeriodShiftCalculator.GetCalendarMonthRange(payrollPeriodCode, month, year);
            }
            catch (ArgumentException)
            {
                throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
            }
        }
        else
        {
            start = new DateTime(year, month, 1);
            end = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        }

        return new BankFilePeriod
        {
            PeriodType = BankFilePeriodType.Other,
            PayrollMonth = month,
            PayrollYear = year,
            StartDate = start,
            EndDate = end
        };
    }
}
