using System;
using System.Linq.Expressions;
using Abp.Json;
using Abp.OpenIddict.Applications;
using Abp.OpenIddict.Authorizations;
using Abp.OpenIddict.EntityFrameworkCore;
using Abp.OpenIddict.Scopes;
using Abp.OpenIddict.Tokens;
using Abp.Zero.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using HCMv3.AccommodationManagement;
using HCMv3.Authorization.Delegation;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Session;
using HCMv3.Authorization.Users;
using HCMv3.BenefitsManagement;
using HCMv3.Chat;
using HCMv3.CompetencyDevelopment;
using HCMv3.Editions;
using HCMv3.EngineRuns;
using HCMv3.ExtraProperties;
using HCMv3.Friendships;
using HCMv3.GatePassManagement;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup;
using HCMv3.GovtRelationsManagement;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.JobProfileManagement;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.MultiTenancy;
using HCMv3.MultiTenancy.Accounting;
using HCMv3.MultiTenancy.Payments;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PerformanceManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.Recruitment;
using HCMv3.ReportManagement;
using HCMv3.Storage;
using HCMv3.SuccessionManagement;
using HCMv3.SurveyManagement;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.TrainingManagement;
using HCMv3.TravelManagement;
using HCMv3.Workflow;

namespace HCMv3.EntityFrameworkCore;

public class HCMv3DbContext : AbpZeroDbContext<Tenant, Role, User, HCMv3DbContext>, IOpenIddictDbContext
{
    /* Define an IDbSet for each entity of the application */

    public virtual DbSet<OpenIddictApplication> Applications { get; }

    public virtual DbSet<OpenIddictAuthorization> Authorizations { get; }

    public virtual DbSet<OpenIddictScope> Scopes { get; }

    public virtual DbSet<OpenIddictToken> Tokens { get; }

    public virtual DbSet<BinaryObject> BinaryObjects { get; set; }

    public virtual DbSet<Friendship> Friendships { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<SubscribableEdition> SubscribableEditions { get; set; }

    public virtual DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }

