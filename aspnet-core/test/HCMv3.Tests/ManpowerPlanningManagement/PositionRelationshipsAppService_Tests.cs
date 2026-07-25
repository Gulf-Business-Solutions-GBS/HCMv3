using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.ManpowerPlanningManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ManpowerPlanningManagement;

// ReSharper disable once InconsistentNaming
public class PositionRelationshipsAppService_Tests : ManpowerPlanningManagementAppServiceTestBase
{
    private PositionRelationshipsAppService CreateAppService()
    {
        return CreateService(() => new PositionRelationshipsAppService(
            FakeRepository.For<PositionRelationship>(Context),
            FakeRepository.For<Position>(Context)));
    }

    private int CreatePosition(int fteLimit)
    {
        var entity = new Position
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = Guid.NewGuid().ToString("N").Substring(0, 6),
            NameEn = "Pos",
            NameAr = "بند",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            Vacant = true,
            Active = true,
            FTELimit = fteLimit
        };
        Context.Positions.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_Relationship_And_Mark_Position_Not_Vacant()
    {
        var positionId = CreatePosition(fteLimit: 1);
        var employeeId = CreateEmployee("E1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPositionRelationshipDto
        {
            PositionId = positionId,
            EmployeeId = employeeId,
            StartDate = DateTime.Now.Date.AddDays(-10),
            EndDate = DateTime.Now.Date.AddDays(10)
        });

        var position = await Context.Positions.FindAsync(positionId);
        position.Vacant.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Throw_When_FTELimit_Exceeded()
    {
        var positionId = CreatePosition(fteLimit: 1);
        var employee1 = CreateEmployee("E2");
        var employee2 = CreateEmployee("E3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPositionRelationshipDto
        {
            PositionId = positionId,
            EmployeeId = employee1,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPositionRelationshipDto
            {
                PositionId = positionId,
                EmployeeId = employee2,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 12, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var positionId = CreatePosition(fteLimit: 1);
        var employeeId = CreateEmployee("E4");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPositionRelationshipDto
            {
                PositionId = positionId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }
}
