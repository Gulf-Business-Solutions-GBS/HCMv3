using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupsAppService(FakeRepository.For<EmployeeSubGroup>(Context)));
    }

    private int CreateEmployeeGroup(string code)
    {
        var entity = new EmployeeGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.EmployeeGroups.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeSubGroup()
    {
        var employeeGroupId = CreateEmployeeGroup("EG1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupDto
        {
            EmployeeGroupId = employeeGroupId,
            Code = "ESG1",
            NameEn = "Managers Sub",
            NameAr = "المدراء الفرعي",
            AirTicketPayType = AirTicketPayType.FareAmount,
            HealthInsurancePeriod = CustomStartMonth.January
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Managers Sub");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var employeeGroupId = CreateEmployeeGroup("EG2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupDto
        {
            EmployeeGroupId = employeeGroupId,
            Code = "ESG2",
            NameEn = "Staff Sub",
            NameAr = "الموظفون الفرعي"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupDto
            {
                EmployeeGroupId = employeeGroupId,
                Code = "ESG2",
                NameEn = "Workers Sub",
                NameAr = "العمال الفرعي"
            }));
    }
}
