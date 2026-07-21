using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using HCMv3.Authorization.Session;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Authorization.Session;

public class ExpiredSessionCleanupWorker_Tests : AppTestBase<HCMv3TestModule>
{
    private readonly ExpiredSessionCleanupWorker _worker;
    private readonly IRepository<UserSession, long> _userSessionRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ExpiredSessionCleanupWorker_Tests()
    {
        _worker = Resolve<ExpiredSessionCleanupWorker>();
        _userSessionRepository = Resolve<IRepository<UserSession, long>>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private async Task ExecuteWorkerAsync()
    {
        var method = typeof(ExpiredSessionCleanupWorker)
            .GetMethod("DoWorkAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        await (Task)method!.Invoke(_worker, null)!;
    }

    private void InsertSession(long userId, int? tenantId, bool isActive, int daysOld)
    {
        UsingDbContext(tenantId, context =>
        {
            var session = new UserSession(userId, tenantId, "192.168.1.1", "TestAgent/1.0");

            context.UserSessions.Add(session);
            context.SaveChanges();

            var oldDate = DateTime.UtcNow.AddDays(-daysOld);
            context.Database.ExecuteSqlRaw(
                "UPDATE AppUserSessions SET CreationTime = {0}, LastActivityTime = {1}, SignInTime = {2}, IsActive = {3} WHERE Id = {4}",
                oldDate, oldDate, oldDate, isActive, session.Id);
        });
    }

    [Fact]
    public async Task Should_Delete_Expired_Inactive_Host_Sessions()
    {
        LoginAsHostAdmin();
        var hostUserId = AbpSession.UserId!.Value;

        InsertSession(hostUserId, null, false, 45);
        InsertSession(hostUserId, null, false, 60);

        await ExecuteWorkerAsync();

        await UsingDbContextAsync(null, async context =>
        {
            var remaining = await context.UserSessions
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == null && s.UserId == hostUserId)
                .ToListAsync();

            remaining.ShouldAllBe(s => s.IsDeleted);
        });
    }

    [Fact]
    public async Task Should_Delete_Expired_Inactive_Tenant_Sessions()
    {
        LoginAsDefaultTenantAdmin();
        var tenantUserId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId!.Value;

        InsertSession(tenantUserId, tenantId, false, 45);
        InsertSession(tenantUserId, tenantId, false, 60);

        await ExecuteWorkerAsync();

        await UsingDbContextAsync(tenantId, async context =>
        {
            var remaining = await context.UserSessions
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.UserId == tenantUserId)
                .ToListAsync();

            remaining.ShouldAllBe(s => s.IsDeleted);
        });
    }

    [Fact]
    public async Task Should_Invalidate_Stale_Active_Host_Sessions()
    {
        LoginAsHostAdmin();
        var hostUserId = AbpSession.UserId!.Value;

        InsertSession(hostUserId, null, true, 45);

        await ExecuteWorkerAsync();

        await UsingDbContextAsync(null, async context =>
        {
            var sessions = await context.UserSessions
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == null && s.UserId == hostUserId && !s.IsDeleted)
                .ToListAsync();

            sessions.ShouldNotBeEmpty();
            sessions.ShouldAllBe(s => !s.IsActive);
        });
    }

    [Fact]
    public async Task Should_Invalidate_Stale_Active_Tenant_Sessions()
    {
        LoginAsDefaultTenantAdmin();
        var tenantUserId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId!.Value;

        InsertSession(tenantUserId, tenantId, true, 45);

        await ExecuteWorkerAsync();

        await UsingDbContextAsync(tenantId, async context =>
        {
            var sessions = await context.UserSessions
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.UserId == tenantUserId && !s.IsDeleted)
                .ToListAsync();

            sessions.ShouldNotBeEmpty();
            sessions.ShouldAllBe(s => !s.IsActive);
        });
    }

    [Fact]
    public async Task Should_Not_Delete_Recent_Sessions()
    {
        LoginAsHostAdmin();
        var hostUserId = AbpSession.UserId!.Value;

        InsertSession(hostUserId, null, false, 5);
        InsertSession(hostUserId, null, true, 5);

        LoginAsDefaultTenantAdmin();
        var tenantUserId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId!.Value;

        InsertSession(tenantUserId, tenantId, false, 10);
        InsertSession(tenantUserId, tenantId, true, 10);

        await ExecuteWorkerAsync();

        await UsingDbContextAsync(null, async context =>
        {
            var allSessions = await context.UserSessions
                .IgnoreQueryFilters()
                .Where(s => !s.IsDeleted)
                .ToListAsync();

            allSessions.Count.ShouldBe(4);
        });
    }

    [Fact]
    public async Task Should_Cleanup_Both_Host_And_Tenant_Sessions_In_Single_Run()
    {
        LoginAsHostAdmin();
        var hostUserId = AbpSession.UserId!.Value;

        InsertSession(hostUserId, null, false, 45);
        InsertSession(hostUserId, null, true, 45);

        LoginAsDefaultTenantAdmin();
        var tenantUserId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId!.Value;

        InsertSession(tenantUserId, tenantId, false, 45);
        InsertSession(tenantUserId, tenantId, true, 45);

        await ExecuteWorkerAsync();

        await UsingDbContextAsync(null, async context =>
        {
            var allSessions = await context.UserSessions
                .IgnoreQueryFilters()
                .ToListAsync();

            var softDeleted = allSessions.Where(s => s.IsDeleted).ToList();
            softDeleted.Count.ShouldBe(2);

            var invalidatedStale = allSessions.Where(s => !s.IsActive && !s.IsDeleted).ToList();
            invalidatedStale.Count.ShouldBe(2);

            allSessions.ShouldAllBe(s => !s.IsActive);
        });
    }
}
