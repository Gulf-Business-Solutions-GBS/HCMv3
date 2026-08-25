using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Status.Dto;

/// <summary>
/// Filter input for the Employee Document Movement analysis grid report (legacy
/// <c>uscRptEmployeeDocMovement.ascx</c>). Adds the report-specific "Doc.Movement Status" dropdown
/// (legacy <c>cmbDocumentMvmtStatus</c>) on top of the four shared filters.
/// </summary>
public class GetEmployeeDocMovementReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbDocumentMvmtStatus</c>. Null/unselected means "all statuses".</summary>
    public DocumentMvmtStatus? DocumentMvmtStatus { get; set; }

    /// <summary>Legacy applied no default sort; employee code ascending keeps paging deterministic.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeDocMovement.ascx.cs:492-678</c> (<c>Table1()</c>): OID, Code, Name, JobTitle,
/// JoiningDate, DocumentType, DocumentMvmtStatus, WithdrawDate, ReturnDate, ActualReturnDate,
/// OrgLevel1..7, WorkLocation, Country.
///
/// DEVIATION: legacy read <c>rec.DocumentMvmtStatus</c> as a STORED column on <c>EmployeeDocument</c>.
/// HCMv3's <c>EmployeeDocument</c> does not persist it -- it is computed the same way
/// <c>EmployeeDocumentsAppService.ToDtoAsync</c> computes it: OUT when an <c>EmployeeDocumentMvmt</c>
/// child has <c>ActualReturnDate == default(DateTime)</c> (not yet returned), else IN. Both the filter
/// and this column reuse that exact rule so they cannot drift from the Employee Documents screen.
/// </summary>
public class EmployeeDocMovementReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public DateTime JoiningDate { get; set; }

    public string DocumentTypeName { get; set; }

    /// <summary>See the class remarks: computed, not stored, on HCMv3's <c>EmployeeDocument</c>.</summary>
    public HCMv3.PersonnelAdministration.DocumentMvmtStatus DocumentMvmtStatus { get; set; }

    /// <summary>
    /// Legacy picked the FIRST movement (arbitrary order, no <c>OrderBy</c>) whose
    /// <c>ActualReturnDate != null</c> -- i.e. the first CLOSED movement. Ordered by Id here for a
    /// deterministic pick, which the legacy in-memory LINQ-to-XPO evaluation did not guarantee.
    /// </summary>
    public DateTime? WithdrawDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public DateTime? ActualReturnDate { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit the seven levels are resolved from. See <see cref="IEmployeeOrgLevelRow"/>.</summary>
    public long? OrganizationUnitId { get; set; }

    public string WorkLocationName { get; set; }

    public string CountryName { get; set; }
}
