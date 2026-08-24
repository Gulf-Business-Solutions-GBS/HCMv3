using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
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
/// Employee Bank analysis grid report -- port of legacy <c>uscRptEmployeeBank.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 482-652).
///
/// NOT PORTED (DevExpress/WebForms UI plumbing): the drill-up/drill-down session stack (single level),
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button and the
/// dead <c>Table1_Old()</c> (lines 654+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeBank)]
public class EmployeeBankReportAppService : GridReportAppServiceBase, IEmployeeBankReportAppService
{
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeBankReportAppService(
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeBankRepository = employeeBankRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeBank";

    protected override string ScreenCaption => "Employee Bank";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeBankReportRowDto>> GetEmployeeBankReport(
        GetEmployeeBankReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildBankParameterString(input), totalCount);

        return new PagedResultDto<EmployeeBankReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeBankReportToExcel(GetEmployeeBankReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeBank.xlsx",
            rows,
            GetExportColumns(),
            BuildBankParameterString(input));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> (uscRptEmployeeBank.ascx.cs:460-476) logs the payroll area on top
    /// of the four common filters. The base helper appends extra parts at the END of the string whereas
    /// legacy printed the payroll area FIRST -- an ordering difference inside a free-text audit field.
    /// </summary>
    private string BuildBankParameterString(GetEmployeeBankReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (uscRptEmployeeBank.ascx.cs:482-652).
    /// </summary>
    private async Task<IQueryable<EmployeeBankReportRowDto>> BuildQueryAsync(GetEmployeeBankReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;
        var payrollAreaCode = input.PayrollAreaCode;
        var hasPayrollAreaFilter = !payrollAreaCode.IsNullOrWhiteSpace();

        // Legacy line 578: the org-assignment lookup for the org columns is keyed on DateTime.Now.
        var today = Today;

        var query = _employeeBankRepository.GetAll()
            // Legacy lines 546-547: !(StartDate > dtTo || EndDate < dtFrom) -- an overlap test.
            .Where(b => b.StartDate.Date <= toDate && b.EndDate.Date >= fromDate)
            // Legacy lines 551-555: Employee.Status == cmbEmpStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, b => b.Employee.Status == employeeStatus.Value)
            // Legacy lines 556-560: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, b => employeeCodes.Contains(b.Employee.Code))
            // Legacy lines 562-574 (cmbPayrollArea): the employee must have an org assignment covering
            // the report's TO date whose PayrollArea has the selected code. Legacy first resolved the
            // code to an Oid and compared Oids; comparing the code directly is equivalent and saves a
            // round trip. dtTo (not dtFrom) is what legacy used here.
            .WhereIf(hasPayrollAreaFilter, b => _employeeOrgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == b.EmployeeId
                          && o.StartDate <= toDate
                          && o.EndDate >= toDate
                          && o.PayrollAreaId != null
                          && o.PayrollArea.Code == payrollAreaCode))
            // Legacy line 576 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, b => rlsEmployeeIds.Contains(b.EmployeeId));

        return query.Select(b => new EmployeeBankReportRowDto
        {
            Id = b.Id,
            EmployeeCode = b.Employee.Code,
            EmployeeName = useArabicNames ? b.Employee.FullNameAr : b.Employee.FullNameEn,
            CivilId = b.Employee.CivilID,
            BankName = b.Bank == null
                ? string.Empty
                : (useArabicNames ? b.Bank.NameAr : b.Bank.NameEn),
            // Legacy line 611: the "EffectiveDate" column is the record's StartDate.
            EffectiveDate = b.StartDate,
            // Legacy OrgLevel1..7 collapsed to one OrganizationUnit column -- see the DTO remarks.
            OrganizationUnitId = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == b.EmployeeId
                            && o.StartDate <= today
                            && o.EndDate >= today
                            && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            BankCountryName = b.BankCountry == null
                ? string.Empty
                : (useArabicNames ? b.BankCountry.NameAr : b.BankCountry.NameEn),
            PaymentType = b.PaymentType,
            AccountNum = b.AccountNum,
            // Legacy line 618 used the Currency NAME here (the CurrencyShortForm* fields are used by the
            // BasicPay report, not this one).
            PaymentCurrencyName = b.PaymentCurrency == null
                ? string.Empty
                : (useArabicNames ? b.PaymentCurrency.NameAr : b.PaymentCurrency.NameEn),
            IBAN = b.IBAN
        });
    }

    /// <summary>
    /// The exported column set, in legacy column order. The technical OID key is omitted and the seven
    /// OrgLevel columns are the single OrganizationUnit column.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeBankReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeBankReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("CivilId", r => r.CivilId),
            new("Bank", r => r.BankName),
            new("EffectiveDate", r => r.EffectiveDate),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("BankCountry", r => r.BankCountryName),
            new("PaymentType", r => r.PaymentType.ToString()),
            new("AccountNumber", r => r.AccountNum),
            new("PaymentCurrency", r => r.PaymentCurrencyName),
            new("IBAN", r => r.IBAN)
        };
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<EmployeeBankReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(UseArabicNames);

        foreach (var row in rows)
        {
            var levels = lookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1 = levels[0];
            row.OrgLevel2 = levels[1];
            row.OrgLevel3 = levels[2];
            row.OrgLevel4 = levels[3];
            row.OrgLevel5 = levels[4];
            row.OrgLevel6 = levels[5];
            row.OrgLevel7 = levels[6];
        }
    }
}