    public virtual DbSet<SubscriptionPaymentProduct> SubscriptionPaymentProducts { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<UserDelegation> UserDelegations { get; set; }

    public virtual DbSet<RecentPassword> RecentPasswords { get; set; }

	public virtual DbSet<UserAccountLink> UserAccountLinks { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<AccessUnit> AccessUnits { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<BankFile> BankFiles { get; set; }

    public virtual DbSet<BenefitType> BenefitTypes { get; set; }

    public virtual DbSet<ContractType> ContractTypes { get; set; }

    public virtual DbSet<DateType> DateTypes { get; set; }

    public virtual DbSet<DocumentPlace> DocumentPlaces { get; set; }

    public virtual DbSet<Education> Educations { get; set; }

    public virtual DbSet<EducationBranch> EducationBranches { get; set; }

    public virtual DbSet<Governorate> Governorates { get; set; }

    public virtual DbSet<GrievanceType> GrievanceTypes { get; set; }

    public virtual DbSet<HRServiceType> HRServiceTypes { get; set; }

    public virtual DbSet<ITServiceType> ITServiceTypes { get; set; }

    public virtual DbSet<LanguageInfo> LanguageInfos { get; set; }

    public virtual DbSet<MaritalStatus> MaritalStatuses { get; set; }

    public virtual DbSet<NationalityGroup> NationalityGroups { get; set; }

    public virtual DbSet<NewsType> NewsTypes { get; set; }

    public virtual DbSet<PassportType> PassportTypes { get; set; }

    public virtual DbSet<PostingGroup> PostingGroups { get; set; }

    public virtual DbSet<Qualification> Qualifications { get; set; }

    public virtual DbSet<RecruitmentType> RecruitmentTypes { get; set; }

    public virtual DbSet<RelationshipType> RelationshipTypes { get; set; }

    public virtual DbSet<Religion> Religions { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<TaskType> TaskTypes { get; set; }

    public virtual DbSet<Title> Titles { get; set; }

    public virtual DbSet<TrainingType> TrainingTypes { get; set; }

    public virtual DbSet<University> Universities { get; set; }

    public virtual DbSet<VisaType> VisaTypes { get; set; }

    public virtual DbSet<WorkVisaType> WorkVisaTypes { get; set; }

    public virtual DbSet<AssetType> AssetTypes { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<Institution> Institutions { get; set; }

    public virtual DbSet<Nationality> Nationalities { get; set; }

    public virtual DbSet<DependantType> DependantTypes { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<SeparationReason> SeparationReasons { get; set; }

    public virtual DbSet<DisciplinaryDeductionType> DisciplinaryDeductionTypes { get; set; }

    public virtual DbSet<PersonalAction> PersonalActions { get; set; }

    public virtual DbSet<PersonalActionReason> PersonalActionReasons { get; set; }

    public virtual DbSet<Bank> Banks { get; set; }

    public virtual DbSet<PayeeBankDetail> PayeeBankDetails { get; set; }

    public virtual DbSet<BudgetingGroup> BudgetingGroups { get; set; }

    public virtual DbSet<BudgetingGroupRateDetail> BudgetingGroupRateDetails { get; set; }

    public virtual DbSet<ClearanceRequestType> ClearanceRequestTypes { get; set; }

    public virtual DbSet<ClearanceRequestField> ClearanceRequestFields { get; set; }

    public virtual DbSet<ClearanceRequestFieldDetail> ClearanceRequestFieldDetails { get; set; }

    public virtual DbSet<DisciplinaryType> DisciplinaryTypes { get; set; }

    public virtual DbSet<DisciplinaryTypeDetail> DisciplinaryTypeDetails { get; set; }

    public virtual DbSet<NotificationConfig> NotificationConfigs { get; set; }

    public virtual DbSet<NotificationGroup> NotificationGroups { get; set; }

    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    public virtual DbSet<DocumentPrintForm> DocumentPrintForms { get; set; }

    public virtual DbSet<LoanType> LoanTypes { get; set; }

    public virtual DbSet<LoanTypeDetail> LoanTypeDetails { get; set; }

    public virtual DbSet<OvertimeType> OvertimeTypes { get; set; }

    public virtual DbSet<OvertimeTypeDetail> OvertimeTypeDetails { get; set; }

    public virtual DbSet<Asset> Assets { get; set; }

    public virtual DbSet<NumberSeries> NumberSeries { get; set; }

    public virtual DbSet<CurrencyConvertor> CurrencyConvertors { get; set; }

    public virtual DbSet<CostCenterGroup> CostCenterGroups { get; set; }

    public virtual DbSet<CostCenter> CostCenters { get; set; }

    public virtual DbSet<CostElementType> CostElementTypes { get; set; }

    public virtual DbSet<CostElement> CostElements { get; set; }

    public virtual DbSet<JobTitle> JobTitles { get; set; }

    public virtual DbSet<OrganizationUnitType> OrganizationUnitTypes { get; set; }

    public virtual DbSet<OrganizationBankInfo> OrganizationBankInfos { get; set; }

    public virtual DbSet<WorkLocation> WorkLocations { get; set; }

    public virtual DbSet<OrgUnitProfile> OrgUnitProfiles { get; set; }

    public virtual DbSet<GoLiveStepState> GoLiveStepStates { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeDate> EmployeeDates { get; set; }

    public virtual DbSet<EmployeeManager> EmployeeManagers { get; set; }

    public virtual DbSet<EmployeeDottedLineManager> EmployeeDottedLineManagers { get; set; }

    public virtual DbSet<EmployeeDelegation> EmployeeDelegations { get; set; }

    public virtual DbSet<EmployeeRelationship> EmployeeRelationships { get; set; }

    public virtual DbSet<EmployeeSignature> EmployeeSignatures { get; set; }

    public virtual DbSet<EmployeeEducation> EmployeeEducations { get; set; }

    public virtual DbSet<EmployeeCertification> EmployeeCertifications { get; set; }

    public virtual DbSet<EmployeeWorkExperience> EmployeeWorkExperiences { get; set; }

    public virtual DbSet<EmployeeDocument> EmployeeDocuments { get; set; }

    public virtual DbSet<EmployeeDocumentMvmt> EmployeeDocumentMvmts { get; set; }

    public virtual DbSet<EmployeeVisaInfo> EmployeeVisaInfos { get; set; }

    public virtual DbSet<EmployeeOrgAssignment> EmployeeOrgAssignments { get; set; }

    public virtual DbSet<EmployeeContract> EmployeeContracts { get; set; }

    public virtual DbSet<EmployeeAction> EmployeeActions { get; set; }

    public virtual DbSet<EmployeeSeparation> EmployeeSeparations { get; set; }

    public virtual DbSet<EmployeeRejoin> EmployeeRejoins { get; set; }

    public virtual DbSet<TimeType> TimeTypes { get; set; }

    public virtual DbSet<TransType> TransTypes { get; set; }

    public virtual DbSet<TimeTransferType> TimeTransferTypes { get; set; }

    public virtual DbSet<BreakShift> BreakShifts { get; set; }

    public virtual DbSet<Duty> Duties { get; set; }

    public virtual DbSet<DailyDuty> DailyDuties { get; set; }

    public virtual DbSet<HolidayCalendar> HolidayCalendars { get; set; }

    public virtual DbSet<Holiday> Holidays { get; set; }

    public virtual DbSet<HolidayPeriod> HolidayPeriods { get; set; }

    public virtual DbSet<TimePolicy> TimePolicies { get; set; }

    public virtual DbSet<DutyPeriod> DutyPeriods { get; set; }

    public virtual DbSet<DutyPeriodDetail> DutyPeriodDetails { get; set; }

    public virtual DbSet<RosterDutyPeriod> RosterDutyPeriods { get; set; }

    public virtual DbSet<RosterDutyPeriodDetail> RosterDutyPeriodDetails { get; set; }

    public virtual DbSet<RosterDutySchedule> RosterDutySchedules { get; set; }

    public virtual DbSet<RosterGroup> RosterGroups { get; set; }

    public virtual DbSet<RosterGroupDetail> RosterGroupDetails { get; set; }

    public virtual DbSet<RosterGroupSchedule> RosterGroupSchedules { get; set; }

    public virtual DbSet<RosterDailyDuty> RosterDailyDuties { get; set; }

    public virtual DbSet<EmployeeDutyPeriod> EmployeeDutyPeriods { get; set; }

    public virtual DbSet<EmployeeTimePolicy> EmployeeTimePolicies { get; set; }

    public virtual DbSet<EmployeeSubstituteDuty> EmployeeSubstituteDuties { get; set; }

    public virtual DbSet<EmployeeDutySchedule> EmployeeDutySchedules { get; set; }

    public virtual DbSet<WorkLocationDutyPeriod> WorkLocationDutyPeriods { get; set; }

    public virtual DbSet<EmployeeTimesheet> EmployeeTimesheets { get; set; }

    public virtual DbSet<TimeEvent> TimeEvents { get; set; }

    public virtual DbSet<TimeAttAPI> TimeAttAPIs { get; set; }

    public virtual DbSet<TimeLog> TimeLogs { get; set; }

    public virtual DbSet<TimeRegister> TimeRegisters { get; set; }

    public virtual DbSet<MonthlyTimeRegister> MonthlyTimeRegisters { get; set; }

    public virtual DbSet<MonthlyTimeRegisterDetail> MonthlyTimeRegisterDetails { get; set; }

    public virtual DbSet<YearlyTimeRegister> YearlyTimeRegisters { get; set; }

    public virtual DbSet<YearlyTimeRegisterDetail> YearlyTimeRegisterDetails { get; set; }

    public virtual DbSet<SimulationTimeRegister> SimulationTimeRegisters { get; set; }

    public virtual DbSet<SimulationTimeRegisterDetail> SimulationTimeRegisterDetails { get; set; }

    public virtual DbSet<TimeTransfer> TimeTransfers { get; set; }

    public virtual DbSet<AXBatch> AXBatches { get; set; }

    public virtual DbSet<AbsenceType> AbsenceTypes { get; set; }

    public virtual DbSet<AbsenceTypeException> AbsenceTypeExceptions { get; set; }

    public virtual DbSet<AbsenceDeductionRule> AbsenceDeductionRules { get; set; }

    public virtual DbSet<AbsenceDedRuleDetail> AbsenceDedRuleDetails { get; set; }

    public virtual DbSet<AbsenceViolationRule> AbsenceViolationRules { get; set; }

    public virtual DbSet<AbsenceViolationRuleDetail> AbsenceViolationRuleDetails { get; set; }

    public virtual DbSet<AbsenceViolationRuleException> AbsenceViolationRuleExceptions { get; set; }

    public virtual DbSet<AttendanceType> AttendanceTypes { get; set; }

    public virtual DbSet<AccountType> PayrollAccountTypes { get; set; }

    public virtual DbSet<AccountInfo> AccountInfos { get; set; }

    public virtual DbSet<EmployeeGroup> EmployeeGroups { get; set; }

    public virtual DbSet<PayrollReasonType> PayrollReasonTypes { get; set; }

    public virtual DbSet<EmployeeSubGroup> EmployeeSubGroups { get; set; }

    public virtual DbSet<EmployeeSubGroupAbsence> EmployeeSubGroupAbsences { get; set; }

    public virtual DbSet<EmployeeSubGroupAbsenceType> EmployeeSubGroupAbsenceTypes { get; set; }

    public virtual DbSet<EmployeeSubGroupAssetAllowance> EmployeeSubGroupAssetAllowances { get; set; }

    public virtual DbSet<EmployeeSubGroupBenefit> EmployeeSubGroupBenefits { get; set; }

    public virtual DbSet<EmployeeSubGroupBenefitDetail> EmployeeSubGroupBenefitDetails { get; set; }

    public virtual DbSet<EmployeeSubGroupBonus> EmployeeSubGroupBonuses { get; set; }

    public virtual DbSet<EmployeeSubGroupCommission> EmployeeSubGroupCommissions { get; set; }

    public virtual DbSet<EmployeeSubGroupFund> EmployeeSubGroupFunds { get; set; }

    public virtual DbSet<EmployeeSubGroupGratuity> EmployeeSubGroupGratuities { get; set; }

    public virtual DbSet<EmployeeSubGroupHealthReimburse> EmployeeSubGroupHealthReimburses { get; set; }

    public virtual DbSet<EmployeeSubGroupPayment> EmployeeSubGroupPayments { get; set; }

    public virtual DbSet<EmployeeSubGroupTicket> EmployeeSubGroupTickets { get; set; }

    public virtual DbSet<EmployeeSubGroupWageType> EmployeeSubGroupWageTypes { get; set; }

    public virtual DbSet<PayrollPeriod> PayrollPeriods { get; set; }

    public virtual DbSet<PayrollArea> PayrollAreas { get; set; }

    public virtual DbSet<PayrollCalendar> PayrollCalendars { get; set; }

    public virtual DbSet<PayrollControlRecord> PayrollControlRecords { get; set; }

    public virtual DbSet<IndemnityPayRule> IndemnityPayRules { get; set; }

    public virtual DbSet<IndemnityPayRuleDetail> IndemnityPayRuleDetails { get; set; }

    public virtual DbSet<OrgUnitWageAccGroup> OrgUnitWageAccGroups { get; set; }

    public virtual DbSet<WageAccGroup> WageAccGroups { get; set; }

    public virtual DbSet<WageAccGroupDetail> WageAccGroupDetails { get; set; }

    public virtual DbSet<WageType> WageTypes { get; set; }

    public virtual DbSet<WageTypeException> WageTypeExceptions { get; set; }

    public virtual DbSet<WageTypeLeave> WageTypeLeaves { get; set; }

    public virtual DbSet<WageTypeSecurityRole> WageTypeSecurityRoles { get; set; }

    public virtual DbSet<WageAccount> WageAccounts { get; set; }

    public virtual DbSet<SocialSecurity> SocialSecurities { get; set; }

    public virtual DbSet<SocialSecurityDetail> SocialSecurityDetails { get; set; }

    public virtual DbSet<SocialSecurityMaritalAllowance> SocialSecurityMaritalAllowances { get; set; }

    public virtual DbSet<SocialSecurityEducationAllowance> SocialSecurityEducationAllowances { get; set; }

    public virtual DbSet<SocialSecurityRule> SocialSecurityRules { get; set; }

    public virtual DbSet<TicketClass> TicketClasses { get; set; }

    public virtual DbSet<TicketPaxType> TicketPaxTypes { get; set; }

    public virtual DbSet<TicketRoute> TicketRoutes { get; set; }

    public virtual DbSet<TicketFare> TicketFares { get; set; }

    public virtual DbSet<TicketType> TicketTypes { get; set; }

    public virtual DbSet<EmployeeWageAccGroup> EmployeeWageAccGroups { get; set; }

    public virtual DbSet<EmployeeWageAccount> EmployeeWageAccounts { get; set; }

    public virtual DbSet<EmployeePayrollStatus> EmployeePayrollStatuses { get; set; }

    public virtual DbSet<AbsencePayment> AbsencePayments { get; set; }

    public virtual DbSet<TempBenefitType> TempBenefitTypes { get; set; }

    public virtual DbSet<TempEmployeeBenefit> TempEmployeeBenefits { get; set; }

    public virtual DbSet<WageTypeRegister> WageTypeRegisters { get; set; }

    public virtual DbSet<PayrollLog> PayrollLogs { get; set; }

    public virtual DbSet<CostRegister> CostRegisters { get; set; }

    public virtual DbSet<CostRegisterDetail> CostRegisterDetails { get; set; }

    public virtual DbSet<SalaryRegister> SalaryRegisters { get; set; }

    public virtual DbSet<SalaryRegisterDetail> SalaryRegisterDetails { get; set; }

    public virtual DbSet<SalaryRegisterDaysDetail> SalaryRegisterDaysDetails { get; set; }

    public virtual DbSet<SimulationSalaryRegister> SimulationSalaryRegisters { get; set; }

    public virtual DbSet<SimulationSalaryRegisterDetail> SimulationSalaryRegisterDetails { get; set; }

    public virtual DbSet<SimulationSalaryRegisterDaysDetail> SimulationSalaryRegisterDaysDetails { get; set; }

    public virtual DbSet<AbsenceViolationActionRecord> AbsenceViolationActionRecords { get; set; }

    public virtual DbSet<AbsenceViolationActionLog> AbsenceViolationActionLogs { get; set; }

    public virtual DbSet<GratuityAccrualSummary> GratuityAccrualSummaries { get; set; }

    public virtual DbSet<LeaveAccrualSummary> LeaveAccrualSummaries { get; set; }

    public virtual DbSet<TicketAccrualSummary> TicketAccrualSummaries { get; set; }

    public virtual DbSet<BusinessType> BusinessTypes { get; set; }

    public virtual DbSet<GovtJobCategory> GovtJobCategories { get; set; }

    public virtual DbSet<GovtJobTitle> GovtJobTitles { get; set; }

    public virtual DbSet<Signatory> Signatories { get; set; }

    public virtual DbSet<Sponsor> Sponsors { get; set; }

    public virtual DbSet<SponsorJobTitle> SponsorJobTitles { get; set; }

    public virtual DbSet<SponsorSignatory> SponsorSignatories { get; set; }

    public virtual DbSet<SponsorGovtRepresentative> SponsorGovtRepresentatives { get; set; }

    public virtual DbSet<SponsorLicenseInfo> SponsorLicenseInfos { get; set; }

    public virtual DbSet<OrganizationDocument> OrganizationDocuments { get; set; }

    public virtual DbSet<EmployeeExitPermit> EmployeeExitPermits { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<Building> Buildings { get; set; }

    public virtual DbSet<BuildingContract> BuildingContracts { get; set; }

    public virtual DbSet<Floor> Floors { get; set; }

    public virtual DbSet<Flat> Flats { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Bed> Beds { get; set; }

    public virtual DbSet<AccommodationType> AccommodationTypes { get; set; }

    public virtual DbSet<AccommodationServiceType> AccommodationServiceTypes { get; set; }

    public virtual DbSet<EmployeeAccommodation> EmployeeAccommodations { get; set; }

    public virtual DbSet<AccommodationServices> AccommodationServices { get; set; }

    public virtual DbSet<ProjectType> ProjectTypes { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectWageType> ProjectWageTypes { get; set; }

    public virtual DbSet<EmployeeProject> EmployeeProjects { get; set; }

    public virtual DbSet<EmployeeProjectDetail> EmployeeProjectDetails { get; set; }

    public virtual DbSet<ProjectRegister> ProjectRegisters { get; set; }

    public virtual DbSet<ProjectRegisterDetail> ProjectRegisterDetails { get; set; }

    public virtual DbSet<ProjectLog> ProjectLogs { get; set; }

    /// <summary>
    /// Live engine-run progress. Written ONLY through
    /// <c>HCMv3.EngineRuns.Services.IEngineRunProgressWriter</c>, which commits on its own connection so a
    /// reader can observe progress while the engine's outer transaction is still open. Nothing inside an
    /// engine's main transaction may touch these two tables -- see <c>EngineRun</c> remarks.
    /// </summary>
    public virtual DbSet<EngineRun> EngineRuns { get; set; }

    /// <inheritdoc cref="EngineRuns"/>
    public virtual DbSet<EngineRunEmployee> EngineRunEmployees { get; set; }

    public virtual DbSet<EmployeeAbsence> EmployeeAbsences { get; set; }

    public virtual DbSet<EmployeeAbsenceQuota> EmployeeAbsenceQuotas { get; set; }

    public virtual DbSet<EmployeeAbsAdjustment> EmployeeAbsAdjustments { get; set; }

    public virtual DbSet<EmployeeAbsQuotaForfeit> EmployeeAbsQuotaForfeits { get; set; }

    public virtual DbSet<EmployeeAttendance> EmployeeAttendances { get; set; }

    public virtual DbSet<EmployeeAttendanceQuota> EmployeeAttendanceQuotas { get; set; }

    public virtual DbSet<EmployeeBasicPay> EmployeeBasicPays { get; set; }

    public virtual DbSet<EmployeeBasicPayDetail> EmployeeBasicPayDetails { get; set; }

    public virtual DbSet<EmployeeOnHold> EmployeeOnHolds { get; set; }

    public virtual DbSet<EmployeeGratuity> EmployeeGratuities { get; set; }

    public virtual DbSet<EmployeeGratuityQuota> EmployeeGratuityQuotas { get; set; }

    public virtual DbSet<EmployeeLoan> EmployeeLoans { get; set; }

    public virtual DbSet<EmployeeLoanExemption> EmployeeLoanExemptions { get; set; }

    public virtual DbSet<EmployeeLoanRecovery> EmployeeLoanRecoveries { get; set; }

    public virtual DbSet<LoanInstallment> LoanInstallments { get; set; }

    public virtual DbSet<EmployeeFund> EmployeeFunds { get; set; }

    public virtual DbSet<EmployeeFundDetail> EmployeeFundDetails { get; set; }

    public virtual DbSet<EmployeeFundExemption> EmployeeFundExemptions { get; set; }

    public virtual DbSet<EmployeeTicket> EmployeeTickets { get; set; }

    public virtual DbSet<EmployeeTicketQuota> EmployeeTicketQuotas { get; set; }

    public virtual DbSet<EmployeeTicketRoute> EmployeeTicketRoutes { get; set; }

    public virtual DbSet<EmployeeTktAdjustment> EmployeeTktAdjustments { get; set; }

    public virtual DbSet<EmployeeSocialSecurity> EmployeeSocialSecurities { get; set; }

    public virtual DbSet<EmployeeSocialSecurityClaim> EmployeeSocialSecurityClaims { get; set; }

    public virtual DbSet<EmployeeDependant> EmployeeDependants { get; set; }

    public virtual DbSet<EmployeeDependantException> EmployeeDependantExceptions { get; set; }

    public virtual DbSet<EmployeeSponsor> EmployeeSponsors { get; set; }

    public virtual DbSet<EmployeeFnFPayment> EmployeeFnFPayments { get; set; }

    public virtual DbSet<SponsorBank> SponsorBanks { get; set; }

    public virtual DbSet<EmployeeBank> EmployeeBanks { get; set; }

    public virtual DbSet<EmployeeAsset> EmployeeAssets { get; set; }

    public virtual DbSet<EmployeeAssetInvoice> EmployeeAssetInvoices { get; set; }

    public virtual DbSet<EmployeeHRService> EmployeeHRServices { get; set; }

    public virtual DbSet<EmployeeITService> EmployeeITServices { get; set; }

    public virtual DbSet<EmployeeDisciplinary> EmployeeDisciplinaries { get; set; }

    public virtual DbSet<EmployeeGrievance> EmployeeGrievances { get; set; }

    public virtual DbSet<EmployeeHealthReimburse> EmployeeHealthReimburses { get; set; }

    public virtual DbSet<EmployeeOvertime> EmployeeOvertimes { get; set; }

    public virtual DbSet<EmployeePayment> EmployeePayments { get; set; }

    public virtual DbSet<EmployeeRecurringPayment> EmployeeRecurringPayments { get; set; }

    public virtual DbSet<EmployeeRemuneration> EmployeeRemunerations { get; set; }

    public virtual DbSet<EmployeeReward> EmployeeRewards { get; set; }

    public virtual DbSet<EmployeeCostDistribution> EmployeeCostDistributions { get; set; }

    public virtual DbSet<EmployeeAccountAssignment> EmployeeAccountAssignments { get; set; }

    public virtual DbSet<EmployeeExtdWorkLocation> EmployeeExtdWorkLocations { get; set; }

    public virtual DbSet<EmployeeBenefitException> EmployeeBenefitExceptions { get; set; }

    public virtual DbSet<EmployeeNonPayrollTrans> EmployeeNonPayrollTrans { get; set; }

    public virtual DbSet<EmployeeTask> EmployeeTasks { get; set; }

    public virtual DbSet<FormType> FormTypes { get; set; }

    public virtual DbSet<KpiCategory> KpiCategories { get; set; }

    public virtual DbSet<KpiInfo> KpiInfos { get; set; }

    public virtual DbSet<KpiByJobTitle> KpiByJobTitles { get; set; }

    public virtual DbSet<KpiByJobTitleDetail> KpiByJobTitleDetails { get; set; }

    public virtual DbSet<KpiByOrgUnit> KpiByOrgUnits { get; set; }

    public virtual DbSet<KpiByOrgUnitDetail> KpiByOrgUnitDetails { get; set; }

    public virtual DbSet<PARating> PARatings { get; set; }

    public virtual DbSet<PACycle> PACycles { get; set; }

    public virtual DbSet<PAPeriod> PAPeriods { get; set; }

    public virtual DbSet<PATimeline> PATimelines { get; set; }

    public virtual DbSet<PATimelineException> PATimelineExceptions { get; set; }

    public virtual DbSet<PAPeerNominee> PAPeerNominees { get; set; }

    public virtual DbSet<PAForm> PAForms { get; set; }

    public virtual DbSet<PAPlan> PAPlans { get; set; }

    public virtual DbSet<PAQuarter1Review> PAQuarter1Reviews { get; set; }

    public virtual DbSet<PAMidYearReview> PAMidYearReviews { get; set; }

    public virtual DbSet<PAQuarter3Review> PAQuarter3Reviews { get; set; }

    public virtual DbSet<PAAnnualYearReview> PAAnnualYearReviews { get; set; }

    public virtual DbSet<PAGoal> PAGoals { get; set; }

    public virtual DbSet<PAPeerReview> PAPeerReviews { get; set; }

    public virtual DbSet<PAEmployee> PAEmployees { get; set; }

    public virtual DbSet<PAEmployeeDetail> PAEmployeeDetails { get; set; }

    public virtual DbSet<PAEmployeeReviewer> PAEmployeeReviewers { get; set; }

    public virtual DbSet<PAEmployeeRating> PAEmployeeRatings { get; set; }

    public virtual DbSet<ExpenseType> ExpenseTypes { get; set; }

    public virtual DbSet<Hotel> Hotels { get; set; }

    public virtual DbSet<TravelAirline> TravelAirlines { get; set; }

    public virtual DbSet<TravelCancelReason> TravelCancelReasons { get; set; }

    public virtual DbSet<TravelDestination> TravelDestinations { get; set; }

    public virtual DbSet<TravelType> TravelTypes { get; set; }

    public virtual DbSet<TravelPerDiem> TravelPerDiems { get; set; }

    public virtual DbSet<Travel> Travels { get; set; }

    public virtual DbSet<TravelDetail> TravelDetails { get; set; }

    public virtual DbSet<TravelDetailCost> TravelDetailCosts { get; set; }

    public virtual DbSet<TravelFeedback> TravelFeedbacks { get; set; }

    public virtual DbSet<WorkflowRole> WorkflowRoles { get; set; }

    public virtual DbSet<WorkflowGroup> WorkflowGroups { get; set; }

    public virtual DbSet<WorkflowGroupDetail> WorkflowGroupDetails { get; set; }

    public virtual DbSet<WorkflowGroupFieldAccess> WorkflowGroupFieldAccesses { get; set; }

    public virtual DbSet<RequestMaster> RequestMasters { get; set; }

    public virtual DbSet<RequestDetail> RequestDetails { get; set; }

    public virtual DbSet<RequestDetailParallel> RequestDetailParallels { get; set; }

    public virtual DbSet<WorkflowRequest> WorkflowRequests { get; set; }

    public virtual DbSet<WorkflowTransactionType> WorkflowTransactionTypes { get; set; }

    public virtual DbSet<WorkflowComment> WorkflowComments { get; set; }

    public virtual DbSet<RequestLog> RequestLogs { get; set; }

    public virtual DbSet<WorkFlowApprover> WorkFlowApprovers { get; set; }

    public virtual DbSet<WorkflowRemark> WorkflowRemarks { get; set; }

    public virtual DbSet<WFReadLog> WFReadLogs { get; set; }

    public virtual DbSet<Applicant> Applicants { get; set; }

    public virtual DbSet<ApplicantCV> ApplicantCVs { get; set; }

    public virtual DbSet<CVQualification> CVQualifications { get; set; }

    public virtual DbSet<CVWorkExp> CVWorkExps { get; set; }

    public virtual DbSet<CVSkill> CVSkills { get; set; }

    public virtual DbSet<CVLanguage> CVLanguages { get; set; }

    public virtual DbSet<Vacancy> Vacancies { get; set; }

    public virtual DbSet<ApplicationStatus> ApplicationStatuses { get; set; }

    public virtual DbSet<InterviewDetails> InterviewDetails { get; set; }

    public virtual DbSet<OnbGroup> OnbGroups { get; set; }

    public virtual DbSet<OnbGroupDetail> OnbGroupDetails { get; set; }

    public virtual DbSet<OnbTask> OnbTasks { get; set; }

    public virtual DbSet<OnbTemplate> OnbTemplates { get; set; }

    public virtual DbSet<OnbTemplateDetails> OnbTemplateDetails { get; set; }

    public virtual DbSet<OnbApplicantTasks> OnbApplicantTasks { get; set; }

    public virtual DbSet<OnbAppTaskComments> OnbAppTaskComments { get; set; }

    public virtual DbSet<OnbVideoLink> OnbVideoLinks { get; set; }

    public virtual DbSet<DomainType> DomainTypes { get; set; }

    public virtual DbSet<ReportType> ReportTypes { get; set; }

    public virtual DbSet<PrintReport> PrintReports { get; set; }

    public virtual DbSet<PrintReportPermission> PrintReportPermissions { get; set; }

    public virtual DbSet<PrintReportPermissionDetail> PrintReportPermissionDetails { get; set; }

    public virtual DbSet<ReportMaster> ReportMasters { get; set; }

    public virtual DbSet<ReportDetail> ReportDetails { get; set; }

    public virtual DbSet<ReportSetting> ReportSettings { get; set; }

    public virtual DbSet<ReportSettingDetail> ReportSettingDetails { get; set; }

    // REPORT DISTRIBUTION / SCHEDULING. No legacy counterpart -- HCM10 had no report scheduling.
    public virtual DbSet<ReportSchedule> ReportSchedules { get; set; }

    public virtual DbSet<ReportScheduleFilter> ReportScheduleFilters { get; set; }

    public virtual DbSet<ReportScheduleRecipient> ReportScheduleRecipients { get; set; }

    public virtual DbSet<ReportScheduleDestination> ReportScheduleDestinations { get; set; }

    public virtual DbSet<ReportScheduleRun> ReportScheduleRuns { get; set; }

    public virtual DbSet<ReportScheduleRunSlice> ReportScheduleRunSlices { get; set; }

    public virtual DbSet<ReportInfo> ReportInfos { get; set; }

    public virtual DbSet<ReportInfoDetail> ReportInfoDetails { get; set; }

    public virtual DbSet<CompetencyType> CompetencyTypes { get; set; }

    public virtual DbSet<Competency> Competencies { get; set; }

    public virtual DbSet<CompetencyByJobTitle> CompetencyByJobTitles { get; set; }

    public virtual DbSet<CompetencyByJobTitleDetail> CompetencyByJobTitleDetails { get; set; }

    public virtual DbSet<CDPeriod> CDPeriods { get; set; }

    public virtual DbSet<CDForm> CDForms { get; set; }

    public virtual DbSet<CDPlan> CDPlans { get; set; }

    public virtual DbSet<CDMidYearReview> CDMidYearReviews { get; set; }

    public virtual DbSet<CDAnnualYearReview> CDAnnualYearReviews { get; set; }

    public virtual DbSet<CDRating> CDRatings { get; set; }

    public virtual DbSet<ManpowerPlanningPeriod> ManpowerPlanningPeriods { get; set; }

    public virtual DbSet<ManpowerPlanning> ManpowerPlannings { get; set; }

    public virtual DbSet<ManpowerPlanningDetail> ManpowerPlanningDetails { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<PositionRelationship> PositionRelationships { get; set; }

    public virtual DbSet<InsurancePlan> InsurancePlans { get; set; }

    public virtual DbSet<InsuranceProvider> InsuranceProviders { get; set; }

    public virtual DbSet<EmployeeInsurance> EmployeeInsurances { get; set; }

    public virtual DbSet<EmployeeInsuranceDetail> EmployeeInsuranceDetails { get; set; }

    public virtual DbSet<CompanyInsuranceDetail> CompanyInsuranceDetails { get; set; }

    public virtual DbSet<GatePassType> GatePassTypes { get; set; }

    public virtual DbSet<GatePassMovement> GatePassMovements { get; set; }

    public virtual DbSet<GatePassStatus> GatePassStatuses { get; set; }

    public virtual DbSet<PersonType> PersonTypes { get; set; }

    public virtual DbSet<VehicleType> VehicleTypes { get; set; }

    public virtual DbSet<VehicleBrand> VehicleBrands { get; set; }

    public virtual DbSet<VehicleModel> VehicleModels { get; set; }

    public virtual DbSet<GatePassVendor> GatePassVendors { get; set; }

    public virtual DbSet<GatePass> GatePasses { get; set; }

    public virtual DbSet<GatePassMaterial> GatePassMaterials { get; set; }

    public virtual DbSet<GatePassPerson> GatePassPersons { get; set; }

    public virtual DbSet<GatePassVehicle> GatePassVehicles { get; set; }

    public virtual DbSet<PoolCar> PoolCars { get; set; }

    public virtual DbSet<EmployeePoolCar> EmployeePoolCars { get; set; }

    public virtual DbSet<ActionStatus> ActionStatuses { get; set; }

    public virtual DbSet<FirstAidMedicine> FirstAidMedicines { get; set; }

    public virtual DbSet<IncidentClaimStatus> IncidentClaimStatuses { get; set; }

    public virtual DbSet<IncidentNature> IncidentNatures { get; set; }

    public virtual DbSet<IncidentSeverity> IncidentSeverities { get; set; }

    public virtual DbSet<IncidentStatus> IncidentStatuses { get; set; }

    public virtual DbSet<IncidentType> IncidentTypes { get; set; }

    public virtual DbSet<InvestigationRootCause> InvestigationRootCauses { get; set; }

    public virtual DbSet<RiskAction> RiskActions { get; set; }

    public virtual DbSet<RiskImpact> RiskImpacts { get; set; }

    public virtual DbSet<RiskPossibility> RiskPossibilities { get; set; }

    public virtual DbSet<RiskStatus> RiskStatuses { get; set; }

    public virtual DbSet<RiskType> RiskTypes { get; set; }

    public virtual DbSet<SafetyAreaAsset> SafetyAreaAssets { get; set; }

    public virtual DbSet<ActionRegister> ActionRegisters { get; set; }

    public virtual DbSet<FirstAidBox> FirstAidBoxes { get; set; }

    public virtual DbSet<FirstAidBoxDetail> FirstAidBoxDetails { get; set; }

    public virtual DbSet<IncidentClaim> IncidentClaims { get; set; }

    public virtual DbSet<IncidentInvestigation> IncidentInvestigations { get; set; }

    public virtual DbSet<IncidentRegister> IncidentRegisters { get; set; }

    public virtual DbSet<RiskRegister> RiskRegisters { get; set; }

    public virtual DbSet<SafetyChecklist> SafetyChecklists { get; set; }

    public virtual DbSet<SiteAudit> SiteAudits { get; set; }

    public virtual DbSet<SiteAuditDetail> SiteAuditDetails { get; set; }

    public virtual DbSet<JobFamily> JobFamilies { get; set; }

    public virtual DbSet<JobFunction> JobFunctions { get; set; }

    public virtual DbSet<JobResponsibility> JobResponsibilities { get; set; }

    public virtual DbSet<EducationCertificate> EducationCertificates { get; set; }

    public virtual DbSet<EducationDegree> EducationDegrees { get; set; }

    public virtual DbSet<InterviewQuestion> InterviewQuestions { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<JobDescription> JobDescriptions { get; set; }

    public virtual DbSet<JobDescriptionCertificate> JobDescriptionCertificates { get; set; }

    public virtual DbSet<JobDescriptionDegree> JobDescriptionDegrees { get; set; }

    public virtual DbSet<JobDescriptionInterviewQuestion> JobDescriptionInterviewQuestions { get; set; }

    public virtual DbSet<JobDescriptionResponsibility> JobDescriptionResponsibilities { get; set; }

    public virtual DbSet<JobDescriptionSkill> JobDescriptionSkills { get; set; }

    public virtual DbSet<JobDescriptionCompetency> JobDescriptionCompetencies { get; set; }

    public virtual DbSet<JobDescriptionAcknowledgement> JobDescriptionAcknowledgements { get; set; }

    public virtual DbSet<ImpactOfLoss> ImpactOfLosses { get; set; }

    public virtual DbSet<NineBoxRating> NineBoxRatings { get; set; }

    public virtual DbSet<Ranking> Rankings { get; set; }

    public virtual DbSet<Readiness> Readinesses { get; set; }

    public virtual DbSet<ReasonForLeaving> ReasonForLeavings { get; set; }

    public virtual DbSet<RiskOfLoss> RiskOfLosses { get; set; }

    public virtual DbSet<SuccessionRating> SuccessionRatings { get; set; }

    public virtual DbSet<TalentPool> TalentPools { get; set; }

    public virtual DbSet<NineBoxMapping> NineBoxMappings { get; set; }

    public virtual DbSet<EmpPerformanceHistory> EmpPerformanceHistories { get; set; }

    public virtual DbSet<EmpPotentialAssessment> EmpPotentialAssessments { get; set; }

    public virtual DbSet<EmpTalentMatrixData> EmpTalentMatrixData { get; set; }

    public virtual DbSet<EmployeeTalentFlag> EmployeeTalentFlags { get; set; }

    public virtual DbSet<SuccessorNominee> SuccessorNominees { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<SurveyDetail> SurveyDetails { get; set; }

    public virtual DbSet<EmployeeSurvey> EmployeeSurveys { get; set; }

    public virtual DbSet<EmployeeSurveyDetail> EmployeeSurveyDetails { get; set; }

    public virtual DbSet<TrainingCourse> TrainingCourses { get; set; }

    public virtual DbSet<TrainingSchedule> TrainingSchedules { get; set; }

    public virtual DbSet<EmployeeTraining> EmployeeTrainings { get; set; }

    public virtual DbSet<TrainingAttendance> TrainingAttendances { get; set; }

    public virtual DbSet<SysUserOrgUnitData> SysUserOrgUnitDatas { get; set; }

    public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }

    public virtual DbSet<EmailToken> EmailTokens { get; set; }

    public virtual DbSet<PayslipConfig> PayslipConfigs { get; set; }

    public virtual DbSet<NotificationMaster> NotificationMasters { get; set; }

    public virtual DbSet<SystemNotification> SystemNotifications { get; set; }

    public virtual DbSet<SysObject> SysObjects { get; set; }

    public virtual DbSet<RetroReasonLog> RetroReasonLogs { get; set; }

    public virtual DbSet<PunchNotificationLog> PunchNotificationLogs { get; set; }

    public virtual DbSet<BulkUploadLog> BulkUploadLogs { get; set; }

    public virtual DbSet<ExecutionLog> ExecutionLogs { get; set; }

    public virtual DbSet<EmailNotificationLog> EmailNotificationLogs { get; set; }

    public virtual DbSet<SysTenantConfig> SysTenantConfigs { get; set; }

    public virtual DbSet<ManageAppraisalSettings> ManageAppraisalSettingses { get; set; }

    public virtual DbSet<ManageDocumentSettings> ManageDocumentSettingses { get; set; }

    public virtual DbSet<ManageFIPostingSettings> ManageFIPostingSettingses { get; set; }

    public virtual DbSet<ManageGeneralSettings> ManageGeneralSettingses { get; set; }

    public virtual DbSet<ManageNotificationSettings> ManageNotificationSettingses { get; set; }

    public virtual DbSet<ManageOrgSettings> ManageOrgSettingses { get; set; }

    public virtual DbSet<ManagePayrollSettings> ManagePayrollSettingses { get; set; }

    public virtual DbSet<ManagePersonnelSettings> ManagePersonnelSettingses { get; set; }

    public virtual DbSet<ManageProjectCostingSettings> ManageProjectCostingSettingses { get; set; }

    public virtual DbSet<ManageReportSettings> ManageReportSettingses { get; set; }

    public virtual DbSet<ManageSecuritySettings> ManageSecuritySettingses { get; set; }

    public virtual DbSet<ManageSelfServiceSettings> ManageSelfServiceSettingses { get; set; }

    public virtual DbSet<ManageTimeSettings> ManageTimeSettingses { get; set; }

    public virtual DbSet<ManageWorkflowSettings> ManageWorkflowSettingses { get; set; }

    public virtual DbSet<PayrollDriver> PayrollDrivers { get; set; }

    public virtual DbSet<News> News { get; set; }

    public virtual DbSet<TaskEvent> TaskEvents { get; set; }

    public virtual DbSet<EmployeesNotification> EmployeesNotifications { get; set; }

    public virtual DbSet<EmployeesNotificationLog> EmployeesNotificationLogs { get; set; }

    public virtual DbSet<EmployeeWorkflowRole> EmployeeWorkflowRoles { get; set; }


    public HCMv3DbContext(DbContextOptions<HCMv3DbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Whether the <c>ApprovedOnly</c> filter is currently on for this unit of work. Mirrors ABP's own
    /// <c>IsSoftDeleteFilterEnabled</c>; EF Core turns the property read into a query parameter, so toggling
    /// it with <c>DisableFilter</c> takes effect without invalidating the compiled query cache.
    /// </summary>
    protected virtual bool IsApprovedOnlyFilterEnabled =>
        CurrentUnitOfWorkProvider?.Current?.IsFilterEnabled(HCMv3DataFilters.ApprovedOnly) == true;

    protected override bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType)
    {
        if (typeof(IWorkflowEnabledEntity).IsAssignableFrom(typeof(TEntity)))
        {
            return true;
        }

        return base.ShouldFilterEntity<TEntity>(entityType);
    }

    protected override Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>(ModelBuilder modelBuilder)
    {
        var expression = base.CreateFilterExpression<TEntity>(modelBuilder);

        if (typeof(IWorkflowEnabledEntity).IsAssignableFrom(typeof(TEntity)))
        {
            // "Effective" = the row counts as a real transaction that the rest of the system may act on:
            //   null      -> not workflow-managed at all. Every row that exists today, plus anything created
            //                while its type has workflow switched off. Treated as effective, which is what
            //                makes this whole change backward compatible.
            //   Approved  -> the approval chain completed.
            //   Closed    -> approved and subsequently closed out (RequestMaster.AutoClose).
            // Everything else -- Draft, Open, Rejected, Transferred -- is in flight or dead, and stays
            // invisible to payroll, quota, attendance, reports and every ordinary query.
            Expression<Func<TEntity, bool>> approvedOnlyFilter =
                e => !IsApprovedOnlyFilterEnabled
                     || ((IWorkflowEnabledEntity)e).WorkflowStatus == null
                     || ((IWorkflowEnabledEntity)e).WorkflowStatus == RequestStatus.Approved
                     || ((IWorkflowEnabledEntity)e).WorkflowStatus == RequestStatus.Closed;

            expression = expression == null
                ? approvedOnlyFilter
                : CombineExpressions(expression, approvedOnlyFilter);
        }

        return expression;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BinaryObject>(b => { b.HasIndex(e => new { e.TenantId }); });

        // Careers portal (wave R2): the Applicant <-> ABP User link that replaced the legacy plaintext
        // Applicant.UserName / Applicant.Password columns.
        //
        // Filtered unique index rather than a plain one: UserId is null for every applicant a recruiter
        // created by hand, and there may be many of those, but a given ABP user must map to at most one
        // applicant within a tenant. Without this, a retried registration or a race between two concurrent
        // registrations could leave one login pointing at two profiles. IsDeleted is part of the filter
        // because Applicant is soft-deleted -- a deleted applicant's row lingers and would otherwise block
        // the same user from ever getting a fresh profile.
        modelBuilder.Entity<Applicant>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.UserId })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");
        });

        // Employee <-> ABP User link, the identity anchor for Employee Self Service. Same shape and same
        // reasoning as the Applicant index directly above: UserId is null for every employee not yet linked
        // (there will be many), but one login must map to at most one employee within a tenant, or the very
        // first thing every ESS screen does -- "who am I" -- becomes ambiguous. IsDeleted is in the filter
        // because Employee is soft-deleted, and a deleted employee's row would otherwise permanently block
        // its user from being linked to a replacement record (rehire, duplicate cleanup).
        // The activity feed reads comments for one request in chronological order, so that is the index.
        modelBuilder.Entity<WorkflowComment>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.WorkflowRequestId, e.CreationTime });

            // Restrict on both: a reply must not silently vanish when its parent is removed (the thread
            // would lose its context), and deleting an employee must not delete what they said -- the
            // comment is part of the request's history.
            b.HasOne(e => e.ParentComment)
                .WithMany(e => e.Replies)
                .HasForeignKey(e => e.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.AuthorEmployee)
                .WithMany()
                .HasForeignKey(e => e.AuthorEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // The transaction-type catalog. Code is what WorkflowRequest.RequestType and
        // WorkflowGroupDetail.RequestTypeName are matched against, so a duplicate would make routing
        // ambiguous -- exactly the failure the catalog exists to remove.
        modelBuilder.Entity<WorkflowTransactionType>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // Restrict, not Cascade: deleting an approval configuration must not silently take the catalog
            // row (and with it the type's ESS menu entry) down with it.
            b.HasOne(e => e.DefaultRequestMaster)
                .WithMany()
                .HasForeignKey(e => e.DefaultRequestMasterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeOrgAssignment>(b =>
        {
            b.HasOne(e => e.WorkflowGroup)
                .WithMany()
                .HasForeignKey(e => e.WorkflowGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Resolving "which request governs this transaction row" -- the lookup behind the Workflow History
        // button on every workflow-enabled screen. Not unique: a row legitimately accumulates several
        // requests over its life (the original submission, then later amendments).
        modelBuilder.Entity<WorkflowRequest>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.RequestType, e.TargetEntityId });
        });

        // The approver inbox: "everything waiting on me, across every request type". This index is what
        // keeps that a single indexed seek rather than a scan, and it is the reason WorkflowRequest carries
        // its own SummaryEn/SummaryAr -- the inbox never touches the ~50 transaction tables.
        modelBuilder.Entity<WorkFlowApprover>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ApproverId, e.IsApproved });
        });

        modelBuilder.Entity<Employee>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.UserId })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");

            // LoginID must also name exactly one employee: it is what EmployeeSessionService falls back to
            // for employees not yet linked by UserId, and what BulkPasswordAppService and
            // WorkflowAdminToolsAppService still resolve people by. A duplicate here means sign-in resolves
            // to whichever row the database happens to return first.
            //
            // Enforced in EmployeesAppService.ValidateAsync as well, which catches it with a readable
            // message; this index is the backstop for the paths that bypass that service (migration tool,
            // bulk import, direct SQL). Empty-string LoginIDs are excluded -- there are many, and they mean
            // "no login", not "the same login". SQL Server ignores trailing spaces when comparing strings,
            // so whitespace-only values fall out of the index too.
            b.HasIndex(e => new { e.TenantId, e.LoginID })
                .IsUnique()
                .HasFilter("[LoginID] IS NOT NULL AND [LoginID] <> '' AND [IsDeleted] = 0");
        });

