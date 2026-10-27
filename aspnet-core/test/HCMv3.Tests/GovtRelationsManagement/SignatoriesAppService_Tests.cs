using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class SignatoriesAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private SignatoriesAppService CreateAppService()
    {
        return CreateService(() => new SignatoriesAppService(FakeRepository.For<Signatory>(Context)));
    }

    [Fact]
    public async Task Should_Create_Signatory()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSignatoryDto
        {
            Code = "SIG1",
            NameEn = "John Doe",
            NameAr = "جون دو",
            CivilId = 123456789012
        });

        var result = await service.GetAll(new GetAllSignatoriesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].CivilId.ShouldBe(123456789012);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSignatoryDto
        {
            Code = "SIG2",
            NameEn = "Jane Doe",
            NameAr = "جين دو",
            CivilId = 1
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSignatoryDto
            {
                Code = "SIG3",
                NameEn = "Jane Doe",
                NameAr = "جين دو2",
                CivilId = 2
            }));
    }
}
