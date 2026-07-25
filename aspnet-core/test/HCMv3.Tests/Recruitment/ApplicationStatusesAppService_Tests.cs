using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class ApplicationStatusesAppService_Tests : RecruitmentAppServiceTestBase
{
    private ApplicationStatusesAppService CreateAppService()
    {
        return CreateService(() => new ApplicationStatusesAppService(
            FakeRepository.For<ApplicationStatus>(Context),
            FakeRepository.For<ApplicantCV>(Context),
            FakeRepository.For<Employee>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ApplicationStatus()
    {
        var applicantCVId = CreateApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Applied
        });

        var result = await service.GetAll(new GetAllApplicationStatusesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].AppStatus.ShouldBe(AppStatus.Applied);
    }

    [Fact]
    public async Task Should_Create_Employee_When_Status_Becomes_Joined()
    {
        var applicantId = CreateApplicant();
        var applicant = Context.Applicants.Single(a => a.Id == applicantId);
        applicant.JoiningDate = new DateTime(2026, 1, 1);
        applicant.ProbationEndDate = new DateTime(2026, 4, 1);
        Context.SaveChanges();

        var applicantCVId = CreateApplicantCV(applicantId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Joined
        });

        var employee = Context.Employees.SingleOrDefault(e => e.FirstNameEn == applicant.FirstNameEn);
        employee.ShouldNotBeNull();
        employee.JoiningDate.ShouldBe(applicant.JoiningDate);
    }
}
