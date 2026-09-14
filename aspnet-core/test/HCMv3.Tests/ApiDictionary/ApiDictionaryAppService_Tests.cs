using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using HCMv3.ApiDictionary;
using HCMv3.ApiDictionary.Dto;
using HCMv3.Authorization.Users;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ApiDictionary;

/// <summary>
/// The API dictionary for Employee.
///
/// <para>
/// The operation expectations are not derived from the builder -- they are what the running web host's Swagger
/// document reported for the Employees service when this was written. The builder reimplements ABP's routing
/// rules instead of asking MVC, so this is the check that the reimplementation still agrees with the server a
/// third party will actually call.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class ApiDictionaryAppService_Tests : AppTestBase
{
    private const string Base = "https://servername/api/services/app/Employees/";

    private readonly IApiDictionaryAppService _service;

    public ApiDictionaryAppService_Tests()
    {
        _service = Resolve<IApiDictionaryAppService>();
    }

    // ---------------------------------------------------------------- the list

    [Fact]
    public async Task Should_List_Employee()
    {
        var entities = await _service.GetEntities();

        entities.Items.ShouldContain(e => e.Name == "Employee");
    }

    [Fact]
    public async Task Should_Refuse_An_Unknown_Record_Type()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => _service.GetEntity("NoSuchRecord"));
    }

    [Fact]
    public async Task Should_Find_The_Record_Type_Whatever_The_Case()
    {
        (await _service.GetEntity("employee")).Name.ShouldBe("Employee");
    }

    // ---------------------------------------------------------------- operations: pinned to the live host

    [Theory]
    [InlineData("Query", "POST", Base + "GetAll")]
    [InlineData("Read", "GET", Base + "GetEmployeeForEdit?Id={id}")]
    [InlineData("Upsert", "POST", Base + "CreateOrEdit")]
    [InlineData("Delete", "DELETE", Base + "Delete?Id={id}")]
    public async Task Should_Describe_Each_Operation_As_The_Server_Serves_It(string operation, string verb, string url)
    {
        var op = await OperationAsync(operation);

        op.HttpMethod.ShouldBe(verb);
        op.Url.ShouldBe(url);
    }

    [Fact]
    public async Task Should_Never_Name_The_Real_Server()
    {
        var entity = await _service.GetEntity("Employee");

        entity.BaseUrl.ShouldBe("https://servername");
        entity.Operations.ShouldAllBe(o => o.Url.StartsWith("https://servername/"));
    }

    [Fact]
    public async Task Should_List_The_Query_Filters_As_The_Request_Body()
    {
        var query = await OperationAsync("Query");

        query.RequestFields.ShouldContain("filter");
        query.RequestFields.ShouldContain("skipCount");
        query.RequestFields.ShouldContain("maxResultCount");
        query.Response.ShouldBe("{ totalCount, items: [ Employee ] }");
    }

    [Fact]
    public async Task Should_Name_The_Permissions_Each_Operation_Needs()
    {
        (await OperationAsync("Query")).Permissions.ShouldContain("Pages.PersonnelAdministration.Employees");
        (await OperationAsync("Delete")).Permissions.ShouldContain("Pages.PersonnelAdministration.Employees.Delete");

        // The upsert checks a different permission for create and for update, inside the service.
        var upsert = await OperationAsync("Upsert");
        upsert.Permissions.ShouldContain("Create: Pages.PersonnelAdministration.Employees.Create");
        upsert.Permissions.ShouldContain("Update: Pages.PersonnelAdministration.Employees.Edit");
    }

    // ---------------------------------------------------------------- fields

    [Fact]
    public async Task Should_Use_The_Names_That_Appear_On_The_Wire()
    {
        // Checked against the live Swagger schema: acronyms are lower-cased at the start, kept inside.
        (await FieldAsync("paciNumber")).ShouldNotBeNull();
        (await FieldAsync("civilID")).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("firstNameEn")]
    [InlineData("lastNameEn")]
    [InlineData("firstNameAr")]
    [InlineData("lastNameAr")]
    [InlineData("birthDate")]
    [InlineData("nationalityId")]
    [InlineData("joiningDate")]
    public async Task Should_Mark_Required_Fields(string name)
    {
        (await FieldAsync(name)).IsRequired.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Mark_An_Optional_Field_Required()
    {
        (await FieldAsync("code")).IsRequired.ShouldBeFalse();
    }

    [Theory]
    [InlineData("firstNameEn", 128)]
    [InlineData("email", 256)]
    [InlineData("code", 64)]
    [InlineData("currentAddress", 1000)]
    public async Task Should_Give_The_Maximum_Length(string name, int maxLength)
    {
        (await FieldAsync(name)).MaxLength.ShouldBe(maxLength);
    }

    [Fact]
    public async Task Should_Mark_The_Id_As_The_Key_And_Not_Settable()
    {
        var id = await FieldAsync("id");

        id.IsKey.ShouldBeTrue();
        id.IsCreatable.ShouldBeFalse();
        id.IsUpdatable.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Show_Computed_Fields_As_Returned_But_Not_Settable()
    {
        foreach (var name in new[] { "fullNameEn", "probationEndDate", "nationalityName" })
        {
            var field = await FieldAsync(name);

            field.InQueryResults.ShouldBeTrue();
            field.IsCreatable.ShouldBeFalse();
            field.IsUpdatable.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Should_Say_Status_Cannot_Be_Set_On_Create()
    {
        // EmployeesAppService.CreateAsync overwrites it with Active.
        var status = await FieldAsync("status");

        status.IsCreatable.ShouldBeFalse();
        status.IsUpdatable.ShouldBeTrue();
        status.Note.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_List_Enum_Values()
    {
        var status = await FieldAsync("status");

        status.Type.ShouldBe("enum");
        status.EnumValues.ShouldContain(v => v.StartsWith("Active = "));
    }

    [Theory]
    [InlineData("nationalityId", "Nationality")]
    [InlineData("religionId", "Religion")]
    [InlineData("birthPlaceId", "City")]
    public async Task Should_Say_Which_Record_A_Foreign_Key_Links_To(string name, string target)
    {
        // BirthPlaceId is the proof it reads the entity: nothing in the name says City.
        (await FieldAsync(name)).LinksTo.ShouldBe(target);
    }

    [Fact]
    public async Task Should_Not_Invent_A_Link_For_A_Field_That_Is_Not_A_Foreign_Key()
    {
        (await FieldAsync("firstNameEn")).LinksTo.ShouldBeNull();
    }

    [Theory]
    [InlineData("civilID", ApiDictionarySensitiveFields.Identity)]
    [InlineData("birthDate", ApiDictionarySensitiveFields.Identity)]
    [InlineData("mobile", ApiDictionarySensitiveFields.Contact)]
    [InlineData("currentAddress", ApiDictionarySensitiveFields.Address)]
    [InlineData("criticalIllness", ApiDictionarySensitiveFields.Health)]
    [InlineData("religionId", ApiDictionarySensitiveFields.Religion)]
    [InlineData("photo", ApiDictionarySensitiveFields.Photo)]
    public async Task Should_Flag_Sensitive_Personal_Data(string name, string category)
    {
        (await FieldAsync(name)).SensitiveCategory.ShouldBe(category);
    }

    [Fact]
    public async Task Should_Not_Flag_An_Ordinary_Field()
    {
        (await FieldAsync("firstNameEn")).SensitiveCategory.ShouldBeNull();
        (await FieldAsync("joiningDate")).SensitiveCategory.ShouldBeNull();
    }

    [Fact]
    public void Should_Not_Flag_A_Field_That_Only_Contains_A_Sensitive_Word()
    {
        // A payroll allowance is not a phone number.
        ApiDictionarySensitiveFields.Categorize("MobileAllowance").ShouldBeNull();
    }

    [Fact]
    public async Task Should_Label_Every_Field()
    {
        var entity = await _service.GetEntity("Employee");

        entity.Fields.ShouldAllBe(f => !string.IsNullOrWhiteSpace(f.Label));
    }

    [Fact]
    public async Task Should_List_Each_Field_Once()
    {
        var entity = await _service.GetEntity("Employee");

        entity.Fields.Select(f => f.PropertyName).ShouldBeUnique();
    }

    // ---------------------------------------------------------------- builder rules

    [Theory]
    [InlineData("FirstNameEn", "First Name En")]
    [InlineData("PACINumber", "PACI Number")]
    [InlineData("CivilID", "Civil ID")]
    public void Should_Space_Out_A_Name_With_No_Translation(string name, string expected)
    {
        ApiDictionaryBuilder.Humanize(name).ShouldBe(expected);
    }

    [Fact]
    public void Should_Strip_The_AppService_Suffix_For_The_Route()
    {
        ApiDictionaryBuilder.ServiceNameOf(typeof(HCMv3.PersonnelAdministration.EmployeesAppService)).ShouldBe("Employees");
    }

    [Fact]
    public void Should_Keep_Every_Catalog_Method_Pointing_At_A_Real_Method()
    {
        // A renamed service method would otherwise break the dictionary only when somebody opens that entity.
        var builder = Resolve<ApiDictionaryBuilder>();

        foreach (var entry in ApiDictionaryCatalog.Entries)
        {
            Should.NotThrow(() => builder.Build(entry));
        }
    }

    // ---------------------------------------------------------------- access

    [Fact]
    public async Task Should_Refuse_A_User_Without_The_Permission()
    {
        var userName = "apidict" + Guid.NewGuid().ToString("N").Substring(0, 8);

        var user = new User
        {
            TenantId = AbpSession.TenantId,
            UserName = userName,
            Name = "No",
            Surname = "Permissions",
            EmailAddress = userName + "@gbskw.com",
            IsEmailConfirmed = true,
            Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
        };

        // The users table requires the normalized names UserManager would normally fill in.
        user.SetNormalizedNames();

        UsingDbContext(context => context.Users.Add(user));

        LoginAsTenant(Abp.MultiTenancy.AbpTenantBase.DefaultTenantName, userName);

        await Should.ThrowAsync<AbpAuthorizationException>(() => _service.GetEntity("Employee"));
    }

    // ---------------------------------------------------------------- helpers

    private async Task<ApiDictionaryOperationDto> OperationAsync(string operation)
    {
        var entity = await _service.GetEntity("Employee");
        return entity.Operations.Single(o => o.Operation == operation);
    }

    private async Task<ApiDictionaryFieldDto> FieldAsync(string propertyName)
    {
        var entity = await _service.GetEntity("Employee");
        return entity.Fields.Single(f => f.PropertyName == propertyName);
    }
}
