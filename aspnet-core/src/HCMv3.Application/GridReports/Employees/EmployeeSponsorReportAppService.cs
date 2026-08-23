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
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Sponsor analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeSponsor.ascx(.cs)</c>, whose entire
/// data contract is <c>Table1()</c> (lines 459-650).
///
/// AS-ON-DATE report: the legacy markup hides <c>dtTo</c> and the sponsor record is the one current on
/// <c>dtFrom</c> (lines 551-553, where the previous <c>DateTime.Now</c> version is still visible in the
/// commented-out tail of each line). The CONTRACT block is separately as of <c>DateTime.Now</c>
/// (lines 574-575) -- that asymmetry is legacy's and is preserved.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack (single "Table1"), the ColumnChooser JSON persistence, the callback-panel state machine, the PDF
/// export button, and the dead <c>Table1_Old()</c> (lines 652+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeSponsor)]
public class EmployeeSponsorReportAppService : GridReportAppServiceBase, IEmployeeSponsorReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public EmployeeSponsorReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _employeeRepository = employeeRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _employeeContractRepository = employeeContractRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeSponsor";

    protected override string ScreenCaption => "Employee Sponsor";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSponsorReportRowDto>> GetEmployeeSponsorReport(
        GetEmployeeSponsorReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeSponsorReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeSponsorReportToExcel(GetEmployeeSponsorReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "EmployeeSponsor.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeSponsor.ascx.cs:459-650).
    /// </summary>
    private async Task<IQueryable<EmployeeSponsorReportRowDto>> BuildQueryAsync(
        GetEmployeeSponsorReportInput input)
    {
        // The single "As On" date. ToDate is deliberately not read -- legacy hides dtTo on this screen.
        var asOnDate = input.ResolveFromDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        // Legacy lines 574-575: the contract block is keyed on DateTime.Now, not on the As-On date.
        var today = Today;

        var query = _employeeRepository.GetAll()
            // Legacy lines 537-541: Status == cmbEmpStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, e => e.Status == employeeStatus.Value)
            // Legacy lines 543-548: employees.Contains(Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            // Legacy line 550 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        // Legacy lines 551-553: sponsor records current on the As-On date.
        var sponsorsAsOn = _employeeSponsorRepository.GetAll()
            .Where(s => s.StartDate <= asOnDate && s.EndDate >= asOnDate);

        // Legacy lines 574-575: contracts current TODAY.
        var contractsToday = _employeeContractRepository.GetAll()
            .Where(c => c.StartDate <= today && c.EndDate >= today);

        return query.Select(e => new EmployeeSponsorReportRowDto
        {
            // Legacy "OID" column -- the row key, not a data column.
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn,
            EmployeeStatus = e.Status,
            // Legacy line 618: "Active" when a sponsor record is current on the As-On date, else "Expired".
            SponsorStatus = sponsorsAsOn.Any(s => s.EmployeeId == e.Id) ? "Active" : "Expired",
            SponsorName = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.Sponsor == null
                    ? string.Empty
                    : (useArabicNames ? s.Sponsor.NameAr : s.Sponsor.NameEn))
                .FirstOrDefault(),
            // Legacy line 556: the English name regardless of culture -- the Arabic one is its own column.
            GovtJobTitleEn = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.GovtJobTitle == null ? string.Empty : s.GovtJobTitle.NameEn)
                .FirstOrDefault(),
            GovtJobTitleAr = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.GovtJobTitle == null ? string.Empty : s.GovtJobTitle.NameAr)
                .FirstOrDefault(),
            GovtJobTitleCategoryName = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.GovtJobTitle == null || s.GovtJobTitle.GovtJobCategory == null
                    ? string.Empty
                    : (useArabicNames ? s.GovtJobTitle.GovtJobCategory.NameAr : s.GovtJobTitle.GovtJobCategory.NameEn))
                .FirstOrDefault(),
            StartDate = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (System.DateTime?)s.StartDate)
                .FirstOrDefault(),
            EndDate = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (System.DateTime?)s.EndDate)
                .FirstOrDefault(),
            ContractSalary = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (double?)s.ContractSalary)
                .FirstOrDefault(),
            SalaryCurrencyName = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.SalaryCurrency == null
                    ? string.Empty
                    : (useArabicNames ? s.SalaryCurrency.NameAr : s.SalaryCurrency.NameEn))
                .FirstOrDefault(),
            EmployeeSponsorRelation = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => s.EmpSponsorRelation)
                .FirstOrDefault(),
            ResidencyType = sponsorsAsOn
                .Where(s => s.EmployeeId == e.Id)
                .OrderBy(s => s.StartDate).ThenBy(s => s.Id)
                .Select(s => (ResidencyType?)s.ResidencyType)
                .FirstOrDefault(),
            // Legacy line 588/616: Employee.CurrentPayrollArea.Country -- resolved here through the org
            // assignment current TODAY, the same chain the ported EmployeeContract report walks.
            CountryName = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == e.Id
                            && o.StartDate <= today
                            && o.EndDate >= today
                            && o.PayrollAreaId != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => o.PayrollArea.Country == null
                    ? null
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault(),
            // Legacy lines 584-585: empty string, not null, when the employee has no nationality group.
            NationalityName = e.Nationality == null
                ? string.Empty
                : (useArabicNames ? e.Nationality.NameAr : e.Nationality.NameEn),
            NationalityGroupName = e.Nationality == null || e.Nationality.NationalityGroup == null
                ? string.Empty
                : (useArabicNames ? e.Nationality.NationalityGroup.NameAr : e.Nationality.NationalityGroup.NameEn),
            ContractTypeName = contractsToday
                .Where(c => c.EmployeeId == e.Id)
                .OrderBy(c => c.StartDate).ThenBy(c => c.Id)
                .Select(c => c.ContractType == null
                    ? string.Empty
                    : (useArabicNames ? c.ContractType.NameAr : c.ContractType.NameEn))
                .FirstOrDefault(),
            ContractStartDate = contractsToday
                .Where(c => c.EmployeeId == e.Id)
                .OrderBy(c => c.StartDate).ThenBy(c => c.Id)
                .Select(c => (System.DateTime?)c.StartDate)
                .FirstOrDefault(),
            ContractEndDate = contractsToday
                .Where(c => c.EmployeeId == e.Id)
                .OrderBy(c => c.StartDate).ThenBy(c => c.Id)
                .Select(c => (System.DateTime?)c.EndDate)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order. The technical OID key is omitted (surrogate id,
    /// no meaning to the reader); every other legacy column is present.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeSponsorReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeSponsorReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("EmployeeStatus", r => r.EmployeeStatus.ToString()),
            new("SponsorStatus", r => r.SponsorStatus),
            new("Sponsor", r => r.SponsorName),
            new("GovtJobTitle", r => r.GovtJobTitleEn),
            new("GovtJobTitleAr", r => r.GovtJobTitleAr),
            new("GovtJobTitleCategory", r => r.GovtJobTitleCategoryName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("ContractSalary", r => r.ContractSalary),
            new("SalaryCurrency", r => r.SalaryCurrencyName),
            new("EmployeeSponsorRelation", r => r.EmployeeSponsorRelation),
            new("Country", r => r.CountryName),
            new("Nationality", r => r.NationalityName),
            new("NationalityGroup", r => r.NationalityGroupName),
            new("ContractType", r => r.ContractTypeName),
            new("ContractStartDate", r => r.ContractStartDate),
            new("ContractEndDate", r => r.ContractEndDate),
            new("ResidencyType", r => r.ResidencyType?.ToString())
        };
    }
}
