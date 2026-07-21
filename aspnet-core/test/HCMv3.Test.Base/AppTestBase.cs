using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization.Users;
using Abp.EntityFrameworkCore.Extensions;
using Abp.Events.Bus;
using Abp.Events.Bus.Entities;
using Abp.Modules;
using Abp.MultiTenancy;
using Abp.Runtime.Session;
using Abp.TestBase;
using Microsoft.EntityFrameworkCore;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using HCMv3.EntityFrameworkCore;
using HCMv3.MultiTenancy;
using HCMv3.Test.Base.TestData;

namespace HCMv3.Test.Base;

/// <summary>
/// This is base class for all our test classes.
/// It prepares ABP system, modules and a fake, in-memory database.
/// Seeds database with initial data.
/// Provides methods to easily work with <see cref="HCMv3DbContext"/>.
/// </summary>
public abstract class AppTestBase<T> : AbpIntegratedTestBase<T> where T : AbpModule
{
    private const string DefaultStringChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    protected AppTestBase()
    {
        SeedTestData();
        LoginAsDefaultTenantAdmin();
    }

    private void SeedTestData()
    {
        void NormalizeDbContext(HCMv3DbContext context)
        {
            context.EntityChangeEventHelper = NullEntityChangeEventHelper.Instance;
            context.EventBus = NullEventBus.Instance;
            context.SuppressAutoSetTenantId = true;
        }

        //Seed initial data for default tenant
        AbpSession.TenantId = 1;

        UsingDbContext(context =>
        {
            NormalizeDbContext(context);
            new TestDataBuilder(context, 1).Create();
        });
    }

    protected IDisposable UsingTenantId(int? tenantId)
    {
        var previousTenantId = AbpSession.TenantId;
        AbpSession.TenantId = tenantId;
        return new DisposeAction(() => AbpSession.TenantId = previousTenantId);
    }

    protected void UsingDbContext(Action<HCMv3DbContext> action)
    {
        UsingDbContext(AbpSession.TenantId, action);
    }

    protected Task UsingDbContextAsync(Func<HCMv3DbContext, Task> action)
    {
        return UsingDbContextAsync(AbpSession.TenantId, action);
    }

    protected TResult UsingDbContext<TResult>(Func<HCMv3DbContext, TResult> func)
    {
        return UsingDbContext(AbpSession.TenantId, func);
    }

    protected Task<TResult> UsingDbContextAsync<TResult>(Func<HCMv3DbContext, Task<TResult>> func)
    {
        return UsingDbContextAsync(AbpSession.TenantId, func);
    }

    protected void UsingDbContext(int? tenantId, Action<HCMv3DbContext> action)
    {
        using (UsingTenantId(tenantId))
        {
            using (var context = LocalIocManager.Resolve<HCMv3DbContext>())
            {
                action(context);
                context.SaveChanges();
            }
        }
    }

    protected async Task UsingDbContextAsync(int? tenantId, Func<HCMv3DbContext, Task> action)
    {
        using (UsingTenantId(tenantId))
        {
            using (var context = LocalIocManager.Resolve<HCMv3DbContext>())
            {
                await action(context);
                await context.SaveChangesAsync();
            }
        }
    }

    protected TResult UsingDbContext<TResult>(int? tenantId, Func<HCMv3DbContext, TResult> func)
    {
        TResult result;

        using (UsingTenantId(tenantId))
        {
            using (var context = LocalIocManager.Resolve<HCMv3DbContext>())
            {
                result = func(context);
                context.SaveChanges();
            }
        }

        return result;
    }

    protected async Task<TResult> UsingDbContextAsync<TResult>(int? tenantId, Func<HCMv3DbContext, Task<TResult>> func)
    {
        TResult result;

        using (UsingTenantId(tenantId))
        {
            using (var context = LocalIocManager.Resolve<HCMv3DbContext>())
            {
                result = await func(context);
                await context.SaveChangesAsync();
            }
        }

        return result;
    }

    #region Login

    protected void LoginAsHostAdmin()
    {
        LoginAsHost(AbpUserBase.AdminUserName);
    }

    protected void LoginAsDefaultTenantAdmin()
    {
        LoginAsTenant(AbpTenantBase.DefaultTenantName, AbpUserBase.AdminUserName);
    }

    protected void LoginAsHost(string userName)
    {
        AbpSession.TenantId = null;

        var user = UsingDbContext(context => context.Users.FirstOrDefault(u => u.TenantId == AbpSession.TenantId && u.UserName == userName));
        if (user == null)
        {
            throw new Exception("There is no user: " + userName + " for host.");
        }

        AbpSession.UserId = user.Id;
    }

    protected void LoginAsTenant(string tenancyName, string userName)
    {
        AbpSession.TenantId = null;

        var tenant = UsingDbContext(context => context.Tenants.FirstOrDefault(t => t.TenancyName == tenancyName));
        if (tenant == null)
        {
            throw new Exception("There is no tenant: " + tenancyName);
        }

        AbpSession.TenantId = tenant.Id;

        var user = UsingDbContext(context => context.Users.FirstOrDefault(u => u.TenantId == AbpSession.TenantId && u.UserName == userName));
        if (user == null)
        {
            throw new Exception("There is no user: " + userName + " for tenant: " + tenancyName);
        }

        AbpSession.UserId = user.Id;
    }

