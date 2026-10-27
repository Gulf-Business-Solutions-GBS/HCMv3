using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Salary Deduction grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryDeduction.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 530-822).
///
/// NOT PORTED -- DevExpress/WebForms plumbing: drill-down session stack, ColumnChooser JSON, callback-panel
/// state machine, PDF export, the dead <c>Table1_Old()</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_SalaryDeduction)]
public class SalaryDeductionReportAppService : GridReportAppServiceBase, ISalaryDeductionReportAppService
{
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;

    public SalaryDeductionReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptSalaryDeduction";

    protected override string ScreenCaption => "Salary Deduction";

    [HttpPost]
    public async Task<SalaryDeductionReportOutput> GetSalaryDeductionReport(GetSalaryDeductionReportInput input)
    {
        input.Normalize();

        var (rows, earningColumns, deductionColumns) = await BuildRowsAsync(input);

        var totalCount = rows.Count;
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();
        var paged = ordered.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new SalaryDeductionReportOutput
        {
            TotalCount = totalCount,
            Items = paged,
            EarningColumns = earningColumns,
            DeductionColumns = deductionColumns
        };
    }

    [HttpPost]
    public async Task<FileDto> GetSalaryDeductionReportToExcel(GetSalaryDeductionReportInput input)
    {
        input.Normalize();

        var (rows, earningColumns, deductionColumns) = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        var columns = new List<GridReportColumn<SalaryDeductionReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("PayrollMonth", r => r.PayrollMonth),
            new("ActualBasicSalary", r => r.ActualBasicSalary),
            new("ActualAllowances", r => r.ActualAllowances),
            new("ActualTotalSalary", r => r.ActualTotalSalary),
            new("MaxAllowedDeduction", r => r.MaxAllowedDeduction),
            new("WorkingDays", r => r.WorkingDays)
        };

        foreach (var name in earningColumns.Concat(deductionColumns))
        {
            var wtName = name;
            columns.Add(GridReportColumn<SalaryDeductionReportRowDto>.FromLiteralHeader(
                wtName, r => r.WageTypeAmounts.TryGetValue(wtName, out var v) ? v : 0));
        }

        columns.Add(new GridReportColumn<SalaryDeductionReportRowDto>("TotalEarning", r => r.TotalEarning));
        columns.Add(new GridReportColumn<SalaryDeductionReportRowDto>("TotalDeduction", r => r.TotalDeduction));
        columns.Add(new GridReportColumn<SalaryDeductionReportRowDto>("Difference", r => r.Difference));
        columns.Add(new GridReportColumn<SalaryDeductionReportRowDto>("Narration", r => r.Narration));

