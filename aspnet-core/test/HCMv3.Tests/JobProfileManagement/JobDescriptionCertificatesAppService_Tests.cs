using System.Threading.Tasks;
using Abp.UI;
using HCMv3.JobProfileManagement;
using HCMv3.JobProfileManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobDescriptionCertificatesAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobDescriptionCertificatesAppService CreateAppService()
    {
        return CreateService(() => new JobDescriptionCertificatesAppService(FakeRepository.For<JobDescriptionCertificate>(Context)));
    }

    private int CreateEducationCertificate(string nameEn = "PMP")
    {
        var entity = new EducationCertificate
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.EducationCertificates.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobDescriptionCertificate()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var certificateId = CreateEducationCertificate();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionCertificateDto
        {
            JobDescriptionId = jobDescriptionId,
            EducationCertificateId = certificateId
        });

        var result = await service.GetAll(new GetAllJobDescriptionCertificatesInput { JobDescriptionId = jobDescriptionId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EducationCertificateNameEn.ShouldBe("PMP");
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Certificate_For_Same_JobDescription()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var certificateId = CreateEducationCertificate();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionCertificateDto
        {
            JobDescriptionId = jobDescriptionId,
            EducationCertificateId = certificateId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditJobDescriptionCertificateDto
            {
                JobDescriptionId = jobDescriptionId,
                EducationCertificateId = certificateId
            }));
    }

    [Fact]
    public async Task Should_Delete_JobDescriptionCertificate()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var certificateId = CreateEducationCertificate();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionCertificateDto
        {
            JobDescriptionId = jobDescriptionId,
            EducationCertificateId = certificateId
        });

        var created = await service.GetAll(new GetAllJobDescriptionCertificatesInput { JobDescriptionId = jobDescriptionId });
        await service.Delete(new Abp.Application.Services.Dto.EntityDto(created.Items[0].Id));

        var result = await service.GetAll(new GetAllJobDescriptionCertificatesInput { JobDescriptionId = jobDescriptionId });
        result.TotalCount.ShouldBe(0);
    }
}
