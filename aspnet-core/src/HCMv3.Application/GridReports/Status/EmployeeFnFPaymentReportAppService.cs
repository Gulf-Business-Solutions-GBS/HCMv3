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
using HCMv3.GridReports.Status.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee Full &amp; Final Payment analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeFnFPayment.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 459-662).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button,
/// and the dead <c>Table1_Old()</c> (lines 663-812).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeFnFPayment)]
public class EmployeeFnFPaymentReportAppService : GridReportAppServiceBase, IEmployeeFnFPaymentReportAppService
{
    private const string PublicHolidayAbsenceTypeCode = "9";

    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _employeeAbsenceQuotaRepository;

    public EmployeeFnFPaymentReportAppService(
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<EmployeeAbsenceQuota> employeeAbsenceQuotaRepository)
    {
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _employeeAbsenceQuotaRepository = employeeAbsenceQuotaRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeFnFPayment";

    protected override string ScreenCaption => "Employee Full & Final Payment";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeFnFPaymentReportRowDto>> GetEmployeeFnFPaymentReport(
        GetEmployeeFnFPaymentReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeFnFPaymentReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeFnFPaymentReportToExcel(GetEmployeeFnFPaymentReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "EmployeeFnFPayment.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeFnFPayment.ascx.cs:459-662).
    /// </summary>
    private async Task<IQueryable<EmployeeFnFPaymentReportRowDto>> BuildQueryAsync(
        GetEmployeeFnFPaymentReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var separationType = input.SeparationType;
        var today = Today;

        var query = _employeeFnFPaymentRepository.GetAll()
            // Legacy lines 522-524.
            .Where(f => f.CompensationDate >= fromDate && f.CompensationDate <= toDate)
            .WhereIf(employeeCodes.Count > 0, f => employeeCodes.Contains(f.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, f => rlsEmployeeIds.Contains(f.EmployeeId));

        if (separationType.HasValue)
        {
            // Legacy lines 526-536: employees with a separation submitted in the same window and of the
            // selected type.
            var separatedEmployeeIds = _employeeSeparationRepository.GetAll()
                .Where(s => s.SubmissionDate >= fromDate
                            && s.SubmissionDate <= toDate
                            && s.SeparationType == separationType.Value)
                .Select(s => s.EmployeeId);
            query = query.Where(f => separatedEmployeeIds.Contains(f.EmployeeId));
        }

        var basicPaysToday = _employeeBasicPayRepository.GetAll()
            .Where(p => p.StartDate.Date <= today && p.EndDate.Date >= today);

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(o => o.StartDate.Date <= today && o.EndDate.Date >= today);

        var absenceQuotas = _employeeAbsenceQuotaRepository.GetAll()
            .Where(q => q.AbsenceType.Code == PublicHolidayAbsenceTypeCode);

        return query.Select(f => new EmployeeFnFPaymentReportRowDto
        {
            Id = f.Id,
            EmployeeCode = f.Employee.Code,
            EmployeeName = UseArabicNames ? f.Employee.FullNameAr : f.Employee.FullNameEn,
            JoiningDate = f.Employee.JoiningDate,
            CompensationDate = f.CompensationDate,
            Salary = basicPaysToday
                .Where(p => p.EmployeeId == f.EmployeeId)
                .OrderBy(p => p.StartDate).ThenBy(p => p.Id)
                .Select(p => (double?)p.Amount)
                .FirstOrDefault() ?? 0,
            JobTitleName = currentOrgAssignments
                .Where(o => o.EmployeeId == f.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null ? string.Empty : (UseArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            WorkLocationName = currentOrgAssignments
                .Where(o => o.EmployeeId == f.EmployeeId)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null ? string.Empty : (UseArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            CountryName = currentOrgAssignments
                .Where(o => o.EmployeeId == f.EmployeeId && o.PayrollAreaId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea.Country == null ? null : (UseArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault(),
            // Legacy lines 611-613: the sponsor window covering the COMPENSATION date, not "today".
            ResidencyType = _employeeSponsorRepository.GetAll()
                .Where(s => s.EmployeeId == f.EmployeeId && s.StartDate <= f.CompensationDate && s.EndDate >= f.CompensationDate)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.ResidencyType)
                .FirstOrDefault(),
            PHBalance = absenceQuotas
                .Where(q => q.EmployeeId == f.EmployeeId)
                .OrderBy(q => q.Id)
                .Select(q => (double?)(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted + q.QuotaForfeited)))
                .FirstOrDefault() ?? 0,
            PayStatus = absenceQuotas
                .Where(q => q.EmployeeId == f.EmployeeId)
                .OrderBy(q => q.Id)
                .Select(q => (double?)q.QuotaUsed)
                .FirstOrDefault() ?? 0
        });
    }

    private static IReadOnlyList<GridReportColumn<EmployeeFnFPaymentReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeFnFPaymentReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("Salary", r => r.Salary),
            new("JoiningDate", r => r.JoiningDate),
            new("CompensationDate", r => r.CompensationDate),
            new("WorkLocation", r => r.WorkLocationName),
            new("ResidencyType", r => r.ResidencyType.ToString()),
            new("PHBalance", r => r.PHBalance),
            new("PayStatus", r => r.PayStatus),
            new("Country", r => r.CountryName)
        };
    }
}
