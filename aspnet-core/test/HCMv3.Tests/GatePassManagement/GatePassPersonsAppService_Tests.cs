using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GatePassManagement;
using HCMv3.GatePassManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class GatePassPersonsAppService_Tests : GatePassManagementAppServiceTestBase
{
    private GatePassPersonsAppService CreateAppService()
    {
        return CreateService(() => new GatePassPersonsAppService(FakeRepository.For<GatePassPerson>(Context)));
    }

    private int CreatePersonType()
    {
        var entity = new PersonType { TenantId = AbpSession.TenantId ?? 1, Code = "1", NameEn = "Visitor", NameAr = "Visitor" };
        Context.PersonTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateVendor()
    {
        var entity = new GatePassVendor { TenantId = AbpSession.TenantId ?? 1, Code = "1", NameEn = "Vendor", NameAr = "Vendor" };
        Context.GatePassVendors.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_GatePassPerson()
    {
        var service = CreateAppService();
        var gatePassId = CreateGatePass();
        var personTypeId = CreatePersonType();
        var employeeId = CreateEmployee();
        var vendorId = CreateVendor();

        await service.CreateOrEdit(new CreateOrEditGatePassPersonDto
        {
            GatePassId = gatePassId,
            PersonTypeId = personTypeId,
            EmployeeId = employeeId,
            PersonFullName = "John Doe",
            GatePassVendorId = vendorId
        });

        var result = await service.GetAll(new GetAllGatePassPersonsInput { GatePassId = gatePassId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].PersonFullName.ShouldBe("John Doe");
    }

    [Fact]
    public async Task Should_Throw_When_PersonType_Employee_Duplicate_In_Same_GatePass()
    {
        var service = CreateAppService();
        var gatePassId = CreateGatePass();
        var personTypeId = CreatePersonType();
        var employeeId = CreateEmployee();
        var vendorId = CreateVendor();

        await service.CreateOrEdit(new CreateOrEditGatePassPersonDto
        {
            GatePassId = gatePassId,
            PersonTypeId = personTypeId,
            EmployeeId = employeeId,
            PersonFullName = "John Doe",
            GatePassVendorId = vendorId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditGatePassPersonDto
            {
                GatePassId = gatePassId,
                PersonTypeId = personTypeId,
                EmployeeId = employeeId,
                PersonFullName = "Jane Doe",
                GatePassVendorId = vendorId
            }));
    }
}
