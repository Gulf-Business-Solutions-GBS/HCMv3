using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// Sets the legacy payroll's salary registers beside HCMv3's for the same month, employee by employee and wage
/// type by wage type, and says for each difference where it most likely comes from.
///
/// <para>Both sides are compared as the legacy export query produces them: the month's own regular, current
/// results, a wage type's lines added up, and the salary record in force on the month end. Time types count
/// days, so they are compared on their number; everything else on its amount. A difference is then traced
/// through what the amount is made of -- the salary record (data), the days (time), the rate (calculation) --
/// in that order, because a wrong input explains everything downstream of it.</para>
///
/// <para>Only employees in the file are compared: a legacy export for a handful of employees should not list
/// everyone else as missing. The file is read in memory and nothing of it is stored or logged.</para>
/// </summary>
public class GoLiveLegacyComparer : AbpServiceBase, ITransientDependency
{
    private const string NetPayCode = "9000";

    /// <summary>Amounts closer than this match: one fils.</summary>
    private const decimal Tolerance = 0.001m;

    /// <summary>Amounts closer than this are put down to rounding.</summary>
    private const decimal RoundingTolerance = 0.010m;

    private const int MaxFileBytes = 30 * 1024 * 1024;

    private const string ExportQueryResource = "HCMv3.GoLiveSetup.LegacySalaryRegisterExport.sql";

    private readonly GoLiveLegacyFileReader _fileReader;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<SalaryRegister> _registerRepository;
    private readonly IRepository<SalaryRegisterDetail> _detailRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _payRecordDetailRepository;
    private readonly GoLiveLegacyPayments _legacyPayments;

