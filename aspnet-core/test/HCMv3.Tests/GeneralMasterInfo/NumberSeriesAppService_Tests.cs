using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>Legacy <c>NumberSeries.PerformValidation</c>: the employment-type uniqueness rules.</summary>
// ReSharper disable once InconsistentNaming
public class NumberSeriesAppService_Tests : AppTestBase
{
    private readonly INumberSeriesAppService _service;

    public NumberSeriesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<INumberSeriesAppService>();
    }

    private static CreateOrEditNumberSeriesDto Dto(string code, EmploymentType type, int? id = null)
    {
        return new CreateOrEditNumberSeriesDto
        {
            Id = id,
            Code = code,
            Name = code,
            Prefix = "E",
            Formatting = "0000",
            EmploymentType = type
        };
    }

    private int IdOf(string code)
    {
        return UsingDbContext(context => context.Set<NumberSeries>().Single(n => n.Code == code).Id);
    }

    [Fact]
    public async Task Should_Store_And_Return_The_Employment_Type()
    {
        await _service.CreateOrEdit(Dto("NS1", EmploymentType.Temporary));

        var item = (await _service.GetAll(new GetAllNumberSeriesInput())).Items.Single(n => n.Code == "NS1");
        item.EmploymentType.ShouldBe(EmploymentType.Temporary);

        var forEdit = await _service.GetNumberSeriesForEdit(new EntityDto(item.Id));
        forEdit.NumberSeries.EmploymentType.ShouldBe(EmploymentType.Temporary);
    }

    [Fact]
    public async Task Should_Allow_One_Series_Each_For_Permanent_And_Temporary()
    {
        await _service.CreateOrEdit(Dto("NS1", EmploymentType.Permanent));
        await _service.CreateOrEdit(Dto("NS2", EmploymentType.Temporary));

        UsingDbContext(context => context.Set<NumberSeries>().Count()).ShouldBe(2);
    }

    [Fact]
    public async Task Should_Refuse_A_Second_Series_For_The_Same_Employment_Type()
    {
        await _service.CreateOrEdit(Dto("NS1", EmploymentType.Permanent));

        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateOrEdit(Dto("NS2", EmploymentType.Permanent)));
    }

    [Fact]
    public async Task Should_Refuse_A_Per_Type_Series_Beside_An_ALL_Series()
    {
        await _service.CreateOrEdit(Dto("NS1", EmploymentType.ALL));

        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateOrEdit(Dto("NS2", EmploymentType.Temporary)));
    }

    [Fact]
    public async Task Should_Refuse_An_ALL_Series_When_Any_Other_Series_Exists()
    {
        await _service.CreateOrEdit(Dto("NS1", EmploymentType.Permanent));

        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateOrEdit(Dto("NS2", EmploymentType.ALL)));
    }

    [Fact]
    public async Task Should_Let_A_Series_Be_Saved_Again_Without_Clashing_With_Itself()
    {
        await _service.CreateOrEdit(Dto("NS1", EmploymentType.ALL));
        var id = IdOf("NS1");

        var edit = Dto("NS1", EmploymentType.ALL, id);
        edit.Prefix = "X";
        await _service.CreateOrEdit(edit);

        UsingDbContext(context => context.Set<NumberSeries>().Single(n => n.Id == id).Prefix).ShouldBe("X");
    }

    [Fact]
    public async Task Should_Refuse_Changing_A_Series_To_A_Type_Another_Series_Already_Has()
    {
        await _service.CreateOrEdit(Dto("NS1", EmploymentType.Permanent));
        await _service.CreateOrEdit(Dto("NS2", EmploymentType.Temporary));
        var id = IdOf("NS2");

        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateOrEdit(Dto("NS2", EmploymentType.Permanent, id)));
    }
}
