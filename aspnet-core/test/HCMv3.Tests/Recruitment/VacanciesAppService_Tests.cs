using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.OrganizationManagement;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class VacanciesAppService_Tests : RecruitmentAppServiceTestBase
{
    private VacanciesAppService CreateAppService()
    {
        return CreateService(() => new VacanciesAppService(
            FakeRepository.For<Vacancy>(Context),
            FakeRepository.For<ApplicationStatus>(Context)));
    }

    private int CreateJobTitle()
    {
        var entity = new JobTitle
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Engineer",
            NameAr = "مهندس"
        };
        Context.JobTitles.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreatePosition()
    {
        var entity = new Position
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "P-01",
            NameEn = "Site Engineer",
            NameAr = "مهندس موقع",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            Active = true
        };
        Context.Positions.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private CreateOrEditVacancyDto ValidDto(int jobTitleId)
    {
        return new CreateOrEditVacancyDto
        {
            JobTitleId = jobTitleId,
            PublishOn = new DateTime(2026, 1, 1),
            ClosingDate = new DateTime(2026, 2, 1),
            VacancyTitle = "Senior Engineer",
            OrganizationUnitId = 1
        };
    }

    private async Task<int> CreateVacancyAsync(VacanciesAppService service, DateTime publishOn)
    {
        var dto = ValidDto(CreateJobTitle());
        dto.PublishOn = publishOn;
        await service.CreateOrEdit(dto);
        return Context.Vacancies.OrderByDescending(v => v.Id).First().Id;
    }

    /// <summary>An application on the vacancy whose applicant joins (or would join) on <paramref name="joiningDate"/>.</summary>
    private void AddApplication(int vacancyId, AppStatus status, DateTime joiningDate)
    {
        var applicantId = CreateApplicant();
        var applicant = Context.Applicants.Single(a => a.Id == applicantId);
        applicant.JoiningDate = joiningDate;
        Context.SaveChanges();

        Context.ApplicationStatuses.Add(new ApplicationStatus
        {
            TenantId = AbpSession.TenantId ?? 1,
            VacancyId = vacancyId,
            ApplicantCVId = CreateApplicantCV(applicantId),
            AppStatus = status
        });
        Context.SaveChanges();
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Vacancy()
    {
        var jobTitleId = CreateJobTitle();
        var service = CreateAppService();

        await service.CreateOrEdit(ValidDto(jobTitleId));

        var result = await service.GetAll(new GetAllVacanciesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].VacancyTitle.ShouldBe("Senior Engineer");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
        result.Items[0].CodeWithTitle.ShouldContain("Senior Engineer");
    }

    [Fact]
    public async Task Should_Throw_When_Code_Duplicate()
    {
        var jobTitleId = CreateJobTitle();
        var service = CreateAppService();

        var dto1 = ValidDto(jobTitleId);
        dto1.Code = "V-001";
        await service.CreateOrEdit(dto1);

        var dto2 = ValidDto(jobTitleId);
        dto2.Code = "V-001";
        dto2.VacancyTitle = "Another Title";

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(dto2));
    }

    [Fact]
    public async Task Should_Store_Position_And_Recruiter_And_Show_Them_In_The_List()
    {
        var positionId = CreatePosition();
        var recruiterId = CreateEmployee();
        var recruiter = Context.Employees.Single(e => e.Id == recruiterId);
        recruiter.Code = "E-7";
        recruiter.FullNameEn = "Recruiter One";
        Context.SaveChanges();

        var service = CreateAppService();
        var dto = ValidDto(CreateJobTitle());
        dto.PositionId = positionId;
        dto.RecruiterId = recruiterId;

        await service.CreateOrEdit(dto);

        var item = (await service.GetAll(new GetAllVacanciesInput())).Items.Single();
        item.PositionId.ShouldBe(positionId);
        item.PositionCode.ShouldBe("P-01");
        item.PositionNameEn.ShouldBe("Site Engineer");
        item.RecruiterId.ShouldBe(recruiterId);
        item.RecruiterCode.ShouldBe("E-7");
        item.RecruiterFullNameEn.ShouldBe("Recruiter One");
    }

    [Fact]
    public async Task Should_Return_Position_In_GetForEdit()
    {
        var positionId = CreatePosition();
        var service = CreateAppService();
        var dto = ValidDto(CreateJobTitle());
        dto.PositionId = positionId;
        await service.CreateOrEdit(dto);
        var id = Context.Vacancies.Single().Id;

        var result = await service.GetVacancyForEdit(new EntityDto(id));

        result.Vacancy.PositionId.ShouldBe(positionId);
    }

    [Fact]
    public async Task Should_Compute_ActualDaysToFill_From_The_Latest_Joined_Applicant()
    {
        var service = CreateAppService();
        var vacancyId = await CreateVacancyAsync(service, new DateTime(2026, 1, 1, 15, 30, 0));

        AddApplication(vacancyId, AppStatus.Joined, new DateTime(2026, 1, 21));
        AddApplication(vacancyId, AppStatus.Joined, new DateTime(2026, 2, 10, 8, 0, 0));

        var item = (await service.GetAll(new GetAllVacanciesInput())).Items.Single();

        // Legacy: (latest joining date).Date - PublishOn.Date, so 1 Jan to 10 Feb is 40 days whatever the time of day.
        item.ActualDaysToFill.ShouldBe(40);
    }

    [Fact]
    public async Task Should_Ignore_Applications_That_Are_Not_Joined_When_Computing_ActualDaysToFill()
    {
        var service = CreateAppService();
        var vacancyId = await CreateVacancyAsync(service, new DateTime(2026, 1, 1));

        AddApplication(vacancyId, AppStatus.Joined, new DateTime(2026, 1, 11));
        AddApplication(vacancyId, AppStatus.Hired, new DateTime(2026, 3, 1));

        var item = (await service.GetAll(new GetAllVacanciesInput())).Items.Single();

        item.ActualDaysToFill.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Return_Zero_ActualDaysToFill_When_Nobody_Has_Joined()
    {
        var service = CreateAppService();
        var vacancyId = await CreateVacancyAsync(service, new DateTime(2026, 1, 1));
        AddApplication(vacancyId, AppStatus.Applied, new DateTime(2026, 1, 11));

        var item = (await service.GetAll(new GetAllVacanciesInput())).Items.Single();

        item.ActualDaysToFill.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Zero_ActualDaysToFill_When_The_Vacancy_Has_No_Publish_Date()
    {
        var service = CreateAppService();
        var vacancyId = await CreateVacancyAsync(service, default);
        AddApplication(vacancyId, AppStatus.Joined, new DateTime(2026, 1, 11));

        var item = (await service.GetAll(new GetAllVacanciesInput())).Items.Single();

        item.ActualDaysToFill.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Allow_A_Negative_ActualDaysToFill_When_Joining_Precedes_Publishing()
    {
        // Pinned legacy behaviour: there is no guard, the subtraction is returned as it is.
        var service = CreateAppService();
        var vacancyId = await CreateVacancyAsync(service, new DateTime(2026, 1, 11));
        AddApplication(vacancyId, AppStatus.Joined, new DateTime(2026, 1, 1));

        var item = (await service.GetAll(new GetAllVacanciesInput())).Items.Single();

        item.ActualDaysToFill.ShouldBe(-10);
    }
}
