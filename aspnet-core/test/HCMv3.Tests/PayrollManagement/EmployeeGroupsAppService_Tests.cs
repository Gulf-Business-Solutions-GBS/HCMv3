using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeGroupsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeGroupsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeGroupsAppService(FakeRepository.For<EmployeeGroup>(Context)));
    }

    [Fact]
    public async Task Should_Create_EmployeeGroup()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeGroupDto
        {
            Code = "EG1",
            NameEn = "Managers",
            NameAr = "المدراء",
            IndemnityMaxNumber = 12,
            IndemnityMaxUnit = Unit.Month,
            IndemnityMaxAccruePercentage = 100
        });

        var result = await service.GetAll(new GetAllEmployeeGroupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Managers");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeGroupDto
        {
            Code = "EG2",
            NameEn = "Staff",
            NameAr = "الموظفون"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeGroupDto
            {
                Code = "EG2",
                NameEn = "Workers",
                NameAr = "العمال"
            }));
    }
}
