using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee CTC (cost-to-company) analysis grid report (legacy
/// <c>uscRptEmployeeCTC.ascx</c>, 1 926 lines).
///
/// Legacy HID both date pickers (uscRptEmployeeCTC.ascx:20-23, <c>Visible="false"</c>); the panel is
/// Employee Status, Payroll Area and Employee Code, and <c>GetParameterStr</c> (lines 486-500) logs exactly
/// those three. Every "as of" column is evaluated against TODAY.
/// </summary>
public class GetEmployeeCTCReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c>, which held the payroll area CODE and resolved its Oid before
    /// filtering (lines 1149-1157). This port takes the id directly -- same filter, one query fewer.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Legacy grid was unsorted; employee code is the deterministic default for server paging.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>One dynamic basic-pay wage-type column (legacy line 1381).</summary>
public class EmployeeCTCWageAmountDto
{
    public string WageTypeCode { get; set; }

    public string WageTypeName { get; set; }

    public double Amount { get; set; }
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptEmployeeCTC.ascx.cs:1124-1687).
///
/// Legacy column set, in order: OID, Code, Employee NameEn, Employee NameAr, Status, BirthDate,
/// ProbationDays, JoiningDate, ProbationEndDate, Nationality, Religion, Gender, MaritalStatus,
/// RecruitmentType, City, Governate, Country, Telephone, Mobile, CurrentAddress, PermanentAddress,
/// CivilID, LoginID, AccessID, PassportNumber, Email, BirthPlace, ResidencyType, ContractType,
/// ContractStartDate, ContractEndDate, AccountNumber, IBAN, BankBranch, Sponsor, Manager, JobTitle,
/// PayrollArea, EmployeeGroup, EmployeeSubGroup, OrgLevel1..7, WorkLocationCode, WorkLocation,
/// CostCenterCode, CostCenter, CostCenterGroup, PostingGroup, BudgetingGroup, WorkflowGroup,
/// EffectiveDate, one column per basic-pay wage type, TotalSalary, Total Absent Unpaid Days,
/// LeaveBalance, LeaveProvisionAmount, IndemnityBalance, IndemnityProvisionAmount, SeparationType,
/// SubmissionDate, LastDayInService, RevokeDate, RevokeComments.
///
/// DEVIATIONS (all structural, none silent):
/// - <c>WorkflowGroup</c> is DROPPED: HCMv3's <c>EmployeeOrgAssignment</c> has no workflow-group FK.
/// - <c>RevokeComments</c> is DROPPED: HCMv3's <c>EmployeeSeparation</c> has no such property (it keeps
///   only the nullable <c>RevokeDate</c>).
/// - Legacy's Passport column additionally required <c>EmployeeDocument.IsActive == Active</c>
///   (line 1462); HCMv3's <c>EmployeeDocument</c> has no such flag, so that clause is dropped.
/// - Legacy's Manager column used the XPO computed <c>Employee.CodeWithFullNameEn</c>; HCMv3 has no such
///   property, so the same "code - full name" string is composed in the projection.
/// - <c>OrgLevelN</c> is resolved from the org unit TREE -- see <c>EmployeeOrgLevelNameResolver</c>.
/// </summary>
public class EmployeeCTCReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee NameEn" column.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "Employee NameAr" column.</summary>
    public string EmployeeNameAr { get; set; }

    public EmployeeStatus Status { get; set; }

    public DateTime BirthDate { get; set; }

    public int ProbationDays { get; set; }

    public DateTime JoiningDate { get; set; }

    public DateTime ProbationEndDate { get; set; }

    public string NationalityName { get; set; }

    public string ReligionName { get; set; }

    public Gender Gender { get; set; }

    public string MaritalStatusName { get; set; }

    public string RecruitmentTypeName { get; set; }

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

    public string PassportNumber { get; set; }

    public string Email { get; set; }

    public string BirthPlaceName { get; set; }

    public ResidencyType? ResidencyType { get; set; }

    public string ContractTypeName { get; set; }

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    public string AccountNumber { get; set; }

    public string IBAN { get; set; }

    public string BankBranch { get; set; }

    public string SponsorName { get; set; }

    /// <summary>Legacy "Manager" column: the current manager's "code - full name".</summary>
    public string ManagerName { get; set; }

    public string JobTitle { get; set; }

    public string PayrollAreaName { get; set; }

    public string EmployeeGroupName { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public string WorkLocationCode { get; set; }

    public string WorkLocationName { get; set; }

    public string CostCenterCode { get; set; }

    public string CostCenterName { get; set; }

    public string CostCenterGroupName { get; set; }

    public string PostingGroupName { get; set; }

    public string BudgetingGroupName { get; set; }

    /// <summary>Legacy "EffectiveDate": the start date of the basic pay in force today.</summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>One entry per wage type in <see cref="EmployeeCTCReportOutput.WageTypeNames"/>, same order.</summary>
    public List<EmployeeCTCWageAmountDto> WageAmounts { get; set; } = new();

    /// <summary>Legacy "TotalSalary": the <c>EmployeeBasicPay.Amount</c> of the pay in force today.</summary>
    public double TotalSalary { get; set; }

    /// <summary>Legacy "Total Absent Unpaid Days": Number of wage type 4024 on the latest regular register.</summary>
    public double TotalAbsentUnpaidDays { get; set; }

    /// <summary>Legacy "LeaveBalance": Number of wage type 3004.</summary>
    public double LeaveBalance { get; set; }

    /// <summary>Legacy "LeaveProvisionAmount": Amount of wage type 3004.</summary>
    public double LeaveProvisionAmount { get; set; }

    /// <summary>Legacy "IndemnityBalance": Number of wage type 3003.</summary>
    public double IndemnityBalance { get; set; }

    /// <summary>Legacy "IndemnityProvisionAmount": Amount of wage type 3003.</summary>
    public double IndemnityProvisionAmount { get; set; }

    public SeparationType? SeparationType { get; set; }

    public DateTime? SubmissionDate { get; set; }

    public DateTime? LastDayInService { get; set; }

    public DateTime? RevokeDate { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }
}

/// <summary>
/// Paged result plus the dynamic basic-pay wage-type column set (legacy lines 1159-1165 / 1381 generated
/// one column per wage type present in any currently effective basic pay, ordered by PayrollOrder).
/// </summary>
public class EmployeeCTCReportOutput : PagedResultDto<EmployeeCTCReportRowDto>
{
    public List<string> WageTypeNames { get; set; } = new();
}
