using System.Threading.Tasks;
using Abp.UI;
using HCMv3.JobProfileManagement;
using HCMv3.JobProfileManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobDescriptionAcknowledgementsAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobDescriptionAcknowledgementsAppService CreateAppService()
    {
        return CreateService(() => new JobDescriptionAcknowledgementsAppService(FakeRepository.For<JobDescriptionAcknowledgement>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobDescriptionAcknowledgement()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var jobTitleId = CreateJobTitle();
        var jobDescriptionId = CreateJobDescription();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionAcknowledgementDto
        {
            EmployeeId = employeeId,
            JobTitleId = jobTitleId,
            JobDescriptionId = jobDescriptionId,
            Version = "1.0",
            Acknowledged = true
        });

        var result = await service.GetAll(new GetAllJobDescriptionAcknowledgementsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Acknowledged.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Employee_JobTitle_JobDescription_Combination()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var jobTitleId = CreateJobTitle();
        var jobDescriptionId = CreateJobDescription();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionAcknowledgementDto
        {
            EmployeeId = employeeId,
            JobTitleId = jobTitleId,
            JobDescriptionId = jobDescriptionId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditJobDescriptionAcknowledgementDto
            {
                EmployeeId = employeeId,
                JobTitleId = jobTitleId,
                JobDescriptionId = jobDescriptionId
            }));
    }
}
