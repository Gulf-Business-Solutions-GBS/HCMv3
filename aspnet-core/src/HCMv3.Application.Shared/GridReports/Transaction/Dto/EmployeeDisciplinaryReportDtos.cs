using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Disciplinary analysis grid report (legacy
/// <c>uscRptEmployeeDisciplinary.ascx</c>). Adds the report-specific Violation/Disciplinary Type dropdown
/// filter (legacy <c>Session[ViolationTypeCode]</c>).
/// </summary>
public class GetEmployeeDisciplinaryReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>Session[ViolationTypeCode]</c> -- filters by <c>DisciplinaryType.Code</c>.</summary>
    public string DisciplinaryTypeCode { get; set; }

    /// <summary>
    /// Legacy applied no explicit sort on this report's Table1 (uscRptEmployeeDisciplinary.ascx.cs:
    /// 495-623). Employee code then disciplinary date ascending is used here for deterministic paging,
    /// same rationale as <c>GetEmployeeContractReportInput</c>.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, DisciplinaryDate ASC";
}

/// <summary>
/// One grid row. Mirrors legacy <c>Table1()</c> column-for-column (uscRptEmployeeDisciplinary.ascx.cs:
/// 495-618): OID, Code, Employee, JobTitle, OrgLevel1..7, CostCenter, PayrollArea, DisciplinaryDate,
/// DisciplinaryType, DisciplinaryDeductionType, Comments, Country.
/// </summary>
public class EmployeeDisciplinaryReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
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

    /// <summary>Leaf org unit of the assignment current as of today (legacy: <c>DateTime.Now</c>).</summary>
    public long? OrganizationUnitId { get; set; }

    public string CostCenter { get; set; }

    public string PayrollArea { get; set; }

    public DateTime DisciplinaryDate { get; set; }

    public string DisciplinaryTypeName { get; set; }

    /// <summary>
    /// Legacy line 583 displayed the raw FK object's <c>ToString()</c> (an XPO business-object default,
    /// not a name) -- a display artefact with no EF Core equivalent. Resolved here to the proper
    /// NameEn/NameAr instead; see class remarks in the app service for the fidelity call.
    /// </summary>
    public string DisciplinaryDeductionTypeName { get; set; }

    public string Comments { get; set; }

    public string Country { get; set; }
}
