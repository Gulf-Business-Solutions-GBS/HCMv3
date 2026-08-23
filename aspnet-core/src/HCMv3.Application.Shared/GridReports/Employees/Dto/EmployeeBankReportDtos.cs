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
/// DEVIATION (structural, family-wide): the legacy seven OrgLevel columns do not exist in HCMv3 --
/// Wave B collapsed them into the single <c>EmployeeOrgAssignment.OrganizationUnitId</c> FK into Zero's
/// hierarchical OrganizationUnit tree -- so they are one <see cref="OrganizationUnitName"/> column here.
/// </summary>
public class EmployeeBankReportRowDto : EntityDto<int>
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

    /// <summary>Stands in for the legacy OrgLevel1..7 columns. Resolved on the assignment current
    /// TODAY -- legacy used <c>DateTime.Now</c> here, not the report dates (line 578).</summary>
    public string OrganizationUnitName { get; set; }

    public string BankCountryName { get; set; }

    /// <summary>Legacy "PaymentType" column -- <c>PaymentType.ToString()</c>.</summary>
    public PaymentType PaymentType { get; set; }

    public string AccountNum { get; set; }

    public string PaymentCurrencyName { get; set; }

    public string IBAN { get; set; }
}
