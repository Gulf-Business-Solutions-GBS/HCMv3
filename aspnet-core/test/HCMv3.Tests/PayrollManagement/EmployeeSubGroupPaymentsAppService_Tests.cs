using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupPaymentsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupPaymentsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupPaymentsAppService(FakeRepository.For<EmployeeSubGroupPayment>(Context)));
    }

    private int CreateEmployeeSubGroup(string code)
    {
        var group = new EmployeeGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.EmployeeGroups.Add(group);
        Context.SaveChanges();

        var subGroup = new EmployeeSubGroup { TenantId = AbpSession.TenantId ?? 1, EmployeeGroupId = group.Id, Code = code, NameEn = code, NameAr = code };
        Context.EmployeeSubGroups.Add(subGroup);
        Context.SaveChanges();
        return subGroup.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeSubGroupPayment()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupPaymentDto
        {
            EmployeeSubGroupId = subGroupId,
            WageTypeId = 100,
            Amount = 500,
            PayInterval = PayInterval.Monthly,
            PayMonth = CustomStartMonth.January
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupPaymentsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(500);
    }

    [Fact]
    public async Task Should_Update_EmployeeSubGroupPayment()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupPaymentDto
        {
            EmployeeSubGroupId = subGroupId,
            Amount = 100,
            PayInterval = PayInterval.Monthly
        });

        var created = await service.GetAll(new GetAllEmployeeSubGroupPaymentsInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupPaymentDto
        {
            Id = id,
            EmployeeSubGroupId = subGroupId,
            Amount = 999,
            PayInterval = PayInterval.Yearly
        });

        var updated = await service.GetEmployeeSubGroupPaymentForEdit(new EntityDto(id));
        updated.EmployeeSubGroupPayment.Amount.ShouldBe(999);
        updated.EmployeeSubGroupPayment.PayInterval.ShouldBe(PayInterval.Yearly);
    }
}
