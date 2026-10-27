using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class FormTypesAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private FormTypesAppService CreateAppService()
    {
        return CreateService(() => new FormTypesAppService(FakeRepository.For<FormType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_FormType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditFormTypeDto
        {
            NameEn = "Annual",
            NameAr = "سنوي",
            MinReviewerLimit = 1,
            MaxReviewerLimit = 3
        });

        var result = await service.GetAll(new GetAllFormTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Annual");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Update_FormType()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditFormTypeDto { NameEn = "A", NameAr = "A" });

        var all = await service.GetAll(new GetAllFormTypesInput());
        var id = all.Items[0].Id;

        var forEdit = await service.GetFormTypeForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        forEdit.FormType.NameEn = "B";
        await service.CreateOrEdit(forEdit.FormType);

        var updated = await service.GetFormTypeForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        updated.FormType.NameEn.ShouldBe("B");
    }

    [Fact]
    public async Task Should_Delete_FormType()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditFormTypeDto { NameEn = "A", NameAr = "A" });

        var all = await service.GetAll(new GetAllFormTypesInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllFormTypesInput());
        afterDelete.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditFormTypeDto { NameEn = "Annual", NameAr = "A1" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditFormTypeDto { NameEn = "Annual", NameAr = "A2" }));
    }
}
