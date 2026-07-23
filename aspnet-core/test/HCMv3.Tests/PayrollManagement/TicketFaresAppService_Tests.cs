using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class TicketFaresAppService_Tests : PayrollManagementAppServiceTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private TicketFaresAppService CreateAppService()
    {
        return CreateService(() => new TicketFaresAppService(FakeRepository.For<TicketFare>(Context)));
    }

    private (int routeId, int paxTypeId, int classId) CreateLookups(string code)
    {
        var route = new TicketRoute { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.TicketRoutes.Add(route);

        var paxType = new TicketPaxType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, AgeFrom = 0, AgeTo = 200 };
        Context.TicketPaxTypes.Add(paxType);

        var ticketClass = new TicketClass { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.TicketClasses.Add(ticketClass);

        Context.SaveChanges();
        return (route.Id, paxType.Id, ticketClass.Id);
    }

    [Fact]
    public async Task Should_Create_TicketFare_With_Infinite_EndDate()
    {
        var (routeId, paxTypeId, classId) = CreateLookups("F1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTicketFareDto
        {
            TicketRouteId = routeId,
            TicketPaxTypeId = paxTypeId,
            TicketClassId = classId,
            StartDate = new DateTime(2025, 1, 1),
            OneWayTicketFare = 100,
            ReturnTicketFare = 180
        });

        var result = await service.GetAll(new GetAllTicketFaresInput { TicketRouteId = routeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Auto_Chain_EndDate_When_New_Record_Added()
    {
        var (routeId, paxTypeId, classId) = CreateLookups("F2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTicketFareDto
        {
            TicketRouteId = routeId,
            TicketPaxTypeId = paxTypeId,
            TicketClassId = classId,
            StartDate = new DateTime(2025, 1, 1),
            OneWayTicketFare = 100,
            ReturnTicketFare = 180
        });

        await service.CreateOrEdit(new CreateOrEditTicketFareDto
        {
            TicketRouteId = routeId,
            TicketPaxTypeId = paxTypeId,
            TicketClassId = classId,
            StartDate = new DateTime(2025, 6, 1),
            OneWayTicketFare = 120,
            ReturnTicketFare = 200
        });

        var records = Context.TicketFares
            .Where(f => f.TicketRouteId == routeId && f.TicketPaxTypeId == paxTypeId && f.TicketClassId == classId)
            .OrderBy(f => f.StartDate)
            .ToList();

        records.Count.ShouldBe(2);
        records[0].EndDate.ShouldBe(new DateTime(2025, 5, 31));
        records[1].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Effective_Date()
    {
        var (routeId, paxTypeId, classId) = CreateLookups("F3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTicketFareDto
        {
            TicketRouteId = routeId,
            TicketPaxTypeId = paxTypeId,
            TicketClassId = classId,
            StartDate = new DateTime(2025, 1, 1),
            OneWayTicketFare = 100,
            ReturnTicketFare = 180
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTicketFareDto
            {
                TicketRouteId = routeId,
                TicketPaxTypeId = paxTypeId,
                TicketClassId = classId,
                StartDate = new DateTime(2025, 1, 1),
                OneWayTicketFare = 150,
                ReturnTicketFare = 250
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Negative_Fare()
    {
        var (routeId, paxTypeId, classId) = CreateLookups("F4");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTicketFareDto
            {
                TicketRouteId = routeId,
                TicketPaxTypeId = paxTypeId,
                TicketClassId = classId,
                StartDate = new DateTime(2025, 1, 1),
                OneWayTicketFare = -10,
                ReturnTicketFare = 100
            }));
    }
}
