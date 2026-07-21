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

        modelBuilder.ConfigureOpenIddict();
    }
}