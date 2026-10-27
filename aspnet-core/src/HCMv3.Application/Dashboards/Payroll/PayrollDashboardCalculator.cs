using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards.Payroll.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Dashboards.Payroll;

/// <summary>What the dashboard needs to know about a wage type to place its amounts.</summary>
public record PayrollWageInfo(int Id, string Code, string NameEn, string NameAr, BillingType BillingType, bool DontImpactNetPay);

/// <summary>One line of a payslip.</summary>
public record PayrollLine(int WageTypeId, double Amount, double Number);

/// <summary>One payroll result (salary register) calculated in the month it is for.</summary>
public record PayrollPayslip(int EmployeeId, DateTime Month, PayrollRunType RunType, IReadOnlyList<PayrollLine> Lines);

public record PayrollEmployee(int Id, string Code, string NameEn, string NameAr, long? OrganizationUnitId, DateTime? JoiningDate);

/// <summary>A payslip reduced to the figures every tile adds up.</summary>
public record PayslipTotals(
    double Basic,
    double Gross,
    double Deductions,
    double Net,
    double EmployerSocialSecurity,
    double IndemnityProvision,
    double LeaveProvision,
    double TicketProvision,
    double IndemnityBalance,
    double LeaveBalance,
    double TicketBalance)
{
    public double EmployerCost => EmployerSocialSecurity + IndemnityProvision + LeaveProvision + TicketProvision;

    public double CostToCompany => Gross + EmployerCost;
}

/// <summary>
/// Every number on the Payroll Dashboard, computed in memory from payslips the app service has already scoped. Kept
/// free of repositories so the rules can be tested directly.
///
/// <para><b>Which payslips.</b> Only results calculated in the month they are for (<c>InPeriod = ForPeriod</c>), as
/// legacy read them: a retro recalculation carries a month again, and its difference is already paid as arrears in
/// the later month's own result. Off-cycle runs are money paid too, so they count in the totals.</para>
///
/// <para><b>Which lines.</b> Gross and deductions follow the payroll engine's net-pay rule: earnings and deductions
/// that affect net pay (<c>DontImpactNetPay</c> off), leaving out net pay itself, which is booked as an earning. Net is
/// the net-pay line. The fixed codes below are the engine's own system wage types, the same ones the payroll reports
/// read; legacy's dashboards also hard-coded allowance codes, which is not repeated here.</para>
/// </summary>
public class PayrollDashboardCalculator
{
    public const int TopCount = 10;

    public const string NetPayCode = "9000";
    public const string BasicCode = "1000";
    public const string EmployerSocialSecurityCode = "3";
    public const string IndemnityProvisionCode = "3000";
    public const string LeaveProvisionCode = "3001";
    public const string TicketProvisionCode = "3002";
    public const string IndemnityBalanceCode = "3003";
    public const string LeaveBalanceCode = "3004";
    public const string TicketBalanceCode = "3005";

    /// <summary>Lower bounds of the basic-salary bands. Every salary falls in exactly one band.</summary>
    public static readonly double[] BasicBandBounds = { 0, 300, 500, 1000, 1500, 3000 };

    private static readonly HashSet<string> EmployerCodes = new()
    {
        EmployerSocialSecurityCode, IndemnityProvisionCode, LeaveProvisionCode, TicketProvisionCode
    };

    private readonly DateTime _from;
    private readonly DateTime _to;
    private readonly DashboardOrgTree _tree;
    private readonly IReadOnlyDictionary<int, PayrollEmployee> _employees;
    private readonly IReadOnlyDictionary<int, PayrollWageInfo> _wageTypes;

    public PayrollDashboardCalculator(
        DateTime from,
        DateTime to,
        DashboardOrgTree tree,
        IEnumerable<PayrollEmployee> employees,
        IEnumerable<PayrollWageInfo> wageTypes)
    {
        _from = from.Date;
        _to = to.Date;
        _tree = tree;
        _employees = employees.ToDictionary(e => e.Id);
        _wageTypes = wageTypes.ToDictionary(w => w.Id);
    }

    public static DateTime MonthOf(DateTime date) => new(date.Year, date.Month, 1);

