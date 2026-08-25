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
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Indemnity Provision As On Date analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptIndemnityProvisionAsOnDate.ascx(.cs)</c>,
/// <c>Table1()</c> (lines 480-729).
///
/// The legacy "Indemnity Quota" figure (<c>Program.GetTotalIndemnityDays</c>) is ALREADY ported as
/// <see cref="IAccrualService.GetTotalIndemnityDaysAsync"/> -- this report calls it directly rather than
/// re-deriving indemnity days from contract/slab/unpaid-day master data, exactly the "reuse, don't
/// re-derive" guidance the wave brief gives for leave-quota balances.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_IndemnityProvisionAsOnDate)]
public class IndemnityProvisionAsOnDateReportAppService : GridReportAppServiceBase, IIndemnityProvisionAsOnDateReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeGratuityQuota> _employeeGratuityQuotaRepository;
    private readonly IRepository<EmployeeLoan> _employeeLoanRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly QuotaProvisionOrgContextResolver _orgContextResolver;
    private readonly IAccrualService _accrualService;

    public IndemnityProvisionAsOnDateReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeGratuityQuota> employeeGratuityQuotaRepository,
        IRepository<EmployeeLoan> employeeLoanRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        QuotaProvisionOrgContextResolver orgContextResolver,
        IAccrualService accrualService)
    {
        _employeeRepository = employeeRepository;
        _employeeGratuityQuotaRepository = employeeGratuityQuotaRepository;
        _employeeLoanRepository = employeeLoanRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _orgContextResolver = orgContextResolver;
        _accrualService = accrualService;
    }

    protected override string LegacyScreenName => "uscRptIndemnityProvisionAsOnDate";

    protected override string ScreenCaption => "Indemnity Provision As On Date";

    [HttpPost]
    public async Task<PagedResultDto<IndemnityProvisionAsOnDateReportRowDto>> GetIndemnityProvisionAsOnDateReport(GetIndemnityProvisionAsOnDateReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query.OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<IndemnityProvisionAsOnDateReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetIndemnityProvisionAsOnDateReportToExcel(GetIndemnityProvisionAsOnDateReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("IndemnityProvisionAsOnDate.xlsx", orderedRows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>.
    /// </summary>
    private async Task<List<IndemnityProvisionAsOnDateReportRowDto>> BuildRowsAsync(GetIndemnityProvisionAsOnDateReportInput input)
    {
        var asOnDate = input.ResolveFromDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var employeeQuery = _employeeRepository.GetAll()
            .WhereIf(input.EmployeeStatus.HasValue, e => e.Status == input.EmployeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn, e.Status, e.JoiningDate })
            .OrderBy(e => e.Code)
            .ToListAsync();
        var employeeIds = employees.Select(e => e.Id).ToList();

        var gratuityQuotas = await _employeeGratuityQuotaRepository.GetAll()
            .Where(q => employeeIds.Contains(q.EmployeeId) && q.EntitlementFrom <= asOnDate && q.EntitlementTo >= asOnDate)
            .Select(q => new { q.EmployeeId, q.DeductionFrom, q.DeductionTo })
            .ToListAsync();

        var loans = await _employeeLoanRepository.GetAll()
            .Where(l => employeeIds.Contains(l.EmployeeId) && l.LoanType.Code == "12")
            .GroupBy(l => l.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(l => l.DueAmount) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var separations = (await _employeeSeparationRepository.GetAll()
            .Where(s => employeeIds.Contains(s.EmployeeId) && s.RevokeDate == null)
            .Select(s => s.EmployeeId).Distinct().ToListAsync()).ToHashSet();

        var fnfPayments = (await _employeeFnFPaymentRepository.GetAll()
            .Where(f => employeeIds.Contains(f.EmployeeId))
            .Select(f => f.EmployeeId).Distinct().ToListAsync()).ToHashSet();

        var indemnityWageType = await _wageTypeRepository.GetAll().Where(w => w.Code == "3003").FirstOrDefaultAsync();
        var includeInIndemnityWageTypeIds = await _wageTypeRepository.GetAll().Where(w => w.IncludeInIndemnity).Select(w => w.Id).ToListAsync();

        var orgContexts = await _orgContextResolver.BuildAsync(employeeIds, asOnDate, useArabicNames);

        var rows = new List<IndemnityProvisionAsOnDateReportRowDto>();
        foreach (var employee in employees)
        {
            var orgContext = orgContexts.Get(employee.Id);

            var row = new IndemnityProvisionAsOnDateReportRowDto
            {
                Id = employee.Id,
                EmployeeCode = employee.Code,
                EmployeeName = employee.EmployeeName,
                Status = employee.Status.ToString(),
                OrgLevel1 = orgContext.OrgLevel1,
                OrgLevel2 = orgContext.OrgLevel2,
                OrgLevel3 = orgContext.OrgLevel3,
                OrgLevel4 = orgContext.OrgLevel4,
                OrgLevel5 = orgContext.OrgLevel5,
                OrgLevel6 = orgContext.OrgLevel6,
                OrgLevel7 = orgContext.OrgLevel7,
                JobTitleName = orgContext.JobTitleName,
                CostCenterName = orgContext.CostCenterName,
                CostCenterGroupName = orgContext.CostCenterGroupName,
                Separation = separations.Contains(employee.Id),
                FnfPayment = fnfPayments.Contains(employee.Id),
                AsOnDate = asOnDate
            };

            if (asOnDate > employee.JoiningDate)
            {
                var quota = gratuityQuotas.FirstOrDefault(q => q.EmployeeId == employee.Id);
                if (quota != null)
                {
                    row.DeductionStart = quota.DeductionFrom;
                    row.DeductionEnd = quota.DeductionTo;

                    var indemnityQuota = await _accrualService.GetTotalIndemnityDaysAsync(
                        employee.Id, asOnDate, employeeGroupId: null, employeeSubGroupId: orgContext.EmployeeSubGroupId, indemnityPayRuleId: null);
                    row.IndemnityQuota = indemnityQuota;

                    var latestRegister = await _salaryRegisterRepository.GetAll()
                        .Where(r => r.EmployeeId == employee.Id && r.InPeriod <= asOnDate && r.ForPeriod <= asOnDate)
                        .OrderByDescending(r => r.InPeriod).ThenByDescending(r => r.ForPeriod)
                        .Select(r => new { r.Id, r.ForPeriod })
                        .FirstOrDefaultAsync();

                    if (latestRegister != null)
                    {
                        var totalBasicPay = await _salaryRegisterDetailRepository.GetAll()
                            .Where(d => d.SalaryRegisterId == latestRegister.Id && includeInIndemnityWageTypeIds.Contains(d.WageTypeId))
                            .SumAsync(d => (double?)d.Rate) ?? 0;

                        row.IndemnityRate = totalBasicPay;

                        var provisionAmount = 0d;
                        if (indemnityWageType != null)
                        {
                            var payDate = latestRegister.ForPeriod;
                            var calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);

                            if (indemnityWageType.BillingMethod == BillingMethod.FixedDaysPerMonth && indemnityWageType.FixedDays != 0)
                            {
                                provisionAmount = totalBasicPay / indemnityWageType.FixedDays * indemnityQuota;
                            }
                            else if (indemnityWageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
                            {
                                provisionAmount = totalBasicPay / calendarDays * indemnityQuota;
                            }
                            else if (indemnityWageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
                            {
                                var weeklyOffDays = await _accrualService.GetActualWeekOffsAsync(employee.Id, payDate);
                                var salaryBasisDays = calendarDays - weeklyOffDays;
                                if (salaryBasisDays != 0)
                                {
                                    provisionAmount = totalBasicPay / salaryBasisDays * indemnityQuota;
                                }
                            }
                        }

                        row.IndemnityAmount = provisionAmount;
                    }

                    loans.TryGetValue(employee.Id, out var paidAmount);
                    row.PaidAmount = paidAmount;
                    row.BalanceAmount = row.IndemnityAmount - paidAmount;
                }
            }
            else
            {
                row.Comment = "AsOnDate is Less then Joining Date : " + employee.JoiningDate.ToString("dd-MM-yyyy");
            }

            rows.Add(row);
        }

        return rows;
    }

    private static IReadOnlyList<GridReportColumn<IndemnityProvisionAsOnDateReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<IndemnityProvisionAsOnDateReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Name", r => r.EmployeeName),
            new("Status", r => r.Status),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Jobtitle", r => r.JobTitleName),
            new("CostCenter", r => r.CostCenterName),
            new("CostCenterGroup", r => r.CostCenterGroupName),
            new("Separation", r => r.Separation),
            new("FnfPayment", r => r.FnfPayment),
            new("AsOnDate", r => r.AsOnDate),
            new("DeductionStart", r => r.DeductionStart),
            new("DeductionEnd", r => r.DeductionEnd),
            new("IndemnityRate", r => r.IndemnityRate),
            new("IndemnityQuota", r => r.IndemnityQuota),
            new("IndemnityAmount", r => r.IndemnityAmount),
            new("PaidAmount", r => r.PaidAmount),
            new("BalanceAmount", r => r.BalanceAmount),
            new("Comment", r => r.Comment)
        };
    }
}
