using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SuccessionManagement;
using HCMv3.SuccessionManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SuccessionManagement;

// ReSharper disable once InconsistentNaming
public class SuccessorNomineesAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private SuccessorNomineesAppService CreateAppService()
    {
        return CreateService(() => new SuccessorNomineesAppService(FakeRepository.For<SuccessorNominee>(Context)));
    }

    private int CreateReadiness()
    {
        var entity = new Readiness { TenantId = AbpSession.TenantId ?? 1, NameEn = "Ready Now", NameAr = "جاهز الآن" };
        Context.Readinesses.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateRanking()
    {
        var entity = new Ranking { TenantId = AbpSession.TenantId ?? 1, NameEn = "1st Choice", NameAr = "الخيار الأول" };
        Context.Rankings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SuccessorNominee()
    {
        var employeeId = CreateEmployee("E1");
        var nomineeId = CreateEmployee("N1");
        var readinessId = CreateReadiness();
        var rankingId = CreateRanking();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSuccessorNomineeDto
        {
            EmployeeId = employeeId,
            NomineeId = nomineeId,
            ReadinessId = readinessId,
            RankingId = rankingId,
            Comments = "Strong candidate"
        });

        var result = await service.GetAll(new GetAllSuccessorNomineesInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items.Single().NomineeId.ShouldBe(nomineeId);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Employee_Nominee_Pair()
    {
        var employeeId = CreateEmployee("E2");
        var nomineeId = CreateEmployee("N2");
        var readinessId = CreateReadiness();
        var rankingId = CreateRanking();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSuccessorNomineeDto
        {
            EmployeeId = employeeId,
            NomineeId = nomineeId,
            ReadinessId = readinessId,
            RankingId = rankingId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSuccessorNomineeDto
            {
                EmployeeId = employeeId,
                NomineeId = nomineeId,
                ReadinessId = readinessId,
                RankingId = rankingId
            }));
    }

    [Fact]
    public async Task Should_Delete_SuccessorNominee()
    {
        var employeeId = CreateEmployee("E3");
        var nomineeId = CreateEmployee("N3");
        var readinessId = CreateReadiness();
        var rankingId = CreateRanking();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSuccessorNomineeDto
        {
            EmployeeId = employeeId,
            NomineeId = nomineeId,
            ReadinessId = readinessId,
            RankingId = rankingId
        });
        var all = await service.GetAll(new GetAllSuccessorNomineesInput { EmployeeId = employeeId });
        var id = all.Items.Single().Id;

        await service.Delete(new EntityDto(id));

        (await service.GetAll(new GetAllSuccessorNomineesInput { EmployeeId = employeeId })).TotalCount.ShouldBe(0);
    }
}
