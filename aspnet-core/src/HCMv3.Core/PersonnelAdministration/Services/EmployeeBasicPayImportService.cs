using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.PayrollLock;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeBasicPayImportService"/>
/// <remarks>
/// <para>Legacy source: <c>HCM10.Module\Controllers\Import\ImportExcelController.cs</c>, lines 3978-4224, plus the
/// save-time checks of <c>EmployeeBasicPayDetail.PerformValidation</c>. The order of the messages, the
/// "Not exists." wording, the skip of a wage type already on the record, the carry-forward on code "1000" and
/// the end-date chain all follow legacy.</para>
///
/// <para><b>Where it differs, and why.</b></para>
/// <list type="bullet">
/// <item><description>A PayType, PayInterval or Amount that does not parse is reported as a row message next to
/// the others. Legacy let <c>Enum.Parse</c>/<c>Double.Parse</c> throw, which lost the WageType and Currency
/// messages of the same row and showed the operator a .NET error text instead.</description></item>
/// <item><description>Enum names are matched ignoring case, and the legacy spelling <c>Yealy</c> is still accepted
/// for <see cref="PayInterval.Yearly"/> so old templates keep working. A number must be a defined value.</description></item>
/// <item><description>Legacy kept a list of the headers it had created earlier in the run. Every row here is
/// committed on its own, so the database lookup already finds them.</description></item>
/// <item><description>The header total is rounded to 3 decimals, as <c>EmployeeBasicPayDetailsAppService</c> does,
/// rather than by the employee's currency (<c>CurrencyService.Rounding</c>).</description></item>
/// </list>
/// </remarks>
public class EmployeeBasicPayImportService : HCMv3DomainServiceBase, IEmployeeBasicPayImportService
{
    /// <summary>The basic salary wage type; legacy hard-codes it for the carry-forward.</summary>
    public const string BasicSalaryWageTypeCode = "1000";

    private const string LegacyYearlySpelling = "Yealy";
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPay> _basicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _detailRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<Currency> _currencyRepository;

