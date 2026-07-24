using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Application service for <see cref="OrganizationDocument"/> (ported from legacy XAF
/// OrganizationDocument business object). See <see cref="OrganizationDocument"/> remarks for the dropped
/// legacy DocAttachment FK and computed DaysLeft field.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments)]
public class OrganizationDocumentsAppService : HCMv3AppServiceBase, IOrganizationDocumentsAppService
{
    private readonly IRepository<OrganizationDocument> _repository;

    public OrganizationDocumentsAppService(IRepository<OrganizationDocument> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OrganizationDocumentDto>> GetAll(GetAllOrganizationDocumentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.OrganizationUnit)
            .Include(d => d.DocumentType)
            .Include(d => d.Sponsor)
            .WhereIf(input.OrganizationUnitId.HasValue, d => d.OrganizationUnitId == input.OrganizationUnitId)
            .WhereIf(input.SponsorId.HasValue, d => d.SponsorId == input.SponsorId)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.DocumentNum.Contains(input.Filter) ||
                d.DocumentPlace.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OrganizationDocumentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetOrganizationDocumentForEditOutput> GetOrganizationDocumentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetOrganizationDocumentForEditOutput
        {
            OrganizationDocument = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOrganizationDocumentDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOrganizationDocumentDto input)
    {
        var entity = MapToEntity(input, new OrganizationDocument());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDateRange(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOrganizationDocumentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateDateRange(entity);

        await _repository.UpdateAsync(entity);
    }

    private static OrganizationDocument MapToEntity(CreateOrEditOrganizationDocumentDto input, OrganizationDocument entity)
    {
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.DocumentTypeId = input.DocumentTypeId;
        entity.DocumentNum = input.DocumentNum;
        entity.IssueDate = input.IssueDate.Date;
        entity.ExpiryDate = input.ExpiryDate.Date;
        entity.Comments = input.Comments;
        entity.DocumentPlace = input.DocumentPlace;
        entity.NotificationSent = input.NotificationSent;
        entity.SponsorId = input.SponsorId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: required fields (enforced via DataAnnotations on the DTO)
    /// plus ExpiryDate cannot be earlier than IssueDate.
    /// </summary>
    private void ValidateDateRange(OrganizationDocument entity)
    {
        if (entity.ExpiryDate < entity.IssueDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static OrganizationDocumentDto ToDto(OrganizationDocument entity)
    {
        return new OrganizationDocumentDto
        {
            Id = entity.Id,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitName = entity.OrganizationUnit?.DisplayName,
            DocumentTypeId = entity.DocumentTypeId,
            DocumentTypeName = entity.DocumentType?.NameEn,
            DocumentNum = entity.DocumentNum,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            Comments = entity.Comments,
            DocumentPlace = entity.DocumentPlace,
            NotificationSent = entity.NotificationSent,
            SponsorId = entity.SponsorId,
            SponsorName = entity.Sponsor?.NameEn
        };
    }

    private static CreateOrEditOrganizationDocumentDto ToCreateOrEditDto(OrganizationDocument entity)
    {
        return new CreateOrEditOrganizationDocumentDto
        {
            Id = entity.Id,
            OrganizationUnitId = entity.OrganizationUnitId,
            DocumentTypeId = entity.DocumentTypeId,
            DocumentNum = entity.DocumentNum,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            Comments = entity.Comments,
            DocumentPlace = entity.DocumentPlace,
            NotificationSent = entity.NotificationSent,
            SponsorId = entity.SponsorId
        };
    }
}
