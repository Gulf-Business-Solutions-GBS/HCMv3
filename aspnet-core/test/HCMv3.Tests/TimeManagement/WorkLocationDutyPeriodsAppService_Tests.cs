using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.OrganizationManagement;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class WorkLocationDutyPeriodsAppService_Tests : TimeManagementAppServiceTestBase
{
    private WorkLocationDutyPeriodsAppService CreateAppService()
    {
        return CreateService(() => new WorkLocationDutyPeriodsAppService(FakeRepository.For<WorkLocationDutyPeriod>(Context)));
    }

    private int CreateWorkLocation(string code)
    {
        var entity = new WorkLocation
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.WorkLocations.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateDutyPeriod(string code)
    {
        var entity = new DutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(9999, 12, 31)
        };
        Context.DutyPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_WorkLocationDutyPeriod()
    {
        var workLocationId = CreateWorkLocation("WL1");
        var dutyPeriodId = CreateDutyPeriod("DP1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkLocationDutyPeriodDto
        {
            WorkLocationId = workLocationId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllWorkLocationDutyPeriodsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].WorkLocationId.ShouldBe(workLocationId);
        result.Items[0].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_StartDate_For_Same_WorkLocation()
    {
        var workLocationId = CreateWorkLocation("WL2");
        var dutyPeriodId = CreateDutyPeriod("DP2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkLocationDutyPeriodDto
        {
            WorkLocationId = workLocationId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2026, 1, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditWorkLocationDutyPeriodDto
            {
                WorkLocationId = workLocationId,
                DutyPeriodId = dutyPeriodId,
                StartDate = new DateTime(2026, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Auto_Chain_EndDate_When_New_Record_Starts()
    {
        var workLocationId = CreateWorkLocation("WL3");
        var dutyPeriodId = CreateDutyPeriod("DP3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkLocationDutyPeriodDto
        {
            WorkLocationId = workLocationId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2020, 1, 1)
        });

        await service.CreateOrEdit(new CreateOrEditWorkLocationDutyPeriodDto
        {
            WorkLocationId = workLocationId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2021, 1, 1)
        });

        var records = Context.WorkLocationDutyPeriods
            .Where(w => w.WorkLocationId == workLocationId)
            .OrderBy(w => w.StartDate)
            .ToList();

        records.Count.ShouldBe(2);
        records[0].EndDate.ShouldBe(new DateTime(2020, 12, 31));
        records[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Restore_Infinite_EndDate_On_Delete_Of_Latest_Record()
    {
        var workLocationId = CreateWorkLocation("WL4");
        var dutyPeriodId = CreateDutyPeriod("DP4");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWorkLocationDutyPeriodDto
        {
            WorkLocationId = workLocationId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2020, 1, 1)
        });

        await service.CreateOrEdit(new CreateOrEditWorkLocationDutyPeriodDto
        {
            WorkLocationId = workLocationId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2021, 1, 1)
        });

        var secondRecordId = Context.WorkLocationDutyPeriods
            .Single(w => w.WorkLocationId == workLocationId && w.StartDate == new DateTime(2021, 1, 1)).Id;

        await service.Delete(new EntityDto(secondRecordId));

        var remaining = Context.WorkLocationDutyPeriods.Single(w => w.WorkLocationId == workLocationId);
        remaining.EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }
}
