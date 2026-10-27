using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Bank analysis grid report (legacy <c>uscRptEmployeeBank.ascx</c>).
/// Beyond the four common filters the legacy panel adds one report-specific combo, <c>cmbPayrollArea</c>,
/// which is carried here by <see cref="PayrollAreaCode"/>.
/// </summary>
public class GetEmployeeBankReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value = <c>PayrollArea.Code</c>). Null/empty means "all payroll
    /// areas". Legacy matched an employee whose org assignment covering the report's TO date points at
    /// that payroll area (uscRptEmployeeBank.ascx.cs:565-574) -- note it uses dtTo, not dtFrom.
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>Legacy sorted the filled DataTable by employee code (uscRptEmployeeBank.ascx.cs:621).</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeBank.ascx.cs:482-652</c>: OID, Code, Name, CivilId, Bank, EffectiveDate,
/// OrgLevel1..7, BankCountry, PaymentType, AccountNumber, PaymentCurrency, IBAN.
///
/// OrgLevel1..OrgLevel7 are kept as SEVEN columns, exactly as legacy rendered them. HCMv3's
/// <c>EmployeeOrgAssignment</c> replaced legacy's seven FKs with one FK into ABP's hierarchical
/// <c>OrganizationUnit</c> tree, so each level is resolved from that tree after materialization --
/// see <c>EmployeeOrgLevelNameResolver</c>.
/// </summary>
public class EmployeeBankReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- FullNameAr when Arabic, else FullNameEn.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "CivilId" column -- <c>rec.Employee.CivilID</c>, a numeric id in HCMv3.</summary>
    public long CivilId { get; set; }

    /// <summary>Legacy "Bank" column -- empty string (not null) when the record has no bank.</summary>
    public string BankName { get; set; }

    /// <summary>Legacy "EffectiveDate" column -- the record's <c>StartDate</c> (line 611).</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Legacy <c>OrgLevel1</c>..<c>OrgLevel7</c> columns. HCMv3 stores ONE
    /// <c>EmployeeOrgAssignment.OrganizationUnitId</c> into Zero's hierarchical OrganizationUnit tree, so
    /// each level is the ancestor-or-self of that leaf classified at the matching level. The values are
    /// stamped after materialization by <c>EmployeeOrgLevelNameResolver</c> and are therefore NOT
    /// server-sortable.
    /// </summary>
    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit the seven levels are resolved from. See <see cref="IEmployeeOrgLevelRow"/>.</summary>
    public long? OrganizationUnitId { get; set; }

    public string BankCountryName { get; set; }

    /// <summary>Legacy "PaymentType" column -- <c>PaymentType.ToString()</c>.</summary>
    public PaymentType PaymentType { get; set; }

    public string AccountNum { get; set; }

    public string PaymentCurrencyName { get; set; }

    public string IBAN { get; set; }
}