        modelBuilder.Entity<SubscribableEdition>(b =>
        {
            b.Property(e => e.MonthlyPrice).HasPrecision(18, 2);
            b.Property(e => e.AnnualPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SubscriptionPayment>(x =>
        {
            x.Property(u => u.ExtraProperties)
                .HasConversion(
                    d => d.ToJsonString(false, false),
                    s => s.FromJsonString<ExtraPropertyDictionary>()
                )
                .Metadata.SetValueComparer(new ValueComparer<ExtraPropertyDictionary>(
                    (c1, c2) => c1.ToJsonString(false, false) == c2.ToJsonString(false, false),
                    c => c.ToJsonString(false, false).GetHashCode(),
                    c => c.ToJsonString(false, false).FromJsonString<ExtraPropertyDictionary>()
                ));
        });

        modelBuilder.Entity<SubscriptionPaymentProduct>(x =>
        {
            x.Property(u => u.ExtraProperties)
                .HasConversion(
                    d => d.ToJsonString(false, false),
                    s => s.FromJsonString<ExtraPropertyDictionary>()
                )
                .Metadata.SetValueComparer(new ValueComparer<ExtraPropertyDictionary>(
                    (c1, c2) => c1.ToJsonString(false, false) == c2.ToJsonString(false, false),
                    c => c.ToJsonString(false, false).GetHashCode(),
                    c => c.ToJsonString(false, false).FromJsonString<ExtraPropertyDictionary>()
                ));

            x.Property(e => e.Amount).HasPrecision(18, 2);
            x.Property(e => e.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ChatMessage>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.UserId, e.ReadState });
            b.HasIndex(e => new { e.TenantId, e.TargetUserId, e.ReadState });
            b.HasIndex(e => new { e.TargetTenantId, e.TargetUserId, e.ReadState });
            b.HasIndex(e => new { e.TargetTenantId, e.UserId, e.ReadState });
        });

        modelBuilder.Entity<Friendship>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.UserId });
            b.HasIndex(e => new { e.TenantId, e.FriendUserId });
            b.HasIndex(e => new { e.FriendTenantId, e.UserId });
            b.HasIndex(e => new { e.FriendTenantId, e.FriendUserId });
        });

        modelBuilder.Entity<Tenant>(b =>
        {
            b.HasIndex(e => new { e.SubscriptionEndDateUtc });
            b.HasIndex(e => new { e.CreationTime });
        });

        modelBuilder.Entity<SubscriptionPayment>(b =>
        {
            b.HasIndex(e => new { e.Status, e.CreationTime });
            b.HasIndex(e => new { PaymentId = e.ExternalPaymentId, e.Gateway });
        });

        modelBuilder.Entity<UserDelegation>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.SourceUserId });
            b.HasIndex(e => new { e.TenantId, e.TargetUserId });
        });

		modelBuilder.Entity<UserAccountLink>(b =>
		{
			b.HasIndex(e => new { e.UserAccountId, e.LinkedUserAccountId }).IsUnique();
		});

        modelBuilder.Entity<UserSession>(b =>
        {
            b.HasIndex(e => new { e.UserId, e.IsActive });
            b.HasIndex(e => e.SessionToken).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.UserId });
        });

        modelBuilder.Entity<AccessUnit>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Code });
            b.HasIndex(e => new { e.TenantId, e.NameEn });
        });

        modelBuilder.Entity<CurrencyConvertor>(b =>
        {
            // Dual FK to Currency: explicit config with Restrict avoids SQL Server's
            // multiple-cascade-path error that a default Cascade delete would trigger here.
            b.HasOne(e => e.FromCurr)
                .WithMany()
                .HasForeignKey(e => e.FromCurrId)
                .HasConstraintName("FK_CurrencyConvertors_Currencies_FromCurrId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.ToCurr)
                .WithMany()
                .HasForeignKey(e => e.ToCurrId)
                .HasConstraintName("FK_CurrencyConvertors_Currencies_ToCurrId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeOrgAssignment>(b =>
        {
            // Three optional FKs into PayrollManagement hub tables, added after Wave B originally
            // deferred them. Restrict rather than the default Cascade: EmployeeOrgAssignment already
            // cascades from Employee, so a second cascade path from any of these hubs is exactly the
            // "multiple cascade paths" error SQL Server rejects at migration time.
            b.HasOne(e => e.EmployeeGroup)
                .WithMany()
                .HasForeignKey(e => e.EmployeeGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.EmployeeSubGroup)
                .WithMany()
                .HasForeignKey(e => e.EmployeeSubGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.PayrollArea)
                .WithMany()
                .HasForeignKey(e => e.PayrollAreaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoLiveStepState>(b =>
        {
            // One draft per step per tenant. The unique index is what makes the save an upsert.
            //
            // Filtered on IsDeleted because this is a soft-deleted entity: clearing a step leaves the
            // row physically present, and without the filter the next save of that same step would hit
            // a unique violation against a row the repository can no longer see.
            b.HasIndex(e => new { e.TenantId, e.StepId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<OrgUnitProfile>(b =>
        {
            b.HasIndex(e => e.OrganizationUnitId).IsUnique();

            // Cascade so deleting via Zero's own built-in OrganizationUnit admin screen (which has no
            // knowledge of this HR-specific profile row) doesn't leave an orphan.
            b.HasOne(e => e.OrganizationUnit)
                .WithMany()
                .HasForeignKey(e => e.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.OrganizationUnitType)
                .WithMany()
                .HasForeignKey(e => e.OrganizationUnitTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeManager>(b =>
        {
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .HasConstraintName("FK_EmployeeManagers_Employees_EmployeeId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .HasConstraintName("FK_EmployeeManagers_Employees_ManagerId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeDottedLineManager>(b =>
        {
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .HasConstraintName("FK_EmployeeDottedLineManagers_Employees_EmployeeId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .HasConstraintName("FK_EmployeeDottedLineManagers_Employees_ManagerId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeDelegation>(b =>
        {
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .HasConstraintName("FK_EmployeeDelegations_Employees_EmployeeId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.DelegateTo)
                .WithMany()
                .HasForeignKey(e => e.DelegateToId)
                .HasConstraintName("FK_EmployeeDelegations_Employees_DelegateToId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeRelationship>(b =>
        {
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .HasConstraintName("FK_EmployeeRelationships_Employees_EmployeeId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.RelatedEmployee)
                .WithMany()
                .HasForeignKey(e => e.RelatedEmployeeId)
                .HasConstraintName("FK_EmployeeRelationships_Employees_RelatedEmployeeId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DutyPeriod>(b =>
        {
            // 7 separate FKs to the same Duty table from one row would otherwise each default to
            // Cascade, which SQL Server rejects as "multiple cascade paths" to DutyPeriods.
            b.HasOne(e => e.SundayDuty).WithMany().HasForeignKey(e => e.SundayDutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.MondayDuty).WithMany().HasForeignKey(e => e.MondayDutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.TuesdayDuty).WithMany().HasForeignKey(e => e.TuesdayDutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.WednesdayDuty).WithMany().HasForeignKey(e => e.WednesdayDutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.ThursdayDuty).WithMany().HasForeignKey(e => e.ThursdayDutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.FridayDuty).WithMany().HasForeignKey(e => e.FridayDutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.SaturdayDuty).WithMany().HasForeignKey(e => e.SaturdayDutyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DutyPeriodDetail>(b =>
        {
            b.HasOne(e => e.Day1Duty).WithMany().HasForeignKey(e => e.Day1DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day2Duty).WithMany().HasForeignKey(e => e.Day2DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day3Duty).WithMany().HasForeignKey(e => e.Day3DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day4Duty).WithMany().HasForeignKey(e => e.Day4DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day5Duty).WithMany().HasForeignKey(e => e.Day5DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day6Duty).WithMany().HasForeignKey(e => e.Day6DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day7Duty).WithMany().HasForeignKey(e => e.Day7DutyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RosterDutyPeriodDetail>(b =>
        {
            b.HasOne(e => e.Day1Duty).WithMany().HasForeignKey(e => e.Day1DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day2Duty).WithMany().HasForeignKey(e => e.Day2DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day3Duty).WithMany().HasForeignKey(e => e.Day3DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day4Duty).WithMany().HasForeignKey(e => e.Day4DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day5Duty).WithMany().HasForeignKey(e => e.Day5DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day6Duty).WithMany().HasForeignKey(e => e.Day6DutyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Day7Duty).WithMany().HasForeignKey(e => e.Day7DutyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AbsenceTypeException>(b =>
        {
            // Same multi-cascade-path concern as WageTypeException: two required FKs to AbsenceTypes.
            b.HasOne(e => e.AbsenceType)
                .WithMany()
                .HasForeignKey(e => e.AbsenceTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.ExceptAbsenceType)
                .WithMany()
                .HasForeignKey(e => e.ExceptAbsenceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WageTypeException>(b =>
        {
            // Two required FKs to the same WageTypes table would otherwise both default to Cascade,
            // which SQL Server rejects as "multiple cascade paths" to WageTypeExceptions.
            b.HasOne(e => e.WageType)
                .WithMany()
                .HasForeignKey(e => e.WageTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.ExceptWageType)
                .WithMany()
                .HasForeignKey(e => e.ExceptWageTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SponsorGovtRepresentative>(b =>
        {
            // Employee is reachable via multiple cascade paths elsewhere in the graph (e.g. self-referencing
            // Employee FKs), so a Cascade FK here triggers "multiple cascade paths" on Employees deletion.
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeDocumentMvmt>(b =>
        {
            // Two FKs into Employee (EmployeeId required, GuarantorId nullable) on a table that Employees
            // already reaches by a second path via EmployeeDocuments -- the multiple-cascade-paths shape
            // handled the same way as WageTypeException and PAEmployeeReviewer. Restrict is also the right
            // behaviour on its own terms: deleting an employee record must not silently erase the log of
            // whose passport is currently out of the safe.
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Guarantor)
                .WithMany()
                .HasForeignKey(e => e.GuarantorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-service lists every one of these rows by employee, and the ApprovedOnly filter reads
            // WorkflowStatus on the same pass.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });
        });

        modelBuilder.Entity<EmployeeAccommodation>(b =>
        {
            // Location/Building/Floor/Flat/Room/Bed form a cascading hierarchy chain (Location->Building->
            // Floor->Flat->Room->Bed), and this entity references all six levels directly, creating multiple
            // cascade paths down to this table. Restrict all six - deleting a facility record shouldn't
            // cascade-delete employee accommodation assignment history anyway.
            b.HasOne(e => e.Location).WithMany().HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Building).WithMany().HasForeignKey(e => e.BuildingId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Floor).WithMany().HasForeignKey(e => e.FloorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Flat).WithMany().HasForeignKey(e => e.FlatId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Room).WithMany().HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Bed).WithMany().HasForeignKey(e => e.BedId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeSponsor>(b =>
        {
            // Sponsor is a hub table with several other cascading children already (SponsorJobTitle,
            // SponsorSignatory, SponsorLicenseInfo), so a second cascade path in via EmployeeSponsor.SponsorId
            // (alongside the EmployeeId cascade from Employee) triggers the multiple-cascade-paths check.
            b.HasOne(e => e.Sponsor)
                .WithMany()
                .HasForeignKey(e => e.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SponsorBank>(b =>
        {
            // PaymentBankId is required (non-nullable) and shares the Banks table with the nullable
            // EmployeeBankId FK, creating a multiple-cascade-paths risk on the required side.
            b.HasOne(e => e.PaymentBank)
                .WithMany()
                .HasForeignKey(e => e.PaymentBankId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PAEmployeeReviewer>(b =>
        {
            // Two FKs into Employee (EmployeeId required, PeerAddedById nullable) - same multi-cascade-path
            // pattern as WageTypeException/SponsorBank.
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.PeerAddedBy)
                .WithMany()
                .HasForeignKey(e => e.PeerAddedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TravelPerDiem>(b =>
        {
            // EmployeeSubGroup is reachable both directly (EmployeeSubGroupId) and transitively via
            // EmployeeGroup->EmployeeSubGroup's own cascading FK - restrict the EmployeeSubGroup path.
            b.HasOne(e => e.EmployeeSubGroup)
                .WithMany()
                .HasForeignKey(e => e.EmployeeSubGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Two required FKs to the same TravelDestinations table (FromDestinationId/ToDestinationId).
            b.HasOne(e => e.FromDestination)
                .WithMany()
                .HasForeignKey(e => e.FromDestinationId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.ToDestination)
                .WithMany()
                .HasForeignKey(e => e.ToDestinationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PAEmployeeRating>(b =>
        {
            // PAEmployeeRating is reachable from PAEmployee via 3 paths: direct PAEmployeeId, via
            // PAEmployeeReviewerId->PAEmployeeReviewer->PAEmployeeId, and via
            // PAEmployeeDetailId->PAEmployeeDetail->PAEmployeeId. Restrict the two indirect paths, keep
            // PAEmployeeId as the sole cascade.
            b.HasOne(e => e.PAEmployeeReviewer)
                .WithMany()
                .HasForeignKey(e => e.PAEmployeeReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.PAEmployeeDetail)
                .WithMany()
                .HasForeignKey(e => e.PAEmployeeDetailId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PAPeerReview>(b =>
        {
            // PAPeerReviews is reachable both via PAFormId->PAForm->EmployeeId cascade and directly via
            // PeerId (Employee) - restrict the Peer path.
            b.HasOne(e => e.Peer)
                .WithMany()
                .HasForeignKey(e => e.PeerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TravelDetail>(b =>
        {
            // Two required FKs to the same TravelDestinations table (DepartureId/ArrivalId).
            b.HasOne(e => e.Departure)
                .WithMany()
                .HasForeignKey(e => e.DepartureId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Arrival)
                .WithMany()
                .HasForeignKey(e => e.ArrivalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PAPeerNominee>(b =>
        {
            // Two required FKs into Employee (EmployeeId + PeerId).
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Peer)
                .WithMany()
                .HasForeignKey(e => e.PeerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CDForm>(b =>
        {
            // Required FK into the Employee hub table, which already has many other convergent cascade paths.
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ManpowerPlanning>(b =>
        {
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ManpowerPlanningStatus is DERIVED from WorkflowStatus and has no column: the workflow engine
            // is the single writer of a plan's approval state. The old stored Status column is dropped by
            // the migration generated from this model -- keeping it would leave a second, writable copy of
            // the same fact, which is exactly what routing this type was meant to end.
            b.Ignore(e => e.Status);
        });

        modelBuilder.Entity<PositionRelationship>(b =>
        {
            // Required FKs into both Position and Employee. Bind to Position's real PositionRelationships
            // collection nav property explicitly - a bare .WithMany() here would create a second, unnamed
            // shadow relationship (PositionId1) alongside the convention-paired one.
            b.HasOne(e => e.Position)
                .WithMany(p => p.PositionRelationships)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrintReportPermission>(b =>
        {
            // Required FK into ABP's built-in Role table.
            b.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeInsurance>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeePoolCar>(b =>
        {
            b.HasOne(e => e.Driver).WithMany().HasForeignKey(e => e.DriverEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GatePassPerson>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ActionRegister>(b =>
        {
            b.HasOne(e => e.AssignedTo).WithMany().HasForeignKey(e => e.AssignedToId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FirstAidBox>(b =>
        {
            b.HasOne(e => e.ResponsiblePerson).WithMany().HasForeignKey(e => e.ResponsiblePersonId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FirstAidBoxDetail>(b =>
        {
            b.HasOne(e => e.AllocatedBy).WithMany().HasForeignKey(e => e.AllocatedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RiskRegister>(b =>
        {
            b.HasOne(e => e.ReportedBy).WithMany().HasForeignKey(e => e.ReportedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SiteAudit>(b =>
        {
            b.HasOne(e => e.ConductedBy).WithMany().HasForeignKey(e => e.ConductedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobDescriptionAcknowledgement>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            // JobTitle is reachable both directly (JobTitleId) and via JobDescriptionId->JobDescription->JobTitleId.
            b.HasOne(e => e.JobTitle).WithMany().HasForeignKey(e => e.JobTitleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmpPerformanceHistory>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmpPotentialAssessment>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmpTalentMatrixData>(b =>
        {
            // Employee FK, plus 4 FKs to the same NineBoxRating table.
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.PerformanceRating).WithMany().HasForeignKey(e => e.PerformanceRatingId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.PotentialRating).WithMany().HasForeignKey(e => e.PotentialRatingId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.OverallRating).WithMany().HasForeignKey(e => e.OverallRatingId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.CalibratedRating).WithMany().HasForeignKey(e => e.CalibratedRatingId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeTalentFlag>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SuccessorNominee>(b =>
        {
            // Two required FKs to Employee (Employee + Nominee).
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Nominee).WithMany().HasForeignKey(e => e.NomineeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeSurvey>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeSurveyDetail>(b =>
        {
            // SurveyDetail is reachable both directly (QuestionId) and via
            // EmployeeSurveyId->EmployeeSurvey->SurveyId->Survey->SurveyDetail.
            b.HasOne(e => e.Question).WithMany().HasForeignKey(e => e.QuestionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeTraining>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            // TrainingCourse is reachable both directly (TrainingCourseId) and via
            // TrainingScheduleId->TrainingSchedule->TrainingCourseId.
            b.HasOne(e => e.TrainingCourse).WithMany().HasForeignKey(e => e.TrainingCourseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrainingAttendance>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BulkUploadLog>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RetroReasonLog>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SysUserOrgUnitData>(b =>
        {
            b.HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.OrganizationUnit).WithMany().HasForeignKey(e => e.OrganizationUnitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PunchNotificationLog>(b =>
        {
            b.HasOne(e => e.EmployeeDutyPeriod).WithMany().HasForeignKey(e => e.EmployeeDutyPeriodId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeesNotification>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeWorkflowRole>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------------------------------------------------------------------------------------
        // Wave P8 entity-gap closures. Every FK below points into a hub table (Employee / WageType) that
        // already has convergent cascade paths, so all of them are Restrict.
        // ---------------------------------------------------------------------------------------------

        modelBuilder.Entity<ProjectLog>(b =>
        {
            // EmployeeId became int? so legacy's employee-less run-level rows can be persisted.
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OvertimeType>(b =>
        {
            b.HasOne(e => e.WageType).WithMany().HasForeignKey(e => e.WageTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        // WAVE P9: LoanType.WageTypeId -- the master-table reference wage code 2001 "Loan Recovery Deduction"
        // is dispatched through (legacy Program.cs line 2700 / EmployeeCalculatedPayService.cs line 73).
        // Restrict for the same reason as OvertimeType above: WageTypes already has convergent cascade paths.
        modelBuilder.Entity<LoanType>(b =>
        {
            b.HasOne(e => e.WageType).WithMany().HasForeignKey(e => e.WageTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------------------------------------------------------------------------------------
        // Live engine-run progress. Deliberately relationship-free: these rows are written on a short-lived
        // independent connection while the engine's own (hour-long) transaction is open, so an FK to
        // Employee or ExecutionLog would either block on, or fail against, uncommitted parent rows.
        // EmployeeId/ExecutionLogId are therefore plain columns.
        // ---------------------------------------------------------------------------------------------

        modelBuilder.Entity<EngineRun>(b =>
        {
            // The status screen's only query: newest run for one engine within one tenant.
            b.HasIndex(e => new { e.TenantId, e.EngineType, e.Id });
        });

        modelBuilder.Entity<EngineRunEmployee>(b =>
        {
            // The grid's only query: one run's rows in processing order.
            b.HasIndex(e => new { e.EngineRunId, e.SequenceNo });
        });

        modelBuilder.ConfigureOpenIddict();
    }
}
