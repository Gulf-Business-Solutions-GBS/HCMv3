using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using HCMv3.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class ApplicantsAppService_Tests : RecruitmentAppServiceTestBase
{
    private ApplicantsAppService CreateAppService()
    {
        var rootResolver = NSubstitute.Substitute.For<HCMv3.Storage.IFileStorageRootResolver>();
        rootResolver.GetCurrentRootNameAsync().Returns(LocalFileStore.DefaultRootName);

        return CreateService(() => new ApplicantsAppService(
            FakeRepository.For<Applicant>(Context),
            new EntityImageStore(
                new LocalFileStore(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hcmv3-applicant-tests")),
                rootResolver)));
    }

    private CreateOrEditApplicantDto ValidDto(int nationalityId)
    {
        return new CreateOrEditApplicantDto
        {
            FirstNameEn = "Jane",
            FirstNameAr = "جين",
            LastNameEn = "Smith",
            LastNameAr = "سميث",
            NationalityId = nationalityId,
            Gender = Gender.Female,
            BirthDate = new DateTime(1990, 5, 1),
            CivilID = 111122223333,
            Email = "jane@example.com"
        };
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Applicant()
    {
        var nationalityId = CreateNationality();
        var service = CreateAppService();

        await service.CreateOrEdit(ValidDto(nationalityId));

        var result = await service.GetAll(new GetAllApplicantsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].FirstNameEn.ShouldBe("Jane");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
        result.Items[0].FullNameEn.ShouldContain("Jane");
    }

    [Fact]
    public async Task Should_Throw_When_BirthDate_Is_Today_Or_Future()
    {
        var nationalityId = CreateNationality();
        var service = CreateAppService();

        var dto = ValidDto(nationalityId);
        dto.BirthDate = DateTime.Now.Date;

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(dto));
    }

    [Fact]
    public async Task Should_Throw_When_Email_Duplicate()
    {
        var nationalityId = CreateNationality();
        var service = CreateAppService();

        await service.CreateOrEdit(ValidDto(nationalityId));

        var dto2 = ValidDto(nationalityId);
        dto2.FirstNameEn = "Other";

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(dto2));
    }
}
