using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Info analysis grid report (legacy <c>uscRptEmployeeInfo.ascx</c>, 2 006
/// lines -- the largest control in the family together with <c>uscRptEmployeeCTC</c>).
///
/// Legacy HID both date pickers (uscRptEmployeeInfo.ascx:19-22, <c>Visible="false"</c>) and
/// <c>GetParameterStr</c> (lines 454-465) logs only Status and Code, so this report has NO date range;
/// every "as of" column is evaluated against TODAY. The inherited <c>FromDate</c>/<c>ToDate</c> are unused
/// and the Angular screen hides them.
/// </summary>
public class GetEmployeeInfoReportInput : GridReportInputBase
{
    /// <summary>Legacy iterated the employees ordered by Code (line 1024).</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>One dynamic basic-pay wage-type column (legacy line 737: one DataColumn per wage type).</summary>
public class EmployeeInfoWageAmountDto
{
    public string WageTypeCode { get; set; }

    public string WageTypeName { get; set; }

    public double Amount { get; set; }
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptEmployeeInfo.ascx.cs:469-1218) -- an "everything about
/// the employee, as of today" sheet.
///
/// Legacy column set, in order: OID, Code, Employee, FullNameAr, ReferenceCode, Status, EmploymentType,
/// BirthDate, Age, BloodGroup, ProbationDays, JoiningDate, ProbationEndDate, Nationality, Religion,
/// Gender, MaritalStatus, City, Governate, Country, Telephone, Mobile, CurrentAddress, PermanentAddress,
/// CivilID, LoginID, AccessID, PassportNumber, DrivingLicense, Email, BirthPlace, Qualification, Manager,
/// JobTitle, JobTitleAr, PayrollArea, EmployeeGroup, EmployeeSubGroup, WorkLocation, CostCenter,
/// WorkflowGroup, OrgLevel1..7 (+ an Ar variant of each), Sponsor, GovtJobTitle, ContractSalary,
/// ResidencyType, WorkVisaType, CivilIdExpiryDate, one column per basic-pay wage type, TotalAllowance,
/// TotalSalary, Currency, SeparationType, SeparationDate, LastDateOfWorking, FnfStatus, Bank, PaymentType,
/// AccountNum, IBAN, ContractType, ContractStartDate, ContractEndDate, AnnualLeaveOpening,
/// AbsentUnpaidOpening, TotalLoanBalance.
///
/// DEVIATIONS (all structural, none silent):
/// - <c>OrgLevelN</c>/<c>OrgLevelNAr</c> are resolved from the org unit TREE -- see
///   <c>EmployeeOrgLevelNameResolver</c>.
/// - Legacy's Passport column additionally required <c>EmployeeDocument.IsActive == Active</c>
///   (line 1183); HCMv3's <c>EmployeeDocument</c> has no such flag, so that clause is dropped.
/// - Legacy's Manager column used the XPO computed <c>Employee.CodeWithFullNameEn/Ar</c>; HCMv3 has no such
///   property, so the same "code - full name" string is composed in the projection.
/// </summary>
public class EmployeeInfoReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- always <c>FullNameEn</c> (line 1031).</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "FullNameAr" column.</summary>
    public string EmployeeNameAr { get; set; }

    public string ReferenceCode { get; set; }

    public EmployeeStatus Status { get; set; }

    public EmploymentType EmploymentType { get; set; }

    public DateTime BirthDate { get; set; }

    /// <summary>Legacy "Age" column: whole years as of today (lines 1036-1041).</summary>
    public int Age { get; set; }

    public string BloodGroup { get; set; }

    public int ProbationDays { get; set; }

    public DateTime JoiningDate { get; set; }

    public DateTime ProbationEndDate { get; set; }

    public string NationalityName { get; set; }

    public string ReligionName { get; set; }

    public Gender Gender { get; set; }

    public string MaritalStatusName { get; set; }

    public string City { get; set; }

    /// <summary>Legacy "Governate" column -- HCMv3 spells the entity <c>Governorate</c>.</summary>
    public string GovernorateName { get; set; }

    public string CountryName { get; set; }

    public long Telephone { get; set; }

    public long Mobile { get; set; }

    public string CurrentAddress { get; set; }

    public string PermanentAddress { get; set; }

    public long CivilID { get; set; }

    public string LoginID { get; set; }

    public string AccessID { get; set; }

    /// <summary>Legacy "PassportNumber" column: a currently-valid document of type "Passport".</summary>
    public string PassportNumber { get; set; }

    /// <summary>Legacy "DrivingLicense" column: a currently-valid document of type "DRIVING LICENSE".</summary>
    public string DrivingLicense { get; set; }

    public string Email { get; set; }

    public string BirthPlaceName { get; set; }

    public string QualificationName { get; set; }

    /// <summary>Legacy "Manager" column: the current manager's "code - full name".</summary>
    public string ManagerName { get; set; }

    /// <summary>Legacy "JobTitle" column -- always the English name (line 1074).</summary>
    public string JobTitle { get; set; }

    public string JobTitleAr { get; set; }

    public string PayrollAreaName { get; set; }

    public string EmployeeGroupName { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public string WorkLocationName { get; set; }

    public string CostCenterName { get; set; }

    /// <summary>Legacy "WorkflowGroup" column: the workflow group of today's org assignment (line 811).</summary>
    public string WorkflowGroupName { get; set; }

    public string SponsorName { get; set; }

    public string GovtJobTitleName { get; set; }

    public double ContractSalary { get; set; }

    public ResidencyType? ResidencyType { get; set; }

    public string WorkVisaTypeName { get; set; }

    /// <summary>
    /// Legacy "CivilIdExpiryDate": the LATEST expiry among documents of type "Civil ID". Legacy wrote
    /// <c>DateTime.MinValue</c> when there was none (line 1112); this port uses null.
    /// </summary>
    public DateTime? CivilIdExpiryDate { get; set; }

    /// <summary>
    /// One entry per wage type in <see cref="EmployeeInfoReportOutput.WageTypeNames"/>, same order.
    /// </summary>
    public List<EmployeeInfoWageAmountDto> WageAmounts { get; set; } = new();

    /// <summary>Legacy "TotalAllowance": sum of every basic-pay amount whose wage type is NOT "1000".</summary>
    public double TotalAllowance { get; set; }

    /// <summary>Legacy "TotalSalary": sum of every basic-pay amount.</summary>
    public double TotalSalary { get; set; }

    /// <summary>Legacy "Currency": the short form of the FIRST non-empty basic-pay detail currency.</summary>
    public string Currency { get; set; }

    public SeparationType? SeparationType { get; set; }

    /// <summary>Legacy "SeparationDate" column -- the separation's <c>SubmissionDate</c>.</summary>
    public DateTime? SeparationDate { get; set; }

    public DateTime? LastDateOfWorking { get; set; }

    /// <summary>
    /// Legacy "FnfStatus" (lines 1168-1178): "In Notice Period" / "Fnf Pending" / "Fnf Done", and blank
    /// when the employee has no separation record at all.
    /// </summary>
    public string FnfStatus { get; set; }

    public string BankName { get; set; }

    public PaymentType? PaymentType { get; set; }

    public string AccountNum { get; set; }

    public string IBAN { get; set; }

    public string ContractTypeName { get; set; }

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    /// <summary>Legacy "AnnualLeaveOpening": SUM(TransferCount) over transfer type code "1".</summary>
    public double AnnualLeaveOpening { get; set; }

    /// <summary>Legacy "AbsentUnpaidOpening": SUM(TransferCount) over transfer type code "5".</summary>
    public double AbsentUnpaidOpening { get; set; }

    /// <summary>Legacy "TotalLoanBalance": SUM(BalanceDueAmount) over ALL of the employee's loans.</summary>
    public double TotalLoanBalance { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public string OrgLevel1Ar { get; set; }
    public string OrgLevel2Ar { get; set; }
    public string OrgLevel3Ar { get; set; }
    public string OrgLevel4Ar { get; set; }
    public string OrgLevel5Ar { get; set; }
    public string OrgLevel6Ar { get; set; }
    public string OrgLevel7Ar { get; set; }

    public long? OrganizationUnitId { get; set; }
}

/// <summary>
/// Paged result plus the dynamic basic-pay wage-type column set (legacy lines 491/737 generated one
/// column per wage type present in any currently effective basic pay).
/// </summary>
public class EmployeeInfoReportOutput : PagedResultDto<EmployeeInfoReportRowDto>
{
    /// <summary>Wage-type display names, ordered by <c>WageType.PayrollOrder</c> as legacy did.</summary>
    public List<string> WageTypeNames { get; set; } = new();
}
