using System.Threading.Tasks;
using Abp.UI;
using HCMv3.JobProfileManagement;
using HCMv3.JobProfileManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobDescriptionDegreesAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobDescriptionDegreesAppService CreateAppService()
    {
        return CreateService(() => new JobDescriptionDegreesAppService(FakeRepository.For<JobDescriptionDegree>(Context)));
    }

    private int CreateEducationDegree(string nameEn = "Bachelor's")
    {
        var entity = new EducationDegree
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.EducationDegrees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobDescriptionDegree()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var degreeId = CreateEducationDegree();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionDegreeDto
        {
            JobDescriptionId = jobDescriptionId,
            EducationDegreeId = degreeId
        });

        var result = await service.GetAll(new GetAllJobDescriptionDegreesInput { JobDescriptionId = jobDescriptionId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EducationDegreeNameEn.ShouldBe("Bachelor's");
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Degree_For_Same_JobDescription()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var degreeId = CreateEducationDegree();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionDegreeDto
        {
            JobDescriptionId = jobDescriptionId,
            EducationDegreeId = degreeId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditJobDescriptionDegreeDto
            {
                JobDescriptionId = jobDescriptionId,
                EducationDegreeId = degreeId
            }));
    }
}
