using System.Threading.Tasks;
using Abp.UI;
using HCMv3.CompetencyDevelopment;
using HCMv3.CompetencyDevelopment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.CompetencyDevelopment;

// ReSharper disable once InconsistentNaming
public class CompetenciesAppService_Tests : CompetencyDevelopmentAppServiceTestBase
{
    private CompetenciesAppService CreateAppService()
    {
        return CreateService(() => new CompetenciesAppService(FakeRepository.For<Competency>(Context)));
    }

    private int CreateCompetencyType(string nameEn)
    {
        var entity = new CompetencyType { TenantId = AbpSession.TenantId ?? 1, NameEn = nameEn, NameAr = nameEn };
        Context.CompetencyTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Competency()
    {
        var typeId = CreateCompetencyType("Behavioral");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCompetencyDto
        {
            NameEn = "Teamwork",
            NameAr = "العمل الجماعي",
            CompetencyTypeId = typeId,
            Active = true
        });

        var result = await service.GetAll(new GetAllCompetenciesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].CompetencyTypeName.ShouldBe("Behavioral");
    }

    [Fact]
    public async Task Should_Throw_When_Names_Duplicate()
    {
        var typeId = CreateCompetencyType("Technical");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCompetencyDto { NameEn = "Coding", NameAr = "A", CompetencyTypeId = typeId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditCompetencyDto { NameEn = "Coding", NameAr = "B", CompetencyTypeId = typeId }));
    }
}
