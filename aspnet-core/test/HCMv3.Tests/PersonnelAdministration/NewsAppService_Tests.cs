using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class NewsAppService_Tests : NewsAppServiceTestBase
{
    private NewsAppService CreateAppService()
    {
        return CreateService(() => new NewsAppService(NewsFakeRepository.For<News>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_News()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNewsDto
        {
            Title = "Holiday Announcement",
            TitleAr = "إعلان العطلة",
            IssueDate = new DateTime(2026, 1, 1),
            ExpiryDate = new DateTime(2026, 12, 31),
            SendNotification = true,
            SendEmail = false
        });

        var result = await service.GetAll(new GetAllNewsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Holiday Announcement");
        result.Items[0].SendNotification.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_IssueDate_After_ExpiryDate()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditNewsDto
            {
                Title = "Bad Dates",
                IssueDate = new DateTime(2026, 12, 31),
                ExpiryDate = new DateTime(2026, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Update_News()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNewsDto
        {
            Title = "Original Title",
            IssueDate = new DateTime(2026, 1, 1),
            ExpiryDate = new DateTime(2026, 12, 31)
        });

        var created = await service.GetAll(new GetAllNewsInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditNewsDto
        {
            Id = id,
            Title = "Updated Title",
            IssueDate = new DateTime(2026, 1, 1),
            ExpiryDate = new DateTime(2026, 12, 31)
        });

        var forEdit = await service.GetNewsForEdit(new EntityDto(id));
        forEdit.News.Title.ShouldBe("Updated Title");
    }

    [Fact]
    public async Task Should_Delete_News()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNewsDto
        {
            Title = "To Delete",
            IssueDate = new DateTime(2026, 1, 1),
            ExpiryDate = new DateTime(2026, 12, 31)
        });

        var created = await service.GetAll(new GetAllNewsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllNewsInput());
        result.TotalCount.ShouldBe(0);
    }
}
