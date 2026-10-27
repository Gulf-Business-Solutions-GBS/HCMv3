using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Authorization;
using HCMv3.Workflow;
using HCMv3.ProjectManagement;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.GovtRelationsManagement;
using HCMv3.GeneralMasterInfo;
using HCMv3.CompetencyDevelopment;
using HCMv3.AccommodationManagement;
using HCMv3.BenefitsManagement;
using HCMv3.PayrollManagement;
using HCMv3.PerformanceManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;

namespace HCMv3.EffectiveDated;

/// <summary>
/// The record types that keep a history per owner, and how to read that history: which fields group it, which date
/// makes a record effective, and which permission lets somebody see it.
///
/// <para>Adding a screen to "fill the form from the record in force on this date" is one line here plus one line in
/// the screen. Nothing else knows the list, so a type that is not here simply has no prefill -- never a wrong one.</para>
/// </summary>
public static class EffectiveDatedCatalog
{
    public static readonly IReadOnlyList<EffectiveDatedRecord> All = new List<EffectiveDatedRecord>
    {
        // ---------------------------------------------------------------- employee history
        new(typeof(EmployeeBasicPay), AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays, "EmployeeId"),
        new(typeof(EmployeeOrgAssignment), AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments, "EmployeeId"),
        new(typeof(EmployeeManager), AppPermissions.Pages_PersonnelAdministration_EmployeeManagers, "EmployeeId"),
        new(typeof(EmployeeContract), AppPermissions.Pages_PersonnelAdministration_EmployeeContracts, "EmployeeId"),
        new(typeof(EmployeeBank), AppPermissions.Pages_PersonnelAdministration_EmployeeBanks, "EmployeeId"),
        new(typeof(EmployeeSocialSecurity), AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurities, "EmployeeId"),
        new(typeof(EmployeeTicketRoute), AppPermissions.Pages_PersonnelAdministration_EmployeeTicketRoutes, "EmployeeId"),
        new(typeof(EmployeeRecurringPayment), AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments, "EmployeeId", "WageTypeId"),
        new(typeof(EmployeeCostDistribution), AppPermissions.Pages_PersonnelAdministration_EmployeeCostDistributions, "EmployeeId"),
        new(typeof(EmployeeAccountAssignment), AppPermissions.Pages_PersonnelAdministration_EmployeeAccountAssignments, "EmployeeId"),
        new(typeof(EmployeeDottedLineManager), AppPermissions.Pages_PersonnelAdministration_EmployeeDottedLineManagers, "EmployeeId"),
        new(typeof(EmployeeSponsor), AppPermissions.Pages_PersonnelAdministration_EmployeeSponsors, "EmployeeId"),
        new(typeof(EmployeeFund), AppPermissions.Pages_PersonnelAdministration_EmployeeFunds, "EmployeeId"),
        new(typeof(EmployeeExtdWorkLocation), AppPermissions.Pages_PersonnelAdministration_EmployeeExtdWorkLocations, "EmployeeId"),
        new(typeof(EmployeeDelegation), AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations, "EmployeeId"),
        new(typeof(EmployeeOnHold), AppPermissions.Pages_PersonnelAdministration_EmployeeOnHolds, "EmployeeId"),
        new(typeof(EmployeeAsset), AppPermissions.Pages_PersonnelAdministration_EmployeeAssets, "EmployeeId"),
        new(typeof(EmployeeLoan), AppPermissions.Pages_PersonnelAdministration_EmployeeLoans, "EmployeeId"),
        new(typeof(EmployeeInsurance), AppPermissions.Pages_BenefitsManagement_EmployeeInsurances, "EmployeeId"),

        // ---------------------------------------------------------------- time
        new(typeof(EmployeeTimePolicy), AppPermissions.Pages_TimeManagement_EmployeeTimePolicies, "EmployeeId"),
        new(typeof(EmployeeDutyPeriod), AppPermissions.Pages_TimeManagement_EmployeeDutyPeriods, "EmployeeId"),
        new(typeof(WorkLocationDutyPeriod), AppPermissions.Pages_TimeManagement_WorkLocationDutyPeriods, "WorkLocationId"),

        // ---------------------------------------------------------------- payroll and performance
        new(typeof(EmployeeWageAccount), AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts, "EmployeeId", "WageTypeId"),
        new(typeof(EmployeeWageAccGroup), AppPermissions.Pages_PayrollManagement_EmployeeWageAccGroups, "EmployeeId"),
        new(typeof(OrgUnitWageAccGroup), AppPermissions.Pages_PayrollManagement_OrgUnitWageAccGroups, "OrganizationUnitId"),
        new(typeof(TicketFare), AppPermissions.Pages_PayrollManagement_TicketFares, "TicketRouteId", "TicketPaxTypeId", "TicketClassId"),
        new(typeof(KpiByJobTitle), AppPermissions.Pages_PerformanceManagement_KpiByJobTitles, "JobTitleId"),
        new(typeof(KpiByOrgUnit), AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits, "OrganizationUnitId"),

        // ---------------------------------------------------------------- more employee history
        new(typeof(EmployeeVisaInfo), AppPermissions.Pages_PersonnelAdministration_EmployeeVisaInfos, "EmployeeId"),
        new(typeof(EmployeeAccommodation), AppPermissions.Pages_AccommodationManagement_EmployeeAccommodations, "EmployeeId"),
        new(typeof(EmployeeDutySchedule), AppPermissions.Pages_TimeManagement_EmployeeDutySchedules, "EmployeeId"),
        new(typeof(EmployeeProject), AppPermissions.Pages_ProjectManagement_EmployeeProjects, "EmployeeId"),
        new(typeof(EmployeeProjectDetail), AppPermissions.Pages_ProjectManagement_EmployeeProjectDetails, "EmployeeProjectId", "ProjectId"),
        new(typeof(EmployeeWorkflowRole), AppPermissions.Pages_Workflow_EmployeeWorkflowRoles, "EmployeeId", "WorkflowRoleId"),

        // A benefit exception is a history per employee AND per kind of benefit, so the kind is part of the key --
        // an air-ticket exception must never be filled in from a health one.
        new(typeof(EmployeeBenefitException), AppPermissions.Pages_PersonnelAdministration_EmployeeBenefitExceptions, "EmployeeId", "ExceptionBenefitType"),
        new(typeof(EmployeeDependantException), AppPermissions.Pages_PersonnelAdministration_EmployeeDependantExceptions, "EmployeeDependantId", "ExceptionBenefitType"),
        new(typeof(EmployeeFundExemption), AppPermissions.Pages_PersonnelAdministration_EmployeeFundExemptions, "EmployeeFundId"),

        // ---------------------------------------------------------------- sponsor
        new(typeof(SponsorBank), AppPermissions.Pages_GovtRelationsManagement_SponsorBanks, "SponsorId"),
        new(typeof(SponsorGovtRepresentative), AppPermissions.Pages_GovtRelationsManagement_SponsorGovtRepresentatives, "SponsorId", "EmployeeId"),
        new(typeof(SponsorJobTitle), AppPermissions.Pages_GovtRelationsManagement_SponsorJobTitles, "SponsorId", "GovtJobTitleId"),

        // ---------------------------------------------------------------- employee sub group rates
        new(typeof(EmployeeSubGroupAbsence), AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsences, "EmployeeSubGroupId", "AbsenceTypeId"),
        new(typeof(EmployeeSubGroupBonus), AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBonuses, "EmployeeSubGroupId"),
        new(typeof(EmployeeSubGroupGratuity), AppPermissions.Pages_PayrollManagement_EmployeeSubGroupGratuities, "EmployeeSubGroupId"),
        new(typeof(EmployeeSubGroupTicket), AppPermissions.Pages_PayrollManagement_EmployeeSubGroupTickets, "EmployeeSubGroupId", "TicketTypeId"),
        new(typeof(EmployeeSubGroupHealthReimburse), AppPermissions.Pages_PayrollManagement_EmployeeSubGroupHealthReimburses, "EmployeeSubGroupId", "DependantTypeId"),
        new(typeof(EmployeeSubGroupBenefitDetail), AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefitDetails, "EmployeeSubGroupBenefitId", "DependantTypeId"),

        // ---------------------------------------------------------------- company rates and rules
        new(typeof(WageAccount), AppPermissions.Pages_PayrollManagement_WageAccounts, "WageTypeId"),
        new(typeof(SocialSecurity), AppPermissions.Pages_PayrollManagement_SocialSecurities, "NationalityId"),

        // A social security rule is a history per scheme and per who it applies to; the flags for dependants and
        // disability are deliberately not keys, so a rule can still start from its nearest match.
        new(typeof(SocialSecurityRule), AppPermissions.Pages_PayrollManagement_SocialSecurityRules, "SocialSecurityId", "QualificationId", "Gender", "MaritalStatusId"),
        new(typeof(AbsenceDedRuleDetail), AppPermissions.Pages_PayrollManagement_AbsenceDedRuleDetails, "AbsenceDeductionRuleId"),
        new(typeof(AbsenceViolationRuleException), AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleExceptions, "AbsenceViolationRuleId", "EmployeeId"),
        new(typeof(BudgetingGroupRateDetail), AppPermissions.Pages_GeneralMasterInfo_BudgetingGroupRateDetails, "BudgetingGroupId"),
        new(typeof(OvertimeTypeDetail), AppPermissions.Pages_GeneralMasterInfo_OvertimeTypeDetails, "OvertimeTypeId", "OvertimeRateType"),
        new(typeof(CompetencyByJobTitle), AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitles, "JobTitleId"),

        // ---------------------------------------------------------------- rosters, buildings, positions
        new(typeof(RosterGroupDetail), AppPermissions.Pages_TimeManagement_RosterGroupDetails, "RosterGroupId", "EmployeeId"),
        new(typeof(RosterGroupSchedule), AppPermissions.Pages_TimeManagement_RosterGroupSchedules, "RosterGroupId", "DutyPeriodId"),
        new(typeof(BuildingContract), AppPermissions.Pages_AccommodationManagement_BuildingContracts, "BuildingId"),
        new(typeof(PositionRelationship), AppPermissions.Pages_ManpowerPlanningManagement_PositionRelationships, "PositionId", "RelationType"),
    };

    public static EffectiveDatedRecord Find(string recordType) =>
        All.FirstOrDefault(r => string.Equals(r.RecordType, recordType, StringComparison.OrdinalIgnoreCase));
}

/// <summary>One entry of <see cref="EffectiveDatedCatalog"/>.</summary>
public class EffectiveDatedRecord
{
    public EffectiveDatedRecord(Type entityType, string viewPermission, params string[] keyProperties)
    {
        EntityType = entityType;
        ViewPermission = viewPermission;
        KeyProperties = keyProperties;
    }

    public Type EntityType { get; }

    /// <summary>What the screen sends: the entity's own name, e.g. "EmployeeBasicPay".</summary>
    public string RecordType => EntityType.Name;

    /// <summary>Seeing the values of an earlier record is seeing the record, so it takes the same permission.</summary>
    public string ViewPermission { get; }

    /// <summary>The fields that separate one history from another: the employee, and anything else that splits it.</summary>
    public string[] KeyProperties { get; }

    /// <summary>Every entry uses StartDate; kept as a property so a type with a different name can join later.</summary>
    public string DateProperty => "StartDate";
}
