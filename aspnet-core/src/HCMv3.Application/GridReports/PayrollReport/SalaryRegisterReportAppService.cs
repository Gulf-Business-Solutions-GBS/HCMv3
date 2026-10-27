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
/// Salary Register analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscSalaryRegister.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 368-795). The widest sheet in this batch: 108 columns, 105 of them a single
/// wage type's amount.
///
/// GENUINE DEFECT DIVERGENCE -- LEGACY NEVER PRODUCED A ROW. The fill loop assigns 14 columns the table
/// never declares (<c>dr["BasicSalary"]</c> at line 663 is the first), so the <c>DataRow</c> indexer threw
/// <c>ArgumentException: Column 'BasicSalary' does not belong to table</c> and <c>Table1()</c> rethrew it
/// as <c>"Table1 : ..."</c> on EVERY execution. Those 14 columns are exactly the column set of the sibling
/// <c>uscBasicSalaryRegister</c> screen: this control was split in two, the <c>Columns.Add</c> calls were
/// commented out (lines 377-378, 388, 390, 393-394, 406-407, 412, 417, 430-431) and the assignments were
/// forgotten. Reproducing the crash would ship a report that can never render, so the port implements the
/// 108 DECLARED columns -- the stated contract -- and the 14 orphans are covered by the sibling report.
///
/// LEGACY DEFECT REPRODUCED: the OTonPublicHoliday / OTonPublicHolidayArr columns read the WEEKEND wage
/// type's amount (lines 681-682). See the marked lines below and
/// <c>Should_Report_Weekend_Amount_For_PublicHoliday_Overtime_Reproducing_Legacy_Defect</c>.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down session stack (dead
/// here: <c>GetTable()</c>'s cases 2 and 3 are commented out at lines 320-328 so <c>Table2()</c> and
/// <c>Table3()</c> are unreachable), the ColumnChooser JSON persistence, the callback-panel state machine
/// and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_SalaryRegister)]
public class SalaryRegisterReportAppService : GridReportAppServiceBase, ISalaryRegisterReportAppService
{
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;

    public SalaryRegisterReportAppService(IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
    }

    protected override string LegacyScreenName => "uscSalaryRegister";

    protected override string ScreenCaption => "Salary Register";

    [HttpPost]
    public async Task<PagedResultDto<SalaryRegisterReportRowDto>> GetSalaryRegisterReport(
        GetSalaryRegisterReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildSalaryRegisterParameterString(input),
            totalCount);