    // ---------------------------------------------------------------- one payslip

    private static bool IsEarning(PayrollWageInfo w) => w.BillingType == BillingType.Earning && !w.DontImpactNetPay && w.Code != NetPayCode;

    private static bool IsDeduction(PayrollWageInfo w) => w.BillingType == BillingType.Deduction && !w.DontImpactNetPay;

    public PayslipTotals Totals(PayrollPayslip payslip)
    {
        double basic = 0, gross = 0, deductions = 0, employerSs = 0, indemnity = 0, leave = 0, ticket = 0;
        double indemnityBalance = 0, leaveBalance = 0, ticketBalance = 0;
        double? net = null;

        foreach (var line in payslip.Lines)
        {
            if (!_wageTypes.TryGetValue(line.WageTypeId, out var w))
            {
                continue;
            }

            switch (w.Code)
            {
                case NetPayCode: net = (net ?? 0) + line.Amount; continue;
                case EmployerSocialSecurityCode: employerSs += line.Amount; continue;
                case IndemnityProvisionCode: indemnity += line.Amount; continue;
                case LeaveProvisionCode: leave += line.Amount; continue;
                case TicketProvisionCode: ticket += line.Amount; continue;
                case IndemnityBalanceCode: indemnityBalance += line.Amount; continue;
                case LeaveBalanceCode: leaveBalance += line.Amount; continue;
                case TicketBalanceCode: ticketBalance += line.Amount; continue;
            }

            if (w.Code == BasicCode)
            {
                basic += line.Amount;
            }

            if (IsEarning(w))
            {
                gross += line.Amount;
            }
            else if (IsDeduction(w))
            {
                deductions += line.Amount;
            }
        }

        // A payslip without a net-pay line (an off-cycle run can have none) still paid gross less deductions.
        return new PayslipTotals(basic, gross, deductions, net ?? gross - deductions, employerSs, indemnity, leave, ticket,
            indemnityBalance, leaveBalance, ticketBalance);
    }

    /// <summary>The last month of the period with a regular result, the "as of" month of the snapshot tiles.</summary>
    public static DateTime? LatestMonth(IReadOnlyCollection<PayrollPayslip> payslips) =>
        payslips.Where(p => p.RunType == PayrollRunType.Regular).Select(p => (DateTime?)MonthOf(p.Month)).Max();

    public static IEnumerable<PayrollPayslip> RegularIn(IEnumerable<PayrollPayslip> payslips, DateTime? month) =>
        month == null
            ? Enumerable.Empty<PayrollPayslip>()
            : payslips.Where(p => p.RunType == PayrollRunType.Regular && MonthOf(p.Month) == month.Value);

    // ---------------------------------------------------------------- tiles

    public PayrollKpisDto Kpis(IReadOnlyCollection<PayrollPayslip> payslips)
    {
        var totals = payslips.Select(Totals).ToList();
        var latest = LatestMonth(payslips);
        var paidMonths = payslips.Where(p => p.RunType == PayrollRunType.Regular)
            .Select(p => (p.EmployeeId, MonthOf(p.Month)))
            .Distinct()
            .Count();
        var ctc = totals.Sum(t => t.CostToCompany);

        return new PayrollKpisDto
        {
            EmployeesPaid = RegularIn(payslips, latest).Select(p => p.EmployeeId).Distinct().Count(),
            Gross = Money(totals.Sum(t => t.Gross)),
            Deductions = Money(totals.Sum(t => t.Deductions)),
            Net = Money(totals.Sum(t => t.Net)),
            EmployerCost = Money(totals.Sum(t => t.EmployerCost)),
            CostToCompany = Money(ctc),
            AverageMonthlyCostPerEmployee = paidMonths == 0 ? null : Money(ctc / paidMonths),
            OffCyclePayments = payslips.Count(p => p.RunType == PayrollRunType.OffCycle)
        };
    }

