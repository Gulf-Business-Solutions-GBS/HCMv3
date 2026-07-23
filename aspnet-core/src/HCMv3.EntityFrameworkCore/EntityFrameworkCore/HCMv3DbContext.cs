using Abp.Json;
using Abp.OpenIddict.Applications;
using Abp.OpenIddict.Authorizations;
using Abp.OpenIddict.EntityFrameworkCore;
using Abp.OpenIddict.Scopes;
using Abp.OpenIddict.Tokens;
using Abp.Zero.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using HCMv3.Authorization.Delegation;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Session;
using HCMv3.Authorization.Users;
using HCMv3.Chat;
using HCMv3.Editions;
using HCMv3.ExtraProperties;
using HCMv3.Friendships;
using HCMv3.GeneralMasterInfo;
using HCMv3.MultiTenancy;
using HCMv3.MultiTenancy.Accounting;
using HCMv3.MultiTenancy.Payments;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;

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


    public HCMv3DbContext(DbContextOptions<HCMv3DbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BinaryObject>(b => { b.HasIndex(e => new { e.TenantId }); });

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

        modelBuilder.ConfigureOpenIddict();
    }
}