    #endregion

    #region GetCurrentUser

    /// <summary>
    /// Gets current user if <see cref="IAbpSession.UserId"/> is not null.
    /// Throws exception if it's null.
    /// </summary>
    protected User GetCurrentUser()
    {
        var userId = AbpSession.GetUserId();
        return UsingDbContext(context => context.Users.Single(u => u.Id == userId));
    }

    /// <summary>
    /// Gets current user if <see cref="IAbpSession.UserId"/> is not null.
    /// Throws exception if it's null.
    /// </summary>
    protected async Task<User> GetCurrentUserAsync()
    {
        var userId = AbpSession.GetUserId();
        return await UsingDbContextAsync(async context =>
        {
            return await context.Users.SingleAsync(u => u.Id == userId);
        });
    }

    #endregion

    #region GetCurrentTenant

    /// <summary>
    /// Gets current tenant if <see cref="IAbpSession.TenantId"/> is not null.
    /// Throws exception if there is no current tenant.
    /// </summary>
    protected Tenant GetCurrentTenant()
    {
        var tenantId = AbpSession.GetTenantId();
        return UsingDbContext(null, context => context.Tenants.Single(t => t.Id == tenantId));
    }

    /// <summary>
    /// Gets current tenant if <see cref="IAbpSession.TenantId"/> is not null.
    /// Throws exception if there is no current tenant.
    /// </summary>
    protected async Task<Tenant> GetCurrentTenantAsync()
    {
        var tenantId = AbpSession.GetTenantId();
        return await UsingDbContext(null, context => context.Tenants.SingleAsync(t => t.Id == tenantId));
    }

    #endregion

    #region GetTenant / GetTenantOrNull

    protected Tenant GetTenant(string tenancyName)
    {
        return UsingDbContext(null, context => context.Tenants.Single(t => t.TenancyName == tenancyName));
    }

    protected async Task<Tenant> GetTenantAsync(string tenancyName)
    {
        return await UsingDbContext(null, async context => await context.Tenants.SingleAsync(t => t.TenancyName == tenancyName));
    }

    protected Tenant GetTenantOrNull(string tenancyName)
    {
        return UsingDbContext(null, context => context.Tenants.FirstOrDefault(t => t.TenancyName == tenancyName));
    }

    protected async Task<Tenant> GetTenantOrNullAsync(string tenancyName)
    {
        return await UsingDbContext(null, async context => await context.Tenants.FirstOrDefaultAsync(t => t.TenancyName == tenancyName));
    }

    #endregion

    #region GetRole

    protected Role GetRole(string roleName)
    {
        return UsingDbContext(context => context.Roles.Single(r => r.Name == roleName && r.TenantId == AbpSession.TenantId));
    }

    protected async Task<Role> GetRoleAsync(string roleName)
    {
        return await UsingDbContext(async context => await context.Roles.SingleAsync(r => r.Name == roleName && r.TenantId == AbpSession.TenantId));
    }

    #endregion

    #region GetUserByUserName

    protected User GetUserByUserName(string userName)
    {
        var user = GetUserByUserNameOrNull(userName);
        if (user == null)
        {
            throw new Exception("Can not find a user with username: " + userName);
        }

        return user;
    }

    protected async Task<User> GetUserByUserNameAsync(string userName)
    {
        var user = await GetUserByUserNameOrNullAsync(userName);
        if (user == null)
        {
            throw new Exception("Can not find a user with username: " + userName);
        }

        return user;
    }

    protected User GetUserByUserNameOrNull(string userName)
    {
        return UsingDbContext(context =>
            context.Users.FirstOrDefault(u =>
                u.UserName == userName &&
                u.TenantId == AbpSession.TenantId
                ));
    }

    protected async Task<User> GetUserByUserNameOrNullAsync(string userName, bool includeRoles = false)
    {
        return await UsingDbContextAsync(async context =>
            await context.Users
                .IncludeIf(includeRoles, u => u.Roles)
                .FirstOrDefaultAsync(u =>
                        u.UserName == userName &&
                        u.TenantId == AbpSession.TenantId
                ));
    }

    #endregion

    #region GetRandomString
    protected string GetRandomString(int maxLength = 13, int minLength = 0, string regexPattern = "[^A-Za-z0-9]")
    {
        if (minLength < 0 || maxLength < 0 || minLength > maxLength)
        {
            throw new AbpException("Invalid minLength or maxLength parameters");
        }

        var random = new Random();
        var regex = new Regex(regexPattern);

        var length = random.Next(minLength, maxLength + 1);

        var filteredChars = new string(DefaultStringChars.Where(c => !regex.IsMatch(c.ToString())).ToArray());

        return new string(Enumerable.Repeat(filteredChars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    #endregion
}