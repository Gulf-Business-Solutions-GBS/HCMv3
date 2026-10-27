using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GatePassManagement;
using HCMv3.GatePassManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class GatePassesAppService_Tests : GatePassManagementAppServiceTestBase
{
    private GatePassesAppService CreateAppService()
    {
        return CreateService(() => new GatePassesAppService(
            FakeRepository.For<GatePass>(Context),
            FakeRepository.For<GatePassMaterial>(Context),
            FakeRepository.For<GatePassPerson>(Context),
            FakeRepository.For<GatePassVehicle>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_GatePass()
    {
        var service = CreateAppService();
        var typeId = CreateGatePassType();
        var movementId = CreateGatePassMovement();

        await service.CreateOrEdit(new CreateOrEditGatePassDto
        {
            GatePassNumber = 100,
            GatePassTypeId = typeId,
            GatePassMovementId = movementId,
            IssueDate = new DateTime(2024, 1, 1),
            ValidUpto = new DateTime(2024, 1, 5)
        });

        var result = await service.GetAll(new GetAllGatePassesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].GatePassNumber.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Throw_When_ValidUpto_Before_IssueDate()
    {
        var service = CreateAppService();
        var typeId = CreateGatePassType();
        var movementId = CreateGatePassMovement();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditGatePassDto
            {
                GatePassNumber = 1,
                GatePassTypeId = typeId,
                GatePassMovementId = movementId,
                IssueDate = new DateTime(2024, 1, 10),
                ValidUpto = new DateTime(2024, 1, 5)
            }));
    }

    [Fact]
    public async Task Should_Delete_GatePass_And_Cascade_Children()
    {
        var service = CreateAppService();
        var gatePassId = CreateGatePass();

        Context.GatePassMaterials.Add(new GatePassMaterial
        {
            TenantId = AbpSession.TenantId ?? 1,
            GatePassId = gatePassId,
            ItemCode = "IC1",
            ItemName = "Laptop",
            Quantity = 1,
            ExpectedReturnDate = new DateTime(2024, 2, 1)
        });
        Context.SaveChanges();

        await service.Delete(new EntityDto(gatePassId));

        Context.GatePasses.Find(gatePassId).ShouldBeNull();
        Context.GatePassMaterials.ShouldBeEmpty();
    }
}
