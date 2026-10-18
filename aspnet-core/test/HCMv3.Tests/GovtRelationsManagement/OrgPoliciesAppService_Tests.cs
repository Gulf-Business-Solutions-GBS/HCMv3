using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

/// <summary>
/// Legacy "Org Policy": organisation documents whose document type is Show To All, read-only. Also pins
/// legacy's DaysLeft (expiry date minus today) on the shared document list row.
/// </summary>
// ReSharper disable once InconsistentNaming
public class OrgPoliciesAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private OrgPoliciesAppService CreateAppService()
    {
        return CreateService(() => new OrgPoliciesAppService(FakeRepository.For<OrganizationDocument>(Context)));
    }

    private long CreateOrganizationUnit(string displayName)
    {
        var entity = new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null) { Code = displayName };
        Context.OrganizationUnits.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateDocumentType(string code, bool showToAll)
    {
        var entity = new DocumentType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            ShowToAll = showToAll
        };
        Context.DocumentTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private void CreateDocument(long organizationUnitId, int documentTypeId, string number, DateTime expiryDate)
    {
        Context.OrganizationDocuments.Add(new OrganizationDocument
        {
            TenantId = AbpSession.TenantId ?? 1,
            OrganizationUnitId = organizationUnitId,
            DocumentTypeId = documentTypeId,
            DocumentNum = number,
            IssueDate = expiryDate.AddYears(-1),
            ExpiryDate = expiryDate
        });
        Context.SaveChanges();
    }

    [Fact]
    public async Task Should_List_Only_Documents_Whose_Type_Is_Show_To_All()
    {
        var unitId = CreateOrganizationUnit("OrgP1");
        CreateDocument(unitId, CreateDocumentType("POLICY", showToAll: true), "HR-POLICY", Clock.Now.Date.AddDays(30));
        CreateDocument(unitId, CreateDocumentType("LICENSE", showToAll: false), "TRADE-LIC", Clock.Now.Date.AddDays(30));

        var result = await CreateAppService().GetAll(new GetAllOrganizationDocumentsInput { MaxResultCount = 10 });

        result.Items.Select(i => i.DocumentNum).ShouldBe(new[] { "HR-POLICY" });
    }

    [Fact]
    public async Task Should_Show_Days_Left_Until_Expiry_Negative_Once_Expired()
    {
        var unitId = CreateOrganizationUnit("OrgP2");
        var typeId = CreateDocumentType("POLICY2", showToAll: true);
        CreateDocument(unitId, typeId, "FUTURE", Clock.Now.Date.AddDays(10));
        CreateDocument(unitId, typeId, "EXPIRED", Clock.Now.Date.AddDays(-3));

        var result = await CreateAppService().GetAll(new GetAllOrganizationDocumentsInput { MaxResultCount = 10 });

        result.Items.Single(i => i.DocumentNum == "FUTURE").DaysLeft.ShouldBe(10);
        result.Items.Single(i => i.DocumentNum == "EXPIRED").DaysLeft.ShouldBe(-3);
    }
}
