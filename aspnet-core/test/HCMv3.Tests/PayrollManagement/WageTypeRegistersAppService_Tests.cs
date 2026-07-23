using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class WageTypeRegistersAppService_Tests : PayrollManagementAppServiceTestBase
{
    private WageTypeRegistersAppService CreateAppService()
    {
        return CreateService(() => new WageTypeRegistersAppService(FakeRepository.For<WageTypeRegister>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_WageTypeRegister()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageTypeRegisterDto
        {
            EmployeeId = null,
            ForPeriod = 202501,
            InPeriod = 202501
        });

        var result = await service.GetAll(new GetAllWageTypeRegistersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].ForPeriod.ShouldBe(202501);
        result.Items[0].EmployeeId.ShouldBeNull();
    }
}
