using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class CVQualificationsAppService_Tests : RecruitmentAppServiceTestBase
{
    private CVQualificationsAppService CreateAppService()
    {
        return CreateService(() => new CVQualificationsAppService(FakeRepository.For<CVQualification>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CVQualification()
    {
        var applicantCVId = CreateApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCVQualificationDto
        {
            ApplicantCVId = applicantCVId,
            University = "MIT",
            Percentage = 85.5,
            PassedYear = 2015
        });

        var result = await service.GetAll(new GetAllCVQualificationsInput { ApplicantCVId = applicantCVId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].University.ShouldBe("MIT");
    }

    [Fact]
    public async Task Should_Update_CVQualification()
    {
        var applicantCVId = CreateApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCVQualificationDto
        {
            ApplicantCVId = applicantCVId,
            University = "MIT",
            PassedYear = 2015
        });

        var created = await service.GetAll(new GetAllCVQualificationsInput { ApplicantCVId = applicantCVId });
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditCVQualificationDto
        {
            Id = id,
            ApplicantCVId = applicantCVId,
            University = "Harvard",
            PassedYear = 2016
        });

        var edit = await service.GetCVQualificationForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        edit.CVQualification.University.ShouldBe("Harvard");
    }
}
