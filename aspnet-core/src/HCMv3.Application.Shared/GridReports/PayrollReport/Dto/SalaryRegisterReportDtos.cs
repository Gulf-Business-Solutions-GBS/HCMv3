using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Salary Register analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscSalaryRegister.ascx</c>).
///
/// ATYPICAL FILTER PANEL: legacy's panel (uscSalaryRegister.ascx:13-21) is <c>dtFrom</c>/<c>dtTo</c> ONLY --
/// no Run Type, no <c>txtEmpCode</c>, no <c>cmbEmpStatus</c>, and <c>GetParameterStr</c> (lines 353-360)
/// logs exactly From Date / To Date. The inherited EmployeeCode/EmployeeStatus fields are unused and the
/// Angular screen hides them.
/// </summary>
public class GetSalaryRegisterReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy applied no sort (the DataTable came back in group order). Employee code ascending, then the
    /// period, is used here because server-side paging needs a deterministic order.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, InPeriod ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscSalaryRegister.ascx.cs:368-795).
///
/// GRAIN: one row per (Employee, <c>SalaryRegister.InPeriod</c>) -- legacy's group key (lines 518-525).
/// Unlike its sibling <c>uscBasicSalaryRegister</c>, this sheet DOES output the period, as
/// <see cref="InPeriod"/>.
///
/// COLUMN SET: the 108 columns legacy declares (lines 374-495) -- Code, Employee, Inperiod and 105
/// wage-type amounts, each the <c>Amount</c> of one <c>SalaryRegisterDetail</c> line. Current-month codes
/// are 1000-1017 (earnings), 2000-2009 (deductions), 3000-3002 (provisions) and 4000-4028 (day counts);
/// the matching arrear codes are 7xxx, 8xxx, 6xxx and 5xxx respectively.
///
/// LEGACY DEFECT -- THIS REPORT NEVER RAN (see the app service remarks): the fill loop writes 14 columns
/// that the table never declares, so <c>Table1()</c> threw
/// <c>ArgumentException: Column 'BasicSalary' does not belong to table</c> on its first row, every time.
/// Those 14 (BasicSalary, NetPay, AnnualLeavePay, LateEarlyDeduction, PIFSSContributionDed,
/// GratuityProvision, AnnualLeaveProvision, AnnualLeaveCurrentMonthAccrual, ActiveDaysinaMonth,
/// PresentDays, AbsentUnpaidDed, MiscellaneousPay, AnnualLeaveOpeningBal, AnnualLeaveClosingBal) are
/// EXACTLY the column set of <c>uscBasicSalaryRegister</c>: this sheet was split in two and the
/// <c>Columns.Add</c> lines were commented out (lines 377-378, 388, 390, 393-394, 406-407, 412, 417,
/// 430-431) while the assignments were left behind. The DECLARED 108 columns are taken as the contract.
///
/// LEGACY DEFECT REPRODUCED (lines 681-682): <c>OTonPublicHoliday</c> and <c>OTonPublicHolidayArr</c> are
/// guarded on their own wage type but read the WEEKEND line's amount
/// (<c>rec.OTonPublicHoliday != null ? rec.OTonWeekend.Amount : 0</c>). Both therefore report wage types
/// 1008/7008, not 1009/7009. Reproduced here and pinned by a test.
/// </summary>
public class SalaryRegisterReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column -- <c>Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column. NOTE: legacy always used <c>FullNameEn</c> here (line 520), never the Arabic name.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "Inperiod" column -- legacy rendered it as a <c>dd/MM/yyyy</c> STRING (line 662); typed here and formatted client-side.</summary>
    public DateTime InPeriod { get; set; }

    /// <summary>Legacy "HousingAllowance" column -- SalaryRegisterDetail.Amount of wage type 1001.</summary>
    public double HousingAllowance { get; set; }

    /// <summary>Legacy "TransportAllowance" column -- SalaryRegisterDetail.Amount of wage type 1002.</summary>
    public double TransportAllowance { get; set; }

    /// <summary>Legacy "AirTicketPay" column -- SalaryRegisterDetail.Amount of wage type 1004.</summary>
    public double AirTicketPay { get; set; }

    /// <summary>Legacy "HajjLeavePay" column -- SalaryRegisterDetail.Amount of wage type 1005.</summary>
    public double HajjLeavePay { get; set; }

    /// <summary>Legacy "LoanPay" column -- SalaryRegisterDetail.Amount of wage type 1006.</summary>
    public double LoanPay { get; set; }

    /// <summary>Legacy "OTonNormalDay" column -- SalaryRegisterDetail.Amount of wage type 1007.</summary>
    public double OTonNormalDay { get; set; }

    /// <summary>Legacy "OTonWeekend" column -- SalaryRegisterDetail.Amount of wage type 1008.</summary>
    public double OTonWeekend { get; set; }

    /// <summary>Legacy "OTonPublicHoliday" column -- SalaryRegisterDetail.Amount of wage type 1009.</summary>
    public double OTonPublicHoliday { get; set; }

    /// <summary>Legacy "GratuityPay" column -- SalaryRegisterDetail.Amount of wage type 1010.</summary>
    public double GratuityPay { get; set; }

    /// <summary>Legacy "LoanRecoveryDeduction" column -- SalaryRegisterDetail.Amount of wage type 2001.</summary>
    public double LoanRecoveryDeduction { get; set; }

    /// <summary>Legacy "SickLeaveUnpaidDed" column -- SalaryRegisterDetail.Amount of wage type 2003.</summary>
    public double SickLeaveUnpaidDed { get; set; }

    /// <summary>Legacy "TicketProvision" column -- SalaryRegisterDetail.Amount of wage type 3002.</summary>
    public double TicketProvision { get; set; }

    /// <summary>Legacy "AnnualLeavePaidDays" column -- SalaryRegisterDetail.Amount of wage type 4000.</summary>
    public double AnnualLeavePaidDays { get; set; }

    /// <summary>Legacy "AbsentUnpaidDays" column -- SalaryRegisterDetail.Amount of wage type 4001.</summary>
    public double AbsentUnpaidDays { get; set; }

    /// <summary>Legacy "SickLeavePaidDays" column -- SalaryRegisterDetail.Amount of wage type 4002.</summary>
    public double SickLeavePaidDays { get; set; }

    /// <summary>Legacy "SickLeaveUnpaidDays" column -- SalaryRegisterDetail.Amount of wage type 4003.</summary>
    public double SickLeaveUnpaidDays { get; set; }

    /// <summary>Legacy "GratuityDays" column -- SalaryRegisterDetail.Amount of wage type 4004.</summary>
    public double GratuityDays { get; set; }

    /// <summary>Legacy "GratuityPaidDays" column -- SalaryRegisterDetail.Amount of wage type 4005.</summary>
    public double GratuityPaidDays { get; set; }

    /// <summary>Legacy "GratuityBalanceDays" column -- SalaryRegisterDetail.Amount of wage type 4006.</summary>
    public double GratuityBalanceDays { get; set; }

    /// <summary>Legacy "PublicHolidayDays" column -- SalaryRegisterDetail.Amount of wage type 4007.</summary>
    public double PublicHolidayDays { get; set; }

    /// <summary>Legacy "WeeklyOffDays" column -- SalaryRegisterDetail.Amount of wage type 4008.</summary>
    public double WeeklyOffDays { get; set; }

    /// <summary>Legacy "RestDays" column -- SalaryRegisterDetail.Amount of wage type 4009.</summary>
    public double RestDays { get; set; }

    /// <summary>Legacy "AdvancedLeavePaidDays" column -- SalaryRegisterDetail.Amount of wage type 4012.</summary>
    public double AdvancedLeavePaidDays { get; set; }

    /// <summary>Legacy "AdvancedTicketPaidDays" column -- SalaryRegisterDetail.Amount of wage type 4013.</summary>
    public double AdvancedTicketPaidDays { get; set; }

    /// <summary>Legacy "MobileAllowance" column -- SalaryRegisterDetail.Amount of wage type 1011.</summary>
    public double MobileAllowance { get; set; }

    /// <summary>Legacy "Commission" column -- SalaryRegisterDetail.Amount of wage type 1012.</summary>
    public double Commission { get; set; }

    /// <summary>Legacy "PaidDays" column -- SalaryRegisterDetail.Amount of wage type 4015.</summary>
    public double PaidDays { get; set; }

    /// <summary>Legacy "MedicalInsuranceDed" column -- SalaryRegisterDetail.Amount of wage type 2004.</summary>
    public double MedicalInsuranceDed { get; set; }

    /// <summary>Legacy "AdvancePaymentDed" column -- SalaryRegisterDetail.Amount of wage type 2005.</summary>
    public double AdvancePaymentDed { get; set; }

    /// <summary>Legacy "CashShortageDed" column -- SalaryRegisterDetail.Amount of wage type 2006.</summary>
    public double CashShortageDed { get; set; }

    /// <summary>Legacy "SickLeavePay" column -- SalaryRegisterDetail.Amount of wage type 1013.</summary>
    public double SickLeavePay { get; set; }

    /// <summary>Legacy "ExtraWorkingDayPay" column -- SalaryRegisterDetail.Amount of wage type 1014.</summary>
    public double ExtraWorkingDayPay { get; set; }

    /// <summary>Legacy "LieuDayPay" column -- SalaryRegisterDetail.Amount of wage type 1015.</summary>
    public double LieuDayPay { get; set; }

    /// <summary>Legacy "ExtraWorkingPaidDays" column -- SalaryRegisterDetail.Amount of wage type 4016.</summary>
    public double ExtraWorkingPaidDays { get; set; }

    /// <summary>Legacy "LieuPaidDays" column -- SalaryRegisterDetail.Amount of wage type 4017.</summary>
    public double LieuPaidDays { get; set; }

    /// <summary>Legacy "AdvanceLeavePay" column -- SalaryRegisterDetail.Amount of wage type 1016.</summary>
    public double AdvanceLeavePay { get; set; }

    /// <summary>Legacy "AdvanceLeaveRecoveryDays" column -- SalaryRegisterDetail.Amount of wage type 4019.</summary>
    public double AdvanceLeaveRecoveryDays { get; set; }

    /// <summary>Legacy "AdvanceLeavePaidRecovery" column -- SalaryRegisterDetail.Amount of wage type 2008.</summary>
    public double AdvanceLeavePaidRecovery { get; set; }

    /// <summary>Legacy "MiscellaneousDed" column -- SalaryRegisterDetail.Amount of wage type 2009.</summary>
    public double MiscellaneousDed { get; set; }

    /// <summary>Legacy "InactiveDays" column -- SalaryRegisterDetail.Amount of wage type 4020.</summary>
    public double InactiveDays { get; set; }

    /// <summary>Legacy "TotalActiveDays" column -- SalaryRegisterDetail.Amount of wage type 4023.</summary>
    public double TotalActiveDays { get; set; }

    /// <summary>Legacy "TotalAbsentUnpaidDays" column -- SalaryRegisterDetail.Amount of wage type 4024.</summary>
    public double TotalAbsentUnpaidDays { get; set; }

    /// <summary>Legacy "TotalGratuityDays" column -- SalaryRegisterDetail.Amount of wage type 4025.</summary>
    public double TotalGratuityDays { get; set; }

    /// <summary>Legacy "TotalGratuityPaidDays" column -- SalaryRegisterDetail.Amount of wage type 4026.</summary>
    public double TotalGratuityPaidDays { get; set; }

    /// <summary>Legacy "EmployeeTicketCurrentMonthAccrual" column -- SalaryRegisterDetail.Amount of wage type 4027.</summary>
    public double EmployeeTicketCurrentMonthAccrual { get; set; }

    /// <summary>Legacy "TotalEmployeeTicketAccrual" column -- SalaryRegisterDetail.Amount of wage type 4028.</summary>
    public double TotalEmployeeTicketAccrual { get; set; }

    /// <summary>Legacy "BasicSalaryArrear" column -- SalaryRegisterDetail.Amount of wage type 7000.</summary>
    public double BasicSalaryArrear { get; set; }

    /// <summary>Legacy "HousingAllowanceArrear" column -- SalaryRegisterDetail.Amount of wage type 7001.</summary>
    public double HousingAllowanceArrear { get; set; }

    /// <summary>Legacy "TransportAllowanceArrear" column -- SalaryRegisterDetail.Amount of wage type 7002.</summary>
    public double TransportAllowanceArrear { get; set; }

    /// <summary>Legacy "AnnualLeavePayArrear" column -- SalaryRegisterDetail.Amount of wage type 7003.</summary>
    public double AnnualLeavePayArrear { get; set; }

    /// <summary>Legacy "AirTicketPayArrear" column -- SalaryRegisterDetail.Amount of wage type 7004.</summary>
    public double AirTicketPayArrear { get; set; }

    /// <summary>Legacy "HajjLeavePayArrear" column -- SalaryRegisterDetail.Amount of wage type 7005.</summary>
    public double HajjLeavePayArrear { get; set; }

    /// <summary>Legacy "LoanPayArrear" column -- SalaryRegisterDetail.Amount of wage type 7006.</summary>
    public double LoanPayArrear { get; set; }

    /// <summary>Legacy "OTonNormalDayArr" column -- SalaryRegisterDetail.Amount of wage type 7007.</summary>
    public double OTonNormalDayArr { get; set; }

    /// <summary>Legacy "OTonWeekendArr" column -- SalaryRegisterDetail.Amount of wage type 7008.</summary>
    public double OTonWeekendArr { get; set; }

    /// <summary>Legacy "OTonPublicHolidayArr" column -- SalaryRegisterDetail.Amount of wage type 7009.</summary>
    public double OTonPublicHolidayArr { get; set; }

    /// <summary>Legacy "GratuityPayArr" column -- SalaryRegisterDetail.Amount of wage type 7010.</summary>
    public double GratuityPayArr { get; set; }

    /// <summary>Legacy "LateEarlyDeductionArrear" column -- SalaryRegisterDetail.Amount of wage type 8000.</summary>
    public double LateEarlyDeductionArrear { get; set; }

    /// <summary>Legacy "LoanRecoveryDeductionArrear" column -- SalaryRegisterDetail.Amount of wage type 8001.</summary>
    public double LoanRecoveryDeductionArrear { get; set; }

    /// <summary>Legacy "PIFSSContributionDedArr" column -- SalaryRegisterDetail.Amount of wage type 8002.</summary>
    public double PIFSSContributionDedArr { get; set; }

    /// <summary>Legacy "SickLeaveUnpaidDedArr" column -- SalaryRegisterDetail.Amount of wage type 8003.</summary>
    public double SickLeaveUnpaidDedArr { get; set; }

    /// <summary>Legacy "GratuityProvisionArr" column -- SalaryRegisterDetail.Amount of wage type 6000.</summary>
    public double GratuityProvisionArr { get; set; }

    /// <summary>Legacy "AnnualLeaveProvisionArr" column -- SalaryRegisterDetail.Amount of wage type 6001.</summary>
    public double AnnualLeaveProvisionArr { get; set; }

    /// <summary>Legacy "TicketProvisionArr" column -- SalaryRegisterDetail.Amount of wage type 6002.</summary>
    public double TicketProvisionArr { get; set; }

    /// <summary>Legacy "AnnualLeavePaidDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5000.</summary>
    public double AnnualLeavePaidDaysArr { get; set; }

    /// <summary>Legacy "AbsentUnpaidDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5001.</summary>
    public double AbsentUnpaidDaysArr { get; set; }

    /// <summary>Legacy "SickLeavePaidDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5002.</summary>
    public double SickLeavePaidDaysArr { get; set; }

    /// <summary>Legacy "SickLeaveUnpaidDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5003.</summary>
    public double SickLeaveUnpaidDaysArr { get; set; }

    /// <summary>Legacy "GratuityDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5004.</summary>
    public double GratuityDaysArr { get; set; }

    /// <summary>Legacy "GratuityPaidDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5005.</summary>
    public double GratuityPaidDaysArr { get; set; }

    /// <summary>Legacy "GratuityBalanceDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5006.</summary>
    public double GratuityBalanceDaysArr { get; set; }

    /// <summary>Legacy "PublicHolidayDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5007.</summary>
    public double PublicHolidayDaysArr { get; set; }

    /// <summary>Legacy "WeeklyOffDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5008.</summary>
    public double WeeklyOffDaysArr { get; set; }

    /// <summary>Legacy "RestDaysArr" column -- SalaryRegisterDetail.Amount of wage type 5009.</summary>
    public double RestDaysArr { get; set; }

    /// <summary>Legacy "AnnualLeaveCurrentMonthAccrualArrear" column -- SalaryRegisterDetail.Amount of wage type 5010.</summary>
    public double AnnualLeaveCurrentMonthAccrualArrear { get; set; }

    /// <summary>Legacy "ActiveDaysinaMonthArrear" column -- SalaryRegisterDetail.Amount of wage type 5011.</summary>
    public double ActiveDaysinaMonthArrear { get; set; }

    /// <summary>Legacy "AdvancedLeavePaidDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5012.</summary>
    public double AdvancedLeavePaidDaysArrear { get; set; }

    /// <summary>Legacy "AdvancedTicketPaidDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5013.</summary>
    public double AdvancedTicketPaidDaysArrear { get; set; }

    /// <summary>Legacy "MobileAllowanceArrear" column -- SalaryRegisterDetail.Amount of wage type 7011.</summary>
    public double MobileAllowanceArrear { get; set; }

    /// <summary>Legacy "CommissionArrear" column -- SalaryRegisterDetail.Amount of wage type 7012.</summary>
    public double CommissionArrear { get; set; }

    /// <summary>Legacy "PresentDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5014.</summary>
    public double PresentDaysArrear { get; set; }

    /// <summary>Legacy "PaidDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5015.</summary>
    public double PaidDaysArrear { get; set; }

    /// <summary>Legacy "MedicalInsuranceDedArr" column -- SalaryRegisterDetail.Amount of wage type 8004.</summary>
    public double MedicalInsuranceDedArr { get; set; }

    /// <summary>Legacy "AdvancePaymentDedArr" column -- SalaryRegisterDetail.Amount of wage type 8005.</summary>
    public double AdvancePaymentDedArr { get; set; }

    /// <summary>Legacy "CashShortageDedArr" column -- SalaryRegisterDetail.Amount of wage type 8006.</summary>
    public double CashShortageDedArr { get; set; }

    /// <summary>Legacy "AbsentUnpaidDedArr" column -- SalaryRegisterDetail.Amount of wage type 8007.</summary>
    public double AbsentUnpaidDedArr { get; set; }

    /// <summary>Legacy "SickLeavePayArrear" column -- SalaryRegisterDetail.Amount of wage type 7013.</summary>
    public double SickLeavePayArrear { get; set; }

    /// <summary>Legacy "ExtraWorkingDayPayArrear" column -- SalaryRegisterDetail.Amount of wage type 7014.</summary>
    public double ExtraWorkingDayPayArrear { get; set; }

    /// <summary>Legacy "LieuDayPayArrear" column -- SalaryRegisterDetail.Amount of wage type 7015.</summary>
    public double LieuDayPayArrear { get; set; }

    /// <summary>Legacy "ExtraWorkingPaidDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5016.</summary>
    public double ExtraWorkingPaidDaysArrear { get; set; }

    /// <summary>Legacy "LieuPaidDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5017.</summary>
    public double LieuPaidDaysArrear { get; set; }

    /// <summary>Legacy "AdvanceLeavePayArrear" column -- SalaryRegisterDetail.Amount of wage type 7016.</summary>
    public double AdvanceLeavePayArrear { get; set; }

    /// <summary>Legacy "AdvanceLeaveRecoveryDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5019.</summary>
    public double AdvanceLeaveRecoveryDaysArrear { get; set; }

    /// <summary>Legacy "AdvanceLeavePaidRecoveryArrear" column -- SalaryRegisterDetail.Amount of wage type 8008.</summary>
    public double AdvanceLeavePaidRecoveryArrear { get; set; }

    /// <summary>Legacy "MiscellaneousPayArrear" column -- SalaryRegisterDetail.Amount of wage type 7017.</summary>
    public double MiscellaneousPayArrear { get; set; }

    /// <summary>Legacy "MiscellaneousDedArrear" column -- SalaryRegisterDetail.Amount of wage type 8009.</summary>
    public double MiscellaneousDedArrear { get; set; }

    /// <summary>Legacy "InactiveDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5020.</summary>
    public double InactiveDaysArrear { get; set; }

    /// <summary>Legacy "AnnualLeaveOpeningBalArrear" column -- SalaryRegisterDetail.Amount of wage type 5021.</summary>
    public double AnnualLeaveOpeningBalArrear { get; set; }

    /// <summary>Legacy "AnnualLeaveClosingBalArrear" column -- SalaryRegisterDetail.Amount of wage type 5022.</summary>
    public double AnnualLeaveClosingBalArrear { get; set; }

    /// <summary>Legacy "TotalActiveDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5023.</summary>
    public double TotalActiveDaysArrear { get; set; }

    /// <summary>Legacy "TotalAbsentUnpaidDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5024.</summary>
    public double TotalAbsentUnpaidDaysArrear { get; set; }

    /// <summary>Legacy "TotalGratuityDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5025.</summary>
    public double TotalGratuityDaysArrear { get; set; }

    /// <summary>Legacy "TotalGratuityPaidDaysArrear" column -- SalaryRegisterDetail.Amount of wage type 5026.</summary>
    public double TotalGratuityPaidDaysArrear { get; set; }

    /// <summary>Legacy "EmployeeTicketCurrentMonthAccrualArrear" column -- SalaryRegisterDetail.Amount of wage type 5027.</summary>
    public double EmployeeTicketCurrentMonthAccrualArrear { get; set; }

    /// <summary>Legacy "TotalEmployeeTicketAccrualArrear" column -- SalaryRegisterDetail.Amount of wage type 5028.</summary>
    public double TotalEmployeeTicketAccrualArrear { get; set; }

}
