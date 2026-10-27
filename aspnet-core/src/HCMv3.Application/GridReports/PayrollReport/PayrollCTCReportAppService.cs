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
/// Payroll CTC grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollCTC.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 547-886).
///
/// LEGACY DEFECT REPRODUCED, NOT DATA-LOSS: the base query is hard-pinned to <c>Employee.Status ==
/// Active</c> (line 663) and the optional <see cref="GridReportInputBase.EmployeeStatus"/> filter is then
/// applied ON TOP of that (line 671-673). Selecting any status other than Active therefore yields zero
/// rows, even though the panel offers the full enum. Reproduced verbatim; pinned by
/// <c>Should_Return_No_Rows_When_A_Non_Active_Status_Is_Selected_Reproducing_Legacy_Defect</c>.
///
/// NOT PORTED -- DevExpress/WebForms plumbing: drill-down session stack, ColumnChooser JSON, callback-panel
/// state machine, PDF export.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollCTC)]
public class PayrollCTCReportAppService : GridReportAppServiceBase, IPayrollCTCReportAppService
{
    private static readonly string[] SalaryWageCodes =
    {
        "1000", "7000", "1007", "1008", "1009", "1042", "7007", "7008", "7009", "7042",
        "1013", "7013", "1004", "1067", "3001", "3000", "3004", "3003"
    };

    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PayrollCTCReportAppService(
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPayrollCTC";

    protected override string ScreenCaption => "Payroll CTC";

    [HttpPost]
    public async Task<PagedResultDto<PayrollCTCReportRowDto>> GetPayrollCTCReport(GetPayrollCTCReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<PayrollCTCReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollCTCReportToExcel(GetPayrollCTCReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("PayrollCTC.xlsx", ordered, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<List<PayrollCTCReportRowDto>> BuildRowsAsync(GetPayrollCTCReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        // Legacy lines 682-691: blank Run Type combo defaults to Regular.
        var effectiveRunType = input.PayrollRunType ?? PayrollManagement.PayrollRunType.Regular;
        var employeeCodes = input.ResolveEmployeeCodes();

        var registerQuery = _salaryRegisterRepository.GetAll()
            // Legacy lines 663-666: hard-pinned to Active, see class remarks for the resulting defect.
            .Where(r => r.Employee.Status == PersonnelAdministration.EmployeeStatus.Active
                        && r.ForPeriod == r.InPeriod
                        && r.ForPeriod == endDate
                        && r.InPeriod == endDate)
            .WhereIf(input.EmployeeStatus.HasValue, r => r.Employee.Status == input.EmployeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            .Where(r => r.PayrollRunType == effectiveRunType)
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId));

        if (input.PayrollAreaId.HasValue)
        {
            var payrollAreaId = input.PayrollAreaId.Value;
            registerQuery = registerQuery.Where(r => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == r.EmployeeId && o.StartDate <= endDate && o.EndDate >= endDate && o.PayrollAreaId == payrollAreaId));
        }

        var registers = await registerQuery
            .Select(r => new { r.Id, r.EmployeeId, Code = r.Employee.Code, NameEn = r.Employee.FullNameEn, NameAr = r.Employee.FullNameAr, r.Employee.Status, r.Employee.JoiningDate, r.PayrollRunType })
            .ToListAsync();

        if (registers.Count == 0)
        {
            return new List<PayrollCTCReportRowDto>();
        }

        var registerIds = registers.Select(r => r.Id).ToList();
        var employeeIds = registers.Select(r => r.EmployeeId).Distinct().ToList();

        var details = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => registerIds.Contains(d.SalaryRegisterId) && SalaryWageCodes.Contains(d.WageType.Code))
            .Select(d => new Detail { SalaryRegisterId = d.SalaryRegisterId, Code = d.WageType.Code, Number = d.Number, Amount = d.Amount })
            .ToListAsync();

        var detailsByRegister = details.GroupBy(d => d.SalaryRegisterId).ToDictionary(g => g.Key, g => g.ToList());

        var basicPay = await _basicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate <= endDate
                        && d.EmployeeBasicPay.EndDate >= endDate
                        && employeeIds.Contains(d.EmployeeBasicPay.EmployeeId)
                        && (d.WageType.Code == "1000" || d.WageType.Code == "1039"))
            .Select(d => new BasicPayDetail { EmployeeId = d.EmployeeBasicPay.EmployeeId, Code = d.WageType.Code, Amount = d.Amount })
            .ToListAsync();

        var basicPayByEmployee = basicPay.GroupBy(d => d.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= Today && o.EndDate >= Today)
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, o.OrganizationUnitId, JobTitleNameEn = o.JobTitle.NameEn, JobTitleNameAr = o.JobTitle.NameAr, PayrollAreaName = o.PayrollArea.Name })
            .ToListAsync();

        var orgAssignmentByEmployee = orgAssignments
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.StartDate).ThenBy(o => o.Id).First());

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        return registers.Select(r =>
        {
            var det = detailsByRegister.TryGetValue(r.Id, out var detList) ? detList : new List<Detail>();

            double NumberOf(string code) => det.Where(d => d.Code == code).Sum(d => d.Number);
            double AmountOf(string code) => det.Where(d => d.Code == code).Sum(d => d.Amount);
            double AmountOfAny(params string[] codes) => det.Where(d => codes.Contains(d.Code)).Sum(d => d.Amount);
            double NumberOfAny(params string[] codes) => det.Where(d => codes.Contains(d.Code)).Sum(d => d.Number);

            var bp = basicPayByEmployee.TryGetValue(r.EmployeeId, out var bpList) ? bpList : new List<BasicPayDetail>();
            double BasicPayAmount(string code) => bp.Where(d => d.Code == code).Sum(d => d.Amount);

            orgAssignmentByEmployee.TryGetValue(r.EmployeeId, out var org);
            var levels = lookup.GetLevelNames(org?.OrganizationUnitId);

            return new PayrollCTCReportRowDto
            {
                Id = r.Id,
                EmployeeCode = r.Code,
                EmployeeName = r.NameEn,
                EmployeeNameAr = r.NameAr,
                Status = r.Status.ToString(),
                JoiningDate = r.JoiningDate,
                PayrollMonth = endDate.ToString("MMMM"),
                Year = endDate.Year,
                PayrollAreaName = org?.PayrollAreaName ?? string.Empty,
                PayrollRunTypeName = r.PayrollRunType.ToString(),
                JobTitle = org == null ? string.Empty : (useArabicNames ? org.JobTitleNameAr : org.JobTitleNameEn) ?? string.Empty,
                OrgLevel1 = levels[0],
                OrgLevel2 = levels[1],
                OrgLevel3 = levels[2],
                OrgLevel4 = levels[3],
                OrgLevel5 = levels[4],
                OrgLevel6 = levels[5],
                OrgLevel7 = levels[6],
                WorkingDays = NumberOf("1000"),
                ActBasicSalary = BasicPayAmount("1000"),
                ActFoodAllowance = BasicPayAmount("1039"),
                BasicSalary = AmountOf("1000"),
                ArrearsBasicSalary = AmountOf("7000"),
                OvertimeHours = NumberOfAny("1007", "1008", "1009", "1042"),
                OvertimeAmount = AmountOfAny("1007", "1008", "1009", "1042"),
                OvertimeAmountArrears = AmountOfAny("7007", "7008", "7009", "7042"),
                SickLeavePayDays = NumberOf("1013"),
                SickLeavePay = AmountOf("1013"),
                SickLeavePayArrear = AmountOf("7013"),
                AirTicketPay = AmountOf("1004"),
                OtherAdjust = AmountOf("1067"),
                LeaveProvisionAmount = AmountOf("3001"),
                IndemnityProvisionAmount = AmountOf("3000"),
                TotalLeaveProvisionAmount = AmountOf("3004"),
                TotalIndemnityProvisionAmount = AmountOf("3003")
            };
        }).OrderBy(r => r.EmployeeCode).ToList();
    }

    private DateTime ResolveEndDate(GetPayrollCTCReportInput input)
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

    private string BuildParameterString(GetPayrollCTCReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        parts.Add("Run Type : " + (input.PayrollRunType ?? PayrollManagement.PayrollRunType.Regular));
        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<PayrollCTCReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PayrollCTCReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Status", r => r.Status),
            new("JoiningDate", r => r.JoiningDate),
            new("Month", r => r.PayrollMonth),
            new("Year", r => r.Year),
            new("PayrollArea", r => r.PayrollAreaName),
            new("PayrollRunType", r => r.PayrollRunTypeName),
            new("JobTitle", r => r.JobTitle),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("WorkingDays", r => r.WorkingDays),
            new("ActBasicSalary", r => r.ActBasicSalary),
            new("ActFoodAllowance", r => r.ActFoodAllowance),
            new("BasicSalary", r => r.BasicSalary),
            new("ArrearsBasicSalary", r => r.ArrearsBasicSalary),
            new("OvertimeHours", r => r.OvertimeHours),
            new("OvertimeAmount", r => r.OvertimeAmount),
            new("OvertimeAmountArrears", r => r.OvertimeAmountArrears),
            new("SickLeavePayDays", r => r.SickLeavePayDays),
            new("SickLeavePay", r => r.SickLeavePay),
            new("SickLeavePayArrear", r => r.SickLeavePayArrear),
            new("AirTicketPay", r => r.AirTicketPay),
            new("OtherAdjust", r => r.OtherAdjust),
            new("LeaveProvisionAmount", r => r.LeaveProvisionAmount),
            new("IndemnityProvisionAmount", r => r.IndemnityProvisionAmount),
            new("TotalLeaveProvisionAmount", r => r.TotalLeaveProvisionAmount),
            new("TotalIndemnityProvisionAmount", r => r.TotalIndemnityProvisionAmount)
        };
    }

    private class Detail
    {
        public int SalaryRegisterId { get; set; }

        public string Code { get; set; }

        public double Number { get; set; }

        public double Amount { get; set; }
    }

    private class BasicPayDetail
    {
        public int EmployeeId { get; set; }

        public string Code { get; set; }

        public double Amount { get; set; }
    }
}
