using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Status.Dto;

/// <summary>
/// Filter input for the Employee On-Hold analysis grid report (legacy <c>uscRptEmployeeOnHold.ascx</c>).
///
/// DEVIATION: legacy's <c>cmbEmpStatus</c> box is labelled "Employee Status" but is bound to the
/// <see cref="OnHoldStatus"/> enum and filtered against <c>EmployeeOnHold.BankTransfer</c> (line 553-555),
/// not <c>Employee.Status</c>. The shared base's <see cref="GridReportInputBase.EmployeeStatus"/> is the
/// wrong type, so this report adds its own correctly-typed <see cref="BankTransfer"/> filter.
///
/// NOTE on the "EmployeeOnHold data issue" mentioned in the port brief: <c>HCMv3.PersonnelAdministration.
/// EmployeeOnHold</c> already carries every field this legacy report reads (StartDate/EndDate/OnHold/
/// OnHoldBasicPay/OnHoldLeavePay/OnHoldIndemnityPay/OnHoldAirTicketPay/OnHoldLoanDeduction/
/// OnHoldAllEarnings/OnHoldAllDeductions/BankTransfer/Comments) 1:1 with the legacy business object, so no
/// further remediation was needed here -- see <c>EmployeeOnHold.cs</c> remarks for that earlier fix.
/// </summary>
public class GetEmployeeOnHoldReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbEmpStatus</c> (mislabelled -- see class remarks): actually a Bank Transfer / OnHold-type filter.</summary>
    public OnHoldStatus? BankTransfer { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeOnHold.ascx.cs:459-655</c> (<c>Table1()</c>): OID, Code, Name, JobTitle, CivilId,
/// StartDate, EndDate, OrgLevel1..7, OnHold, OnHoldBasicPay, OnHoldLeavePay, OnHoldIndemnityPay,
/// OnHoldAirTicketPay, OnHoldLoanDeduction, OnHoldAllEarnings, OnHoldAllDeductions, BankTransfer, Comments.
/// </summary>
public class EmployeeOnHoldReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public long CivilId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    public bool OnHold { get; set; }

    public bool OnHoldBasicPay { get; set; }

    public bool OnHoldLeavePay { get; set; }

    public bool OnHoldIndemnityPay { get; set; }

    public bool OnHoldAirTicketPay { get; set; }

    public bool OnHoldLoanDeduction { get; set; }

    public bool OnHoldAllEarnings { get; set; }

    public bool OnHoldAllDeductions { get; set; }

    public OnHoldStatus BankTransfer { get; set; }

    public string Comments { get; set; }
}
