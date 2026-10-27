using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.JobProfileManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class EducationCertificatesAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private EducationCertificatesAppService CreateAppService()
    {
        return CreateService(() => new EducationCertificatesAppService(FakeRepository.For<EducationCertificate>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EducationCertificate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "PMP",
            NameAr = "PMP"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("PMP");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "CPA", NameAr = "CPA" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "CPA", NameAr = "Other" }));
    }
}
