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
/// Employee Contract analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeContract.ascx(.cs)</c>.
///
/// REFERENCE IMPLEMENTATION for the ~140-report wave; see <see cref="GridReportAppServiceBase"/>.
/// The whole legacy data path is three methods here: <c>BuildQueryAsync</c> (legacy <c>Table1()</c>
/// filters + projection, lines 461-555), <c>GetEmployeeContractReport</c> (paged read) and
/// <c>GetEmployeeContractReportToExcel</c> (legacy <c>gridExport1</c>, exports every matching row).
///
/// NOT PORTED from the legacy control -- all of it DevExpress/WebForms UI plumbing with no data meaning:
/// the drill-up/drill-down session stack (<c>oldSelectedvalues</c>/<c>indexKey</c>/<c>DrillUp</c>, which
/// only ever has one level here because <c>GetKey()</c>/<c>GetTable()</c> define a single "Table1");
/// the ColumnChooser JSON persistence (lines 797-836); the callback-panel state machine (lines 657-762);
/// the PDF export button (<c>btnExportPDF_Click</c>, line 770 -- this wave targets Excel); and the dead
/// <c>Table1_Old()</c> (lines 556-653), an earlier equivalent formulation of the same overlap predicate.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeContract)]
public class EmployeeContractReportAppService : GridReportAppServiceBase, IEmployeeContractReportAppService
{
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public EmployeeContractReportAppService(
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _employeeContractRepository = employeeContractRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeContract";

    protected override string ScreenCaption => "Employee Contract";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeContractReportRowDto>> GetEmployeeContractReport(
        GetEmployeeContractReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeContractReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeContractReportToExcel(GetEmployeeContractReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data
        // set, so exporting only the current page would be silent data loss.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "EmployeeContract.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeContract.ascx.cs:461-555).
    /// </summary>
    private async Task<IQueryable<EmployeeContractReportRowDto>> BuildQueryAsync(
        GetEmployeeContractReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        var query = _employeeContractRepository.GetAll()
            // Legacy lines 497-498: r.Employee != null && !(r.StartDate.Date > dtTo.Date || r.EndDate.Date < dtFrom.Date),
            // i.e. contracts whose date range overlaps the requested window.
            .Where(c => c.StartDate.Date <= toDate && c.EndDate.Date >= fromDate)
            // Legacy line 505: r.Employee.Status == employeeStatus, applied only when a status was picked.
            .WhereIf(employeeStatus.HasValue, c => c.Employee.Status == employeeStatus.Value)
            // Legacy lines 509-510: employees.Contains(r.Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, c => employeeCodes.Contains(c.Employee.Code))
            // Legacy line 513 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, c => rlsEmployeeIds.Contains(c.EmployeeId));

        return query.Select(c => new EmployeeContractReportRowDto
        {
            // Legacy "OID" column (line 537). Exposed as Id because it is the row key, not a data column.
            Id = c.Id,
            EmployeeCode = c.Employee.Code,
            EmployeeName = useArabicNames ? c.Employee.FullNameAr : c.Employee.FullNameEn,
            // Legacy line 525: empty string, not null, when the contract has no contract type.
            ContractTypeName = c.ContractType == null
                ? string.Empty
                : (useArabicNames ? c.ContractType.NameAr : c.ContractType.NameEn),
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            // Legacy lines 528-530/543: PayrollArea country of the org assignment current as of the
            // FROM date (the TO date is not consulted), null when there is none.
            CountryName = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == c.EmployeeId
                            && o.StartDate <= fromDate
                            && o.EndDate >= fromDate
                            && o.PayrollAreaId != null)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.PayrollArea.Country == null
                    ? null
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order (Code, Name, ContractType, StartDate, EndDate,
    /// Country). Legacy exported the grid columns including the technical OID key; OID is omitted here
    /// because it is a surrogate database id with no meaning to the reader.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeContractReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeContractReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("ContractType", r => r.ContractTypeName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("Country", r => r.CountryName)
        };
    }
}