    public GoLiveLegacyComparer(
        GoLiveLegacyFileReader fileReader,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IRepository<Employee> employeeRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<SalaryRegister> registerRepository,
        IRepository<SalaryRegisterDetail> detailRepository,
        IRepository<EmployeeBasicPayDetail> payRecordDetailRepository,
        GoLiveLegacyPayments legacyPayments)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _fileReader = fileReader;
        _payrollAreaResolver = payrollAreaResolver;
        _employeeRepository = employeeRepository;
        _wageTypeRepository = wageTypeRepository;
        _registerRepository = registerRepository;
        _detailRepository = detailRepository;
        _payRecordDetailRepository = payRecordDetailRepository;
        _legacyPayments = legacyPayments;
    }

    /// <summary>
    /// The export query, filled in: the month, and the payroll area's employee codes, so the legacy side
    /// exports exactly the employees HCMv3 has.
    /// </summary>
    public async Task<GoLiveLegacyExportQueryDto> GetExportQueryAsync(string payrollAreaName, DateTime monthEnd)
    {
        string sql;
        using (var stream = typeof(GoLiveLegacyComparer).GetTypeInfo().Assembly.GetManifestResourceStream(ExportQueryResource))
        using (var reader = new StreamReader(stream!))
        {
            sql = await reader.ReadToEndAsync();
        }

        var ids = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName.Trim(), monthEnd);
        var codes = await _employeeRepository.GetAll()
            .Where(e => ids.Contains(e.Id))
            .OrderBy(e => e.Code)
            .Select(e => e.Code)
            .ToListAsync();

        sql = sql
            .Replace("DECLARE @PayMonth        date          = '2025-03-31';",
                $"DECLARE @PayMonth        date          = '{monthEnd:yyyy-MM-dd}';")
            .Replace("DECLARE @EmployeeCodes   nvarchar(max) = N'';",
                $"DECLARE @EmployeeCodes   nvarchar(max) = N'{string.Join(",", codes).Replace("'", "''")}';");

        return new GoLiveLegacyExportQueryDto
        {
            FileName = $"Legacy Salary Register {monthEnd:yyyy-MM}.sql",
            Sql = sql,
        };
    }

    public async Task<GoLiveLegacyCompareOutput> CompareAsync(GoLiveLegacyCompareInput input, DateTime monthEnd)
    {
        var file = _fileReader.Read(input.FileName, Decode(input.Base64Content));
        if (file.Rows.Count == 0)
        {
            throw new UserFriendlyException(L("GoLiveCompareFileEmpty"));
        }

        var output = new GoLiveLegacyCompareOutput
        {
            PayMonth = monthEnd,
            FileRows = file.Rows.Count + file.Unreadable,
            RowsUnreadable = file.Unreadable,
        };

        var legacyRows = file.Rows.Where(r => r.PayMonth == null || r.PayMonth.Value.Date == monthEnd.Date).ToList();
        output.RowsForOtherMonths = file.Rows.Count - legacyRows.Count;

        // The legacy side, a wage type's rows added up the way the query adds up register lines.
        var legacy = legacyRows
            .GroupBy(r => (Employee: r.EmployeeCode, Wage: r.WageTypeCode))
            .ToDictionary(g => g.Key, g => new Side
            {
                Rate = g.Max(r => r.Rate),
                Number = SumOrNull(g.Select(r => r.Number)),
                Amount = SumOrNull(g.Select(r => r.Amount)),
            });
        var legacyWageNames = legacyRows
            .GroupBy(r => r.WageTypeCode)
            .ToDictionary(g => g.Key, g => g.Select(r => r.WageTypeName).FirstOrDefault(n => n != null));

        var fileCodes = legacyRows.Select(r => r.EmployeeCode).Distinct().ToList();
        output.Employees = fileCodes.Count;

        // The HCMv3 side, for the same employees.
        var employees = await _employeeRepository.GetAll()
            .Where(e => fileCodes.Contains(e.Code))
            .Select(e => new { e.Id, e.Code })
            .ToListAsync();
        var employeeIds = employees.Select(e => e.Id).ToList();
        // Keyed by the file's own spelling of the code, so an employee written "e001" in one and "E001 " in the
        // other is one employee, not two.
        var fileCodeByUpper = fileCodes
            .GroupBy(c => c.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        var codeById = employees.ToDictionary(
            e => e.Id,
            e => fileCodeByUpper.GetValueOrDefault(e.Code.Trim().ToUpperInvariant()) ?? e.Code);
        output.LegacyEmployeesNotInHcmv3 = fileCodes
            .Where(c => !codeById.Values.Contains(c))
            .OrderBy(c => c)
            .ToList();

        var areaIds = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(input.PayrollAreaName.Trim(), monthEnd);
        output.Hcmv3EmployeesNotInFile = areaIds.Count(id => !employeeIds.Contains(id));

        var registers = await _registerRepository.GetAll()
            .Where(r => employeeIds.Contains(r.EmployeeId)
                        && r.InPeriod == monthEnd
                        && r.ForPeriod == monthEnd
                        && r.PayrollRunType == PayrollRunType.Regular
                        && r.PayrollResultType == PayrollResultType.Current)
            .Select(r => new { r.Id, r.EmployeeId })
            .ToListAsync();
        var registerIds = registers.Select(r => r.Id).ToList();
        var withRegister = registers.Select(r => codeById[r.EmployeeId]).ToHashSet();

        var hcmv3 = (await _detailRepository.GetAll()
                .Where(d => registerIds.Contains(d.SalaryRegisterId))
                .Select(d => new { d.SalaryRegister.EmployeeId, d.WageType.Code, d.Rate, d.Number, d.Amount })
                .ToListAsync())
            .GroupBy(d => (Employee: codeById[d.EmployeeId], Wage: d.Code))
            .ToDictionary(g => g.Key, g => new Side
            {
                Rate = Round(g.Max(d => d.Rate)),
                Number = Round(g.Sum(d => d.Number)),
                Amount = Round(g.Sum(d => d.Amount)),
            });

        var payRecords = (await _payRecordDetailRepository.GetAll()
                .Where(d => employeeIds.Contains(d.EmployeeBasicPay.EmployeeId)
                            && d.EmployeeBasicPay.StartDate <= monthEnd
                            && d.EmployeeBasicPay.EndDate >= monthEnd)
                .Select(d => new { d.EmployeeBasicPay.EmployeeId, d.WageType.Code, d.Amount })
                .ToListAsync())
            .GroupBy(d => (Employee: codeById[d.EmployeeId], Wage: d.Code))
            .ToDictionary(g => g.Key, g => Round(g.Sum(d => d.Amount)));

        foreach (var (key, amount) in payRecords)
        {
            if (!hcmv3.TryGetValue(key, out var side))
            {
                hcmv3[key] = side = new Side();
            }

            side.PayRecordAmount = amount;
        }

        var wageTypes = await _wageTypeRepository.GetAll()
            .Select(w => new { w.Code, w.NameEn, w.BillingType })
            .ToListAsync();
        var wageByCode = wageTypes.GroupBy(w => w.Code).ToDictionary(g => g.Key, g => g.First());

        var hcmv3Codes = codeById.Values.ToHashSet();
        var payableWageTypes = await _legacyPayments.PayableWageTypeCodesAsync();
        var monthPayments = await _legacyPayments.MonthPaymentsAsync(employeeIds, monthEnd);
        var idByCode = codeById.ToDictionary(p => p.Value, p => p.Key);

        foreach (var key in legacy.Keys.Union(hcmv3.Keys).Distinct())
        {
            legacy.TryGetValue(key, out var old);
            hcmv3.TryGetValue(key, out var now);

            // On HCMv3's salary record but calculated by neither payroll: nothing to compare.
            if (old == null && now?.Amount == null)
            {
                continue;
            }
            wageByCode.TryGetValue(key.Wage, out var wage);

            var billing = wage?.BillingType ?? ParseBilling(legacyRows.FirstOrDefault(r => r.WageTypeCode == key.Wage)?.BillingType);
            var line = new GoLiveLegacyCompareLineDto
            {
                EmployeeCode = key.Employee,
                WageTypeCode = key.Wage,
                WageTypeName = wage?.NameEn ?? legacyWageNames.GetValueOrDefault(key.Wage),
                BillingType = billing,
                ComparesDays = billing == BillingType.TimeType,
                LegacyRate = old?.Rate,
                LegacyNumber = old?.Number,
                LegacyAmount = old?.Amount,
                Hcmv3Rate = now?.Rate,
                Hcmv3Number = now?.Number,
                Hcmv3Amount = now?.Amount,
                Hcmv3PayRecordAmount = now?.PayRecordAmount,
            };

            Judge(line, hcmv3Codes.Contains(key.Employee), wage != null, withRegister.Contains(key.Employee));
            OfferPayment(line, idByCode, payableWageTypes, monthPayments);
            output.Lines.Add(line);
        }

        ExplainNetPay(output.Lines);

        output.Lines = output.Lines
            .OrderBy(l => l.EmployeeCode)
            .ThenBy(l => l.WageTypeCode)
            .ToList();
        output.Matched = output.Lines.Count(l => l.Status == GoLiveCompareStatus.Matched);
        output.Different = output.Lines.Count(l => l.Status == GoLiveCompareStatus.Different);
        output.OnlyInLegacy = output.Lines.Count(l => l.Status == GoLiveCompareStatus.OnlyInLegacy);
        output.OnlyInHcmv3 = output.Lines.Count(l => l.Status == GoLiveCompareStatus.OnlyInHcmv3);

        return output;
    }

    /// <summary>
    /// Whether the line matches -- rate, number and amount all three, never one or two of them -- and if not, the
    /// most likely reason.
    /// </summary>
    private void Judge(GoLiveLegacyCompareLineDto line, bool employeeInHcmv3, bool wageTypeInHcmv3, bool hasHcmv3Register)
    {
        line.RateMatches = Same(line.LegacyRate, line.Hcmv3Rate);
        line.NumberMatches = Same(line.LegacyNumber, line.Hcmv3Number);
        line.AmountMatches = Same(line.LegacyAmount, line.Hcmv3Amount);
        line.Difference = (line.Hcmv3Amount ?? 0) - (line.LegacyAmount ?? 0);

        if (line.RateMatches && line.NumberMatches && line.AmountMatches)
        {
            Set(line, GoLiveCompareStatus.Matched, GoLiveCompareCause.None, L("GoLiveCompareMatched"));
            return;
        }

        var inLegacy = line.LegacyRate != null || line.LegacyNumber != null || line.LegacyAmount != null;
        var inHcmv3 = line.Hcmv3Rate != null || line.Hcmv3Number != null || line.Hcmv3Amount != null;

        if (inLegacy && inHcmv3)
        {
            // For fixed pay the legacy rate is the salary record's amount, so it tells whether the records agree.
            var recordDiffers = line.Hcmv3PayRecordAmount != null
                                && line.LegacyRate != null
                                && !Same(line.LegacyRate, line.Hcmv3PayRecordAmount);

            if (!line.NumberMatches && line.ComparesDays)
            {
                Set(line, GoLiveCompareStatus.Different, GoLiveCompareCause.Days,
                    L("GoLiveCompareDaysDiffer", Days(line.LegacyNumber), Days(line.Hcmv3Number)));
            }
            else if (!line.RateMatches && recordDiffers)
            {
                Set(line, GoLiveCompareStatus.Different, GoLiveCompareCause.Data,
                    L("GoLiveCompareSalaryRecordDiffers", Money(line.LegacyRate), Money(line.Hcmv3PayRecordAmount)));
            }
            else if (!line.RateMatches && !line.NumberMatches)
            {
                Set(line, GoLiveCompareStatus.Different, GoLiveCompareCause.Logic,
                    L("GoLiveCompareRateAndDaysDiffer", Money(line.LegacyRate), Money(line.Hcmv3Rate),
                        Days(line.LegacyNumber), Days(line.Hcmv3Number)));
            }
            else if (!line.NumberMatches)
            {
                Set(line, GoLiveCompareStatus.Different, GoLiveCompareCause.Days,
                    L("GoLiveCompareSameRateDaysDiffer", Days(line.LegacyNumber), Days(line.Hcmv3Number)));
            }
            else if (!line.RateMatches)
            {
                Set(line, GoLiveCompareStatus.Different, GoLiveCompareCause.Logic,
                    L("GoLiveCompareRateDiffers", Money(line.LegacyRate), Money(line.Hcmv3Rate)));
            }
            else if (Math.Abs(line.Difference.Value) < RoundingTolerance)
            {
                Set(line, GoLiveCompareStatus.Different, GoLiveCompareCause.Rounding, L("GoLiveCompareRounding"));
            }
            else
            {
                Set(line, GoLiveCompareStatus.Different, GoLiveCompareCause.Logic, L("GoLiveCompareFormula"));
            }

            return;
        }

        if (inLegacy)
        {
            if (!employeeInHcmv3)
            {
                Set(line, GoLiveCompareStatus.OnlyInLegacy, GoLiveCompareCause.Data, L("GoLiveCompareEmployeeNotLoaded"));
            }
            else if (!wageTypeInHcmv3)
            {
                Set(line, GoLiveCompareStatus.OnlyInLegacy, GoLiveCompareCause.Setup,
                    L("GoLiveCompareWageTypeMissing", line.WageTypeCode));
            }
            else if (!hasHcmv3Register)
            {
                Set(line, GoLiveCompareStatus.OnlyInLegacy, GoLiveCompareCause.Logic, L("GoLiveCompareNoHcmv3Register"));
            }
            else if (line.Hcmv3PayRecordAmount == null && line.BillingType is BillingType.Earning or BillingType.Deduction)
            {
                Set(line, GoLiveCompareStatus.OnlyInLegacy, GoLiveCompareCause.Data, L("GoLiveCompareNotOnHcmv3Record"));
            }
            else
            {
                Set(line, GoLiveCompareStatus.OnlyInLegacy, GoLiveCompareCause.Logic, L("GoLiveCompareNotCalculated"));
            }

            return;
        }

        if (line.Hcmv3PayRecordAmount != null)
        {
            Set(line, GoLiveCompareStatus.OnlyInHcmv3, GoLiveCompareCause.Data, L("GoLiveCompareNotOnLegacyRecord"));
        }
        else
        {
            Set(line, GoLiveCompareStatus.OnlyInHcmv3, GoLiveCompareCause.Logic, L("GoLiveCompareExtraInHcmv3"));
        }
    }

    /// <summary>
    /// Whether a payment can make a not-matched earning or deduction match, and for how much -- or why not. Only
    /// money lines are offered: time types and provisions are worked out, never paid.
    /// </summary>
    private void OfferPayment(
        GoLiveLegacyCompareLineDto line,
        IReadOnlyDictionary<string, int> idByCode,
        HashSet<string> payableWageTypes,
        IReadOnlyDictionary<(int EmployeeId, string WageCode), (double Ours, double Others)> monthPayments)
    {
        if (line.Status == GoLiveCompareStatus.Matched
            || line.BillingType is not (BillingType.Earning or BillingType.Deduction))
        {
            return;
        }

        if (!idByCode.TryGetValue(line.EmployeeCode, out var employeeId))
        {
            line.PaymentBlockedReason = L("GoLiveCompareEmployeeNotLoaded");
        }
        else if (!payableWageTypes.Contains(line.WageTypeCode))
        {
            line.PaymentBlockedReason = L("GoLivePaymentNotPayable", line.WageTypeCode);
        }
        else
        {
            var others = monthPayments.TryGetValue((employeeId, line.WageTypeCode), out var paid) ? paid.Others : 0;
            var suggested = Math.Round((line.LegacyAmount ?? 0) - (decimal)others, 3);
            if (suggested == 0)
            {
                line.PaymentBlockedReason = L("GoLivePaymentNoAmount");
                return;
            }

            line.CanAddPayment = true;
            line.SuggestedPaymentAmount = suggested;
        }
    }

    /// <summary>Net pay adds everything up: when anything else of the employee's differs, that is the reason.</summary>
    private void ExplainNetPay(List<GoLiveLegacyCompareLineDto> lines)
    {
        var employeesWithDifferences = lines
            .Where(l => l.WageTypeCode != NetPayCode
                        && l.Status != GoLiveCompareStatus.Matched
                        && l.BillingType is BillingType.Earning or BillingType.Deduction)
            .Select(l => l.EmployeeCode)
            .ToHashSet();

        foreach (var netPay in lines.Where(l => l.WageTypeCode == NetPayCode
                                                && l.Status != GoLiveCompareStatus.Matched
                                                && employeesWithDifferences.Contains(l.EmployeeCode)))
        {
            Set(netPay, netPay.Status, GoLiveCompareCause.FollowsOtherLines, L("GoLiveCompareNetPayFollows"));
        }
    }

    private static void Set(GoLiveLegacyCompareLineDto line, GoLiveCompareStatus status, GoLiveCompareCause cause, string reason)
    {
        line.Status = status;
        line.Cause = cause;
        line.Reason = reason;
    }

    private byte[] Decode(string base64)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64 ?? string.Empty);
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
        }

        if (bytes.Length > MaxFileBytes)
        {
            throw new UserFriendlyException(L("GoLiveCompareFileTooLarge"));
        }

        return bytes;
    }

    /// <summary>Equal within a fils; nothing on one side and zero on the other counts as equal.</summary>
    private static bool Same(decimal? a, decimal? b) =>
        a == null || b == null
            ? Math.Abs(a ?? b ?? 0) < Tolerance
            : Math.Abs(a.Value - b.Value) < Tolerance;

    private static decimal? SumOrNull(IEnumerable<decimal?> values)
    {
        var present = values.Where(v => v != null).ToList();
        return present.Count == 0 ? null : present.Sum();
    }

    private static decimal Round(double value) => Math.Round((decimal)value, 6);

    private static BillingType? ParseBilling(string text) =>
        Enum.TryParse<BillingType>(text, ignoreCase: true, out var billing) ? billing : null;

    private static string Money(decimal? value) =>
        value?.ToString("#,0.000", CultureInfo.InvariantCulture) ?? "—";

    private static string Days(decimal? value) =>
        value?.ToString("0.##", CultureInfo.InvariantCulture) ?? "—";

    private class Side
    {
        public decimal? Rate { get; set; }

        public decimal? Number { get; set; }

        public decimal? Amount { get; set; }

        public decimal? PayRecordAmount { get; set; }
    }
}