    /// <summary>One point per month of the period, months without payroll included.</summary>
    public List<PayrollTrendPointDto> Trend(IReadOnlyCollection<PayrollPayslip> payslips)
    {
        var byMonth = payslips.ToLookup(p => MonthOf(p.Month));
        var points = new List<PayrollTrendPointDto>();

        for (var month = MonthOf(_from); month <= _to; month = month.AddMonths(1))
        {
            var totals = byMonth[month].Select(Totals).ToList();
            points.Add(new PayrollTrendPointDto
            {
                MonthStart = month,
                Employees = byMonth[month].Select(p => p.EmployeeId).Distinct().Count(),
                Gross = Money(totals.Sum(t => t.Gross)),
                Deductions = Money(totals.Sum(t => t.Deductions)),
                Net = Money(totals.Sum(t => t.Net)),
                EmployerCost = Money(totals.Sum(t => t.EmployerCost)),
                CostToCompany = Money(totals.Sum(t => t.CostToCompany))
            });
        }

        return points;
    }

    /// <summary>Cost to company split into basic, other earnings and each employer cost; empty parts left out.</summary>
    public List<PayrollCostMixDto> CostMix(IReadOnlyCollection<PayrollPayslip> payslips)
    {
        var totals = payslips.Select(Totals).ToList();
        var basic = totals.Sum(t => t.Basic);

        return new List<PayrollCostMixDto>
            {
                new() { Component = PayrollCostComponent.Basic, Amount = Money(basic) },
                new() { Component = PayrollCostComponent.OtherEarnings, Amount = Money(totals.Sum(t => t.Gross) - basic) },
                new() { Component = PayrollCostComponent.EmployerSocialSecurity, Amount = Money(totals.Sum(t => t.EmployerSocialSecurity)) },
                new() { Component = PayrollCostComponent.IndemnityProvision, Amount = Money(totals.Sum(t => t.IndemnityProvision)) },
                new() { Component = PayrollCostComponent.LeaveProvision, Amount = Money(totals.Sum(t => t.LeaveProvision)) },
                new() { Component = PayrollCostComponent.TicketProvision, Amount = Money(totals.Sum(t => t.TicketProvision)) }
            }
            .Where(c => c.Amount != 0)
            .ToList();
    }

    public List<PayrollOrgUnitBucketDto> ByOrgUnit(IReadOnlyCollection<PayrollPayslip> payslips, long? selectedUnitId)
    {
        return payslips
            .Select(p => (Payslip: p, Bucket: _employees.TryGetValue(p.EmployeeId, out var e)
                ? _tree.BucketFor(e.OrganizationUnitId, selectedUnitId)
                : ((long? Id, bool Direct)?)null))
            .Where(x => x.Bucket != null)
            .GroupBy(x => x.Bucket.Value)
            .Select(g =>
            {
                var totals = g.Select(x => Totals(x.Payslip)).ToList();
                return new PayrollOrgUnitBucketDto
                {
                    OrganizationUnitId = g.Key.Id,
                    IsDirectStaff = g.Key.Direct,
                    Name = g.Key.Id.HasValue ? _tree.NameOf(g.Key.Id.Value) : null,
                    HasChildren = !g.Key.Direct && g.Key.Id.HasValue && _tree.HasChildren(g.Key.Id.Value),
                    Employees = g.Select(x => x.Payslip.EmployeeId).Distinct().Count(),
                    Gross = Money(totals.Sum(t => t.Gross)),
                    Net = Money(totals.Sum(t => t.Net)),
                    CostToCompany = Money(totals.Sum(t => t.CostToCompany))
                };
            })
            .OrderByDescending(b => b.CostToCompany)
            .ThenBy(b => b.Name)
            .ToList();
    }

