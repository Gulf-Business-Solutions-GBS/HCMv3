using System.Threading.Tasks;
using Abp.Domain.Entities;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AXBatchesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AXBatchesAppService CreateAppService()
    {
        return CreateService(() => new AXBatchesAppService(FakeRepository.For<AXBatch>(Context)));
    }

    [Fact]
    public async Task Should_Create_AXBatch()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAXBatchDto
        {
            JournalBatchNumber = "JB001",
            Name = "January Batch",
            Description = "January payroll journal batch"
        });

        var result = await service.GetAll(new GetAllAXBatchesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("January Batch");
    }

    [Fact]
    public async Task Should_Throw_When_Updating_Nonexistent_AXBatch()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAXBatchDto
            {
                Id = 999,
                JournalBatchNumber = "JB999",
                Name = "Missing Batch",
                Description = "Should not exist"
            }));
    }
}
