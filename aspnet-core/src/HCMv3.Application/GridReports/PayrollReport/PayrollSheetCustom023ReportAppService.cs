using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll Sheet (Custom 023) grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollSheet_Custom023.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 614-1195).
///
/// Same shape as the generic <see cref="PayrollSheetReportAppService"/> -- one row per (employee,
/// month-end period), the same fixed identity/org/bank/sponsor columns, the same month-range settings --
/// but the wage-type columns are a HARD-CODED catalogue of client 023's wage types instead of being
/// derived from the data, and there is no employee-code filter.
///
/// See <see cref="PayrollSheetCustom023ReportRowDto"/> for the one deviation that matters: legacy filled
/// the 56 earning/deduction cells by switching on client-023 database primary keys, which cannot be
/// carried across, so this port matches wage types by <c>NameEn</c> -- the very captions legacy printed.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down stack, the
/// ColumnChooser JSON persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollSheetCustom023)]
public class PayrollSheetCustom023ReportAppService
    : GridReportAppServiceBase, IPayrollSheetCustom023ReportAppService
{
    /// <summary>Legacy line 935: "WorkingDays" is the <c>Number</c> of the basic-salary line.</summary>
    private const string BasicSalaryWageTypeCode = "1000";

    /// <summary>Legacy line 849/1183: the net-pay column, always declared on this sheet.</summary>
    private const string NetPayColumnName = "Net Pay";

    /// <summary>Legacy line 1032: the fallback when the employee is not paid through a bank.</summary>
    private const string ChequeSalaryVia = "Cheque";

    /// <summary>
    /// Legacy lines 1078-1088: the seven <c>"Act ..."</c> columns, keyed by wage type CODE. Codes are
    /// portable master data, so this mapping is reproduced verbatim.
    /// </summary>
    private static readonly (string Code, string Column)[] ActualBasicPayColumnsByCode =
    {
        ("1000", "Basic Salary"),
        ("1002", "Transport Allowance"),
        ("1031", "Petrol Allowance"),
        ("1032", "Petrol Allowance(NP)"),
        ("1035", "Transport Allowance(NP)"),
        ("1069", "Additional Tasks"),
        ("1070", "Parking Allowance")
    };

    /// <summary>
    /// Legacy lines 792-849: the 56 earning/deduction/net columns, in legacy declaration order. Each name
    /// is BOTH the column caption and (see the row DTO remarks) the wage-type name matched against.
    /// </summary>
    private static readonly string[] EarningDeductionColumns =
    {
        // Earnings (legacy lines 794-833).
        "Basic Salary",
        "Basic Salary Arrear",
        "Transport Allowance",
        "Air Ticket Allowances",
        "Dependant 1 Air Ticket Pay",
        "Dependant 2 Air Ticket Pay",
        "Dependant 3 Air Ticket Pay",
        "Loan Pay",
        "Collection Commission",
        "Gift Allowance",
        "Petrol Allowance",
        "Petrol Allowance Arrear",
        "Petrol Allowance(NP)",
        "Transport Allowance(NP)",
        "Transport Allowance(NP) Arrear",
        "Additional Tasks",
        "Parking Allowance",
        "Parking Allowance Arrear",
        "Annual Leave Pay",
        "Annual Leave Pay Arrear",
        "Sick Leave Pay Arrear",
        "Paternity Leave Pay Arrear",
        "Accidental Leave Pay",
        "Accidental Leave Pay Arrear",
        "Insurance Claim Pay",
        "Designated Support Person Leave Pay Arrear",
        "Dental and Optical Allowance",
        "Service Bonus",
        "Quarterly Bonus",
        "Day in Lieu Leave Pay Arrear",
        "Other Additions",
        "Sales Commissions",
        "Mandub Commissions",
        "Previous Fund Adjustment",
        "Legal Commission",
        "Available Arrear",
        "Loan Recovery Adjust Pay",
        "Mobile Charges Recovery Adjustment",
        "Incentive Pay",

        // Deductions (legacy lines 836-848).
        "Advance Payment Ded",
        "Car Loan Ded",
        "Last Month Recovery Ded",
        "Mobile Charges Ded",
        "Mobile Charges Ded Arrear",
        "Advance Salary Reimbursement",
        "Employee PIFSS Ded",
        "Staff Fund Cleansing Ded",
        "Penalties",
        "Air Ticket Adjust Ded",
        "Basic Adjust Ded",
        "Staff Fund Deduction",
        "Fund Deduction Arrear",
        "Health Insurance Loan Recovery Ded",
        "Cash Loan Recovery Ded",
        "Cash Loan Recovery Ded Arrear",

        // Net (legacy line 849).
        NetPayColumnName
    };

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeBasicPay> _basicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PayrollSheetCustom023ReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeBasicPay> basicPayRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _basicPayRepository = basicPayRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _employeeBankRepository = employeeBankRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPayrollSheet_Custom023";

    protected override string ScreenCaption => "Payroll Sheet (Custom 023)";

    [HttpPost]
    public async Task<PayrollSheetCustom023ReportOutput> GetPayrollSheetCustom023Report(
        GetPayrollSheetCustom023ReportInput input)
    {
        input.Normalize();

        var periods = await ResolvePeriodsAsync(input);
        var query = await BuildQueryAsync(input, periods);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampDerivedValuesAsync(rows, input, periods, input.SkipCount);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildCustom023ParameterString(input, periods),
            totalCount);

        return new PayrollSheetCustom023ReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            ActualBasicPayColumns = ActualBasicPayColumnsByCode.Select(c => c.Column).ToList(),
            EarningDeductionColumns = EarningDeductionColumns.ToList()
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollSheetCustom023ReportToExcel(
        GetPayrollSheetCustom023ReportInput input)
    {
        input.Normalize();

        var periods = await ResolvePeriodsAsync(input);
        var query = await BuildQueryAsync(input, periods);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampDerivedValuesAsync(rows, input, periods, 0);

        return await ExportToFileAsync(
            "PayrollSheetCustom023.xlsx",
            rows,
            GetExportColumns(),
            BuildCustom023ParameterString(input, periods));
    }

    /// <summary>
    /// Single source of truth for filtering, shared by the paged read and the export. Ports the filter
    /// block legacy repeated inside its month loop (lines 884-905). Note the absence of an employee-code
    /// predicate -- unlike the generic sheet, this fork never had one.
    /// </summary>
    private async Task<IQueryable<PayrollSheetCustom023ReportRowDto>> BuildQueryAsync(
        GetPayrollSheetCustom023ReportInput input,
        List<DateTime> periods)
    {
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var query = ApplyFilters(_salaryRegisterDetailRepository.GetAll(), input, periods)
            // Legacy ApplyRowLevelSecurity (line 905) -- an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        // Legacy lines 907-986: group by employee inside each period.
        return query
            .GroupBy(d => new
            {
                d.SalaryRegister.EmployeeId,
                d.SalaryRegister.Employee.Code,
                d.SalaryRegister.Employee.FullNameEn,
                d.SalaryRegister.Employee.FullNameAr,
                d.SalaryRegister.Employee.CivilID,
                d.SalaryRegister.ForPeriod
            })
            .Select(g => new PayrollSheetCustom023ReportRowDto
            {
                // Legacy "Oid" column (line 1011): the EMPLOYEE id.
                Id = g.Key.EmployeeId,
                EmployeeCode = g.Key.Code,
                EmployeeName = useArabicNames ? g.Key.FullNameAr : g.Key.FullNameEn,
                CivilId = g.Key.CivilID,
                Period = g.Key.ForPeriod,
                WorkingDays = g.Max(d => d.WageType.Code == BasicSalaryWageTypeCode ? d.Number : 0)
            });
    }

    /// <summary>Ports legacy lines 886-905; re-applied by the stamping pass so the two agree exactly.</summary>
    private IQueryable<SalaryRegisterDetail> ApplyFilters(
        IQueryable<SalaryRegisterDetail> query,
        GetPayrollSheetCustom023ReportInput input,
        List<DateTime> periods)
    {
        var payrollRunType = input.PayrollRunType;
        var reasonTypeId = input.PayrollReasonTypeId;
        var payrollAreaId = input.PayrollAreaId;

        return query
            // Legacy lines 630-631 / 886-887: one of the requested months, and an in-period run only.
            .Where(d => periods.Contains(d.SalaryRegister.ForPeriod)
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod)
            .WhereIf(payrollRunType.HasValue, d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            .WhereIf(reasonTypeId.HasValue, d => d.SalaryRegister.PayrollReasonTypeId == reasonTypeId.Value)
            // Legacy lines 896-903: assignment current AS OF THE ROW'S PERIOD on the selected payroll area.
            .WhereIf(payrollAreaId.HasValue, d => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= d.SalaryRegister.ForPeriod
                     && o.EndDate >= d.SalaryRegister.ForPeriod
                     && o.PayrollAreaId == payrollAreaId.Value));
    }

    /// <summary>
    /// Phase two of materialization: fills every column no SQL projection can produce. Called identically
    /// by the paged read and the export, so the two can never disagree. Ports legacy lines 907-1190.
    /// </summary>
    private async Task StampDerivedValuesAsync(
        List<PayrollSheetCustom023ReportRowDto> rows,
        GetPayrollSheetCustom023ReportInput input,
        List<DateTime> periods,
        int skipCount)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var useArabicNames = UseArabicNames;
        var today = Today;
        var fromPeriod = periods.First();
        var employeeIds = rows.Select(r => r.Id).Distinct().ToList();
        var rowPeriods = rows.Select(r => r.Period).Distinct().ToList();

        var registers = await ApplyFilters(_salaryRegisterDetailRepository.GetAll(), input, periods)
            .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new
            {
                d.SalaryRegister.EmployeeId,
                d.SalaryRegister.ForPeriod,
                d.SalaryRegister.PayrollRunType,
                // Legacy line 947 hard-coded NameEn for the reason type.
                ReasonTypeName = d.SalaryRegister.PayrollReasonType == null
                    ? string.Empty
                    : d.SalaryRegister.PayrollReasonType.NameEn,
                d.SalaryRegister.OffCyclePaymentDate
            })
            .Distinct()
            .ToListAsync();

        var registerByKey = registers
            .GroupBy(r => (r.EmployeeId, r.ForPeriod))
            .ToDictionary(g => g.Key, g => g.First());

        var assignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId))
            .Select(o => new Custom023AssignmentInfo
            {
                EmployeeId = o.EmployeeId,
                StartDate = o.StartDate,
                EndDate = o.EndDate,
                Id = o.Id,
                OrganizationUnitId = o.OrganizationUnitId,
                PayrollAreaName = o.PayrollArea == null ? string.Empty : o.PayrollArea.Name,
                JobTitle = o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn),
                WorkLocation = o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn),
                CostCenterCode = o.CostCenter == null ? string.Empty : o.CostCenter.Code,
                CostCenterName = o.CostCenter == null
                    ? string.Empty
                    : (useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn)
            })
            .ToListAsync();

        var orgLevelLookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        var basicPays = await _basicPayRepository.GetAll()
            .Where(b => employeeIds.Contains(b.EmployeeId))
            .Select(b => new { b.Id, b.EmployeeId, b.StartDate, b.EndDate, b.Amount })
            .ToListAsync();

        var basicPayIds = basicPays.Select(b => b.Id).ToList();

        // Legacy lines 1078-1088 keyed the "Act" cells on the wage type CODE.
        var basicPayDetails = await _basicPayDetailRepository.GetAll()
            .Where(d => basicPayIds.Contains(d.EmployeeBasicPayId))
            .Select(d => new
            {
                d.EmployeeBasicPayId,
                WageTypeCode = d.WageType.Code,
                d.Amount,
                CurrencyShortForm = d.Currency == null
                    ? string.Empty
                    : (useArabicNames ? d.Currency.CurrencyShortFormAr : d.Currency.CurrencyShortFormEn),
                d.WageType.PayrollOrder
            })
            .ToListAsync();

        var detailsByBasicPay = basicPayDetails
            .GroupBy(d => d.EmployeeBasicPayId)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.PayrollOrder).ToList());

        var banks = await _employeeBankRepository.GetAll()
            .Where(b => employeeIds.Contains(b.EmployeeId))
            .Select(b => new
            {
                b.EmployeeId,
                b.StartDate,
                b.EndDate,
                b.Id,
                BankCode = b.Bank == null ? string.Empty : b.Bank.Code,
                b.PaymentType,
                b.IBAN,
                b.AccountNum,
                PayerBank = b.PayerBank == null
                    ? string.Empty
                    : b.PayerBank.Code + "-" + b.PayerBank.NameEn
            })
            .ToListAsync();

        var sponsors = await _employeeSponsorRepository.GetAll()
            .Where(s => employeeIds.Contains(s.EmployeeId))
            .Select(s => new
            {
                s.EmployeeId,
                s.StartDate,
                s.EndDate,
                s.Id,
                NameOfSponsor = s.Sponsor == null
                    ? string.Empty
                    : (useArabicNames ? s.Sponsor.NameAr : s.Sponsor.NameEn),
                MosalId = s.Sponsor == null ? 0L : s.Sponsor.MOSALID
            })
            .ToListAsync();

        // Legacy lines 1121-1185 switched on the wage type Oid; this port matches the NAME instead --
        // see the row DTO remarks. NameEn is used regardless of culture because the 56 captions are the
        // English wage-type names legacy printed.
        var wageCells = await ApplyFilters(_salaryRegisterDetailRepository.GetAll(), input, periods)
            .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId)
                        && rowPeriods.Contains(d.SalaryRegister.ForPeriod))
            .Select(d => new
            {
                d.SalaryRegister.EmployeeId,
                d.SalaryRegister.ForPeriod,
                WageTypeName = d.WageType.NameEn,
                d.Amount
            })
            .ToListAsync();

        var wageCellsByKey = wageCells
            .GroupBy(c => (c.EmployeeId, c.ForPeriod))
            .ToDictionary(g => g.Key, g => g.ToList());

        var actualBasicPayColumnByCode = ActualBasicPayColumnsByCode
            .ToDictionary(c => c.Code, c => c.Column);
        var earningDeductionColumnSet = EarningDeductionColumns.ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            row.SerialNumber = skipCount + i + 1;
            row.MonthYear = row.Period.ToString("MMM-yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

            if (registerByKey.TryGetValue((row.Id, row.Period), out var register))
            {
                row.PayrollRunType = register.PayrollRunType.ToString();
                row.PayrollReasonType = register.ReasonTypeName;
                row.OffCyclePaymentDate = register.OffCyclePaymentDate;
            }

            // Legacy lines 916-917/933: PayrollArea, JobTitle and WorkLocation are the Employee.Current*
            // properties, i.e. the assignment current AS OF TODAY.
            var currentAssignment = PickAssignment(assignments, row.Id, today);
            row.PayrollAreaName = currentAssignment?.PayrollAreaName ?? string.Empty;
            row.JobTitle = currentAssignment?.JobTitle ?? string.Empty;
            row.WorkLocation = currentAssignment?.WorkLocation ?? string.Empty;

            // Legacy line 932: the cost centre comes from the assignment current at the ROW'S period.
            var periodAssignment = PickAssignment(assignments, row.Id, row.Period);
            row.CostCenterCode = periodAssignment?.CostCenterCode ?? string.Empty;
            row.CostCenterName = periodAssignment?.CostCenterName ?? string.Empty;

            // Legacy lines 632-634: empOrgAss is built once for the FROM month-end, so every row of a
            // multi-month range shows the org levels of the first month. Reproduced.
            var fromPeriodAssignment = PickAssignment(assignments, row.Id, fromPeriod);
            var levels = orgLevelLookup.GetLevelNames(fromPeriodAssignment?.OrganizationUnitId);
            row.OrgLevel1 = levels[0];
            row.OrgLevel2 = levels[1];
            row.OrgLevel3 = levels[2];
            row.OrgLevel4 = levels[3];
            row.OrgLevel5 = levels[4];
            row.OrgLevel6 = levels[5];
            row.OrgLevel7 = levels[6];

            var bank = banks
                .Where(b => b.EmployeeId == row.Id && b.StartDate <= row.Period && b.EndDate >= row.Period)
                .OrderBy(b => b.StartDate)
                .ThenBy(b => b.Id)
                .FirstOrDefault();

            row.SalaryVia = ChequeSalaryVia;
            if (bank != null)
            {
                // Legacy PaymentType.BANK; this port's enum spells the same member Bank.
                if (bank.PaymentType == PaymentType.Bank)
                {
                    row.SalaryVia = bank.BankCode;
                }

                row.IbanNumber = bank.IBAN;
                row.PaymentType = bank.PaymentType.ToString();
                row.AccountNo = bank.AccountNum;
                row.PayerBank = bank.PayerBank;
            }

            var sponsor = sponsors
                    .Where(s => s.EmployeeId == row.Id && s.StartDate <= row.Period && s.EndDate >= row.Period)
                    .OrderBy(s => s.StartDate)
                    .ThenBy(s => s.Id)
                    .FirstOrDefault()
                ?? sponsors
                    .Where(s => s.EmployeeId == row.Id)
                    .OrderByDescending(s => s.EndDate)
                    .ThenBy(s => s.Id)
                    .FirstOrDefault();

            if (sponsor != null)
            {
                row.MosalId = sponsor.MosalId;
                row.NameOfSponsor = sponsor.NameOfSponsor;
            }

            var basicPay = basicPays
                .Where(b => b.EmployeeId == row.Id && b.StartDate <= row.Period && b.EndDate >= row.Period)
                .OrderBy(b => b.StartDate)
                .ThenBy(b => b.Id)
                .FirstOrDefault();

            if (basicPay != null && detailsByBasicPay.TryGetValue(basicPay.Id, out var details))
            {
                foreach (var detail in details)
                {
                    if (detail.WageTypeCode != null
                        && actualBasicPayColumnByCode.TryGetValue(detail.WageTypeCode, out var column))
                    {
                        row.ActualBasicPayAmounts[column] = detail.Amount;
                    }
                }

                // Legacy lines 1090-1096: the first detail line that carries a currency wins.
                row.Currency = details
                    .Select(d => d.CurrencyShortForm)
                    .FirstOrDefault(c => !string.IsNullOrEmpty(c)) ?? string.Empty;
            }

            row.TotalSalary = basicPay?.Amount ?? 0;

            if (wageCellsByKey.TryGetValue((row.Id, row.Period), out var cells))
            {
                foreach (var cell in cells)
                {
                    if (cell.WageTypeName == null || !earningDeductionColumnSet.Contains(cell.WageTypeName))
                    {
                        // Legacy: an Oid with no case label simply fell through and wrote nothing.
                        continue;
                    }

                    // Legacy overwrote the cell on every match, so the LAST line of a repeated wage type
                    // won; the register carries one line per wage type, making this moot in practice.
                    row.WageAmounts[cell.WageTypeName] = cell.Amount;
                }
            }

            // Legacy line 1188.
            var netPay = row.WageAmounts.TryGetValue(NetPayColumnName, out var netPayAmount)
                ? netPayAmount
                : 0;

            row.Difference = row.TotalSalary - netPay;
        }
    }

    /// <summary>
    /// Legacy lines 516-560: Month/Year become the month END (blank meaning the current month/year), and
    /// the TO half counts only when <c>ShowPeriodInPayrollSheet</c> is on. The two range guards become
    /// <see cref="UserFriendlyException"/>s.
    ///
    /// LEGACY DEFECT NOT REPRODUCED (lines 557-561): legacy caught its OWN guard exceptions and silently
    /// reset both dates to 1900-01-31, so exceeding the month limit produced an EMPTY sheet with no
    /// message rather than an error. Swallowing the operator's mistake is a defect worth fixing, so the
    /// guards surface here.
    /// </summary>
    private async Task<List<DateTime>> ResolvePeriodsAsync(GetPayrollSheetCustom023ReportInput input)
    {
        var fromMonth = input.Month ?? Today.Month;
        var fromYear = input.Year ?? Today.Year;
        var start = MonthEnd(fromYear, fromMonth);

        var reportSettings = await SysSettingsAccessor.GetReportSettingsAsync();
        var showPeriod = reportSettings != null && reportSettings.ShowPeriodInPayrollSheet;

        if (!showPeriod)
        {
            return new List<DateTime> { start };
        }

        var end = MonthEnd(input.ToYear ?? fromYear, input.ToMonth ?? fromMonth);

        if (start > end)
        {
            throw new UserFriendlyException(L("PayrollSheetFromPeriodMustNotBeAfterToPeriod"));
        }

        var totalMonths = ((end.Year - start.Year) * 12) + (end.Month - start.Month) + 1;
        if (reportSettings.PeriodMonthLimitInPayrollSheet != 0
            && reportSettings.PeriodMonthLimitInPayrollSheet < totalMonths)
        {
            throw new UserFriendlyException(
                L("PayrollSheetAllowedMonths", reportSettings.PeriodMonthLimitInPayrollSheet));
        }

        var periods = new List<DateTime>();
        var cursor = new DateTime(start.Year, start.Month, 1);
        var last = new DateTime(end.Year, end.Month, 1);
        while (cursor <= last)
        {
            periods.Add(MonthEnd(cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(1);
        }

        return periods;
    }

    private static DateTime MonthEnd(int year, int month)
    {
        return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
    }

    private static Custom023AssignmentInfo PickAssignment(
        List<Custom023AssignmentInfo> assignments,
        int employeeId,
        DateTime asOf)
    {
        return assignments
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= asOf && a.EndDate >= asOf)
            .OrderBy(a => a.StartDate)
            .ThenBy(a => a.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Ports legacy <c>GetParameterStr</c> (lines 594-608). Legacy joined the parts with newlines and
    /// logged the FROM boxes again under the TO labels; this port uses the family's " | " separator and
    /// the real TO values, for the same reasons given on the generic Payroll Sheet.
    /// </summary>
    private string BuildCustom023ParameterString(
        GetPayrollSheetCustom023ReportInput input,
        List<DateTime> periods)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        if (input.PayrollReasonTypeId.HasValue)
        {
            parts.Add("Reason Type : " + input.PayrollReasonTypeId.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        if (periods.Count > 1)
        {
            var last = periods.Last();
            parts.Add("TO Month : " + last.Month);
            parts.Add("TO Year : " + last.Year);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The exported column set, in legacy declaration order (lines 745-851). The wage-type columns use
    /// <c>FromLiteralHeader</c>: they are this customer's wage-type names, not localization keys.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<PayrollSheetCustom023ReportRowDto>> GetExportColumns()
    {
        var columns = new List<GridReportColumn<PayrollSheetCustom023ReportRowDto>>
        {
            new("SrNo", r => r.SerialNumber),
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("PayrollArea", r => r.PayrollAreaName),
            new("MonthYear", r => r.MonthYear),
            new("RunType", r => r.PayrollRunType),
            new("ReasonType", r => r.PayrollReasonType),
            new("OffCyclePaymentDate", r => r.OffCyclePaymentDate),
            new("JobTitle", r => r.JobTitle),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkLocation", r => r.WorkLocation),
            new("SalaryVia", r => r.SalaryVia),
            new("Currency", r => r.Currency),
            new("MOSALID", r => r.MosalId),
            new("NameOfSponsor", r => r.NameOfSponsor),
            new("IBANNumber", r => r.IbanNumber),
            new("CivilId", r => r.CivilId),
            new("CostCenterCode", r => r.CostCenterCode),
            new("CostCenter", r => r.CostCenterName),
            new("PaymentType", r => r.PaymentType),
            new("AccountNo", r => r.AccountNo),
            new("PayerBank", r => r.PayerBank)
        };

        foreach (var (_, column) in ActualBasicPayColumnsByCode)
        {
            var key = column;
            columns.Add(GridReportColumn<PayrollSheetCustom023ReportRowDto>.FromLiteralHeader(
                "Act " + key,
                r => r.ActualBasicPayAmounts.TryGetValue(key, out var value) ? value : 0));
        }

        columns.Add(new GridReportColumn<PayrollSheetCustom023ReportRowDto>(
            "TotalSalary", r => r.TotalSalary));
        columns.Add(new GridReportColumn<PayrollSheetCustom023ReportRowDto>(
            "WorkingDays", r => r.WorkingDays));

        foreach (var name in EarningDeductionColumns)
        {
            var key = name;
            columns.Add(GridReportColumn<PayrollSheetCustom023ReportRowDto>.FromLiteralHeader(
                key,
                r => r.WageAmounts.TryGetValue(key, out var value) ? value : 0));
        }

        columns.Add(new GridReportColumn<PayrollSheetCustom023ReportRowDto>(
            "Difference", r => r.Difference));

        return columns;
    }

    /// <summary>The org-assignment fields this report reads; see the generic Payroll Sheet for the rationale.</summary>
    private class Custom023AssignmentInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int Id { get; set; }

        public long? OrganizationUnitId { get; set; }

        public string PayrollAreaName { get; set; }

        public string JobTitle { get; set; }

        public string WorkLocation { get; set; }

        public string CostCenterCode { get; set; }

        public string CostCenterName { get; set; }
    }
}
