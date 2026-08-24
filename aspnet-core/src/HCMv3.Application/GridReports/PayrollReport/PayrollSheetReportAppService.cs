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
/// Payroll Sheet grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollSheet.ascx(.cs)</c>,
/// <c>Table1()</c>'s generic ("For ALL") branch, lines 2675-3348.
///
/// SCOPE: legacy <c>Table1()</c> is seven sheets in one method, switched on
/// <c>ManagePayrollSettings.ClientId</c> ("004", "007", "008", "019", "024", "026", else). Only the
/// <c>else</c> branch -- the sheet every client without a fork gets -- is ported here; see
/// <see cref="PayrollSheetReportRowDto"/> remarks for why the six customer forks are separate work.
///
/// SHAPE: one row per (employee, month-end period), with a DATA-DRIVEN column set -- one
/// <c>"Act {wage type}"</c> column per basic-pay wage type and one column (plus an optional
/// <c>"{wage type} Days"</c>) per earning/deduction wage type present in the filtered registers
/// (lines 2830-2846). The column lists travel with the rows in
/// <see cref="PayrollSheetReportOutput"/>.
///
/// MONTH RANGE: <c>ManageReportSettings.ShowPeriodInPayrollSheet</c> decides whether the TO Month/Year
/// half of the panel counts at all; when it does, <c>PeriodMonthLimitInPayrollSheet</c> caps the range
/// (lines 541-559).
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the (single-level, therefore dead)
/// drill-down stack, the ColumnChooser JSON persistence, the callback-panel state machine and the PDF
/// export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollSheet)]
public class PayrollSheetReportAppService : GridReportAppServiceBase, IPayrollSheetReportAppService
{
    /// <summary>Legacy line 2891: "WorkingDays" is the <c>Number</c> of the basic-salary line.</summary>
    private const string BasicSalaryWageTypeCode = "1000";

    /// <summary>
    /// Legacy line 2848: <c>string netPay = dt.Columns.Contains("Net Pay") ? "Net Pay" : "";</c> -- the
    /// Difference column is driven by a wage type whose NAME is literally this.
    /// </summary>
    private const string NetPayColumnName = "Net Pay";

    /// <summary>Legacy line 2926: the fallback when the employee is not paid through a bank.</summary>
    private const string ChequeSalaryVia = "Cheque";

    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeBasicPay> _basicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PayrollSheetReportAppService(
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeBasicPay> basicPayRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _basicPayRepository = basicPayRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _employeeBankRepository = employeeBankRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPayrollSheet";

    protected override string ScreenCaption => "Payroll Sheet";

    [HttpPost]
    public async Task<PayrollSheetReportOutput> GetPayrollSheetReport(GetPayrollSheetReportInput input)
    {
        input.Normalize();

        var periods = await ResolvePeriodsAsync(input);
        var query = await BuildQueryAsync(input, periods);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var columns = await StampDerivedValuesAsync(rows, input, periods, input.SkipCount);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildPayrollSheetParameterString(input, periods),
            totalCount);

        return new PayrollSheetReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            ActualBasicPayColumns = columns.ActualBasicPayColumns,
            EarningDeductionColumns = columns.EarningDeductionColumns,
            LeaveDaysColumns = columns.LeaveDaysColumns
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollSheetReportToExcel(GetPayrollSheetReportInput input)
    {
        input.Normalize();

        var periods = await ResolvePeriodsAsync(input);
        var query = await BuildQueryAsync(input, periods);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        var columns = await StampDerivedValuesAsync(rows, input, periods, 0);

        return await ExportToFileAsync(
            "PayrollSheet.xlsx",
            rows,
            GetExportColumns(columns),
            BuildPayrollSheetParameterString(input, periods));
    }

    /// <summary>
    /// Single source of truth for filtering, shared by the paged read and the export -- this is what
    /// guarantees the export honours exactly the same filters as the grid. Ports the filter block legacy
    /// repeated inside its month loop (lines 2851-2882).
    ///
    /// Only the columns a SQL projection can produce are selected here; everything else (the org, bank,
    /// sponsor, basic-pay and per-wage-type columns) is stamped on afterwards by
    /// <see cref="StampDerivedValuesAsync"/>, which both entry points also call.
    /// </summary>
    private async Task<IQueryable<PayrollSheetReportRowDto>> BuildQueryAsync(
        GetPayrollSheetReportInput input,
        List<DateTime> periods)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var query = ApplyFilters(_salaryRegisterDetailRepository.GetAll(), input, periods, employeeCodes)
            // Legacy ApplyRowLevelSecurity (line 2882) -- an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        // Legacy lines 2884-2895: group by employee inside each period.
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
            .Select(g => new PayrollSheetReportRowDto
            {
                // Legacy "Oid" column (line 2859): the EMPLOYEE id, not the register id.
                Id = g.Key.EmployeeId,
                EmployeeCode = g.Key.Code,
                EmployeeName = useArabicNames ? g.Key.FullNameAr : g.Key.FullNameEn,
                CivilId = g.Key.CivilID,
                Period = g.Key.ForPeriod,
                // Legacy line 2891: the Number of the wage type 1000 line.
                WorkingDays = g.Max(d => d.WageType.Code == BasicSalaryWageTypeCode ? d.Number : 0)
            });
    }