    public EmployeeBasicPayImportService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPay> basicPayRepository,
        IRepository<EmployeeBasicPayDetail> detailRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<Currency> currencyRepository)
    {
        _employeeRepository = employeeRepository;
        _basicPayRepository = basicPayRepository;
        _detailRepository = detailRepository;
        _wageTypeRepository = wageTypeRepository;
        _currencyRepository = currencyRepository;
    }

    /// <summary>
    /// Property-injected, as on <c>EmployeeManagersAppService</c>: a row re-closed by the end-date chain must be
    /// marked before it is saved. See <see cref="IPayrollRetroTrigger.MarkEndDateAutoChange"/>.
    /// </summary>
    public IPayrollRetroTrigger RetroTrigger { get; set; }

    public async Task<EmployeeBasicPayImportRowResult> ImportRowAsync(int tenantId, EmployeeBasicPayImportRow row, bool carryAllowanceOnSalaryImport)
    {
        var employeeCode = row.EmployeeCode?.Trim();
        if (string.IsNullOrEmpty(employeeCode))
        {
            return new EmployeeBasicPayImportRowResult { Status = EmployeeBasicPayImportRowStatus.Skipped };
        }

        #region << Validation (legacy order) >>

        var messages = new List<string>();

        var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.Code == employeeCode);
        if (employee == null)
        {
            messages.Add(NotExists(EmployeeBasicPayImportRow.EmployeeCodeColumn, employeeCode));
        }

        var effectiveDate = ReadEffectiveDate(row, messages);

        if (!TryParseEnum(row.PayType, out PayType payType))
        {
            messages.Add(NotExists(EmployeeBasicPayImportRow.PayTypeColumn, row.PayType?.Trim() ?? ""));
        }

        if (!TryParseEnum(row.PayInterval, out PayInterval payInterval, (LegacyYearlySpelling, PayInterval.Yearly)))
        {
            messages.Add(NotExists(EmployeeBasicPayImportRow.PayIntervalColumn, row.PayInterval?.Trim() ?? ""));
        }

        var wageTypeName = row.WageType?.Trim() ?? "";
        var wageType = await _wageTypeRepository.FirstOrDefaultAsync(w => w.NameEn == wageTypeName);
        if (wageType == null)
        {
            messages.Add(NotExists(EmployeeBasicPayImportRow.WageTypeColumn, wageTypeName));
        }

        var currencyName = row.Currency?.Trim() ?? "";
        var currency = await _currencyRepository.FirstOrDefaultAsync(c => c.NameEn == currencyName);
        if (currency == null)
        {
            messages.Add(NotExists(EmployeeBasicPayImportRow.CurrencyColumn, currencyName));
        }

        var amount = ReadAmount(row, messages);

        if (messages.Count > 0)
        {
            return new EmployeeBasicPayImportRowResult
            {
                Status = EmployeeBasicPayImportRowStatus.Invalid,
                Message = string.Join(", ", messages)
            };
        }

        #endregion

        // Everything is decided before anything is written, so a refusal (a zero amount on the new line or on a
        // carried one) leaves no half-written header behind even without a transaction. Legacy reached the same
        // end state by rolling back; the save-time payroll lock and retro checks still rely on the caller's
        // unit of work.

        #region << Header: the record starting on the effective date, else a new one >>

        var basicPay = await FindHeaderAsync(employee.Id, effectiveDate);

        var existingWageTypeIds = basicPay == null
            ? new List<int>()
            : await _detailRepository.GetAll()
                .Where(d => d.EmployeeBasicPayId == basicPay.Id)
                .Select(d => d.WageTypeId)
                .ToListAsync();

        // Legacy: the wage type is already on the record -> success, and nothing else happens for this row.
        if (existingWageTypeIds.Contains(wageType.Id))
        {
            return new EmployeeBasicPayImportRowResult { Status = EmployeeBasicPayImportRowStatus.AlreadyPresent };
        }

        EnsureAmountIsSet(amount);
        existingWageTypeIds.Add(wageType.Id);

        #endregion

        #region << CarryAllowanceOnSalaryImport: which lines of the previous record to copy >>

        var carriedLines = new List<EmployeeBasicPayDetail>();
        if (carryAllowanceOnSalaryImport && wageType.Code == BasicSalaryWageTypeCode)
        {
            var previous = await _basicPayRepository.GetAll()
                .Where(p => p.EmployeeId == employee.Id && p.StartDate < effectiveDate)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync();

            if (previous != null)
            {
                var previousLines = await _detailRepository.GetAll()
                    .Where(d => d.EmployeeBasicPayId == previous.Id && d.WageTypeId != wageType.Id)
                    .OrderBy(d => d.Id)
                    .ToListAsync();

                foreach (var line in previousLines.Where(l => !existingWageTypeIds.Contains(l.WageTypeId)))
                {
                    // The copy is a new detail line, so legacy's save-time zero check applies to it too.
                    EnsureAmountIsSet(line.Amount);
                    carriedLines.Add(line);
                    existingWageTypeIds.Add(line.WageTypeId);
                }
            }
        }

        #endregion

        #region << Writes >>

        if (basicPay == null)
        {
            basicPay = new EmployeeBasicPay
            {
                EmployeeId = employee.Id,
                StartDate = effectiveDate,
                EndDate = InfiniteEndDate,
                PayType = payType,
                PayInterval = payInterval
            };
            basicPay.TenantId = tenantId;
            await _basicPayRepository.InsertAsync(basicPay);
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        await InsertDetailAsync(tenantId, basicPay.Id, wageType.Id, amount, currency.Id);

        // WageType, Amount and Currency only -- the Dependant* fields are not carried, as in legacy.
        foreach (var line in carriedLines)
        {
            await InsertDetailAsync(tenantId, basicPay.Id, line.WageTypeId, line.Amount, line.CurrencyId);
        }

        #endregion

        await CurrentUnitOfWork.SaveChangesAsync();
        var total = await _detailRepository.GetAll()
            .Where(d => d.EmployeeBasicPayId == basicPay.Id)
            .SumAsync(d => d.Amount);
        basicPay.Amount = Math.Round(total, 3);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(employee.Id);
        await CurrentUnitOfWork.SaveChangesAsync();

        return new EmployeeBasicPayImportRowResult { Status = EmployeeBasicPayImportRowStatus.Imported };
    }

    private async Task<EmployeeBasicPay> FindHeaderAsync(int employeeId, DateTime effectiveDate)
    {
        // StartDate.Date == effectiveDate, written as a range so it translates on every provider.
        var nextDay = effectiveDate.AddDays(1);
        return await _basicPayRepository.GetAll()
            .Where(p => p.EmployeeId == employeeId && p.StartDate >= effectiveDate && p.StartDate < nextDay)
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>Legacy <c>EmployeeBasicPayDetail.PerformValidation</c> refuses a zero amount when the line is saved.</summary>
    private void EnsureAmountIsSet(double amount)
    {
        if (amount == 0)
        {
            throw new UserFriendlyException(L("BasicPayImportAmountBlankOrZero"));
        }
    }

    private async Task InsertDetailAsync(int tenantId, int basicPayId, int wageTypeId, double amount, int currencyId)
    {
        var detail = new EmployeeBasicPayDetail
        {
            EmployeeBasicPayId = basicPayId,
            WageTypeId = wageTypeId,
            Amount = amount,
            CurrencyId = currencyId
        };
        detail.TenantId = tenantId;
        await _detailRepository.InsertAsync(detail);
    }

    /// <summary>
    /// Legacy's explicit pass after the row: every basic pay of the employee, ordered by StartDate, ends the day
    /// before the next one starts; the last one is open-ended (9999-12-31).
    /// </summary>
    private async Task SetEndDatesAsync(int employeeId)
    {
        var records = await _basicPayRepository.GetAll()
            .Where(p => p.EmployeeId == employeeId)
            .OrderBy(p => p.StartDate)
            .ToListAsync();

        for (var i = 0; i < records.Count; i++)
        {
            var current = records[i];
            var next = records.Skip(i + 1).FirstOrDefault(r => r.StartDate > current.StartDate);
            var newEndDate = next != null ? next.StartDate.AddDays(-1) : InfiniteEndDate;
            if (current.EndDate != newEndDate)
            {
                current.EndDate = newEndDate;

                // Re-closed because a neighbour arrived, not a change of its own. Legacy sets IsEndDateAutoChange.
                RetroTrigger?.MarkEndDateAutoChange(current);
            }
        }
    }

    /// <summary>Legacy <c>GetDateValue</c> with <c>IsMendatory = true</c>.</summary>
    private DateTime ReadEffectiveDate(EmployeeBasicPayImportRow row, List<string> messages)
    {
        if (row.EffectiveDateValue.HasValue)
        {
            return row.EffectiveDateValue.Value.Date;
        }

        var text = row.EffectiveDateText?.Trim() ?? "";
        if (text.Length == 0)
        {
            messages.Add(L("BasicPayImportValueBlank", EmployeeBasicPayImportRow.EffectiveDateColumn));
            return default;
        }

        if (!DateTime.TryParseExact(text, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            messages.Add(L("BulkImportValueNotValid", EmployeeBasicPayImportRow.EffectiveDateColumn));
            return default;
        }

        return date.Date;
    }

    /// <summary>Legacy <c>Double.Parse(SetIfNull(value, 0))</c>: a blank cell is 0, which the detail save then refuses.</summary>
    private double ReadAmount(EmployeeBasicPayImportRow row, List<string> messages)
    {
        if (row.AmountValue.HasValue)
        {
            return row.AmountValue.Value;
        }

        var text = row.AmountText?.Trim() ?? "";
        if (text.Length == 0)
        {
            return 0;
        }

        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var amount))
        {
            return amount;
        }

        messages.Add(L("BulkImportValueNotValid", EmployeeBasicPayImportRow.AmountColumn + " : '" + text + "'"));
        return 0;
    }

    /// <summary>
    /// Legacy <c>Enum.Parse</c>: a member name or its number. Names ignore case here; <paramref name="aliases"/>
    /// carries legacy spellings that were renamed in HCMv3.
    /// </summary>
    private static bool TryParseEnum<TEnum>(string text, out TEnum value, params (string Name, TEnum Value)[] aliases)
        where TEnum : struct, Enum
    {
        value = default;
        var trimmed = text?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            if (!Enum.IsDefined(typeof(TEnum), number))
            {
                return false;
            }

            value = (TEnum)Enum.ToObject(typeof(TEnum), number);
            return true;
        }

        foreach (var alias in aliases)
        {
            if (string.Equals(alias.Name, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                value = alias.Value;
                return true;
            }
        }

        var name = Enum.GetNames(typeof(TEnum)).FirstOrDefault(n => string.Equals(n, trimmed, StringComparison.OrdinalIgnoreCase));
        if (name == null)
        {
            return false;
        }

        value = Enum.Parse<TEnum>(name);
        return true;
    }

    private string NotExists(string column, string value)
    {
        return L("BulkImportReferenceNotFound", column, value);
    }
}