        return await ExportToFileAsync("SalaryDeduction.xlsx", ordered, columns, BuildParameterString(input));
    }

    private async Task<(List<SalaryDeductionReportRowDto> Rows, List<string> EarningColumns, List<string> DeductionColumns)>
        BuildRowsAsync(GetSalaryDeductionReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var query = _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == endDate
                        && d.SalaryRegister.InPeriod == endDate
                        && !d.WageType.DontImpactNetPay);

        if (rlsEmployeeIds.Count > 0)
        {
            query = query.Where(d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));
        }

        if (input.PayrollAreaId.HasValue)
        {
            var payrollAreaId = input.PayrollAreaId.Value;
            query = query.Where(d => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= endDate
                     && o.EndDate >= endDate
                     && o.PayrollAreaId == payrollAreaId));
        }

        var salaryData = await query
            .Select(d => new
            {
                d.SalaryRegister.EmployeeId,
                Code = d.SalaryRegister.Employee.Code,
                NameEn = d.SalaryRegister.Employee.FullNameEn,
                NameAr = d.SalaryRegister.Employee.FullNameAr,
                WageTypeCode = d.WageType.Code,
                WageTypeNameEn = d.WageType.NameEn,
                WageTypeNameAr = d.WageType.NameAr,
                WageTypeBillingType = d.WageType.BillingType,
                WageTypePayrollOrder = d.WageType.PayrollOrder,
                d.Number,
                d.Amount
            })
            .ToListAsync();

        if (salaryData.Count == 0)
        {
            return (new List<SalaryDeductionReportRowDto>(), new List<string>(), new List<string>());
        }

        var employeeIds = salaryData.Select(d => d.EmployeeId).Distinct().ToList();

        var basicPayData = await _basicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate <= endDate
                        && d.EmployeeBasicPay.EndDate >= endDate
                        && employeeIds.Contains(d.EmployeeBasicPay.EmployeeId))
            .Select(d => new { EmployeeId = d.EmployeeBasicPay.EmployeeId, Code = d.WageType.Code, d.WageType.BillingType, d.Amount })
            .ToListAsync();

        string WtName(string en, string ar) => useArabicNames ? (ar ?? en) : en;

        var earningWageList = salaryData
            .Where(d => d.WageTypeBillingType == BillingType.Earning && d.WageTypeCode != "9000")
            .Select(d => new { d.WageTypeCode, Name = WtName(d.WageTypeNameEn, d.WageTypeNameAr), d.WageTypePayrollOrder })
            .Distinct()
            .OrderBy(w => w.WageTypePayrollOrder)
            .ToList();

        var deductionWageList = salaryData
            .Where(d => d.WageTypeBillingType == BillingType.Deduction)
            .Select(d => new { d.WageTypeCode, Name = WtName(d.WageTypeNameEn, d.WageTypeNameAr), d.WageTypePayrollOrder })
            .Distinct()
            .OrderBy(w => w.WageTypePayrollOrder)
            .ToList();

        var rows = new List<SalaryDeductionReportRowDto>();

        foreach (var g in salaryData.GroupBy(d => d.EmployeeId).OrderBy(g => g.First().Code))
        {
            var empBasicPay = basicPayData.Where(d => d.EmployeeId == g.Key).ToList();
            var actualBasic = empBasicPay.Where(d => d.Code == "1000").Sum(d => d.Amount);
            var actualAllowance = empBasicPay.Where(d => d.BillingType == BillingType.Earning && d.Code != "1000").Sum(d => d.Amount);

            var workingDays = g.Where(d => d.WageTypeCode == "1000").Sum(d => d.Number);
            var totalEarning = g.Where(d => d.WageTypeBillingType == BillingType.Earning && d.WageTypeCode != "9000").Sum(d => d.Amount);
            var totalDeduction = g.Where(d => d.WageTypeBillingType == BillingType.Deduction).Sum(d => d.Amount);
            var maxAllowedDeduction = (actualBasic + actualAllowance) * 0.10;

            var wageAmounts = new Dictionary<string, double>();
            foreach (var wt in earningWageList.Concat(deductionWageList))
            {
                wageAmounts[wt.Name] = g.Where(d => d.WageTypeCode == wt.WageTypeCode).Sum(d => d.Amount);
            }

            var narration = string.Join(", ", g
                .Where(d => d.WageTypeBillingType == BillingType.Deduction)
                .Select(d => WtName(d.WageTypeNameEn, d.WageTypeNameAr) + " (" + d.Amount.ToString("N2") + " KD)"));

            rows.Add(new SalaryDeductionReportRowDto
            {
                Id = g.Key,
                EmployeeCode = g.First().Code,
                EmployeeName = useArabicNames ? g.First().NameAr : g.First().NameEn,
                PayrollMonth = endDate.ToString("MMM-yyyy"),
                ActualBasicSalary = actualBasic,
                ActualAllowances = actualAllowance,
                ActualTotalSalary = actualBasic + actualAllowance,
                MaxAllowedDeduction = maxAllowedDeduction,
                WorkingDays = workingDays,
                WageTypeAmounts = wageAmounts,
                TotalEarning = totalEarning,
                TotalDeduction = totalDeduction,
                Difference = maxAllowedDeduction - totalDeduction,
                Narration = narration
            });
        }

        return (rows, earningWageList.Select(w => w.Name).ToList(), deductionWageList.Select(w => w.Name).ToList());
    }

    private DateTime ResolveEndDate(GetSalaryDeductionReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        try
        {
            return new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Today;
        }
    }

    private string BuildParameterString(GetSalaryDeductionReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        return string.Join(" | ", parts);
    }
}
