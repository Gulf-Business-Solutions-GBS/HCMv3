using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.Recruitment;
using HCMv3.Storage;
using HCMv3.Storage.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Storage;

/// <summary>
/// The attachment policies of admin-maintained screens: files follow the screen's own permission. Viewing
/// needs the screen's read permission, adding or removing needs its Edit permission, and a record that does
/// not exist is refused. Each user here is created with exactly the permissions the test names, because the
/// tenant admin holds every permission and would pass any check.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AttachmentOwnerPolicies_Tests : AppTestBase
{
    private readonly IAttachmentAppService _attachments;

    public AttachmentOwnerPolicies_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _attachments = Resolve<IAttachmentAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- seeding

    /// <summary>A user with no roles, holding only the permissions given.</summary>
    private string CreateUser(params string[] grantedPermissions)
    {
        var userName = "att" + Guid.NewGuid().ToString("N")[..8];
        UsingDbContext(context =>
        {
            var user = new User
            {
                TenantId = TenantId,
                UserName = userName,
                Name = "Plain",
                Surname = "User",
                EmailAddress = userName + "@gbskw.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
            };
            user.SetNormalizedNames();
            context.Users.Add(user);
            context.SaveChanges();

            foreach (var permission in grantedPermissions)
            {
                context.UserPermissions.Add(new UserPermissionSetting
                {
                    TenantId = TenantId,
                    UserId = user.Id,
                    Name = permission,
                    IsGranted = true
                });
            }
        });

        return userName;
    }

    private int CreateAsset()
    {
        Asset asset = null;
        UsingDbContext(context =>
        {
            asset = context.Set<Asset>().Add(new Asset
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N")[..8],
                NameEn = "Laptop",
                NameAr = "حاسوب",
                ExpiryDate = DateTime.Today.AddYears(3)
            }).Entity;
        });

        return asset.Id;
    }

    private int CreateVacancy()
    {
        Vacancy vacancy = null;
        UsingDbContext(context =>
        {
            vacancy = context.Set<Vacancy>().Add(new Vacancy
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N")[..8],
                VacancyTitle = "Accountant",
                RequestionDate = DateTime.Today,
                PublishOn = DateTime.Today,
                ClosingDate = DateTime.Today.AddMonths(1)
            }).Entity;
        });

        return vacancy.Id;
    }

    private Task<GetAttachmentsOutput> List(string entityType, int entityId) =>
        _attachments.GetAttachments(new AttachmentOwnerInput { EntityType = entityType, EntityId = entityId.ToString() });

    // ---------------------------------------------------------------- registration

    [Theory]
    [InlineData("EmployeeEducation")]
    [InlineData("EmployeeAsset")]
    [InlineData("EmployeeTraining")]
    [InlineData("EmployeeSeparation")]
    [InlineData("EmployeeDisciplinary")]
    [InlineData("EmployeeLoan")]
    [InlineData("EmployeeTicket")]
    [InlineData("EmployeeHealthReimburse")]
    [InlineData("ApplicantCV")]
    [InlineData("Vacancy")]
    [InlineData("TravelFeedback")]
    [InlineData("OrganizationDocument")]
    [InlineData("Asset")]
    public void Should_Register_Exactly_One_Policy_For_The_Entity_Type(string entityType)
    {
        var policies = LocalIocManager.ResolveAll<IAttachmentOwnerPolicy>();

        policies.Count(p => p.EntityType == entityType).ShouldBe(1);
    }

    // ---------------------------------------------------------------- Asset (General Master Info)

    [Fact]
    public async Task Should_Let_Asset_Editors_See_And_Change_Asset_Files()
    {
        var assetId = CreateAsset();
        LoginAsTenant("Default", CreateUser(AppPermissions.Pages_GeneralMasterInfo_Assets, AppPermissions.Pages_GeneralMasterInfo_Assets_Edit));

        var list = await List(AssetAttachmentOwnerPolicy.Type, assetId);

        list.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Let_Asset_Readers_See_But_Not_Change_Asset_Files()
    {
        var assetId = CreateAsset();
        LoginAsTenant("Default", CreateUser(AppPermissions.Pages_GeneralMasterInfo_Assets));

        var list = await List(AssetAttachmentOwnerPolicy.Type, assetId);

        list.CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_Asset_Files_Without_The_Asset_Permission()
    {
        var assetId = CreateAsset();
        LoginAsTenant("Default", CreateUser());

        await Should.ThrowAsync<AbpAuthorizationException>(() => List(AssetAttachmentOwnerPolicy.Type, assetId));
    }

    // ---------------------------------------------------------------- Vacancy

    [Fact]
    public async Task Should_Let_Vacancy_Editors_See_And_Change_Vacancy_Files()
    {
        var vacancyId = CreateVacancy();
        LoginAsTenant("Default", CreateUser(AppPermissions.Pages_Recruitment_Vacancies, AppPermissions.Pages_Recruitment_Vacancies_Edit));

        var list = await List(VacancyAttachmentOwnerPolicy.Type, vacancyId);

        list.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_Vacancy_Files_Without_The_Vacancy_Permission()
    {
        var vacancyId = CreateVacancy();
        LoginAsTenant("Default", CreateUser(AppPermissions.Pages_GeneralMasterInfo_Assets));

        await Should.ThrowAsync<AbpAuthorizationException>(() => List(VacancyAttachmentOwnerPolicy.Type, vacancyId));
    }

    [Fact]
    public async Task Should_Refuse_A_Vacancy_That_Does_Not_Exist()
    {
        // Tenant admin: holds the Vacancies permissions, so only the missing record can refuse it.
        await Should.ThrowAsync<AbpAuthorizationException>(() => List(VacancyAttachmentOwnerPolicy.Type, 999999));
    }

    // ---------------------------------------------------------------- Organization Document via Org Policy

    private int CreateOrganizationDocument(bool showToAll)
    {
        OrganizationDocument document = null;
        UsingDbContext(context =>
        {
            var unit = context.OrganizationUnits.Add(new Abp.Organizations.OrganizationUnit(TenantId, "Head Office")
            {
                Code = Abp.Organizations.OrganizationUnit.CreateCode(Guid.NewGuid().GetHashCode() & 0xFFFF)
            }).Entity;
            var type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N")[..8],
                NameEn = "Policy",
                NameAr = "سياسة",
                ShowToAll = showToAll
            }).Entity;
            context.SaveChanges();
            document = context.Set<OrganizationDocument>().Add(new OrganizationDocument
            {
                TenantId = TenantId,
                OrganizationUnitId = unit.Id,
                DocumentTypeId = type.Id,
                DocumentNum = "POL-1",
                IssueDate = DateTime.Today,
                ExpiryDate = DateTime.Today.AddYears(1)
            }).Entity;
        });

        return document.Id;
    }

    [Fact]
    public async Task Should_Let_Org_Policy_Readers_See_But_Not_Change_A_Show_To_All_Document()
    {
        var documentId = CreateOrganizationDocument(showToAll: true);
        LoginAsTenant("Default", CreateUser(AppPermissions.Pages_GovtRelationsManagement_OrgPolicies));

        var list = await List(OrganizationDocumentAttachmentOwnerPolicy.Type, documentId);

        list.CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_Org_Policy_Readers_A_Document_Not_Shown_To_All()
    {
        var documentId = CreateOrganizationDocument(showToAll: false);
        LoginAsTenant("Default", CreateUser(AppPermissions.Pages_GovtRelationsManagement_OrgPolicies));

        await Should.ThrowAsync<AbpAuthorizationException>(() => List(OrganizationDocumentAttachmentOwnerPolicy.Type, documentId));
    }
}
