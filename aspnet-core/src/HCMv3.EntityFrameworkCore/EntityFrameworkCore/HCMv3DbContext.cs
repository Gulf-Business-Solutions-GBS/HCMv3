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
using HCMv3.Storage;

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

        modelBuilder.ConfigureOpenIddict();
    }
}