    public static int BandOf(double basic)
    {
        for (var i = BasicBandBounds.Length - 1; i > 0; i--)
        {
            if (basic >= BasicBandBounds[i])
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>Employees by basic salary in the latest paid month; every band is returned so the chart keeps its shape.</summary>
    public List<PayrollBasicBandDto> BasicBands(IReadOnlyCollection<PayrollPayslip> payslips)
    {
        var counts = new int[BasicBandBounds.Length];
        foreach (var basic in BasicByEmployee(payslips).Values)
        {
            counts[BandOf(basic)]++;
        }

        return BasicBandBounds
            .Select((from, i) => new PayrollBasicBandDto
            {
                Index = i,
                From = from,
                To = i + 1 < BasicBandBounds.Length ? BasicBandBounds[i + 1] : null,
                Employees = counts[i]
            })
            .ToList();
    }

    /// <summary>Each employee's basic salary in the latest paid month's regular result.</summary>
    public Dictionary<int, double> BasicByEmployee(IReadOnlyCollection<PayrollPayslip> payslips) =>
        RegularIn(payslips, LatestMonth(payslips))
            .GroupBy(p => p.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Sum(p => Totals(p).Basic));

    public List<PayrollWageTypeAmountDto> Earnings(IReadOnlyCollection<PayrollPayslip> payslips) => ByWageType(payslips, IsEarning);

    public List<PayrollWageTypeAmountDto> Deductions(IReadOnlyCollection<PayrollPayslip> payslips) => ByWageType(payslips, IsDeduction);

    private List<PayrollWageTypeAmountDto> ByWageType(IReadOnlyCollection<PayrollPayslip> payslips, Func<PayrollWageInfo, bool> include)
    {
        return payslips
            .SelectMany(p => p.Lines.Select(l => (p.EmployeeId, Line: l)))
            .Where(x => _wageTypes.TryGetValue(x.Line.WageTypeId, out var w) && include(w))
            .GroupBy(x => x.Line.WageTypeId)
            .Select(g =>
            {
                var w = _wageTypes[g.Key];
                return new PayrollWageTypeAmountDto
                {
                    WageTypeId = w.Id,
                    Code = w.Code,
                    NameEn = w.NameEn,
                    NameAr = w.NameAr,
                    Amount = Money(g.Sum(x => x.Line.Amount)),
                    Employees = g.Where(x => x.Line.Amount != 0).Select(x => x.EmployeeId).Distinct().Count()
                };
            })
            .Where(b => b.Amount != 0)
            .OrderByDescending(b => Math.Abs(b.Amount))
            .ThenBy(b => b.Code)
            .Take(TopCount)
            .ToList();
    }

    /// <summary>The latest paid month's regular payslips by how each employee is paid; no method on file is its own group.</summary>
    public List<PayrollPaymentMethodBucketDto> PaymentMethods(IReadOnlyCollection<PayrollPayslip> payslips, IReadOnlyDictionary<int, PaymentType> paymentTypes)
    {
        return RegularIn(payslips, LatestMonth(payslips))
            .GroupBy(p => paymentTypes.TryGetValue(p.EmployeeId, out var t) ? t : (PaymentType?)null)
            .Select(g => new PayrollPaymentMethodBucketDto
            {
                PaymentType = g.Key,
                Employees = g.Select(p => p.EmployeeId).Distinct().Count(),
                Net = Money(g.Sum(p => Totals(p).Net))
            })
            .OrderByDescending(b => b.Employees)
            .ToList();
    }

    public List<PayrollRunTypeBucketDto> RunTypes(IReadOnlyCollection<PayrollPayslip> payslips)
    {
        return payslips
            .GroupBy(p => p.RunType)
            .Select(g => new PayrollRunTypeBucketDto
            {
                RunType = g.Key,
                Payslips = g.Count(),
                Employees = g.Select(p => p.EmployeeId).Distinct().Count(),
                Net = Money(g.Sum(p => Totals(p).Net))
            })
            .OrderBy(b => b.RunType)
            .ToList();
    }

    /// <summary>The liability balances carried on the latest paid month's regular payslips.</summary>
    public PayrollProvisionsDto Provisions(IReadOnlyCollection<PayrollPayslip> payslips)
    {
        var totals = RegularIn(payslips, LatestMonth(payslips)).Select(Totals).ToList();
        return BalancesOf(totals);
    }

    private static PayrollProvisionsDto BalancesOf(IReadOnlyCollection<PayslipTotals> totals)
    {
        var indemnity = totals.Sum(t => t.IndemnityBalance);
        var leave = totals.Sum(t => t.LeaveBalance);
        var ticket = totals.Sum(t => t.TicketBalance);
        return new PayrollProvisionsDto
        {
            Indemnity = Money(indemnity),
            Leave = Money(leave),
            Ticket = Money(ticket),
            Total = Money(indemnity + leave + ticket)
        };
    }

    // ---------------------------------------------------------------- lists

    public PayrollEmployeeRowDto ToRow(PayrollPayslip payslip, IReadOnlyDictionary<int, PaymentType> paymentTypes)
    {
        _employees.TryGetValue(payslip.EmployeeId, out var e);
        var t = Totals(payslip);
        return new PayrollEmployeeRowDto
        {
            EmployeeId = payslip.EmployeeId,
            EmployeeCode = e?.Code,
            EmployeeNameEn = e?.NameEn,
            EmployeeNameAr = e?.NameAr,
            OrganizationUnitName = e?.OrganizationUnitId is long unitId ? _tree.NameOf(unitId) : null,
            Month = MonthOf(payslip.Month),
            RunType = payslip.RunType,
            Basic = Money(t.Basic),
            Gross = Money(t.Gross),
            Deductions = Money(t.Deductions),
            Net = Money(t.Net),
            EmployerCost = Money(t.EmployerCost),
            CostToCompany = Money(t.CostToCompany),
            PaymentType = paymentTypes.TryGetValue(payslip.EmployeeId, out var p) ? p : null
        };
    }

    /// <summary>One employee's cost to company, line by line, from one regular payslip (or none).</summary>
    public PayrollCostCardDto CostCard(int employeeId, PayrollPayslip payslip)
    {
        _employees.TryGetValue(employeeId, out var e);
        var card = new PayrollCostCardDto
        {
            EmployeeId = employeeId,
            EmployeeCode = e?.Code,
            EmployeeNameEn = e?.NameEn,
            EmployeeNameAr = e?.NameAr,
            OrganizationUnitName = e?.OrganizationUnitId is long unitId ? _tree.NameOf(unitId) : null,
            JoiningDate = e?.JoiningDate,
            Balances = new PayrollProvisionsDto()
        };

        if (payslip == null)
        {
            return card;
        }

        var lines = payslip.Lines
            .Where(l => _wageTypes.ContainsKey(l.WageTypeId))
            .GroupBy(l => l.WageTypeId)
            .Select(g => (Wage: _wageTypes[g.Key], Amount: g.Sum(l => l.Amount), Number: g.Sum(l => l.Number)))
            .Where(x => x.Amount != 0)
            .OrderBy(x => x.Wage.Code, StringComparer.Ordinal)
            .ToList();

        PayrollCostLineDto Line((PayrollWageInfo Wage, double Amount, double Number) x) => new()
        {
            WageTypeId = x.Wage.Id, Code = x.Wage.Code, NameEn = x.Wage.NameEn, NameAr = x.Wage.NameAr, Amount = Money(x.Amount)
        };

        var t = Totals(payslip);
        card.Month = MonthOf(payslip.Month);
        card.Earnings = lines.Where(x => IsEarning(x.Wage)).Select(Line).ToList();
        card.Deductions = lines.Where(x => IsDeduction(x.Wage)).Select(Line).ToList();
        card.EmployerCosts = lines.Where(x => EmployerCodes.Contains(x.Wage.Code)).Select(Line).ToList();
        card.Gross = Money(t.Gross);
        card.TotalDeductions = Money(t.Deductions);
        card.Net = Money(t.Net);
        card.EmployerCost = Money(t.EmployerCost);
        card.CostToCompany = Money(t.CostToCompany);
        card.AnnualCostToCompany = Money(t.CostToCompany * 12);
        card.Balances = BalancesOf(new[] { t });
        card.IndemnityDays = NumberOf(lines, IndemnityBalanceCode);
        card.LeaveDays = NumberOf(lines, LeaveBalanceCode);
        return card;
    }

    private static double? NumberOf(IEnumerable<(PayrollWageInfo Wage, double Amount, double Number)> lines, string code)
    {
        var match = lines.Where(x => x.Wage.Code == code).ToList();
        return match.Count == 0 ? null : Math.Round(match.Sum(x => x.Number), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Kuwaiti dinar has three decimals (fils).</summary>
    private static double Money(double value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
