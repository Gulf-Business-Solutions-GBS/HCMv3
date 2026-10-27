using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class OrganizationDocumentsAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private OrganizationDocumentsAppService CreateAppService()
    {
        return CreateService(() => new OrganizationDocumentsAppService(FakeRepository.For<OrganizationDocument>(Context)));
    }

    private long CreateOrganizationUnit(string displayName)
    {
        var entity = new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null)
        {
            Code = displayName
        };
        Context.OrganizationUnits.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateDocumentType(string code)
    {
        var entity = new DocumentType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.DocumentTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_OrganizationDocument()
    {
        var organizationUnitId = CreateOrganizationUnit("Org1");
        var documentTypeId = CreateDocumentType("DT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOrganizationDocumentDto
        {
            OrganizationUnitId = organizationUnitId,
            DocumentTypeId = documentTypeId,
            DocumentNum = "DOC1",
            IssueDate = new DateTime(2025, 1, 1),
            ExpiryDate = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllOrganizationDocumentsInput { OrganizationUnitId = organizationUnitId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].DocumentNum.ShouldBe("DOC1");
    }

    [Fact]
    public async Task Should_Throw_When_ExpiryDate_Earlier_Than_IssueDate()
    {
        var organizationUnitId = CreateOrganizationUnit("Org2");
        var documentTypeId = CreateDocumentType("DT2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditOrganizationDocumentDto
            {
                OrganizationUnitId = organizationUnitId,
                DocumentTypeId = documentTypeId,
                DocumentNum = "DOC2",
                IssueDate = new DateTime(2026, 1, 1),
                ExpiryDate = new DateTime(2025, 1, 1)
            }));
    }
}
