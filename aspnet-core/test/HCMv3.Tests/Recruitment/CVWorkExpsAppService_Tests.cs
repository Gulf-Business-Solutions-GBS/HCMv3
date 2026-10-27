using System;
using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class CVWorkExpsAppService_Tests : RecruitmentAppServiceTestBase
{
    private CVWorkExpsAppService CreateAppService()
    {
        return CreateService(() => new CVWorkExpsAppService(FakeRepository.For<CVWorkExp>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CVWorkExp()
    {
        var applicantCVId = CreateApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCVWorkExpDto
        {
            ApplicantCVId = applicantCVId,
            Company = "Acme Corp",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2022, 1, 1),
            Position = "Developer"
        });

        var result = await service.GetAll(new GetAllCVWorkExpsInput { ApplicantCVId = applicantCVId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Company.ShouldBe("Acme Corp");
    }

    [Fact]
    public async Task Should_Delete_CVWorkExp()
    {
        var applicantCVId = CreateApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCVWorkExpDto
        {
            ApplicantCVId = applicantCVId,
            Company = "Acme Corp",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2022, 1, 1)
        });

        var list = await service.GetAll(new GetAllCVWorkExpsInput { ApplicantCVId = applicantCVId });
        await service.Delete(new Abp.Application.Services.Dto.EntityDto(list.Items[0].Id));

        var afterDelete = await service.GetAll(new GetAllCVWorkExpsInput { ApplicantCVId = applicantCVId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
