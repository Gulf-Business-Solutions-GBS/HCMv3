using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Salary Retro Result grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryRetroResult.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 739-927; the near-duplicate <c>Table1_Old()</c>, lines 538-737, is dead code --
/// never called by <c>GetTable()</c> -- and additionally declares a "DifferenceWageType" column this port
/// does not carry, matching the LIVE method).
///
/// NOT PORTED -- DevExpress/WebForms plumbing: drill-down session stack, ColumnChooser JSON, callback-panel
/// state machine.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_SalaryRetroResult)]
public class SalaryRetroResultReportAppService : GridReportAppServiceBase, ISalaryRetroResultReportAppService
{
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public SalaryRetroResultReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    protected override string LegacyScreenName => "uscRptSalaryRetroResult";

    protected override string ScreenCaption => "Salary Retro Result";

    [HttpPost]
    public async Task<PagedResultDto<SalaryRetroResultReportRowDto>> GetSalaryRetroResultReport(
        GetSalaryRetroResultReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<SalaryRetroResultReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetSalaryRetroResultReportToExcel(GetSalaryRetroResultReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "SalaryRetroResult.xlsx", ordered, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<List<SalaryRetroResultReportRowDto>> BuildRowsAsync(GetSalaryRetroResultReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeCodes = input.ResolveEmployeeCodes();
        // Legacy line 595: the local `payrollRunType` defaults to Regular when the combo carries no value.
        var effectiveRunType = input.PayrollRunType ?? PayrollRunType.Regular;

        var query = _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == endDate
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.WageType.IncludeInArrearPay)
            .WhereIf(input.PayrollRunType.HasValue, d => d.SalaryRegister.PayrollRunType == input.PayrollRunType.Value)
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.SalaryRegister.Employee.Code))
            .WhereIf(input.BillingType.HasValue, d => d.WageType.BillingType == input.BillingType.Value)
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        if (input.PayrollAreaId.HasValue)
        {
            var payrollAreaId = input.PayrollAreaId.Value;
            query = query.Where(d => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= endDate
                     && o.EndDate >= endDate
                     && o.PayrollAreaId == payrollAreaId));
        }

        var employees = await query
            .Select(d => new { d.SalaryRegister.EmployeeId, Code = d.SalaryRegister.Employee.Code, NameEn = d.SalaryRegister.Employee.FullNameEn, NameAr = d.SalaryRegister.Employee.FullNameAr })
            .Distinct()
            .ToListAsync();

        if (employees.Count == 0)
        {
            return new List<SalaryRetroResultReportRowDto>();
        }

        var employeeIds = employees.Select(e => e.EmployeeId).ToList();

        var arrearWageTypes = await query
            .Select(d => new { d.SalaryRegister.EmployeeId, WageTypeId = d.WageTypeId, NameEn = d.WageType.NameEn, NameAr = d.WageType.NameAr, ArrearSourceWageTypeId = d.WageType.ArrearSourceWageTypeId })
            .Distinct()
            .ToListAsync();

        var payrollAreaNames = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= endDate && o.EndDate >= endDate)
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, PayrollAreaName = o.PayrollArea.Name })
            .ToListAsync();

        var payrollAreaByEmployee = payrollAreaNames
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.StartDate).ThenBy(o => o.Id).First().PayrollAreaName ?? string.Empty);

        var retroDetails = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId)
                        && d.SalaryRegister.InPeriod == endDate
                        && d.SalaryRegister.ForPeriod != d.SalaryRegister.InPeriod
                        && d.SalaryRegister.PayrollRunType == effectiveRunType)
            .Select(d => new { d.SalaryRegister.EmployeeId, d.SalaryRegister.ForPeriod, d.WageTypeId, d.Rate, d.Number, d.Amount })
            .ToListAsync();

        var regularDetails = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId)
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.SalaryRegister.PayrollRunType == PayrollRunType.Regular)
            .Select(d => new { d.SalaryRegister.EmployeeId, d.SalaryRegister.ForPeriod, d.WageTypeId, d.Rate, d.Number, d.Amount })
            .ToListAsync();

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var decimals = payrollSettings?.DecimalInSalaryRegister ?? 3;

        var rows = new List<SalaryRetroResultReportRowDto>();
        var rowId = 0;

        foreach (var emp in employees)
        {
            var empArrearWageTypes = arrearWageTypes.Where(w => w.EmployeeId == emp.EmployeeId);
            var empRetroPeriods = retroDetails.Where(r => r.EmployeeId == emp.EmployeeId).ToList();

            foreach (var arrearWageType in empArrearWageTypes)
            {
                if (arrearWageType.ArrearSourceWageTypeId == null)
                {
                    continue;
                }

                foreach (var retro in empRetroPeriods.Select(r => r.ForPeriod).Distinct())
                {
                    var retroDetail = empRetroPeriods.FirstOrDefault(
                        r => r.ForPeriod == retro && r.WageTypeId == arrearWageType.ArrearSourceWageTypeId);
                    var regularDetail = regularDetails.FirstOrDefault(
                        r => r.EmployeeId == emp.EmployeeId && r.ForPeriod == retro && r.WageTypeId == arrearWageType.ArrearSourceWageTypeId);

                    var rateDiff = Math.Round((retroDetail?.Rate ?? 0) - (regularDetail?.Rate ?? 0), decimals);
                    var numberDiff = Math.Round((retroDetail?.Number ?? 0) - (regularDetail?.Number ?? 0), decimals);
                    var amountDiff = Math.Round((retroDetail?.Amount ?? 0) - (regularDetail?.Amount ?? 0), decimals);

                    if (rateDiff == 0 && numberDiff == 0 && amountDiff == 0)
                    {
                        continue;
                    }

                    rows.Add(new SalaryRetroResultReportRowDto
                    {
                        Id = ++rowId,
                        EmployeeCode = emp.Code,
                        EmployeeName = useArabicNames ? emp.NameAr : emp.NameEn,
                        PayrollAreaName = payrollAreaByEmployee.TryGetValue(emp.EmployeeId, out var pa) ? pa : string.Empty,
                        PayrollRunTypeName = effectiveRunType.ToString(),
                        CurrentMonthYear = endDate.ToString("MMM-yyyy"),
                        WageTypeName = useArabicNames ? arrearWageType.NameAr : arrearWageType.NameEn,
                        RetroMonthYear = retro.ToString("MMM-yyyy"),
                        RateRegular = regularDetail?.Rate ?? 0,
                        RateArrear = retroDetail?.Rate ?? 0,
                        RateDifference = rateDiff,
                        NumberRegular = regularDetail?.Number ?? 0,
                        NumberArrear = retroDetail?.Number ?? 0,
                        NumberDifference = numberDiff,
                        AmountRegular = regularDetail?.Amount ?? 0,
                        AmountArrear = retroDetail?.Amount ?? 0,
                        AmountDifference = amountDiff
                    });
                }
            }
        }

        return rows;
    }

    private DateTime ResolveEndDate(GetSalaryRetroResultReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return new DateTime(1900, 1, 31);
        }
    }

    private string BuildParameterString(GetSalaryRetroResultReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        parts.Add("Run Type : " + (input.PayrollRunType ?? PayrollRunType.Regular));
        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        if (input.BillingType.HasValue)
        {
            parts.Add("Billing Type : " + input.BillingType.Value);
        }

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<SalaryRetroResultReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<SalaryRetroResultReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Name", r => r.EmployeeName),
            new("PayrollArea", r => r.PayrollAreaName),
            new("PayrollRunType", r => r.PayrollRunTypeName),
            new("CurrentMonthYear", r => r.CurrentMonthYear),
            new("WageType", r => r.WageTypeName),
            new("RetroMonthYear", r => r.RetroMonthYear),
            new("RateRegular", r => r.RateRegular),
            new("RateArrear", r => r.RateArrear),
            new("RateDifference", r => r.RateDifference),
            new("NumberRegular", r => r.NumberRegular),
            new("NumberArrear", r => r.NumberArrear),
            new("NumberDifference", r => r.NumberDifference),
            new("AmountRegular", r => r.AmountRegular),
            new("AmountArrear", r => r.AmountArrear),
            new("AmountDifference", r => r.AmountDifference)
        };
    }
}
