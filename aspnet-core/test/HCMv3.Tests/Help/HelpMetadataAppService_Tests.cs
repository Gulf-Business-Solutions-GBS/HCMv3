using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.Help;
using HCMv3.Help.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Help;

/// <summary>
/// Help's technical tab: the table and column behind a field (docs/help/DESIGN.md §9).
///
/// <para>
/// The assertions name real tables and columns on purpose. The point of generating this from the EF model
/// is that it cannot drift from the database; a test that only checked "something came back" would pass
/// just as happily if the service described the wrong table.
/// </para>
/// </summary>
public class HelpMetadataAppService_Tests : AppTestBase
{
    private const string EmployeeEntity = "HCMv3.PersonnelAdministration.Employee";

    private readonly IHelpMetadataAppService _service;

    public HelpMetadataAppService_Tests()
    {
        _service = Resolve<IHelpMetadataAppService>();
    }

    private async Task<PropertySchemaDto> EmployeeProperty(string name)
    {
        var schema = await _service.GetEntitySchema(EmployeeEntity);
        var property = schema.Properties.SingleOrDefault(p => p.PropertyName == name);
        property.ShouldNotBeNull($"Employee.{name} was not described.");
        return property;
    }

    [Fact]
    public async Task Should_Return_The_Table_The_Entity_Is_Stored_In()
    {
        var schema = await _service.GetEntitySchema(EmployeeEntity);

        schema.EntityFullName.ShouldBe(EmployeeEntity);
        schema.TableName.ShouldBe("Employees");
    }

    [Fact]
    public async Task Should_Return_The_Column_Behind_A_Property()
    {
        var firstName = await EmployeeProperty("FirstNameEn");

        firstName.ColumnName.ShouldBe("FirstNameEn");
        firstName.ClrType.ShouldBe("string");
        firstName.StoreType.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Report_Max_Length_From_The_Entity_Annotation()
    {
        // [StringLength(128)] on Employee.FirstNameEn.
        (await EmployeeProperty("FirstNameEn")).MaxLength.ShouldBe(128);
    }

    [Fact]
    public async Task Should_Report_A_Required_Column_As_Not_Nullable()
    {
        (await EmployeeProperty("FirstNameEn")).IsNullable.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Report_An_Optional_Column_As_Nullable()
    {
        (await EmployeeProperty("SecondNameEn")).IsNullable.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Name_The_Table_A_Lookup_Points_At()
    {
        // The field a reader sees as "Nationality" is an int to the database. The technical tab is only
        // useful if it says which table that int indexes.
        var nationality = await EmployeeProperty("NationalityId");

        nationality.ForeignKeyEntity.ShouldBe("Nationality");
        nationality.ForeignKeyTable.ShouldBe("Nationalities");
        nationality.ForeignKeyColumn.ShouldBe("Id");
    }

    [Fact]
    public async Task Should_Leave_Foreign_Key_Fields_Empty_For_A_Plain_Column()
    {
        var firstName = await EmployeeProperty("FirstNameEn");

        firstName.ForeignKeyEntity.ShouldBeNull();
        firstName.ForeignKeyTable.ShouldBeNull();
        firstName.ForeignKeyColumn.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Mark_The_Primary_Key()
    {
        (await EmployeeProperty("Id")).IsPrimaryKey.ShouldBeTrue();
        (await EmployeeProperty("FirstNameEn")).IsPrimaryKey.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Flag_An_Enum_Column_And_Name_Its_Type()
    {
        var gender = await EmployeeProperty("Gender");

        gender.IsEnum.ShouldBeTrue();
        gender.ClrType.ShouldBe("Gender");
    }

    [Fact]
    public async Task Should_Write_Nullable_Types_The_Way_A_Developer_Would()
    {
        // TitleId is int? on the entity; "Nullable`1" would be accurate and useless.
        (await EmployeeProperty("TitleId")).ClrType.ShouldBe("int?");
    }

    [Fact]
    public async Task Should_Refuse_An_Entity_Outside_The_Application()
    {
        // Mapped in the same EF model, but ABP infrastructure -- not something help describes.
        await Should.ThrowAsync<UserFriendlyException>(() => _service.GetEntitySchema("Abp.Auditing.AuditLog"));
    }

    [Fact]
    public async Task Should_Refuse_An_Unknown_Entity()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => _service.GetEntitySchema("HCMv3.NoSuchEntity"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Refuse_A_Blank_Name(string entityFullName)
    {
        await Should.ThrowAsync<UserFriendlyException>(() => _service.GetEntitySchema(entityFullName));
    }

    [Fact]
    public async Task A_User_Without_The_Permission_Should_Be_Refused()
    {
        // Every other test runs as the tenant admin, who holds every permission -- so without this one,
        // nothing would notice if the gate were removed and schema were served to anyone signed in.
        var userId = 0L;
        UsingDbContext(context =>
        {
            var user = new User
            {
                TenantId = AbpSession.TenantId,
                UserName = "nohelp" + Guid.NewGuid().ToString("N")[..8],
                Name = "No",
                Surname = "Permissions",
                EmailAddress = "nohelp" + Guid.NewGuid().ToString("N")[..8] + "@testdomain.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw==",
            };
            user.SetNormalizedNames();
            context.Users.Add(user);
            context.SaveChanges();
            userId = user.Id;
        });

        AbpSession.UserId = userId;

        await Should.ThrowAsync<AbpAuthorizationException>(() => _service.GetEntitySchema(EmployeeEntity));
    }

    [Fact]
    public void The_Service_Should_Be_Gated_On_Its_Own_Permission()
    {
        var authorize = typeof(HelpMetadataAppService).GetCustomAttribute<AbpAuthorizeAttribute>();

        authorize.ShouldNotBeNull("Schema would be served to anyone signed in.");
        authorize.Permissions.ShouldBe(new[] { AppPermissions.Pages_Administration_HelpTechnicalInfo });
    }
}
