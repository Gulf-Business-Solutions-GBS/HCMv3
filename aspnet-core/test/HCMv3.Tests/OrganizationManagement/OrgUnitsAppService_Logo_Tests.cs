using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Dto;
using HCMv3.Storage;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace HCMv3.Tests.OrganizationManagement;

/// <summary>
/// The organization unit logo (legacy <c>OrganizationUnit.Logo</c>): kept on disk through
/// <see cref="IEntityImageStore"/> with only its path on <see cref="OrgUnitProfile.LogoPath"/>, never in the
/// database and never on Zero's own <c>OrganizationUnit</c>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class OrgUnitsAppService_Logo_Tests : AppTestBase
{
    private readonly IOrgUnitsAppService _appService;
    private readonly IEntityImageStore _images;

    public OrgUnitsAppService_Logo_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IOrgUnitsAppService>();
        _images = Resolve<IEntityImageStore>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static byte[] Png(int width = 2, int height = 2, SKColor? color = null)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color ?? SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private int CreateUnitType()
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var type = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = TenantId,
                Code = OrganizationUnitType.Level1Code,
                NameEn = "Company",
                NameAr = "الشركة"
            }).Entity;
            context.SaveChanges();
            id = type.Id;
        });

        return id;
    }

    private async Task<long> CreateUnitAsync(string name, byte[] logo = null)
    {
        await _appService.CreateOrEdit(new CreateOrEditOrgUnitDto
        {
            NameEn = name,
            NameAr = name + " Ar",
            OrganizationUnitTypeId = CreateUnitType(),
            Logo = logo
        });

        long id = 0;
        UsingDbContext(context => id = context.OrganizationUnits.First(u => u.DisplayName == name).Id);
        return id;
    }

    private OrgUnitProfile LoadProfile(long organizationUnitId)
    {
        OrgUnitProfile profile = null;
        UsingDbContext(context =>
            profile = context.Set<OrgUnitProfile>().First(p => p.OrganizationUnitId == organizationUnitId));
        return profile;
    }

    [Fact]
    public async Task Should_Store_A_New_Units_Logo_On_Disk_And_Return_It_For_Edit()
    {
        var logo = Png();

        var id = await CreateUnitAsync("LOGO-CREATE", logo);

        var profile = LoadProfile(id);
        profile.LogoPath.ShouldStartWith($"tenant-{TenantId}/org-unit-logos/{profile.Id}-");
        (await _images.ReadOrNullAsync(TenantId, profile.LogoPath)).ShouldBe(logo);

        var edit = await _appService.GetOrgUnitForEdit(new EntityDto<long>(id));
        edit.OrgUnit.Logo.ShouldBe(logo);
    }

    [Fact]
    public async Task Should_Create_A_Unit_Without_A_Logo()
    {
        var id = await CreateUnitAsync("LOGO-NONE");

        LoadProfile(id).LogoPath.ShouldBeNull();
        (await _appService.GetOrgUnitForEdit(new EntityDto<long>(id))).OrgUnit.Logo.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Keep_The_Logo_When_An_Update_Sends_None()
    {
        // Callers that know nothing about logos (Go-Live setup, the AI assistant) send Logo = null.
        var id = await CreateUnitAsync("LOGO-KEEP", Png());
        var path = LoadProfile(id).LogoPath;

        var dto = (await _appService.GetOrgUnitForEdit(new EntityDto<long>(id))).OrgUnit;
        dto.Logo = null;
        await _appService.CreateOrEdit(dto);

        LoadProfile(id).LogoPath.ShouldBe(path);
    }

    [Fact]
    public async Task Should_Keep_The_Same_File_When_The_Same_Logo_Is_Sent_Back()
    {
        var id = await CreateUnitAsync("LOGO-SAME", Png());
        var path = LoadProfile(id).LogoPath;

        await _appService.CreateOrEdit((await _appService.GetOrgUnitForEdit(new EntityDto<long>(id))).OrgUnit);

        LoadProfile(id).LogoPath.ShouldBe(path);
    }

    [Fact]
    public async Task Should_Replace_The_Logo_And_Delete_The_Old_File()
    {
        var id = await CreateUnitAsync("LOGO-REPLACE", Png(color: SKColors.Red));
        var oldPath = LoadProfile(id).LogoPath;
        var newLogo = Png(color: SKColors.Blue);

        var dto = (await _appService.GetOrgUnitForEdit(new EntityDto<long>(id))).OrgUnit;
        dto.Logo = newLogo;
        await _appService.CreateOrEdit(dto);

        var newPath = LoadProfile(id).LogoPath;
        newPath.ShouldNotBe(oldPath);
        (await _images.ReadOrNullAsync(TenantId, newPath)).ShouldBe(newLogo);
        (await _images.ReadOrNullAsync(TenantId, oldPath)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Remove_The_Logo_And_Its_File()
    {
        var id = await CreateUnitAsync("LOGO-REMOVE", Png());
        var path = LoadProfile(id).LogoPath;

        var dto = (await _appService.GetOrgUnitForEdit(new EntityDto<long>(id))).OrgUnit;
        dto.RemoveLogo = true;
        await _appService.CreateOrEdit(dto);

        LoadProfile(id).LogoPath.ShouldBeNull();
        (await _images.ReadOrNullAsync(TenantId, path)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_A_File_That_Is_Not_Png_Jpeg_Or_Gif()
    {
        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateUnitAsync("LOGO-NOT-IMAGE", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D })); // "%PDF-"

        UsingDbContext(context =>
            context.OrganizationUnits.Any(u => u.DisplayName == "LOGO-NOT-IMAGE").ShouldBeFalse());
    }

    [Fact]
    public async Task Should_Refuse_A_Png_Header_That_Is_Not_A_Real_Image()
    {
        var fake = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

        await Should.ThrowAsync<UserFriendlyException>(() => CreateUnitAsync("LOGO-FAKE-PNG", fake));
    }

    [Fact]
    public async Task Should_Refuse_A_Logo_Over_The_Size_Limit()
    {
        var tooLarge = new byte[OrgUnitProfileConsts.MaxLogoBytes + 1];
        Png().CopyTo(tooLarge, 0);

        await Should.ThrowAsync<Exception>(() => CreateUnitAsync("LOGO-TOO-LARGE", tooLarge));

        UsingDbContext(context =>
            context.OrganizationUnits.Any(u => u.DisplayName == "LOGO-TOO-LARGE").ShouldBeFalse());
    }

    [Fact]
    public async Task Should_Flag_Units_With_A_Logo_In_The_List()
    {
        await CreateUnitAsync("LOGO-LIST-WITH", Png());
        await CreateUnitAsync("LOGO-LIST-WITHOUT");

        var list = await _appService.GetAll(new GetAllOrgUnitsInput { Filter = "LOGO-LIST-", MaxResultCount = 10 });

        list.Items.Single(i => i.NameEn == "LOGO-LIST-WITH").HasLogo.ShouldBeTrue();
        list.Items.Single(i => i.NameEn == "LOGO-LIST-WITHOUT").HasLogo.ShouldBeFalse();
    }
}
