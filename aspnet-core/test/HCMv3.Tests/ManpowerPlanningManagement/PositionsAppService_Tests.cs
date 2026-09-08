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
public class PositionsAppService_Tests : ManpowerPlanningManagementAppServiceTestBase
{
    private PositionsAppService CreateAppService()
    {
        return CreateService(() => new PositionsAppService(FakeRepository.For<Position>(Context)));
    }

    [Fact]
    public async Task Should_Create_Position_And_Auto_Assign_Code()
    {
        var jobTitleId = CreateJobTitle("JT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPositionDto
        {
            NameEn = "Accountant",
            NameAr = "محاسب",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            JobTitleId = jobTitleId,
            MinSalary = 1000,
            MidSalary = 1500,
            MaxSalary = 2000,
            FTELimit = 1
        });

        var result = await service.GetAll(new GetAllPositionsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
        result.Items[0].NameEn.ShouldBe("Accountant");
    }

    [Fact]
    public async Task Should_Throw_When_FTELimit_Not_Greater_Than_Zero()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPositionDto
            {
                NameEn = "Bad Position",
                NameAr = "بند سيء",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                FTELimit = 0
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPositionDto
        {
            Code = "P1",
            NameEn = "First",
            NameAr = "الأول",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            FTELimit = 1
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPositionDto
            {
                Code = "P1",
                NameEn = "Second",
                NameAr = "الثاني",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                FTELimit = 1
            }));
    }

    [Fact]
    public async Task Should_Reject_A_Parent_Position_That_Would_Create_A_Loop()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(NewPosition("Top"));
        var top = (await service.GetAll(new GetAllPositionsInput { Filter = "Top" })).Items.Single();

        var child = NewPosition("Child");
        child.ParentPositionId = top.Id;
        await service.CreateOrEdit(child);
        var childSaved = (await service.GetAll(new GetAllPositionsInput { Filter = "Child" })).Items.Single();

        // Putting the top under its own descendant closes the loop, which would hang every traversal
        // that walks this chain.
        var edit = (await service.GetPositionForEdit(new Abp.Application.Services.Dto.EntityDto(top.Id))).Position;
        edit.ParentPositionId = childSaved.Id;

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(edit));
    }

    [Fact]
    public async Task Should_Reject_A_Position_Reporting_To_Itself()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(NewPosition("Solo"));
        var saved = (await service.GetAll(new GetAllPositionsInput { Filter = "Solo" })).Items.Single();

        var edit = (await service.GetPositionForEdit(new Abp.Application.Services.Dto.EntityDto(saved.Id))).Position;
        edit.ParentPositionId = saved.Id;

        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateOrEdit(edit));
    }

    private static CreateOrEditPositionDto NewPosition(string nameEn)
    {
        return new CreateOrEditPositionDto
        {
            NameEn = nameEn,
            NameAr = nameEn,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            FTELimit = 1
        };
    }
}
