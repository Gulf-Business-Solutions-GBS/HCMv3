using System.Threading.Tasks;
using Abp.UI;
using HCMv3.CompetencyDevelopment;
using HCMv3.CompetencyDevelopment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.CompetencyDevelopment;

// ReSharper disable once InconsistentNaming
public class CDAnnualYearReviewsAppService_Tests : CompetencyDevelopmentAppServiceTestBase
{
    private CDAnnualYearReviewsAppService CreateAppService()
    {
        return CreateService(() => new CDAnnualYearReviewsAppService(FakeRepository.For<CDAnnualYearReview>(Context)));
    }

    private int CreateForm()
    {
        var period = new CDPeriod { TenantId = AbpSession.TenantId ?? 1, Name = "P1" };
        Context.CDPeriods.Add(period);
        Context.SaveChanges();

        var employeeId = CreateEmployee();

        var form = new CDForm { TenantId = AbpSession.TenantId ?? 1, CDPeriodId = period.Id, EmployeeId = employeeId };
        Context.CDForms.Add(form);
        Context.SaveChanges();
        return form.Id;
    }

    private int CreateCompetency(string nameEn)
    {
        var type = new CompetencyType { TenantId = AbpSession.TenantId ?? 1, NameEn = "T", NameAr = "T" };
        Context.CompetencyTypes.Add(type);
        Context.SaveChanges();

        var entity = new Competency { TenantId = AbpSession.TenantId ?? 1, NameEn = nameEn, NameAr = nameEn, CompetencyTypeId = type.Id };
        Context.Competencies.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateRating(string nameEn)
    {
        var entity = new CDRating { TenantId = AbpSession.TenantId ?? 1, NameEn = nameEn, NameAr = nameEn };
        Context.CDRatings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CDAnnualYearReview()
    {
        var formId = CreateForm();
        var competencyId = CreateCompetency("Teamwork");
        var ratingId = CreateRating("Meets Expectations");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCDAnnualYearReviewDto
        {
            CDFormId = formId,
            CompetencyId = competencyId,
            Weight = 50,
            Target = "Deliver on time",
            CDRatingId = ratingId
        });

        var result = await service.GetAll(new GetAllCDAnnualYearReviewsInput { CDFormId = formId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CompetencyName.ShouldBe("Teamwork");
        result.Items[0].CDRatingName.ShouldBe("Meets Expectations");
    }

    [Fact]
    public async Task Should_Throw_When_Competency_Already_Reviewed()
    {
        var formId = CreateForm();
        var competencyId = CreateCompetency("Communication");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCDAnnualYearReviewDto { CDFormId = formId, CompetencyId = competencyId, Weight = 40 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditCDAnnualYearReviewDto { CDFormId = formId, CompetencyId = competencyId, Weight = 30 }));
    }
}
