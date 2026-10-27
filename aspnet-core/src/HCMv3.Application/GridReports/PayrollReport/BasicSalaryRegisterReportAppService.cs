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
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Basic Salary Register analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscBasicSalaryRegister.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 421-585).
///
/// The legacy data path is: SalaryRegister rows whose <c>InPeriod</c> falls in the requested window
/// (optionally restricted to one <c>PayrollRunType</c>), joined to their SalaryRegisterDetail lines,
/// grouped by (Employee, Code, InPeriod), with one column per wage type.
///
/// NOT PORTED -- all DevExpress/WebForms UI plumbing with no data meaning: the drill-down session stack
/// (<c>oldSelectedvalues</c>/<c>indexKey</c>, single-level here since <c>GetTable()</c> has only
/// <c>case 1</c>, lines 373-379), the ColumnChooser JSON persistence, the callback-panel state machine,
/// the <c>WebChartControl</c> on ASPxCallbackPanel2, and the (commented-out) PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_BasicSalaryRegister)]
public class BasicSalaryRegisterReportAppService
    : GridReportAppServiceBase, IBasicSalaryRegisterReportAppService
{
    // Legacy lines 530-546: the thirteen wage types this sheet reports.
    private const string BasicSalaryWageTypeCode = "1000";
    private const string AnnualLeavePayWageTypeCode = "1003";
    private const string MiscellaneousPayWageTypeCode = "1017";
    private const string LateEarlyDeductionWageTypeCode = "2000";
    private const string PifssContributionWageTypeCode = "2002";
    private const string AbsentUnpaidDedWageTypeCode = "2007";
    private const string GratuityProvisionWageTypeCode = "3000";
    private const string AnnualLeaveProvisionWageTypeCode = "3001";
    private const string AnnualLeaveAccrualWageTypeCode = "4010";
    private const string ActiveDaysWageTypeCode = "4011";
    private const string AnnualLeaveOpeningWageTypeCode = "4021";
    private const string AnnualLeaveClosingWageTypeCode = "4022";
    private const string NetPayWageTypeCode = "9000";

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;

    public BasicSalaryRegisterReportAppService(IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
    }

    protected override string LegacyScreenName => "uscBasicSalaryRegister";

    protected override string ScreenCaption => "Basic Salary Register";

    [HttpPost]
    public async Task<PagedResultDto<BasicSalaryRegisterReportRowDto>> GetBasicSalaryRegisterReport(
        GetBasicSalaryRegisterReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        StampSerialNumbers(rows, input.SkipCount);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildBasicSalaryRegisterParameterString(input),
            totalCount);

        return new PagedResultDto<BasicSalaryRegisterReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetBasicSalaryRegisterReportToExcel(GetBasicSalaryRegisterReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        StampSerialNumbers(rows, 0);

        return await ExportToFileAsync(
            "BasicSalaryRegister.xlsx",
            rows,
            GetExportColumns(),
            BuildBasicSalaryRegisterParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND aggregation, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s data region (lines 490-550).
    ///
    /// WAGE-TYPE PICK: legacy took <c>result.Where(wageType == X).FirstOrDefault().Amount</c>, which EF
    /// Core cannot translate inside a GroupBy projection. <c>Max</c> is used instead: identical to legacy
    /// whenever the group holds at most one line of that wage type (the payroll invariant -- one
    /// SalaryRegisterDetail per register per wage type), and unlike <c>Sum</c> it never double-counts if a
    /// second register lands in the same InPeriod. It is also deterministic, which server-side paging needs
    /// and legacy's FirstOrDefault was not.
    /// </summary>
    private async Task<IQueryable<BasicSalaryRegisterReportRowDto>> BuildQueryAsync(
        GetBasicSalaryRegisterReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var payrollRunType = input.PayrollRunType;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var query = _salaryRegisterDetailRepository.GetAll()
            // Legacy lines 495-497 / 505-508: InPeriod inside the window, both ends inclusive.
            .Where(d => d.SalaryRegister.InPeriod >= fromDate && d.SalaryRegister.InPeriod <= toDate)
            // Legacy lines 502-508: applied only when the Run Type combo held a value.
            .WhereIf(payrollRunType.HasValue, d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            // Legacy ApplyRowLevelSecurity (lines 837-846) -- an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        // Legacy lines 516-524: group key is (Employee name, Employee code, InPeriod).
        return query
            .GroupBy(d => new
            {
                d.SalaryRegister.EmployeeId,
                d.SalaryRegister.Employee.Code,
                d.SalaryRegister.Employee.FullNameEn,
                d.SalaryRegister.Employee.FullNameAr,
                d.SalaryRegister.InPeriod
            })
            .Select(g => new BasicSalaryRegisterReportRowDto
            {
                Id = g.Key.EmployeeId,
                EmployeeCode = g.Key.Code,
                EmployeeName = useArabicNames ? g.Key.FullNameAr : g.Key.FullNameEn,
                InPeriod = g.Key.InPeriod,
                NetPay = g.Max(d => d.WageType.Code == NetPayWageTypeCode ? d.Amount : 0),
                BasicSalary = g.Max(d => d.WageType.Code == BasicSalaryWageTypeCode ? d.Amount : 0),
                AnnualLeavePay = g.Max(d => d.WageType.Code == AnnualLeavePayWageTypeCode ? d.Amount : 0),
                LateEarlyDeduction = g.Max(d => d.WageType.Code == LateEarlyDeductionWageTypeCode ? d.Amount : 0),
                PifssContributionDed = g.Max(d => d.WageType.Code == PifssContributionWageTypeCode ? d.Amount : 0),
                GratuityProvision = g.Max(d => d.WageType.Code == GratuityProvisionWageTypeCode ? d.Amount : 0),
                AnnualLeaveProvision = g.Max(d => d.WageType.Code == AnnualLeaveProvisionWageTypeCode ? d.Amount : 0),
                AnnualLeaveCurrentMonthAccrual =
                    g.Max(d => d.WageType.Code == AnnualLeaveAccrualWageTypeCode ? d.Amount : 0),
                ActiveDaysInAMonth = g.Max(d => d.WageType.Code == ActiveDaysWageTypeCode ? d.Amount : 0),
                // LEGACY DEFECT REPRODUCED: the "PresentDays" column is declared (line 452) but never
                // projected and never assigned, so legacy always showed it empty. Kept at 0, pinned by
                // Should_Always_Leave_PresentDays_Empty_Reproducing_Legacy_Defect.
                PresentDays = 0,
                AbsentUnpaidDed = g.Max(d => d.WageType.Code == AbsentUnpaidDedWageTypeCode ? d.Amount : 0),
                MiscellaneousPay = g.Max(d => d.WageType.Code == MiscellaneousPayWageTypeCode ? d.Amount : 0),
                AnnualLeaveOpeningBal = g.Max(d => d.WageType.Code == AnnualLeaveOpeningWageTypeCode ? d.Amount : 0),
                AnnualLeaveClosingBal = g.Max(d => d.WageType.Code == AnnualLeaveClosingWageTypeCode ? d.Amount : 0)
            });
    }

    /// <summary>
    /// Legacy "SrNo" (line 553) was <c>table.Rows.Count + 1</c>, i.e. the position in the rendered result.
    /// With server-side paging that has to be the position in the OVERALL result, so the page offset is
    /// added back. The export always starts at 1 because it returns every row.
    /// </summary>
    private static void StampSerialNumbers(List<BasicSalaryRegisterReportRowDto> rows, int skipCount)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].SerialNumber = skipCount + i + 1;
        }
    }

    /// <summary>
    /// Ports legacy <c>GetParameterStr</c> (lines 405-416): Run Type / From Date / To Date only -- this
    /// screen has no Employee Status and no Employee Code, so the base implementation is not reused.
    /// </summary>
    private string BuildBasicSalaryRegisterParameterString(GetBasicSalaryRegisterReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy"));
        parts.Add("To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy"));

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The exported column set, in legacy column order (lines 440-457). The technical OID key is omitted;
    /// so is <c>InPeriod</c>, which legacy did not expose as a column (see the row DTO remarks).
    /// </summary>
    private static IReadOnlyList<GridReportColumn<BasicSalaryRegisterReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<BasicSalaryRegisterReportRowDto>>
        {
            new("SrNo", r => r.SerialNumber),
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("NetPay", r => r.NetPay),
            new("BasicSalary", r => r.BasicSalary),
            new("AnnualLeavePay", r => r.AnnualLeavePay),
            new("LateEarlyDeduction", r => r.LateEarlyDeduction),
            new("PIFSSContributionDed", r => r.PifssContributionDed),
            new("GratuityProvision", r => r.GratuityProvision),
            new("AnnualLeaveProvision", r => r.AnnualLeaveProvision),
            new("AnnualLeaveCurrentMonthAccrual", r => r.AnnualLeaveCurrentMonthAccrual),
            new("ActiveDaysInAMonth", r => r.ActiveDaysInAMonth),
            new("PresentDays", r => r.PresentDays),
            new("AbsentUnpaidDed", r => r.AbsentUnpaidDed),
            new("MiscellaneousPay", r => r.MiscellaneousPay),
            new("AnnualLeaveOpeningBal", r => r.AnnualLeaveOpeningBal),
            new("AnnualLeaveClosingBal", r => r.AnnualLeaveClosingBal)
        };
    }
}
