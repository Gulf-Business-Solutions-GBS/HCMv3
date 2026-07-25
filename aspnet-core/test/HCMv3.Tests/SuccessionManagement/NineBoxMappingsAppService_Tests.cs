using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SuccessionManagement;
using HCMv3.SuccessionManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SuccessionManagement;

// ReSharper disable once InconsistentNaming
public class NineBoxMappingsAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private NineBoxMappingsAppService CreateAppService()
    {
        return CreateService(() => new NineBoxMappingsAppService(FakeRepository.For<NineBoxMapping>(Context)));
    }

    private int CreateRating()
    {
        var entity = new SuccessionRating { TenantId = AbpSession.TenantId ?? 1, NameEn = "Exceeds", NameAr = "يفوق" };
        Context.SuccessionRatings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateNineBoxRating()
    {
        var entity = new NineBoxRating { TenantId = AbpSession.TenantId ?? 1, NameEn = "Star", NameAr = "نجم" };
        Context.NineBoxRatings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_NineBoxMapping()
    {
        var ratingId = CreateRating();
        var nineBoxRatingId = CreateNineBoxRating();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNineBoxMappingDto { RatingId = ratingId, NineBoxRatingId = nineBoxRatingId });

        var result = await service.GetAll(new GetAllNineBoxMappingsInput());
        result.TotalCount.ShouldBe(1);
        result.Items.Single().NineBoxRatingId.ShouldBe(nineBoxRatingId);
    }

    [Fact]
    public async Task Should_Update_NineBoxMapping()
    {
        var ratingId = CreateRating();
        var nineBoxRatingId = CreateNineBoxRating();
        var newNineBoxRatingId = CreateNineBoxRating();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNineBoxMappingDto { RatingId = ratingId, NineBoxRatingId = nineBoxRatingId });
        var all = await service.GetAll(new GetAllNineBoxMappingsInput());
        var id = all.Items.Single().Id;

        await service.CreateOrEdit(new CreateOrEditNineBoxMappingDto { Id = id, RatingId = ratingId, NineBoxRatingId = newNineBoxRatingId });

        var edit = await service.GetNineBoxMappingForEdit(new EntityDto(id));
        edit.NineBoxMapping.NineBoxRatingId.ShouldBe(newNineBoxRatingId);
    }

    [Fact]
    public async Task Should_Delete_NineBoxMapping()
    {
        var ratingId = CreateRating();
        var nineBoxRatingId = CreateNineBoxRating();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNineBoxMappingDto { RatingId = ratingId, NineBoxRatingId = nineBoxRatingId });
        var all = await service.GetAll(new GetAllNineBoxMappingsInput());
        var id = all.Items.Single().Id;

        await service.Delete(new EntityDto(id));

        (await service.GetAll(new GetAllNineBoxMappingsInput())).TotalCount.ShouldBe(0);
    }
}
