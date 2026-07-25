using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class DomainTypesAppService_Tests : ReportManagementAppServiceTestBase
{
    private DomainTypesAppService CreateAppService()
    {
        return CreateService(() => new DomainTypesAppService(FakeRepository.For<DomainType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_DomainType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditDomainTypeDto
        {
            NameEn = "Employee",
            EntityType = "HCM10.Module.BusinessObjects.PersonnelAdministration.Employee"
        });

        var result = await service.GetAll(new GetAllDomainTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Employee");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditDomainTypeDto { NameEn = "Employee" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditDomainTypeDto { NameEn = "Employee" }));
    }
}
