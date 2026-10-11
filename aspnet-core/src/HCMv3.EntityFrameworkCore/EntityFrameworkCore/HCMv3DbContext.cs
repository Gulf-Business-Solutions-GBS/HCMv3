using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Abp.Json;
using Abp.UI;
using HCMv3.BusinessRules.AfterSave;
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
using HCMv3.AiAgent;
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
using HCMv3.GovtRelationsManagement;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.JobProfileManagement;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.MultiTenancy;
using HCMv3.MultiTenancy.Accounting;
using HCMv3.MultiTenancy.Payments;
using HCMv3.Navigation;
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
using HCMv3.BackgroundJobs;
using HCMv3.BusinessRules;
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

    public virtual DbSet<AppAttachment> AppAttachments { get; set; }

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

    /// <summary>
    /// Proposed changes to an employee's own personnel file, awaiting approval. A separate table on purpose:
    /// see <see cref="EmployeePersonalDataChange"/> for why Employee itself is deliberately NOT
    /// workflow-enabled.
    /// </summary>
    public virtual DbSet<EmployeePersonalDataChange> EmployeePersonalDataChanges { get; set; }

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

    /// <summary>
    /// Handsets employees have registered so the mobile app will accept attendance punches from them. One
    /// row is both the registration and the request -- there is no MobileAppDeviceRequest table.
    /// </summary>
    public virtual DbSet<EmployeeMobileDevice> EmployeeMobileDevices { get; set; }

    public virtual DbSet<TimeAttAPI> TimeAttAPIs { get; set; }

    public virtual DbSet<TimeLog> TimeLogs { get; set; }

    public virtual DbSet<TimeRegister> TimeRegisters { get; set; }

    /// <summary>
    /// Batch approval of calculated timesheet days. The one header/detail pair in the workflow wave -- see
    /// <see cref="TimeLogApproval"/> for why a rectangle of employees-by-days cannot be addressed by
    /// <c>WorkflowRequest.TargetEntityId</c> on its own.
    /// </summary>
    public virtual DbSet<TimeLogApproval> TimeLogApprovals { get; set; }

    public virtual DbSet<TimeLogApprovalDetail> TimeLogApprovalDetails { get; set; }

    public virtual DbSet<TimeLogHistory> TimeLogHistories { get; set; }

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

    public virtual DbSet<EmployeeLetterRequest> EmployeeLetterRequests { get; set; }

    public virtual DbSet<EmployeeITService> EmployeeITServices { get; set; }

    /// <summary>
    /// Work ASSIGNED to an employee (legacy <c>TaskRequest</c>). Deliberately a separate table from
    /// <see cref="EmployeeTasks"/>, which is the HR reminder/monitoring record -- see
    /// <see cref="EmployeeAssignedTask"/> for why the two are not one thing.
    /// </summary>
    public virtual DbSet<EmployeeAssignedTask> EmployeeAssignedTasks { get; set; }

    public virtual DbSet<EmployeeDisciplinary> EmployeeDisciplinaries { get; set; }

    public virtual DbSet<EmployeeClearance> EmployeeClearances { get; set; }

    public virtual DbSet<EmployeeClearanceDetail> EmployeeClearanceDetails { get; set; }

    public virtual DbSet<EmployeeGrievance> EmployeeGrievances { get; set; }

    public virtual DbSet<EmployeeHealthReimburse> EmployeeHealthReimburses { get; set; }

    public virtual DbSet<EmployeeOvertime> EmployeeOvertimes { get; set; }

    public virtual DbSet<EmployeePayment> EmployeePayments { get; set; }

    /// <summary>
    /// The HR payment batch -- one wage type, many employees, one approval. A header/detail pair, and the
    /// second one in this port after <see cref="TimeLogApproval"/>, for the same reason: what is approved
    /// covers many employees, and <c>WorkflowRequest.TargetEntityId</c> addresses a single row. See
    /// <see cref="EmployeePaymentBatch"/> for why this is NOT the shadow-request shape the port exists to
    /// remove.
    /// </summary>
    public virtual DbSet<EmployeePaymentBatch> EmployeePaymentBatches { get; set; }

    public virtual DbSet<EmployeePaymentBatchDetail> EmployeePaymentBatchDetails { get; set; }

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

    public virtual DbSet<RequestNumberSequence> RequestNumberSequences { get; set; }

    public virtual DbSet<WorkflowTransactionType> WorkflowTransactionTypes { get; set; }

    public virtual DbSet<WorkflowComment> WorkflowComments { get; set; }

    // AI assistant (feat/ai-agent) -- see AiAgent/ROLLBACK.md before removing.
    public virtual DbSet<AiConversation> AiConversations { get; set; }

    public virtual DbSet<AiMessage> AiMessages { get; set; }

    public virtual DbSet<AiPendingAction> AiPendingActions { get; set; }

    public virtual DbSet<RequestLog> RequestLogs { get; set; }

    public virtual DbSet<WorkFlowApprover> WorkFlowApprovers { get; set; }

    public virtual DbSet<WorkflowRemark> WorkflowRemarks { get; set; }

    public virtual DbSet<WFReadLog> WFReadLogs { get; set; }

    public virtual DbSet<BusinessRule> BusinessRules { get; set; }

    public virtual DbSet<BusinessRuleAssignment> BusinessRuleAssignments { get; set; }

    public virtual DbSet<BusinessRuleExecutionLog> BusinessRuleExecutionLogs { get; set; }

    public virtual DbSet<BackgroundWorkerStatus> BackgroundWorkerStatuses { get; set; }

    public virtual DbSet<BackgroundJobFailure> BackgroundJobFailures { get; set; }

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

    public virtual DbSet<HCMv3.Integrations.IntegrationSetting> IntegrationSettings { get; set; }

    public virtual DbSet<HCMv3.Integrations.IntegrationConnectionLog> IntegrationConnectionLogs { get; set; }

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

    /// <summary>Per-user screen favourites and visit history. See <see cref="UserScreenUsage"/>.</summary>
    public virtual DbSet<UserScreenUsage> UserScreenUsages { get; set; }

    /// <summary>The screen catalogue and its Screen Codes. See <see cref="Screen"/>.</summary>
    public virtual DbSet<Screen> Screens { get; set; }

    /// <summary>Tenant-wide quick links published by an administrator.</summary>
    public virtual DbSet<SharedQuickLink> SharedQuickLinks { get; set; }


    public HCMv3DbContext(DbContextOptions<HCMv3DbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Property-injected, the way ABP injects EventBus and the rest of this context's collaborators: the
    /// constructor has to stay usable by the design-time factory that builds migrations, which has no container.
    /// Without the container this is the null dispatcher, and no AfterSave rule runs -- which is correct for a
    /// migration and caught by BusinessRuleAfterSave_Tests for everything else.
    /// </summary>
    public IBusinessRuleAfterSaveDispatcher BusinessRuleAfterSaveDispatcher { get; set; } =
        NullBusinessRuleAfterSaveDispatcher.Instance;

    private bool _isRunningAfterSaveRules;

    public override int SaveChanges()
    {
        var saved = base.SaveChanges();
        return saved + RunAfterSaveRules();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var saved = await base.SaveChangesAsync(cancellationToken);
        return saved + await RunAfterSaveRulesAsync(cancellationToken);
    }

    /// <summary>
    /// Runs the AfterSave rules of what this save just wrote, and saves their follow-up changes -- in the same
    /// transaction, which is the whole reason this lives here rather than in an EntityCreated handler. ABP raises
    /// EntityCreated only after the unit of work has committed, when a failed follow-up could no longer undo the
    /// save it followed. Here the transaction is still open, so any failure rolls everything back.
    ///
    /// <para>
    /// In rounds, because a follow-up that saves a rule-enabled record queues that record's AfterSave rules in
    /// turn. The dispatcher stops a chain that will not settle. The flag stops a SaveChanges made from inside a
    /// rule from starting a second, nested drain; the outer loop picks up whatever it queued.
    /// </para>
    /// </summary>
    private int RunAfterSaveRules()
    {
        if (_isRunningAfterSaveRules || !BusinessRuleAfterSaveDispatcher.HasPending)
        {
            return 0;
        }

        _isRunningAfterSaveRules = true;

        try
        {
            var saved = 0;

            for (var round = 1; BusinessRuleAfterSaveDispatcher.HasPending; round++)
            {
                BusinessRuleAfterSaveDispatcher.RunPending(round);

                try
                {
                    saved += base.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    throw new UserFriendlyException(BusinessRuleAfterSaveDispatcher.DescribeFollowUpSaveFailure(ex));
                }
            }

            return saved;
        }
        finally
        {
            _isRunningAfterSaveRules = false;
        }
    }

    private async Task<int> RunAfterSaveRulesAsync(CancellationToken cancellationToken)
    {
        if (_isRunningAfterSaveRules || !BusinessRuleAfterSaveDispatcher.HasPending)
        {
            return 0;
        }

        _isRunningAfterSaveRules = true;

        try
        {
            var saved = 0;

            for (var round = 1; BusinessRuleAfterSaveDispatcher.HasPending; round++)
            {
                BusinessRuleAfterSaveDispatcher.RunPending(round);

                try
                {
                    saved += await base.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex)
                {
                    throw new UserFriendlyException(BusinessRuleAfterSaveDispatcher.DescribeFollowUpSaveFailure(ex));
                }
            }

            return saved;
        }
        finally
        {
            _isRunningAfterSaveRules = false;
        }
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

        // Per-step workflow notifications. Seven optional FKs from one table to NotificationConfig, so every
        // one is spelled out: EF's default cascade would give SQL Server seven cascade paths into the same
        // table and it refuses to create the constraints at all. Restrict is also the behaviour that belongs
        // here -- deleting a notification configuration must not take approval steps with it.
        modelBuilder.Entity<HCMv3.Workflow.RequestDetail>(b =>
        {
            b.HasOne(e => e.NotificationConfig)
                .WithMany()
                .HasForeignKey(e => e.NotificationConfigId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.SubmitNotificationConfig)
                .WithMany()
                .HasForeignKey(e => e.SubmitNotificationConfigId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.RejectNotificationConfig)
                .WithMany()
                .HasForeignKey(e => e.RejectNotificationConfigId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.SendBackNotificationConfig)
                .WithMany()
                .HasForeignKey(e => e.SendBackNotificationConfigId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.RemarksNotificationConfig)
                .WithMany()
                .HasForeignKey(e => e.RemarksNotificationConfigId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.AssignNotificationConfig)
                .WithMany()
                .HasForeignKey(e => e.AssignNotificationConfigId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.ReminderNotificationConfig)
                .WithMany()
                .HasForeignKey(e => e.ReminderNotificationConfigId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HCMv3.GeneralMasterInfo.NotificationConfig>(b =>
        {
            b.HasOne(e => e.EmailTemplate)
                .WithMany()
                .HasForeignKey(e => e.EmailTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(e => e.NotificationGroups)
                .WithOne(e => e.NotificationConfig)
                .HasForeignKey(e => e.NotificationConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Legacy AbsenceViolationRuleDetail.NotificationConfig: who is told when an escalation step fires.
        // Restrict: a configuration still named on a step must not be deletable out from under it.
        modelBuilder.Entity<AbsenceViolationRuleDetail>(b =>
        {
            b.HasOne(e => e.NotificationConfig)
                .WithMany()
                .HasForeignKey(e => e.NotificationConfigId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HCMv3.GeneralMasterInfo.NotificationGroup>(b =>
        {
            b.HasOne(e => e.WorkflowRole)
                .WithMany()
                .HasForeignKey(e => e.WorkflowRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.WorkflowRoleOrg)
                .WithMany()
                .HasForeignKey(e => e.WorkflowRoleOrgId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Every read of attachments is "the files of this one record".
        modelBuilder.Entity<AppAttachment>(b => { b.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId }); });

        // Integration Center. One row per field: a duplicate would make "which value is live" depend on
        // row order, and the setting store does find-then-insert, which two concurrent saves can race.
        modelBuilder.Entity<HCMv3.Integrations.IntegrationSetting>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.IntegrationKey, e.SettingKey })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        // The card shows the latest attempt per integration, and the history list reads one integration
        // newest-first. Both are this index.
        modelBuilder.Entity<HCMv3.Integrations.IntegrationConnectionLog>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.IntegrationKey, e.CreationTime });
        });

        // NOT "SysObjects". SQL Server resolves that name -- dbo-qualified or not, any case -- to its own
        // backward-compatibility view sys.sysobjects, so every query against the table failed with "Invalid column
        // name" and the System Objects screen could never have worked. The payroll lock reads this table.
        modelBuilder.Entity<SysObject>().ToTable("AppSysObjects");

        // AI assistant (feat/ai-agent). The conversation list is "my conversations, newest first"; the
        // thread and the pending-action card are both read per conversation. Messages and actions are
        // never deleted with their conversation (Restrict): a deleted conversation is soft-deleted, and the
        // record of what data the model was shown must outlive the user tidying their chat list.
        modelBuilder.Entity<AiConversation>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.UserId, e.LastMessageTime });
        });

        modelBuilder.Entity<AiMessage>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ConversationId });

            b.HasOne(e => e.Conversation)
                .WithMany(e => e.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AiPendingAction>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ConversationId, e.Status });

            b.HasOne(e => e.Conversation)
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

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

        // Positions report to positions, which is what the Position Org Chart draws. Restrict on delete for
        // the same reason as the comment thread above: removing a position must not silently take the
        // positions beneath it with it -- they need re-parenting, and a cascade would destroy a branch of
        // the structure on one click.
        modelBuilder.Entity<Position>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ParentPositionId });

            b.HasOne(e => e.ParentPosition)
                .WithMany(e => e.ChildPositions)
                .HasForeignKey(e => e.ParentPositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // The screen catalogue. Both uniques are enforced HERE and not only in the app service: the
        // service checks for a clashing code before writing, which is check-then-act and races with
        // itself under concurrent edits. The index is what actually makes a duplicate impossible.
        modelBuilder.Entity<Screen>(b =>
        {
            // Identity. Two rows for one screen would split a user's favourites across both.
            b.HasIndex(e => new { e.TenantId, e.ScreenKey })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // Codes are how people refer to screens to each other; two screens answering to one code
            // makes every such reference ambiguous.
            b.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<UserScreenUsage>(b =>
        {
            // One row per user per screen -- the service reads it with FirstOrDefault and updates in
            // place, so a second row would be silently ignored and its visits lost.
            b.HasIndex(e => new { e.TenantId, e.UserId, e.ScreenKey }).IsUnique();
        });

        modelBuilder.Entity<SharedQuickLink>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.DisplayOrder });
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

        // The rule engine. Code is unique per tenant because seed data, imports and the routing hook all
        // find a rule by its code -- a duplicate would make "which rule is PROBATION_END_DATE" depend on
        // insertion order.
        modelBuilder.Entity<BackgroundWorkerStatus>(b =>
        {
            // One row per task: the recorder finds a task's row by name and updates it in place.
            b.HasIndex(e => e.WorkerName)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<BackgroundJobFailure>(b =>
        {
            // The screen lists a company's recent failures newest first; the prune deletes by age.
            b.HasIndex(e => new { e.TenantId, e.FailedTime });
            b.HasIndex(e => e.JobId);
        });

        modelBuilder.Entity<BusinessRule>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            b.HasIndex(e => new { e.TenantId, e.BaseObject });
        });

        // The runner's hot-path query: "every rule armed on this base object and event". It runs inside
        // saves, so it must be an indexed seek; without this index the engine would table-scan the
        // assignments on every employee save even when the answer is "none".
        modelBuilder.Entity<BusinessRuleAssignment>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.BaseObject, e.EventType, e.IsActive });

            // Cascade, unusually for this codebase: an assignment whose rule is gone is not a record worth
            // keeping, it is a binding to nothing.
            b.HasOne(e => e.BusinessRule)
                .WithMany(e => e.Assignments)
                .HasForeignKey(e => e.BusinessRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // No foreign key to BusinessRule on purpose -- see the entity's remarks. A log entry must remain
        // readable after the rule it describes has been deleted, which is precisely when someone is most
        // likely to be reading it.
        modelBuilder.Entity<BusinessRuleExecutionLog>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.BusinessRuleId, e.ExecutedAt });
            b.HasIndex(e => new { e.TenantId, e.Succeeded, e.ExecutedAt });
        });

        modelBuilder.Entity<EmployeeOrgAssignment>(b =>
        {
            b.HasOne(e => e.WorkflowGroup)
                .WithMany()
                .HasForeignKey(e => e.WorkflowGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Legacy EmployeeOrgAssignment.Position. Restrict: a position still named on an assignment must not
            // be deletable, and Employee already cascades to this table.
            b.HasOne(e => e.Position)
                .WithMany()
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Resolving "which request governs this transaction row" -- the lookup behind the Workflow History
        // button on every workflow-enabled screen. Not unique: a row legitimately accumulates several
        // requests over its life (the original submission, then later amendments).
        modelBuilder.Entity<WorkflowRequest>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.RequestType, e.TargetEntityId });
        });

        // The tenant's request-number counter. Unique, and that is the point rather than tidiness: a second
        // counter row for one tenant would let two callers each increment their own and hand out the same
        // number to different requests. It is also what makes the generator's seed-once path safe -- the
        // index is what turns "two callers created the counter at the same moment" into a caught exception
        // and a retry, instead of into a duplicate that nothing would ever notice.
        modelBuilder.Entity<RequestNumberSequence>(b =>
        {
            b.HasIndex(e => e.TenantId).IsUnique();
        });

        // The approver inbox: "everything waiting on me, across every request type". This index is what
        // keeps that a single indexed seek rather than a scan, and it is the reason WorkflowRequest carries
        // its own SummaryEn/SummaryAr -- the inbox never touches the ~50 transaction tables.
        modelBuilder.Entity<WorkFlowApprover>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ApproverId, e.IsApproved });

            // Created by Added_WorkflowStep_To_WorkFlowApprovers, which was written by hand and did not say so
            // here. Declared now so the model matches the database: without it every later "add migration"
            // offers to drop an index the application relies on.
            b.HasIndex(e => new { e.WorkflowRequestId, e.WorkflowStep });
        });

        // Created by Added_Feedback_Fields_To_WorkflowRemarks, by hand, for the same reason as above.
        modelBuilder.Entity<WorkflowRemark>(b =>
        {
            b.HasIndex(e => new { e.CCReceiverId, e.RespondedAt });
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

            // Read on the hot path twice over: self-service lists a delegator's own rows by employee, and
            // the notification/substitution lookups ask "who is covering for this employee today" on every
            // routed request -- both with the ApprovedOnly filter reading WorkflowStatus on the same pass.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });
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

            // An extension points at the movement it replaced. Never cascading: the replaced row is the
            // record of the earlier due date, and SQL Server refuses a cascading self-reference anyway.
            b.HasOne<EmployeeDocumentMvmt>()
                .WithMany()
                .HasForeignKey(e => e.ExtendsMovementId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-service lists every one of these rows by employee, and the ApprovedOnly filter reads
            // WorkflowStatus on the same pass.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });

            // "Has a withdrawal already been raised from this leave?" is asked on every leave action at the
            // configured step. Not unique and not a foreign key: see EmployeeDocumentMvmt.SourceEmployeeAbsenceId.
            b.HasIndex(e => e.SourceEmployeeAbsenceId);
        });

        modelBuilder.Entity<TimeLogApproval>(b =>
        {
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // The approver inbox and the admin grid both list these by employee, and the ApprovedOnly filter
            // reads WorkflowStatus on the same pass.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });
        });

        modelBuilder.Entity<TimeLogApprovalDetail>(b =>
        {
            b.HasOne(e => e.TimeLogApproval)
                .WithMany(e => e.Details)
                .HasForeignKey(e => e.TimeLogApprovalId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict, not Cascade: TimeRegister is already reachable from TimeLogApproval through Employee,
            // so a second cascade path would fail the migration -- and a detail row is the record of what was
            // proposed for a day, which must not vanish with the day.
            b.HasOne(e => e.TimeRegister)
                .WithMany()
                .HasForeignKey(e => e.TimeRegisterId)
                .OnDelete(DeleteBehavior.Restrict);

            // The handler's "is this day already in another in-flight batch?" check queries by TimeRegisterId.
            b.HasIndex(e => new { e.TenantId, e.TimeRegisterId });
        });

        modelBuilder.Entity<TimeLogHistory>(b =>
        {
            // Restrict on both, for the same reason: this row exists precisely to outlive the approval it
            // records, so that "somebody approved a different set of figures" survives.
            b.HasOne(e => e.TimeLogApproval)
                .WithMany()
                .HasForeignKey(e => e.TimeLogApprovalId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.TimeRegister)
                .WithMany()
                .HasForeignKey(e => e.TimeRegisterId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(e => new { e.TenantId, e.TimeRegisterId });
        });

        modelBuilder.Entity<TimeRegister>(b =>
        {
            // Bare FK, no navigation on TimeRegister -- see TimeRegister.TimeLogApprovalId. Restrict because
            // deleting an approval must not take the calculated days with it.
            b.HasOne<TimeLogApproval>()
                .WithMany()
                .HasForeignKey(e => e.TimeLogApprovalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeePersonalDataChange>(b =>
        {
            // Every FK on this table is Restrict. Employee is already reachable by several cascade paths
            // through the rest of the graph, and the lookups reach one another as well (City -> Governorate
            // -> Country), so anything but Restrict produces "multiple cascade paths" at migration time --
            // handled the same way as EmployeeDocumentMvmt and SponsorGovtRepresentative above. Restrict is
            // right on its own terms too: an approved change is the audit trail of who altered somebody's
            // identity and when, so deleting a lookup row must not take it with them.
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Title)
                .WithMany()
                .HasForeignKey(e => e.TitleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Nationality)
                .WithMany()
                .HasForeignKey(e => e.NationalityId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Religion)
                .WithMany()
                .HasForeignKey(e => e.ReligionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.MaritalStatus)
                .WithMany()
                .HasForeignKey(e => e.MaritalStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Governorate)
                .WithMany()
                .HasForeignKey(e => e.GovernorateId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.BirthPlace)
                .WithMany()
                .HasForeignKey(e => e.BirthPlaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-service lists these by employee and the ApprovedOnly filter reads WorkflowStatus on the
            // same pass.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });
        });

        modelBuilder.Entity<EmployeeLoanExemption>(b =>
        {
            // Self-service lists an employee's exemptions and the ApprovedOnly filter reads WorkflowStatus
            // on the same pass, so both belong in the one index.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });

            // Restrict, not the convention's Cascade. Two paths reach Employee from this row -- directly via
            // EmployeeId, and again through the parent EmployeeLoan -- and SQL Server refuses to create the
            // second cascading one ("may cause cycles or multiple cascade paths"). Restrict is right on the
            // merits too: a skipped instalment is part of a loan's repayment history, and deleting an
            // employee record must not quietly rewrite what was repaid.
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeAssignedTask>(b =>
        {
            // The assignee's task list and the approver's view both read by employee, and the ApprovedOnly
            // filter reads WorkflowStatus on the same pass, so both belong in the one index.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });

            // Restrict, not the convention's Cascade. Employee is reached from several rows in this model at
            // once and SQL Server refuses the second cascading path ("may cause cycles or multiple cascade
            // paths") at migration time -- we have hit that twice this week. It is right on the merits too:
            // work assigned to somebody who has since left is history, and deleting an employee record must
            // not quietly erase what they were asked to do.
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeMobileDevice>(b =>
        {
            // Self-service lists an employee's own handsets and the ApprovedOnly filter reads WorkflowStatus
            // on the same pass, so both belong in the one index.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });

            // The lookup index. IMobileDeviceRegistrationService is asked "is this device id registered" on
            // what is intended to become the mobile punch path, i.e. once per punch per employee per day,
            // and the duplicate rule queries by device id across employees -- neither of which the index
            // above can serve, because both start from the device id rather than from the employee.
            b.HasIndex(e => new { e.TenantId, e.DeviceId });

            // Restrict, not the convention's Cascade. Employee already has cascading paths reaching it from
            // several transaction tables and SQL Server refuses another ("may cause cycles or multiple
            // cascade paths"). It is right on the merits too: this row is the record of which handset was
            // allowed to punch as this person, which is evidence about attendance that must not disappear
            // with the employee record.
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeePaymentBatch>(b =>
        {
            // No EmployeeId on this row -- a batch is about a group -- so the index is the one the list
            // screen and the ApprovedOnly filter actually read together.
            b.HasIndex(e => new { e.TenantId, e.WorkflowStatus });

            b.HasOne(e => e.WageType)
                .WithMany()
                .HasForeignKey(e => e.WageTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Currency)
                .WithMany()
                .HasForeignKey(e => e.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeePaymentBatchDetail>(b =>
        {
            b.HasOne(e => e.EmployeePaymentBatch)
                .WithMany(e => e.Details)
                .HasForeignKey(e => e.EmployeePaymentBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict on Employee, NOT Cascade. Employee is reachable from this row by more than one path
            // once the batch's payments exist, and SQL Server refuses the second cascading one ("may cause
            // cycles or multiple cascade paths") -- the same failure EmployeeLoanExemption and
            // EmployeeDocumentMvmt hit. It is right on the merits too: a line is the record of what somebody
            // was going to be paid, and deleting an employee must not quietly rewrite a batch's total.
            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // The handler and the list screen both fetch a batch's lines by parent id.
            b.HasIndex(e => new { e.TenantId, e.EmployeePaymentBatchId });
        });

        modelBuilder.Entity<EmployeePayment>(b =>
        {
            // Bare FK, no navigation on EmployeePayment -- see EmployeePayment.EmployeePaymentBatchId.
            // Restrict because this link is what stops a batch paying out twice; a cascade would delete the
            // evidence along with the batch.
            b.HasOne<EmployeePaymentBatch>()
                .WithMany()
                .HasForeignKey(e => e.EmployeePaymentBatchId)
                .OnDelete(DeleteBehavior.Restrict);

            // The handler's "has this batch already paid out?" guard queries by this column.
            b.HasIndex(e => new { e.TenantId, e.EmployeePaymentBatchId });
        });


        modelBuilder.Entity<EmployeeClearance>(b =>
        {
            // Self-service lists these by employee and the ApprovedOnly filter reads WorkflowStatus on the
            // same pass, so both belong in the one index.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });

            b.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.ClearanceRequestType)
                .WithMany()
                .HasForeignKey(e => e.ClearanceRequestTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeClearanceDetail>(b =>
        {
            // One answer per line per clearance -- the shape the checklist upsert relies on.
            b.HasIndex(e => new { e.TenantId, e.EmployeeClearanceId, e.ClearanceRequestFieldId });

            // Two FKs reach Employee here (the clearance's own employee, and the approver who answered the
            // line), which is the multiple-cascade-paths error SQL Server rejects at migration time. Restrict
            // is right on its own merits too: who signed a line off is audit history, and deleting the
            // approver's employee record must not quietly erase it.
            b.HasOne(e => e.EmployeeClearance)
                .WithMany()
                .HasForeignKey(e => e.EmployeeClearanceId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.ClearanceRequestField)
                .WithMany()
                .HasForeignKey(e => e.ClearanceRequestFieldId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClearanceRequestFieldDetail>(b =>
        {
            // The role FK is what makes a checklist line belong to a department. Restrict: deleting a
            // workflow role must not silently take the checklist lines it owns with it -- the clearance
            // form would lose questions rather than fail loudly.
            b.HasOne(e => e.WorkflowRole)
                .WithMany()
                .HasForeignKey(e => e.WorkflowRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rendering reads every detail row for a set of roles, on every clearance an approver opens.
            b.HasIndex(e => new { e.TenantId, e.WorkflowRoleId });
        });

        modelBuilder.Entity<EmployeeLetterRequest>(b =>
        {
            // Same two-FK shape as EmployeeDocument (Employee + DocumentType, both required), which needs no
            // explicit relationship configuration, so only the index is stated here. Self-service lists these
            // rows by employee and the ApprovedOnly filter reads WorkflowStatus on the same pass -- and for
            // this type WorkflowStatus is also what the CanPrint flag is computed from, so it is on the hot
            // path twice.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });
        });

        modelBuilder.Entity<EmployeeHealthReimburse>(b =>
        {
            // Self-service lists these by employee and the ApprovedOnly filter reads WorkflowStatus on the
            // same pass. PaymentDate is in the index too, because it is on the hot path twice over: the
            // balance calculation sums a date window, and the duplicate guard looks up a single date.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.PaymentDate, e.WorkflowStatus });
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

        modelBuilder.Entity<Travel>(b =>
        {
            // Self-service lists an employee's own trips and the ApprovedOnly filter reads WorkflowStatus on
            // the same pass -- and it is this row's status alone that decides whether the trip's legs and
            // cost lines are reachable, since neither of those carries a status of its own.
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkflowStatus });
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
