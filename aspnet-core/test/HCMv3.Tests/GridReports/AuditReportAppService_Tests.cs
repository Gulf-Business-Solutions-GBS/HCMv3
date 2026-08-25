using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.EntityHistory;
using HCMv3.Authorization.Users;
using HCMv3.GridReports.Administration;
using HCMv3.GridReports.Administration.Dto;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Audit Report port. Since ABP's automatic Entity History tracking is a global/per-entity
/// opt-in concern orthogonal to this report, rows are inserted directly into
/// EntityChangeSet/EntityChange/EntityPropertyChange here -- this exercises exactly the query/join/mapping
/// logic in <see cref="AuditReportAppService"/> regardless of which entities are configured for tracking.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AuditReportAppService_Tests : AppTestBase
{
    private readonly IAuditReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public AuditReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IAuditReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private long CreateUser(string userName)
    {
        long userId = 0;
        UsingDbContext(context =>
        {
            var user = context.Set<User>().Add(new User
            {
                TenantId = TenantId,
                UserName = userName,
                Name = userName,
                Surname = userName,
                EmailAddress = userName + "@test.com",
                Password = "AM4OyzZuGHwzeIP+dzk/UjBiEzUEofn9zwEBUUJTf3M2gd+dCcQXKQeIP4vJdG8W",
                IsActive = true
            }).Entity;
            context.SaveChanges();
            userId = user.Id;
        });
        return userId;
    }

    private void CreatePropertyChange(long userId, DateTime changeTime, string entityTypeFullName, string propertyName, string oldValue, string newValue)
    {
        UsingDbContext(context =>
        {
            var changeSet = context.Set<EntityChangeSet>().Add(new EntityChangeSet
            {
                TenantId = TenantId,
                UserId = userId,
                CreationTime = changeTime
            }).Entity;
            context.SaveChanges();

            // ChangeType is left at its default value -- this suite exercises the query/join/mapping logic,
            // not the specific Created/Updated/Deleted enum member, which is untyped-name-agnostic here on
            // purpose (avoids a hard dependency on the exact assembly the enum ships in).
            var change = context.Set<EntityChange>().Add(new EntityChange
            {
                TenantId = TenantId,
                EntityChangeSetId = changeSet.Id,
                ChangeTime = changeTime,
                EntityTypeFullName = entityTypeFullName,
                EntityId = "1"
            }).Entity;
            context.SaveChanges();

            context.Set<EntityPropertyChange>().Add(new EntityPropertyChange
            {
                TenantId = TenantId,
                EntityChangeId = change.Id,
                PropertyName = propertyName,
                PropertyTypeFullName = typeof(string).FullName,
                OriginalValue = oldValue,
                NewValue = newValue
            });
        });
    }

    [Fact]
    public async Task Should_Return_Only_Changes_In_The_Requested_Window()
    {
        var userId = CreateUser("AUD-USER1");
        CreatePropertyChange(userId, new DateTime(2024, 6, 15), "HCMv3.PersonnelAdministration.Employee", "Status", "Active", "Inactive");
        CreatePropertyChange(userId, new DateTime(2023, 1, 1), "HCMv3.PersonnelAdministration.Employee", "Status", "Active", "Inactive");

        var result = await _appService.GetAuditReport(new GetAuditReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            MaxResultCount = 100
        });

        result.Items.Count.ShouldBe(1);
        result.Items.Single().PropertyName.ShouldBe("Status");
    }

    [Fact]
    public async Task Should_Filter_By_UserName()
    {
        var userA = CreateUser("AUD-USERA");
        var userB = CreateUser("AUD-USERB");
        CreatePropertyChange(userA, new DateTime(2024, 6, 1), "HCMv3.Foo.Bar", "P1", "old-a", "new-a");
        CreatePropertyChange(userB, new DateTime(2024, 6, 1), "HCMv3.Foo.Bar", "P1", "old-b", "new-b");

        var result = await _appService.GetAuditReport(new GetAuditReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            AuditUserName = "AUD-USERA",
            MaxResultCount = 100
        });

        result.Items.Single().NewValue.ShouldBe("new-a");
    }

    /// <summary>Legacy's "EntityObject" column is the short type name (after the last '.').</summary>
    [Fact]
    public async Task Should_Shorten_The_Entity_Type_Full_Name_To_Its_Last_Segment()
    {
        var userId = CreateUser("AUD-SHORT");
        CreatePropertyChange(userId, new DateTime(2024, 6, 1), "HCMv3.PersonnelAdministration.EmployeeContract", "StartDate", "a", "b");

        var result = await _appService.GetAuditReport(new GetAuditReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            MaxResultCount = 100
        });

        result.Items.Single().EntityObject.ShouldBe("EmployeeContract");
    }

    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var userId = CreateUser("AUD-HDR");
        CreatePropertyChange(userId, new DateTime(2024, 6, 1), "HCMv3.Foo.Bar", "P1", "old", "new");

        var file = await _appService.GetAuditReportToExcel(new GetAuditReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            MaxResultCount = 100
        });

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