    /// <summary>
    /// The filter block itself, factored out because <see cref="StampDerivedValuesAsync"/> has to re-apply
    /// exactly the same predicates when it re-reads the registers and the wage-type column list.
    /// Ports legacy lines 2853-2882.
    /// </summary>
    private IQueryable<SalaryRegisterDetail> ApplyFilters(
        IQueryable<SalaryRegisterDetail> query,
        GetPayrollSheetReportInput input,
        List<DateTime> periods,
        List<string> employeeCodes)
    {
        var payrollRunType = input.PayrollRunType;
        var reasonTypeId = input.PayrollReasonTypeId;
        var payrollAreaId = input.PayrollAreaId;

        return query
            // Legacy lines 2676-2677 / 2853-2854: the register's period must be one of the requested
            // months AND the run must be an in-period one (ForPeriod == InPeriod), i.e. never a retro run.
            .Where(d => periods.Contains(d.SalaryRegister.ForPeriod)
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod)
            // Legacy lines 2856-2861.
            .WhereIf(payrollRunType.HasValue, d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            // Legacy lines 2863-2869.
            .WhereIf(reasonTypeId.HasValue, d => d.SalaryRegister.PayrollReasonTypeId == reasonTypeId.Value)
            // Legacy lines 2871-2879: the employee must have an org assignment current AS OF THE ROW'S
            // PERIOD on the selected payroll area.
            .WhereIf(payrollAreaId.HasValue, d => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= d.SalaryRegister.ForPeriod
                     && o.EndDate >= d.SalaryRegister.ForPeriod
                     && o.PayrollAreaId == payrollAreaId.Value))
            // Legacy lines 2881-2886 in the 004 branch / the same block here: comma-split employee codes.
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.SalaryRegister.Employee.Code));
    }

    /// <summary>
    /// Phase two of materialization: fills every column no SQL projection can produce and returns the
    /// data-driven column lists. Called identically by the paged read and the export, so the two can never
    /// disagree.
    ///
    /// The COLUMN LISTS are computed from the filtered data set as a whole, never from the current page --
    /// legacy built them once, before the row loop (lines 2683-2727), so a narrow page must not narrow the
    /// sheet.
    /// </summary>
    private async Task<PayrollSheetColumnSet> StampDerivedValuesAsync(
        List<PayrollSheetReportRowDto> rows,
        GetPayrollSheetReportInput input,
        List<DateTime> periods,
        int skipCount)
    {
        var useArabicNames = UseArabicNames;
        var columns = await BuildColumnSetAsync(input, periods, useArabicNames);

        if (rows.Count == 0)
        {
            return columns;
        }

        var today = Today;
        var fromPeriod = periods.First();
        var employeeIds = rows.Select(r => r.Id).Distinct().ToList();
        var rowPeriods = rows.Select(r => r.Period).Distinct().ToList();

        // --- registers (run type / reason type / off-cycle date), legacy lines 2880-2884 -------------
        var registers = await ApplyFilters(_salaryRegisterDetailRepository.GetAll(), input, periods, input.ResolveEmployeeCodes())
            .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new
            {
                d.SalaryRegister.EmployeeId,
                d.SalaryRegister.ForPeriod,
                d.SalaryRegister.InPeriod,
                d.SalaryRegister.PayrollRunType,
                // Legacy line 2884 hard-coded NameEn for the reason type, even in Arabic.
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

        // --- org assignments: three different "as of" dates, exactly as legacy read them ---------------
        var assignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId))
            .Select(o => new AssignmentInfo
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

        // --- basic pay (TotalSalary, Currency and the "Act" columns), legacy lines 2896-2902 ----------
        var basicPays = await _basicPayRepository.GetAll()
            .Where(b => employeeIds.Contains(b.EmployeeId))
            .Select(b => new { b.Id, b.EmployeeId, b.StartDate, b.EndDate, b.Amount })
            .ToListAsync();

        var basicPayIds = basicPays.Select(b => b.Id).ToList();

        var basicPayDetails = await _basicPayDetailRepository.GetAll()
            .Where(d => basicPayIds.Contains(d.EmployeeBasicPayId))
            .Select(d => new
            {
                d.EmployeeBasicPayId,
                d.WageTypeId,
                WageTypeName = useArabicNames
                    ? (d.WageType.NameAr ?? d.WageType.NameEn)
                    : d.WageType.NameEn,
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

        // --- banks and sponsors, legacy lines 2904-2918 -----------------------------------------------
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

        // --- the per-wage-type cells, legacy lines 2888-2890 / 3330-3340 ------------------------------
        var wageCells = await ApplyFilters(_salaryRegisterDetailRepository.GetAll(), input, periods, input.ResolveEmployeeCodes())
            .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId)
                        && rowPeriods.Contains(d.SalaryRegister.ForPeriod))
            .Select(d => new
            {
                d.SalaryRegister.EmployeeId,
                d.SalaryRegister.ForPeriod,
                WageTypeName = useArabicNames
                    ? (d.WageType.NameAr ?? d.WageType.NameEn)
                    : d.WageType.NameEn,
                d.Amount,
                d.Number
            })
            .ToListAsync();

        var wageCellsByKey = wageCells
            .GroupBy(c => (c.EmployeeId, c.ForPeriod))
            .ToDictionary(g => g.Key, g => g.ToList());

        var actualBasicPayColumnSet = columns.ActualBasicPayColumns.ToHashSet();
        var earningDeductionColumnSet = columns.EarningDeductionColumns.ToHashSet();
        var leaveDaysColumnSet = columns.LeaveDaysColumns.ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            // Legacy line 2856: SrNo is the position in the rendered table; with server paging that has
            // to be the position in the overall result, so the page offset is added back.
            row.SerialNumber = skipCount + i + 1;

            // Legacy line 2862: "MMM-yyyy" upper-cased, in the INVARIANT culture (legacy's ToString had no
            // culture argument but ran under an English UI thread for this column).
            row.MonthYear = row.Period.ToString("MMM-yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

            if (registerByKey.TryGetValue((row.Id, row.Period), out var register))
            {
                row.PayrollRunType = register.PayrollRunType.ToString();
                row.PayrollReasonType = register.ReasonTypeName;
                row.OffCyclePaymentDate = register.OffCyclePaymentDate;
            }

            // Legacy lines 2869-2870/2887: PayrollArea, JobTitle and WorkLocation came from the
            // Employee.Current* properties, i.e. the assignment current AS OF TODAY -- not as of the
            // reported period. Reproduced deliberately.
            var currentAssignment = PickAssignment(assignments, row.Id, today);
            row.PayrollAreaName = currentAssignment?.PayrollAreaName ?? string.Empty;
            row.JobTitle = currentAssignment?.JobTitle ?? string.Empty;
            row.WorkLocation = currentAssignment?.WorkLocation ?? string.Empty;

            // Legacy line 2886: the cost centre came from the assignment current at the ROW'S period.
            var periodAssignment = PickAssignment(assignments, row.Id, row.Period);
            row.CostCenterCode = periodAssignment?.CostCenterCode ?? string.Empty;
            row.CostCenterName = periodAssignment?.CostCenterName ?? string.Empty;

            // Legacy lines 2679-2681: empOrgAss was built ONCE for the FROM month-end, so every row of a
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

            // --- bank ---------------------------------------------------------------------------------
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

            // --- sponsor: the record current at the period, else the one with the LATEST EndDate -------
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

            // --- basic pay: TotalSalary, Currency and the "Act" cells ---------------------------------
            var basicPay = basicPays
                .Where(b => b.EmployeeId == row.Id && b.StartDate <= row.Period && b.EndDate >= row.Period)
                .OrderBy(b => b.StartDate)
                .ThenBy(b => b.Id)
                .FirstOrDefault();

            if (basicPay != null && detailsByBasicPay.TryGetValue(basicPay.Id, out var details))
            {
                foreach (var detail in details.Where(d => actualBasicPayColumnSet.Contains(d.WageTypeName)))
                {
                    row.ActualBasicPayAmounts[detail.WageTypeName] = detail.Amount;
                }

                // Legacy lines 2957-2963: the first detail line that carries a currency wins.
                row.Currency = details
                    .Select(d => d.CurrencyShortForm)
                    .FirstOrDefault(c => !string.IsNullOrEmpty(c)) ?? string.Empty;
            }

            row.TotalSalary = basicPay?.Amount ?? 0;

            // --- earning/deduction cells ---------------------------------------------------------------
            if (wageCellsByKey.TryGetValue((row.Id, row.Period), out var cells))
            {
                foreach (var cell in cells)
                {
                    if (cell.WageTypeName == null)
                    {
                        continue;
                    }

                    if (earningDeductionColumnSet.Contains(cell.WageTypeName))
                    {
                        // Legacy took FirstOrDefault() per wage type, so a second line of the same wage
                        // type in the same register was ignored; keeping the first has the same effect.
                        if (!row.WageAmounts.ContainsKey(cell.WageTypeName))
                        {
                            row.WageAmounts[cell.WageTypeName] = cell.Amount;
                        }
                    }

                    if (leaveDaysColumnSet.Contains(cell.WageTypeName)
                        && !row.WageDays.ContainsKey(cell.WageTypeName))
                    {
                        row.WageDays[cell.WageTypeName] = cell.Number;
                    }
                }
            }

            // Legacy line 3346: TotalSalary minus the "Net Pay" column, or minus nothing when no wage type
            // is named "Net Pay".
            var netPay = row.WageAmounts.TryGetValue(NetPayColumnName, out var netPayAmount)
                ? netPayAmount
                : 0;

            row.Difference = row.TotalSalary - netPay;
        }

        return columns;
    }

    /// <summary>
    /// Builds the three data-driven column lists. Ports legacy lines 2683-2727:
    /// <list type="bullet">
    /// <item><description>"Act" columns: EVERY wage type used by ANY employee basic pay, with no date
    /// filter at all (line 2686) -- master data, not the reported period.</description></item>
    /// <item><description>Earning/deduction columns: the wage types in <c>dsTemp</c>, which is the register
    /// set filtered ONLY by the month list and <c>ForPeriod == InPeriod</c> (line 2676) -- deliberately NOT
    /// by run type, reason type, payroll area, employee code or RLS. Legacy therefore shows a column even
    /// when the current filter leaves every cell of it empty; reproduced.</description></item>
    /// <item><description>Leave columns: those of the above that are also an
    /// <c>AbsenceType.WageTypeId</c>.</description></item>
    /// </list>
    /// </summary>
    private async Task<PayrollSheetColumnSet> BuildColumnSetAsync(
        GetPayrollSheetReportInput input,
        List<DateTime> periods,
        bool useArabicNames)
    {
        var actualBasicPayColumns = await _basicPayDetailRepository.GetAll()
            .Select(d => new
            {
                d.WageTypeId,
                Name = useArabicNames ? (d.WageType.NameAr ?? d.WageType.NameEn) : d.WageType.NameEn,
                d.WageType.PayrollOrder
            })
            .Distinct()
            .OrderBy(d => d.PayrollOrder)
            .ToListAsync();

        var earningDeduction = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => periods.Contains(d.SalaryRegister.ForPeriod)
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod)
            .Where(d => (d.WageType.BillingType == BillingType.Earning
                         || d.WageType.BillingType == BillingType.Deduction)
                        && !d.WageType.DontImpactNetPay
                        && (d.Rate != 0 || d.Number != 0 || d.Amount != 0))
            .Select(d => new
            {
                d.WageTypeId,
                Name = useArabicNames ? (d.WageType.NameAr ?? d.WageType.NameEn) : d.WageType.NameEn,
                d.WageType.PayrollOrder
            })
            .Distinct()
            .OrderBy(d => d.PayrollOrder)
            .ToListAsync();

        var leaveWageTypeIds = await _absenceTypeRepository.GetAll()
            .Where(a => a.WageTypeId != null)
            .Select(a => a.WageTypeId.Value)
            .Distinct()
            .ToListAsync();

        return new PayrollSheetColumnSet
        {
            ActualBasicPayColumns = actualBasicPayColumns
                .Select(c => c.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList(),
            EarningDeductionColumns = earningDeduction
                .Select(c => c.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList(),
            LeaveDaysColumns = earningDeduction
                .Where(c => leaveWageTypeIds.Contains(c.WageTypeId))
                .Select(c => c.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList()
        };
    }

    /// <summary>
    /// Legacy lines 518-559: Month/Year become the month END, and the TO half is honoured only when
    /// <c>ShowPeriodInPayrollSheet</c> is on -- otherwise the sheet covers the FROM month alone
    /// (<c>GetMonthYearList(StartDate, StartDate)</c>, line 670).
    ///
    /// Legacy's two range guards (lines 546-556) threw raw <c>Exception</c>s that the WebForms page showed
    /// as an error banner; they become <see cref="UserFriendlyException"/>s here so the same messages reach
    /// the user through the standard channel.
    /// </summary>
    private async Task<List<DateTime>> ResolvePeriodsAsync(GetPayrollSheetReportInput input)
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

        // Legacy SysService.GetMonthYearList: every month END from start to end, inclusive.
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

    /// <summary>The org assignment covering <paramref name="asOf"/>, oldest first -- legacy's FirstOrDefault.</summary>
    private static AssignmentInfo PickAssignment(
        List<AssignmentInfo> assignments,
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
    /// The org-assignment fields this report reads, as a named type so the one materialized list can be
    /// re-scanned for three different "as of" dates (today, the row's period, and the range's first month).
    /// </summary>
    private class AssignmentInfo
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

    /// <summary>
    /// Ports legacy <c>GetParameterStr</c> (lines 606-638): Payroll Area / Run Type / Reason Type /
    /// Month / Year, plus the TO pair when the period range is enabled.
    ///
    /// LEGACY DEFECT NOT REPRODUCED (lines 628-634): legacy logged the FROM boxes again under the
    /// "TO Month"/"TO Year" labels (<c>txtMonth.Text</c>, not <c>txtMonthTo.Text</c>), so the audit trail
    /// recorded the wrong range. That is a pure logging bug with no user-visible report effect and
    /// reproducing it would poison the audit log, so the real TO values are written here.
    /// </summary>
    private string BuildPayrollSheetParameterString(
        GetPayrollSheetReportInput input,
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

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The exported column set, in legacy column order (lines 2795-2847). The wage-type columns use
    /// <c>FromLiteralHeader</c> because their headers are master data, not localization keys.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<PayrollSheetReportRowDto>> GetExportColumns(
        PayrollSheetColumnSet columnSet)
    {
        var columns = new List<GridReportColumn<PayrollSheetReportRowDto>>
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

        foreach (var name in columnSet.ActualBasicPayColumns)
        {
            var key = name;
            columns.Add(GridReportColumn<PayrollSheetReportRowDto>.FromLiteralHeader(
                "Act " + key,
                r => r.ActualBasicPayAmounts.TryGetValue(key, out var value) ? value : 0));
        }

        columns.Add(new GridReportColumn<PayrollSheetReportRowDto>("TotalSalary", r => r.TotalSalary));
        columns.Add(new GridReportColumn<PayrollSheetReportRowDto>("WorkingDays", r => r.WorkingDays));

        foreach (var name in columnSet.EarningDeductionColumns)
        {
            var key = name;

            // Legacy line 2841: the " Days" column comes BEFORE the amount column.
            if (columnSet.LeaveDaysColumns.Contains(key))
            {
                columns.Add(GridReportColumn<PayrollSheetReportRowDto>.FromLiteralHeader(
                    key + " Days",
                    r => r.WageDays.TryGetValue(key, out var days) ? days : 0));
            }

            columns.Add(GridReportColumn<PayrollSheetReportRowDto>.FromLiteralHeader(
                key,
                r => r.WageAmounts.TryGetValue(key, out var value) ? value : 0));
        }

        columns.Add(new GridReportColumn<PayrollSheetReportRowDto>("Difference", r => r.Difference));

        return columns;
    }

    /// <summary>The three data-driven column lists, produced once and used by both the grid and the export.</summary>
    private class PayrollSheetColumnSet
    {
        public List<string> ActualBasicPayColumns { get; set; } = new();

        public List<string> EarningDeductionColumns { get; set; } = new();

        public List<string> LeaveDaysColumns { get; set; } = new();
    }
}
