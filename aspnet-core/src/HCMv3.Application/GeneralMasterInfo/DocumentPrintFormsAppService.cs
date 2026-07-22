using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the DocumentPrintForm detail record (ported from legacy XAF DocumentPrintForm).
/// Pure detail record (no Code/NameEn/NameAr): plain CRUD without auto-code or duplicate validation.
/// The legacy <c>ReportDataV2</c> field is not ported.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DocumentPrintForms)]
public class DocumentPrintFormsAppService : HCMv3AppServiceBase, IDocumentPrintFormsAppService
{
    private readonly IRepository<DocumentPrintForm> _repository;

    public DocumentPrintFormsAppService(IRepository<DocumentPrintForm> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DocumentPrintFormDto>> GetAll(GetAllDocumentPrintFormsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(p => p.DocumentType);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DocumentPrintFormDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDocumentPrintFormForEditOutput> GetDocumentPrintFormForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetDocumentPrintFormForEditOutput
        {
            DocumentPrintForm = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDocumentPrintFormDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DocumentPrintForms_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDocumentPrintFormDto input)
    {
        var entity = new DocumentPrintForm
        {
            DocumentTypeId = input.DocumentTypeId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DocumentPrintForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDocumentPrintFormDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.DocumentTypeId = input.DocumentTypeId;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DocumentPrintForms_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static DocumentPrintFormDto ToDto(DocumentPrintForm entity)
    {
        return new DocumentPrintFormDto
        {
            Id = entity.Id,
            DocumentTypeId = entity.DocumentTypeId,
            DocumentTypeName = entity.DocumentType?.NameEn
        };
    }

    private static CreateOrEditDocumentPrintFormDto ToCreateOrEditDto(DocumentPrintForm entity)
    {
        return new CreateOrEditDocumentPrintFormDto
        {
            Id = entity.Id,
            DocumentTypeId = entity.DocumentTypeId
        };
    }
}
