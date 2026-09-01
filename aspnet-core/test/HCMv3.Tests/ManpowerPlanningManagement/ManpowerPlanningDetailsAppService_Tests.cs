using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.ManpowerPlanningManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ManpowerPlanningManagement;

// ReSharper disable once InconsistentNaming
public class ManpowerPlanningDetailsAppService_Tests : ManpowerPlanningManagementAppServiceTestBase
{
    private ManpowerPlanningDetailsAppService CreateAppService()
    {
        return CreateService(() => new ManpowerPlanningDetailsAppService(
            FakeRepository.For<ManpowerPlanningDetail>(Context),
            FakeRepository.For<HCMv3.ManpowerPlanningManagement.ManpowerPlanning>(Context),
            FakeRepository.For<Position>(Context)));
    }

    private int CreateManpowerPlanning()
    {
        var employeeId = CreateEmployee("D" + Guid.NewGuid().ToString("N").Substring(0, 4));
        var orgUnitId = CreateOrganizationUnit("Dept-" + Guid.NewGuid().ToString("N").Substring(0, 4));

        var period = new ManpowerPlanningPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Name = "P-" + Guid.NewGuid().ToString("N").Substring(0, 4),
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        Context.ManpowerPlanningPeriods.Add(period);
        Context.SaveChanges();

        var manpowerPlanning = new HCMv3.ManpowerPlanningManagement.ManpowerPlanning
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = period.Id,

            // Status is derived from WorkflowStatus and has no setter -- null means "not routed", which
            // reads as Draft.
            WorkflowStatus = null
        };
        Context.ManpowerPlannings.Add(manpowerPlanning);
        Context.SaveChanges();

        return manpowerPlanning.Id;
    }

    [Fact]
    public async Task Should_Create_Detail_And_Recalculate_TotalAmount_And_GrandTotal()
    {
        var manpowerPlanningId = CreateManpowerPlanning();
        var jobTitleId = CreateJobTitle("JTD1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDetailDto
        {
            ManpowerPlanningId = manpowerPlanningId,
            JobTitleId = jobTitleId,
            January = 1000,
            February = 1000,
            March = 1000
        });

        var result = await service.GetAll(new GetAllManpowerPlanningDetailsInput { ManpowerPlanningId = manpowerPlanningId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TotalAmount.ShouldBe(3000);

        var manpowerPlanning = await Context.ManpowerPlannings.FindAsync(manpowerPlanningId);
        manpowerPlanning.GrandTotalAmount.ShouldBe(3000);
    }

    [Fact]
    public async Task Should_Throw_When_Amount_Outside_Linked_Position_Salary_Range()
    {
        var manpowerPlanningId = CreateManpowerPlanning();
        var jobTitleId = CreateJobTitle("JTD2");

        var position = new Position
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "POSD2",
            NameEn = "PosD2",
            NameAr = "بند2",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            MinSalary = 1000,
            MaxSalary = 2000,
            FTELimit = 1
        };
        Context.Positions.Add(position);
        Context.SaveChanges();

        var detail = new ManpowerPlanningDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            ManpowerPlanningId = manpowerPlanningId,
            JobTitleId = jobTitleId,
            PositionId = position.Id
        };
        Context.ManpowerPlanningDetails.Add(detail);
        Context.SaveChanges();

        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditManpowerPlanningDetailDto
            {
                Id = detail.Id,
                ManpowerPlanningId = manpowerPlanningId,
                JobTitleId = jobTitleId,
                January = 5000
            }));
    }
}