        return new PagedResultDto<SalaryRegisterReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetSalaryRegisterReportToExcel(GetSalaryRegisterReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "SalaryRegister.xlsx",
            rows,
            GetExportColumns(),
            BuildSalaryRegisterParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND aggregation, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s data region (lines 497-655).
    ///
    /// WAGE-TYPE PICK: legacy took <c>result.Where(wageType == X).FirstOrDefault().Amount</c>, which EF
    /// Core cannot translate inside a GroupBy projection. <c>Max</c> is used instead: identical whenever
    /// the group holds at most one line of that wage type (the payroll invariant), never double-counting
    /// the way <c>Sum</c> would, and deterministic -- which server-side paging needs.
    /// </summary>
    private async Task<IQueryable<SalaryRegisterReportRowDto>> BuildQueryAsync(
        GetSalaryRegisterReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        var query = _salaryRegisterDetailRepository.GetAll()
            // Legacy lines 497-498: InPeriod inside the window, both ends inclusive. This screen has no
            // Run Type, Employee Code or Employee Status filter at all.
            .Where(d => d.SalaryRegister.InPeriod >= fromDate && d.SalaryRegister.InPeriod <= toDate)
            // Legacy ApplyRowLevelSecurity -- an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        // Legacy lines 518-525: group key is (Employee name, Employee code, InPeriod).
        return query
            .GroupBy(d => new
            {
                d.SalaryRegister.EmployeeId,
                d.SalaryRegister.Employee.Code,
                d.SalaryRegister.Employee.FullNameEn,
                d.SalaryRegister.InPeriod
            })
            .Select(g => new SalaryRegisterReportRowDto
            {
                Id = g.Key.EmployeeId,
                EmployeeCode = g.Key.Code,
                // Legacy line 520 used FullNameEn unconditionally -- no Arabic switch on this screen,
                // unlike every other report in the family.
                EmployeeName = g.Key.FullNameEn,
                InPeriod = g.Key.InPeriod,
                HousingAllowance = g.Max(d => d.WageType.Code == "1001" ? d.Amount : 0),
                TransportAllowance = g.Max(d => d.WageType.Code == "1002" ? d.Amount : 0),
                AirTicketPay = g.Max(d => d.WageType.Code == "1004" ? d.Amount : 0),
                HajjLeavePay = g.Max(d => d.WageType.Code == "1005" ? d.Amount : 0),
                LoanPay = g.Max(d => d.WageType.Code == "1006" ? d.Amount : 0),
                OTonNormalDay = g.Max(d => d.WageType.Code == "1007" ? d.Amount : 0),
                OTonWeekend = g.Max(d => d.WageType.Code == "1008" ? d.Amount : 0),
                OTonPublicHoliday = g.Max(d => d.WageType.Code == "1008" ? d.Amount : 0), // LEGACY DEFECT (line 681): guarded on 1009 but reads the WEEKEND (1008) amount.
                GratuityPay = g.Max(d => d.WageType.Code == "1010" ? d.Amount : 0),
                LoanRecoveryDeduction = g.Max(d => d.WageType.Code == "2001" ? d.Amount : 0),
                SickLeaveUnpaidDed = g.Max(d => d.WageType.Code == "2003" ? d.Amount : 0),
                TicketProvision = g.Max(d => d.WageType.Code == "3002" ? d.Amount : 0),
                AnnualLeavePaidDays = g.Max(d => d.WageType.Code == "4000" ? d.Amount : 0),
                AbsentUnpaidDays = g.Max(d => d.WageType.Code == "4001" ? d.Amount : 0),
                SickLeavePaidDays = g.Max(d => d.WageType.Code == "4002" ? d.Amount : 0),
                SickLeaveUnpaidDays = g.Max(d => d.WageType.Code == "4003" ? d.Amount : 0),
                GratuityDays = g.Max(d => d.WageType.Code == "4004" ? d.Amount : 0),
                GratuityPaidDays = g.Max(d => d.WageType.Code == "4005" ? d.Amount : 0),
                GratuityBalanceDays = g.Max(d => d.WageType.Code == "4006" ? d.Amount : 0),
                PublicHolidayDays = g.Max(d => d.WageType.Code == "4007" ? d.Amount : 0),
                WeeklyOffDays = g.Max(d => d.WageType.Code == "4008" ? d.Amount : 0),
                RestDays = g.Max(d => d.WageType.Code == "4009" ? d.Amount : 0),
                AdvancedLeavePaidDays = g.Max(d => d.WageType.Code == "4012" ? d.Amount : 0),
                AdvancedTicketPaidDays = g.Max(d => d.WageType.Code == "4013" ? d.Amount : 0),
                MobileAllowance = g.Max(d => d.WageType.Code == "1011" ? d.Amount : 0),
                Commission = g.Max(d => d.WageType.Code == "1012" ? d.Amount : 0),
                PaidDays = g.Max(d => d.WageType.Code == "4015" ? d.Amount : 0),
                MedicalInsuranceDed = g.Max(d => d.WageType.Code == "2004" ? d.Amount : 0),
                AdvancePaymentDed = g.Max(d => d.WageType.Code == "2005" ? d.Amount : 0),
                CashShortageDed = g.Max(d => d.WageType.Code == "2006" ? d.Amount : 0),
                SickLeavePay = g.Max(d => d.WageType.Code == "1013" ? d.Amount : 0),
                ExtraWorkingDayPay = g.Max(d => d.WageType.Code == "1014" ? d.Amount : 0),
                LieuDayPay = g.Max(d => d.WageType.Code == "1015" ? d.Amount : 0),
                ExtraWorkingPaidDays = g.Max(d => d.WageType.Code == "4016" ? d.Amount : 0),
                LieuPaidDays = g.Max(d => d.WageType.Code == "4017" ? d.Amount : 0),
                AdvanceLeavePay = g.Max(d => d.WageType.Code == "1016" ? d.Amount : 0),
                AdvanceLeaveRecoveryDays = g.Max(d => d.WageType.Code == "4019" ? d.Amount : 0),
                AdvanceLeavePaidRecovery = g.Max(d => d.WageType.Code == "2008" ? d.Amount : 0),
                MiscellaneousDed = g.Max(d => d.WageType.Code == "2009" ? d.Amount : 0),
                InactiveDays = g.Max(d => d.WageType.Code == "4020" ? d.Amount : 0),
                TotalActiveDays = g.Max(d => d.WageType.Code == "4023" ? d.Amount : 0),
                TotalAbsentUnpaidDays = g.Max(d => d.WageType.Code == "4024" ? d.Amount : 0),
                TotalGratuityDays = g.Max(d => d.WageType.Code == "4025" ? d.Amount : 0),
                TotalGratuityPaidDays = g.Max(d => d.WageType.Code == "4026" ? d.Amount : 0),
                EmployeeTicketCurrentMonthAccrual = g.Max(d => d.WageType.Code == "4027" ? d.Amount : 0),
                TotalEmployeeTicketAccrual = g.Max(d => d.WageType.Code == "4028" ? d.Amount : 0),
                BasicSalaryArrear = g.Max(d => d.WageType.Code == "7000" ? d.Amount : 0),
                HousingAllowanceArrear = g.Max(d => d.WageType.Code == "7001" ? d.Amount : 0),
                TransportAllowanceArrear = g.Max(d => d.WageType.Code == "7002" ? d.Amount : 0),
                AnnualLeavePayArrear = g.Max(d => d.WageType.Code == "7003" ? d.Amount : 0),
                AirTicketPayArrear = g.Max(d => d.WageType.Code == "7004" ? d.Amount : 0),
                HajjLeavePayArrear = g.Max(d => d.WageType.Code == "7005" ? d.Amount : 0),
                LoanPayArrear = g.Max(d => d.WageType.Code == "7006" ? d.Amount : 0),
                OTonNormalDayArr = g.Max(d => d.WageType.Code == "7007" ? d.Amount : 0),
                OTonWeekendArr = g.Max(d => d.WageType.Code == "7008" ? d.Amount : 0),
                OTonPublicHolidayArr = g.Max(d => d.WageType.Code == "7008" ? d.Amount : 0), // LEGACY DEFECT (line 682): guarded on 7009 but reads the WEEKEND ARREAR (7008) amount.
                GratuityPayArr = g.Max(d => d.WageType.Code == "7010" ? d.Amount : 0),
                LateEarlyDeductionArrear = g.Max(d => d.WageType.Code == "8000" ? d.Amount : 0),
                LoanRecoveryDeductionArrear = g.Max(d => d.WageType.Code == "8001" ? d.Amount : 0),
                PIFSSContributionDedArr = g.Max(d => d.WageType.Code == "8002" ? d.Amount : 0),
                SickLeaveUnpaidDedArr = g.Max(d => d.WageType.Code == "8003" ? d.Amount : 0),
                GratuityProvisionArr = g.Max(d => d.WageType.Code == "6000" ? d.Amount : 0),
                AnnualLeaveProvisionArr = g.Max(d => d.WageType.Code == "6001" ? d.Amount : 0),
                TicketProvisionArr = g.Max(d => d.WageType.Code == "6002" ? d.Amount : 0),
                AnnualLeavePaidDaysArr = g.Max(d => d.WageType.Code == "5000" ? d.Amount : 0),
                AbsentUnpaidDaysArr = g.Max(d => d.WageType.Code == "5001" ? d.Amount : 0),
                SickLeavePaidDaysArr = g.Max(d => d.WageType.Code == "5002" ? d.Amount : 0),
                SickLeaveUnpaidDaysArr = g.Max(d => d.WageType.Code == "5003" ? d.Amount : 0),
                GratuityDaysArr = g.Max(d => d.WageType.Code == "5004" ? d.Amount : 0),
                GratuityPaidDaysArr = g.Max(d => d.WageType.Code == "5005" ? d.Amount : 0),
                GratuityBalanceDaysArr = g.Max(d => d.WageType.Code == "5006" ? d.Amount : 0),
                PublicHolidayDaysArr = g.Max(d => d.WageType.Code == "5007" ? d.Amount : 0),
                WeeklyOffDaysArr = g.Max(d => d.WageType.Code == "5008" ? d.Amount : 0),
                RestDaysArr = g.Max(d => d.WageType.Code == "5009" ? d.Amount : 0),
                AnnualLeaveCurrentMonthAccrualArrear = g.Max(d => d.WageType.Code == "5010" ? d.Amount : 0),
                ActiveDaysinaMonthArrear = g.Max(d => d.WageType.Code == "5011" ? d.Amount : 0),
                AdvancedLeavePaidDaysArrear = g.Max(d => d.WageType.Code == "5012" ? d.Amount : 0),
                AdvancedTicketPaidDaysArrear = g.Max(d => d.WageType.Code == "5013" ? d.Amount : 0),
                MobileAllowanceArrear = g.Max(d => d.WageType.Code == "7011" ? d.Amount : 0),
                CommissionArrear = g.Max(d => d.WageType.Code == "7012" ? d.Amount : 0),
                PresentDaysArrear = g.Max(d => d.WageType.Code == "5014" ? d.Amount : 0),
                PaidDaysArrear = g.Max(d => d.WageType.Code == "5015" ? d.Amount : 0),
                MedicalInsuranceDedArr = g.Max(d => d.WageType.Code == "8004" ? d.Amount : 0),
                AdvancePaymentDedArr = g.Max(d => d.WageType.Code == "8005" ? d.Amount : 0),
                CashShortageDedArr = g.Max(d => d.WageType.Code == "8006" ? d.Amount : 0),
                AbsentUnpaidDedArr = g.Max(d => d.WageType.Code == "8007" ? d.Amount : 0),
                SickLeavePayArrear = g.Max(d => d.WageType.Code == "7013" ? d.Amount : 0),
                ExtraWorkingDayPayArrear = g.Max(d => d.WageType.Code == "7014" ? d.Amount : 0),
                LieuDayPayArrear = g.Max(d => d.WageType.Code == "7015" ? d.Amount : 0),
                ExtraWorkingPaidDaysArrear = g.Max(d => d.WageType.Code == "5016" ? d.Amount : 0),
                LieuPaidDaysArrear = g.Max(d => d.WageType.Code == "5017" ? d.Amount : 0),
                AdvanceLeavePayArrear = g.Max(d => d.WageType.Code == "7016" ? d.Amount : 0),
                AdvanceLeaveRecoveryDaysArrear = g.Max(d => d.WageType.Code == "5019" ? d.Amount : 0),
                AdvanceLeavePaidRecoveryArrear = g.Max(d => d.WageType.Code == "8008" ? d.Amount : 0),
                MiscellaneousPayArrear = g.Max(d => d.WageType.Code == "7017" ? d.Amount : 0),
                MiscellaneousDedArrear = g.Max(d => d.WageType.Code == "8009" ? d.Amount : 0),
                InactiveDaysArrear = g.Max(d => d.WageType.Code == "5020" ? d.Amount : 0),
                AnnualLeaveOpeningBalArrear = g.Max(d => d.WageType.Code == "5021" ? d.Amount : 0),
                AnnualLeaveClosingBalArrear = g.Max(d => d.WageType.Code == "5022" ? d.Amount : 0),
                TotalActiveDaysArrear = g.Max(d => d.WageType.Code == "5023" ? d.Amount : 0),
                TotalAbsentUnpaidDaysArrear = g.Max(d => d.WageType.Code == "5024" ? d.Amount : 0),
                TotalGratuityDaysArrear = g.Max(d => d.WageType.Code == "5025" ? d.Amount : 0),
                TotalGratuityPaidDaysArrear = g.Max(d => d.WageType.Code == "5026" ? d.Amount : 0),
                EmployeeTicketCurrentMonthAccrualArrear = g.Max(d => d.WageType.Code == "5027" ? d.Amount : 0),
                TotalEmployeeTicketAccrualArrear = g.Max(d => d.WageType.Code == "5028" ? d.Amount : 0)
            });
    }

    /// <summary>
    /// Ports legacy <c>GetParameterStr</c> (lines 353-360): From Date / To Date only -- this screen has no
    /// Employee Status and no Employee Code, so the base implementation is not reused.
    /// </summary>
    private string BuildSalaryRegisterParameterString(GetSalaryRegisterReportInput input)
    {
        return "From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy")
               + " | To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy");
    }

    /// <summary>The exported column set, in legacy declaration order (lines 374-495).</summary>
    private static IReadOnlyList<GridReportColumn<SalaryRegisterReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<SalaryRegisterReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Inperiod", r => r.InPeriod),
            new("HousingAllowance", r => r.HousingAllowance),
            new("TransportAllowance", r => r.TransportAllowance),
            new("AirTicketPay", r => r.AirTicketPay),
            new("HajjLeavePay", r => r.HajjLeavePay),
            new("LoanPay", r => r.LoanPay),
            new("OTonNormalDay", r => r.OTonNormalDay),
            new("OTonWeekend", r => r.OTonWeekend),
            new("OTonPublicHoliday", r => r.OTonPublicHoliday),
            new("GratuityPay", r => r.GratuityPay),
            new("LoanRecoveryDeduction", r => r.LoanRecoveryDeduction),
            new("SickLeaveUnpaidDed", r => r.SickLeaveUnpaidDed),
            new("TicketProvision", r => r.TicketProvision),
            new("AnnualLeavePaidDays", r => r.AnnualLeavePaidDays),
            new("AbsentUnpaidDays", r => r.AbsentUnpaidDays),
            new("SickLeavePaidDays", r => r.SickLeavePaidDays),
            new("SickLeaveUnpaidDays", r => r.SickLeaveUnpaidDays),
            new("GratuityDays", r => r.GratuityDays),
            new("GratuityPaidDays", r => r.GratuityPaidDays),
            new("GratuityBalanceDays", r => r.GratuityBalanceDays),
            new("PublicHolidayDays", r => r.PublicHolidayDays),
            new("WeeklyOffDays", r => r.WeeklyOffDays),
            new("RestDays", r => r.RestDays),
            new("AdvancedLeavePaidDays", r => r.AdvancedLeavePaidDays),
            new("AdvancedTicketPaidDays", r => r.AdvancedTicketPaidDays),
            new("MobileAllowance", r => r.MobileAllowance),
            new("Commission", r => r.Commission),
            new("PaidDays", r => r.PaidDays),
            new("MedicalInsuranceDed", r => r.MedicalInsuranceDed),
            new("AdvancePaymentDed", r => r.AdvancePaymentDed),
            new("CashShortageDed", r => r.CashShortageDed),
            new("SickLeavePay", r => r.SickLeavePay),
            new("ExtraWorkingDayPay", r => r.ExtraWorkingDayPay),
            new("LieuDayPay", r => r.LieuDayPay),
            new("ExtraWorkingPaidDays", r => r.ExtraWorkingPaidDays),
            new("LieuPaidDays", r => r.LieuPaidDays),
            new("AdvanceLeavePay", r => r.AdvanceLeavePay),
            new("AdvanceLeaveRecoveryDays", r => r.AdvanceLeaveRecoveryDays),
            new("AdvanceLeavePaidRecovery", r => r.AdvanceLeavePaidRecovery),
            new("MiscellaneousDed", r => r.MiscellaneousDed),
            new("InactiveDays", r => r.InactiveDays),
            new("TotalActiveDays", r => r.TotalActiveDays),
            new("TotalAbsentUnpaidDays", r => r.TotalAbsentUnpaidDays),
            new("TotalGratuityDays", r => r.TotalGratuityDays),
            new("TotalGratuityPaidDays", r => r.TotalGratuityPaidDays),
            new("EmployeeTicketCurrentMonthAccrual", r => r.EmployeeTicketCurrentMonthAccrual),
            new("TotalEmployeeTicketAccrual", r => r.TotalEmployeeTicketAccrual),
            new("BasicSalaryArrear", r => r.BasicSalaryArrear),
            new("HousingAllowanceArrear", r => r.HousingAllowanceArrear),
            new("TransportAllowanceArrear", r => r.TransportAllowanceArrear),
            new("AnnualLeavePayArrear", r => r.AnnualLeavePayArrear),
            new("AirTicketPayArrear", r => r.AirTicketPayArrear),
            new("HajjLeavePayArrear", r => r.HajjLeavePayArrear),
            new("LoanPayArrear", r => r.LoanPayArrear),
            new("OTonNormalDayArr", r => r.OTonNormalDayArr),
            new("OTonWeekendArr", r => r.OTonWeekendArr),
            new("OTonPublicHolidayArr", r => r.OTonPublicHolidayArr),
            new("GratuityPayArr", r => r.GratuityPayArr),
            new("LateEarlyDeductionArrear", r => r.LateEarlyDeductionArrear),
            new("LoanRecoveryDeductionArrear", r => r.LoanRecoveryDeductionArrear),
            new("PIFSSContributionDedArr", r => r.PIFSSContributionDedArr),
            new("SickLeaveUnpaidDedArr", r => r.SickLeaveUnpaidDedArr),
            new("GratuityProvisionArr", r => r.GratuityProvisionArr),
            new("AnnualLeaveProvisionArr", r => r.AnnualLeaveProvisionArr),
            new("TicketProvisionArr", r => r.TicketProvisionArr),
            new("AnnualLeavePaidDaysArr", r => r.AnnualLeavePaidDaysArr),
            new("AbsentUnpaidDaysArr", r => r.AbsentUnpaidDaysArr),
            new("SickLeavePaidDaysArr", r => r.SickLeavePaidDaysArr),
            new("SickLeaveUnpaidDaysArr", r => r.SickLeaveUnpaidDaysArr),
            new("GratuityDaysArr", r => r.GratuityDaysArr),
            new("GratuityPaidDaysArr", r => r.GratuityPaidDaysArr),
            new("GratuityBalanceDaysArr", r => r.GratuityBalanceDaysArr),
            new("PublicHolidayDaysArr", r => r.PublicHolidayDaysArr),
            new("WeeklyOffDaysArr", r => r.WeeklyOffDaysArr),
            new("RestDaysArr", r => r.RestDaysArr),
            new("AnnualLeaveCurrentMonthAccrualArrear", r => r.AnnualLeaveCurrentMonthAccrualArrear),
            new("ActiveDaysinaMonthArrear", r => r.ActiveDaysinaMonthArrear),
            new("AdvancedLeavePaidDaysArrear", r => r.AdvancedLeavePaidDaysArrear),
            new("AdvancedTicketPaidDaysArrear", r => r.AdvancedTicketPaidDaysArrear),
            new("MobileAllowanceArrear", r => r.MobileAllowanceArrear),
            new("CommissionArrear", r => r.CommissionArrear),
            new("PresentDaysArrear", r => r.PresentDaysArrear),
            new("PaidDaysArrear", r => r.PaidDaysArrear),
            new("MedicalInsuranceDedArr", r => r.MedicalInsuranceDedArr),
            new("AdvancePaymentDedArr", r => r.AdvancePaymentDedArr),
            new("CashShortageDedArr", r => r.CashShortageDedArr),
            new("AbsentUnpaidDedArr", r => r.AbsentUnpaidDedArr),
            new("SickLeavePayArrear", r => r.SickLeavePayArrear),
            new("ExtraWorkingDayPayArrear", r => r.ExtraWorkingDayPayArrear),
            new("LieuDayPayArrear", r => r.LieuDayPayArrear),
            new("ExtraWorkingPaidDaysArrear", r => r.ExtraWorkingPaidDaysArrear),
            new("LieuPaidDaysArrear", r => r.LieuPaidDaysArrear),
            new("AdvanceLeavePayArrear", r => r.AdvanceLeavePayArrear),
            new("AdvanceLeaveRecoveryDaysArrear", r => r.AdvanceLeaveRecoveryDaysArrear),
            new("AdvanceLeavePaidRecoveryArrear", r => r.AdvanceLeavePaidRecoveryArrear),
            new("MiscellaneousPayArrear", r => r.MiscellaneousPayArrear),
            new("MiscellaneousDedArrear", r => r.MiscellaneousDedArrear),
            new("InactiveDaysArrear", r => r.InactiveDaysArrear),
            new("AnnualLeaveOpeningBalArrear", r => r.AnnualLeaveOpeningBalArrear),
            new("AnnualLeaveClosingBalArrear", r => r.AnnualLeaveClosingBalArrear),
            new("TotalActiveDaysArrear", r => r.TotalActiveDaysArrear),
            new("TotalAbsentUnpaidDaysArrear", r => r.TotalAbsentUnpaidDaysArrear),
            new("TotalGratuityDaysArrear", r => r.TotalGratuityDaysArrear),
            new("TotalGratuityPaidDaysArrear", r => r.TotalGratuityPaidDaysArrear),
            new("EmployeeTicketCurrentMonthAccrualArrear", r => r.EmployeeTicketCurrentMonthAccrualArrear),
            new("TotalEmployeeTicketAccrualArrear", r => r.TotalEmployeeTicketAccrualArrear)
        };
    }
}
