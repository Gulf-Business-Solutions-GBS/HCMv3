using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Health Reimburse analysis grid report (legacy
/// <c>uscRptEmployeeHealthReimburse.ascx</c>).
///
/// SCOPE REDUCTION (documented, not silent): legacy's report is built around a per-employee eligibility
/// computation -- <c>EmployeeHealthReimburse.GetEligibleHealthReimburseAmount</c> and
/// <c>AccrualService.GetHealthReimbAccrualStartEnd</c> (uscRptEmployeeHealthReimburse.ascx.cs:627/656) --
/// that depends on <c>EmployeeSubGroup.HealthReimburse</c> (a ManpowerPlanning/Payroll salary-profile
/// field). That whole quota/eligibility/accrual computation is explicitly OUT OF SCOPE on the
/// <c>HCMv3.PersonnelAdministration.EmployeeHealthReimburse</c> entity itself (see its class remarks:
/// "NOT ported on the entity itself, consistent with every other cross-module cascade omission"). This
/// port therefore reduces the report to its transactional core -- actual reimbursement PAYMENTS made in
/// the requested window -- and drops the eligibility/accrual/quota columns, matching the same scope
/// boundary already drawn on the entity.
/// </summary>
public class GetEmployeeHealthReimburseReportInput : GridReportInputBase
{
    protected override string DefaultSorting => "EmployeeCode ASC, PaymentDate ASC";
}

/// <summary>
/// One grid row: the employee, org context, and the actual <c>EmployeeHealthReimburse</c> payment. See
/// <see cref="GetEmployeeHealthReimburseReportInput"/> remarks for the scope reduction from legacy's
/// eligibility-focused report.
/// </summary>
public class EmployeeHealthReimburseReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitle { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit of the assignment current as of the payment date.</summary>
    public long? OrganizationUnitId { get; set; }

    public DateTime PaymentDate { get; set; }

    public double Amount { get; set; }

    public string Comments { get; set; }
}
