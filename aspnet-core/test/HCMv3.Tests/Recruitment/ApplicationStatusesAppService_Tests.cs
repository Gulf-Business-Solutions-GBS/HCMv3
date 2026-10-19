using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
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
        var employeeRepository = FakeRepository.For<Employee>(Context);

        return CreateService(() => new ApplicationStatusesAppService(
            FakeRepository.For<ApplicationStatus>(Context),
            FakeRepository.For<ApplicantCV>(Context),
            employeeRepository,
            new EmployeeCodeGenerator(employeeRepository, FakeRepository.For<HCMv3.GeneralMasterInfo.NumberSeries>(Context)),
            new EmployeeValidator(employeeRepository)));
    }

    /// <summary>An applicant with all four name parts in both languages and the dates Joined requires.</summary>
    private int CreateJoinableApplicantCV()
    {
        var applicantId = CreateApplicant();
        var applicant = Context.Applicants.Single(a => a.Id == applicantId);
        applicant.FirstNameEn = "John";
        applicant.SecondNameEn = "Paul";
        applicant.ThirdNameEn = "George";
        applicant.LastNameEn = "Doe";
        applicant.FirstNameAr = "جون";
        applicant.SecondNameAr = "بول";
        applicant.ThirdNameAr = "جورج";
        applicant.LastNameAr = "دو";
        applicant.JoiningDate = new DateTime(2026, 1, 1);
        applicant.ProbationEndDate = new DateTime(2026, 4, 1);
        Context.SaveChanges();

        return CreateApplicantCV(applicantId);
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
        var applicantCVId = CreateJoinableApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Joined
        });

        var employee = Context.Employees.SingleOrDefault(e => e.FirstNameEn == "John");
        employee.ShouldNotBeNull();
        employee.JoiningDate.ShouldBe(new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task Should_Build_Full_Names_From_All_Name_Parts_When_Joined()
    {
        var applicantCVId = CreateJoinableApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Joined
        });

        var employee = Context.Employees.Single(e => e.FirstNameEn == "John");
        employee.FullNameEn.ShouldBe("John Paul George Doe");
        employee.FullNameAr.ShouldBe("جون بول جورج دو");
    }

    [Fact]
    public async Task Should_Assign_Next_Employee_Code_When_Joined()
    {
        var existingEmployeeId = CreateEmployee();
        Context.Employees.Single(e => e.Id == existingEmployeeId).Code = "41";
        Context.SaveChanges();

        var applicantCVId = CreateJoinableApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Joined
        });

        var employee = Context.Employees.Single(e => e.FirstNameEn == "John");
        employee.Code.ShouldNotBeNullOrWhiteSpace();
        employee.Code.ShouldBe("42");
    }

    [Fact]
    public async Task Should_Not_Create_Second_Employee_When_Joined_Is_Saved_Again()
    {
        var applicantCVId = CreateJoinableApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Joined
        });
        var statusId = Context.ApplicationStatuses.Single().Id;

        await service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            Id = statusId,
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Joined
        });

        Context.Employees.Count(e => e.FirstNameEn == "John").ShouldBe(1);
    }

    [Fact]
    public async Task Should_Reject_Joined_When_An_Active_Employee_Has_The_Same_Civil_Id()
    {
        var existingEmployeeId = CreateEmployee();
        var existing = Context.Employees.Single(e => e.Id == existingEmployeeId);
        existing.CivilID = 123456789012; // same as CreateApplicant's
        existing.Status = EmployeeStatus.Active;
        Context.SaveChanges();

        var applicantCVId = CreateJoinableApplicantCV();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Joined
        }));

        Context.Employees.Any(e => e.FirstNameEn == "John").ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reject_Joined_When_Joining_Date_Is_Not_After_Birth_Date()
    {
        var applicantCVId = CreateJoinableApplicantCV();
        var applicant = Context.ApplicantCVs.Where(c => c.Id == applicantCVId).Select(c => c.Applicant).Single();
        applicant.JoiningDate = applicant.BirthDate;
        Context.SaveChanges();

        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Joined
        }));

        Context.Employees.Any(e => e.FirstNameEn == "John").ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reject_Joined_When_Applicant_Has_No_Nationality()
    {
        var applicantCVId = CreateJoinableApplicantCV();
        var applicant = Context.ApplicantCVs.Where(c => c.Id == applicantCVId).Select(c => c.Applicant).Single();
        applicant.NationalityId = null;
        Context.SaveChanges();

        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(new CreateOrEditApplicationStatusDto
        {
            ApplicantCVId = applicantCVId,
            AppStatus = AppStatus.Joined
        }));

        Context.Employees.Any(e => e.FirstNameEn == "John").ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Return_The_Applicant_Summary_Of_An_Application()
    {
        var applicantId = CreateApplicant("Sara", "sara@example.com");
        var applicantCVId = CreateApplicantCV(applicantId);
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditApplicationStatusDto { ApplicantCVId = applicantCVId, AppStatus = AppStatus.Shortlisted });
        var id = Context.ApplicationStatuses.Single(a => a.ApplicantCVId == applicantCVId).Id;

        var summary = await service.GetApplicationStatusSummary(new Abp.Application.Services.Dto.EntityDto(id));

        summary.Id.ShouldBe(id);
        summary.AppStatus.ShouldBe(AppStatus.Shortlisted);
        summary.ApplicantId.ShouldBe(applicantId);
        summary.ApplicantFullNameEn.ShouldBe("Sara Doe");
        summary.ApplicantFullNameAr.ShouldBe("ف دو");
        summary.Email.ShouldBe("sara@example.com");
        summary.NationalityNameEn.ShouldBe("N");
        summary.CVUploadDate.ShouldBe(new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task Should_Return_A_Summary_Without_Applicant_When_No_CV_Is_Linked()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditApplicationStatusDto { AppStatus = AppStatus.Applied });
        var id = Context.ApplicationStatuses.Single().Id;

        var summary = await service.GetApplicationStatusSummary(new Abp.Application.Services.Dto.EntityDto(id));

        summary.ApplicantId.ShouldBeNull();
        summary.ApplicantFullNameEn.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Throw_When_The_Application_Does_Not_Exist()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<Abp.Domain.Entities.EntityNotFoundException>(
            () => service.GetApplicationStatusSummary(new Abp.Application.Services.Dto.EntityDto(999999)));
    }
}
