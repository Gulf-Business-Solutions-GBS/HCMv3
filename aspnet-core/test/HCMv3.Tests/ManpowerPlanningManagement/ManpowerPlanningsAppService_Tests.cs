using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.ManpowerPlanningManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ManpowerPlanningManagement;

// ReSharper disable once InconsistentNaming
public class ManpowerPlanningsAppService_Tests : ManpowerPlanningManagementAppServiceTestBase
{
    private HCMv3.ManpowerPlanningManagement.ManpowerPlanningsAppService CreateAppService()
    {
        return CreateService(() => new HCMv3.ManpowerPlanningManagement.ManpowerPlanningsAppService(
            FakeRepository.For<HCMv3.ManpowerPlanningManagement.ManpowerPlanning>(Context),
            FakeRepository.For<ManpowerPlanningDetail>(Context),
            FakeRepository.For<ManpowerPlanningPeriod>(Context),
            FakeRepository.For<Position>(Context)));
    }

    private int CreatePeriod(string name)
    {
        var entity = new ManpowerPlanningPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Name = name,
            Active = true,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        Context.ManpowerPlanningPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ManpowerPlanning()
    {
        var employeeId = CreateEmployee("M1");
        var orgUnitId = CreateOrganizationUnit("Dept1");
        var periodId = CreatePeriod("2026");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = periodId,
            RequestComments = "Initial plan"
        });

        var result = await service.GetAll(new GetAllManpowerPlanningsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Status.ShouldBe(ManpowerPlanningStatus.Draft);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_OrgUnit_And_Period()
    {
        var employeeId = CreateEmployee("M2");
        var orgUnitId = CreateOrganizationUnit("Dept2");
        var periodId = CreatePeriod("2026-B");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = periodId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
            {
                EmployeeId = employeeId,
                OrganizationUnitId = orgUnitId,
                PeriodId = periodId
            }));
    }

    [Fact]
    public async Task Should_Create_Position_For_Detail_Without_One_On_Approve()
    {
        var employeeId = CreateEmployee("M3");
        var orgUnitId = CreateOrganizationUnit("Dept3");
        var periodId = CreatePeriod("2026-C");
        var jobTitleId = CreateJobTitle("JTApprove");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = periodId
        });

        var manpowerPlanning = Context.ManpowerPlannings.Single(m => m.PeriodId == periodId);

        var detail = new ManpowerPlanningDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            ManpowerPlanningId = manpowerPlanning.Id,
            JobTitleId = jobTitleId,
            January = 3000
        };
        Context.ManpowerPlanningDetails.Add(detail);
        Context.SaveChanges();

        await service.Approve(new ApproveManpowerPlanningDto { Id = manpowerPlanning.Id });

        var updatedPlanning = await Context.ManpowerPlannings.FindAsync(manpowerPlanning.Id);
        updatedPlanning.Status.ShouldBe(ManpowerPlanningStatus.Approved);

        var updatedDetail = await Context.ManpowerPlanningDetails.FindAsync(detail.Id);
        updatedDetail.PositionId.ShouldNotBeNull();

        var position = await Context.Positions.FindAsync(updatedDetail.PositionId.Value);
        position.ShouldNotBeNull();
        position.JobTitleId.ShouldBe(jobTitleId);
        position.MinSalary.ShouldBe(3000);
        position.Vacant.ShouldBeTrue();
        position.FTELimit.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Approving_Already_Processed_Request()
    {
        var employeeId = CreateEmployee("M4");
        var orgUnitId = CreateOrganizationUnit("Dept4");
        var periodId = CreatePeriod("2026-D");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = periodId
        });

        var manpowerPlanning = Context.ManpowerPlannings.Single(m => m.PeriodId == periodId);

        await service.Approve(new ApproveManpowerPlanningDto { Id = manpowerPlanning.Id });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.Approve(new ApproveManpowerPlanningDto { Id = manpowerPlanning.Id }));
    }
}
