using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Auditing;
using Abp.Authorization.Users;
using Abp.Configuration.Startup;
using Abp.Domain.Entities.Auditing;
using Abp.EntityHistory;
using Abp.Events.Bus.Entities;
using Abp.Timing;
using HCMv3.Auditing;
using HCMv3.Auditing.Dto;
using HCMv3.Authorization.Roles;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Auditing;

// ReSharper disable once InconsistentNaming
public class AuditLogAppService_Tests : AppTestBase
{
    private readonly IAuditLogAppService _auditLogAppService;

    public AuditLogAppService_Tests()
    {
        _auditLogAppService = Resolve<IAuditLogAppService>();
    }

    [Fact]
    public async Task Should_Get_Audit_Logs()
    {
        //Arrange
        UsingDbContext(
            context =>
            {
                context.AuditLogs.Add(
                    new AuditLog
                    {
                        TenantId = AbpSession.TenantId,
                        UserId = AbpSession.UserId,
                        ServiceName = "ServiceName-Test-1",
                        MethodName = "MethodName-Test-1",
                        Parameters = "{}",
                        ExecutionTime = Clock.Now.AddMinutes(-1),
                        ExecutionDuration = 123
                    });

                context.AuditLogs.Add(
                    new AuditLog
                    {
                        TenantId = AbpSession.TenantId,
                        ServiceName = "ServiceName-Test-2",
                        MethodName = "MethodName-Test-2",
                        Parameters = "{}",
                        ExecutionTime = Clock.Now,
                        ExecutionDuration = 456
                    });
            });

        //Act
        var output = await _auditLogAppService.GetAuditLogs(new GetAuditLogsInput
        {
            StartDate = Clock.Now.AddMinutes(-10),
            EndDate = Clock.Now.AddMinutes(10)
        });

        output.TotalCount.ShouldBe(2);

        output.Items[0].ServiceName.ShouldBe("ServiceName-Test-2");
        output.Items[0].UserName.ShouldBe(null);

        output.Items[1].ServiceName.ShouldBe("ServiceName-Test-1");

        output.Items[1].UserName.ShouldBe(AbpUserBase.AdminUserName, StringCompareShould.IgnoreCase);
    }

    [Fact]
    public async Task Should_Get_Entity_Changes()
    {
        LoginAsHostAdmin();

        //Arrange
        UsingDbContext(
            context =>
            {
                var aTenant = context.Tenants.FirstOrDefault();

                aTenant.Name = "changed name";

                var aUser = context.Users.FirstOrDefault(u => u.TenantId == null);

                if (aUser != null)
                {
                    aUser.Name = "changed name";
                }

                context.SaveChanges();
            });

        //Act
        var entityChangeList = await _auditLogAppService.GetEntityChanges(new GetEntityChangeInput
        {
            StartDate = Clock.Now.AddMinutes(-10),
            EndDate = Clock.Now.AddMinutes(10)
        });

        entityChangeList.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Not_Throw_Exception_GetEntityTypeChanges()
    {
        var configuration = Substitute.For<IAbpStartupConfiguration>();
        configuration.EntityHistory.IsEnabled.Returns(true);
        configuration.EntityHistory.Selectors.Add(
            new NamedTypeSelector(
                "HCMv3Entities",
                type => typeof (Role).IsAssignableFrom(type)
            )
        );
        
        //Arrange
        UsingDbContext(
            context =>
            {
                context.Roles.Add(new Role(null, "TestRole", "Test Role"));

                context.SaveChanges();
            });

        var roleId = UsingDbContext(context =>
        {
            return context.Roles.FirstOrDefault(e=> e.Name == "TestRole")?.Id;
        });
        
        // Act
        await Should.NotThrowAsync(async () => await _auditLogAppService.GetEntityTypeChanges(new GetEntityTypeChangeInput()
        {
            EntityTypeFullName = typeof(Role).FullName,
            EntityId = roleId.ToString(),
        }));
        
    }
}
