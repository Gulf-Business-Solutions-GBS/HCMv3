using System;
using Abp.Application.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Base class for the <see cref="HCMv3.PersonnelAdministration.News"/> bespoke app-service unit tests.
/// Provides a private in-memory Sqlite <see cref="NewsTestDbContext"/> per test instance and a helper to
/// construct app services directly (bypassing the real Abp DI/interceptor pipeline -- so
/// <c>[AbpAuthorize]</c> is not enforced in these tests, only the app service's own business/validation
/// logic is exercised) with <c>AbpSession</c>/<c>LocalizationManager</c> wired so <c>L(...)</c> and
/// <c>AbpSession.TenantId</c> work. Same pattern as
/// <c>HCMv3.Tests.SystemSecurity.SystemSecurityAppServiceTestBase</c>.
/// </summary>
public abstract class NewsAppServiceTestBase : AppTestBase
{
    protected readonly NewsTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected NewsAppServiceTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NewsTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new NewsTestDbContext(options);
        Context.Database.EnsureCreated();
    }

    protected TService CreateService<TService>(Func<TService> factory) where TService : ApplicationService
    {
        var service = factory();
        service.AbpSession = AbpSession;
        service.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();
        service.UnitOfWorkManager = CreateFakeUnitOfWorkManager();
        return service;
    }

    private IUnitOfWorkManager CreateFakeUnitOfWorkManager()
    {
        var activeUnitOfWork = Substitute.For<IActiveUnitOfWork>();
        activeUnitOfWork.SaveChangesAsync().Returns(_ => Context.SaveChangesAsync());

        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Current.Returns(activeUnitOfWork);

        return unitOfWorkManager;
    }
}
