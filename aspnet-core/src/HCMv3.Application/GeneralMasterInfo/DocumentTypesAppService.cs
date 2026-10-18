using System;
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
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.ReportManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the DocumentType master (ported from legacy XAF DocumentType).
/// Mirrors <c>CitiesAppService</c>. The legacy aggregated <c>PrintForm</c> collection is ported
/// separately as <see cref="DocumentPrintForm"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DocumentTypes)]
public class DocumentTypesAppService : HCMv3AppServiceBase, IDocumentTypesAppService
{
    private readonly IRepository<DocumentType> _repository;
    private readonly IRepository<PrintReport> _printReportRepository;

    public DocumentTypesAppService(IRepository<DocumentType> repository, IRepository<PrintReport> printReportRepository)
    {
        _repository = repository;
        _printReportRepository = printReportRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DocumentTypeDto>> GetAll(GetAllDocumentTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.NotificationConfig)
            .Include(d => d.PrintReport)
            // How the letter-request screen fills its "what letter do you need" dropdown. Legacy expressed
            // the same rule as AllowUpdate == false; see DocumentType.IsLetterType.
            .WhereIf(input.OnlyLetterTypes, d => d.IsLetterType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.Code.Contains(input.Filter) ||
                d.NameEn.Contains(input.Filter) ||
                d.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DocumentTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDocumentTypeForEditOutput> GetDocumentTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetDocumentTypeForEditOutput
        {
            DocumentType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDocumentTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DocumentTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDocumentTypeDto input)
    {
        var entity = new DocumentType
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            NotificationPeriod = input.NotificationPeriod,
            NotificationConfigId = input.NotificationConfigId,
            NotificationFrequency = input.NotificationFrequency,
            NotificationCountLimit = input.NotificationCountLimit,
            DocumentBelongsTo = input.DocumentBelongsTo,
            IsLetterType = input.IsLetterType,
            PrintReportId = input.PrintReportId,
            IsWithdrawable = input.IsWithdrawable,
            IncludeInWorkflow = input.IncludeInWorkflow,
            IsHiddenFromEmployee = input.IsHiddenFromEmployee,
            ShowToAll = input.ShowToAll,
            IsExpiryDateRequired = input.IsExpiryDateRequired,
            AllowDateOverlap = input.AllowDateOverlap,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);
        await ValidatePrintReportAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DocumentTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDocumentTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.NotificationPeriod = input.NotificationPeriod;
        entity.NotificationConfigId = input.NotificationConfigId;
        entity.NotificationFrequency = input.NotificationFrequency;
        entity.NotificationCountLimit = input.NotificationCountLimit;
        entity.DocumentBelongsTo = input.DocumentBelongsTo;
        entity.IsLetterType = input.IsLetterType;
        entity.PrintReportId = input.PrintReportId;
        entity.IsWithdrawable = input.IsWithdrawable;
        entity.IncludeInWorkflow = input.IncludeInWorkflow;
        entity.IsHiddenFromEmployee = input.IsHiddenFromEmployee;
        entity.ShowToAll = input.ShowToAll;
        entity.IsExpiryDateRequired = input.IsExpiryDateRequired;
        entity.AllowDateOverlap = input.AllowDateOverlap;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);
        await ValidatePrintReportAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DocumentTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// The reports offered in the type's Print Report picker. Only ones that can actually be printed: active,
    /// and with a report class in this build -- the same test the print itself applies.
    /// </summary>
    public async Task<ListResultDto<SimpleLookupDto>> GetPrintReportsForLookup()
    {
        var ported = PortedReportCatalog.Names;

        var reports = (await _printReportRepository.GetAll()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Code)
                .ToListAsync())
            .Where(r => ported.Contains(r.ReportName, StringComparer.OrdinalIgnoreCase))
            .Select(r => new SimpleLookupDto { Id = r.Id, Code = r.Code, NameEn = r.NameEn, NameAr = r.NameAr })
            .ToList();

        return new ListResultDto<SimpleLookupDto>(reports);
    }

    /// <summary>A chosen report has to be one of this tenant's; the repository's own filters see to "this tenant's".</summary>
    private async Task ValidatePrintReportAsync(DocumentType entity)
    {
        if (!entity.PrintReportId.HasValue)
        {
            return;
        }

        if (!await _printReportRepository.GetAll().AnyAsync(r => r.Id == entity.PrintReportId.Value))
        {
            throw new UserFriendlyException(L("PrintReportNotFound"));
        }
    }

    private async Task AssignCodeIfEmptyAsync(DocumentType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(d => d.Code != null && d.Code != "")
            .Select(d => d.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(DocumentType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           (d.NameEn == entity.NameEn || d.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static DocumentTypeDto ToDto(DocumentType entity)
    {
        return new DocumentTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            NotificationPeriod = entity.NotificationPeriod,
            NotificationConfigId = entity.NotificationConfigId,
            NotificationConfigName = entity.NotificationConfig?.Name,
            PrintReportNameEn = entity.PrintReport?.NameEn,
            PrintReportNameAr = entity.PrintReport?.NameAr,
            NotificationFrequency = entity.NotificationFrequency,
            NotificationCountLimit = entity.NotificationCountLimit,
            DocumentBelongsTo = entity.DocumentBelongsTo,
            IsLetterType = entity.IsLetterType,
            PrintReportId = entity.PrintReportId,
            IsWithdrawable = entity.IsWithdrawable,
            IncludeInWorkflow = entity.IncludeInWorkflow,
            IsHiddenFromEmployee = entity.IsHiddenFromEmployee,
            ShowToAll = entity.ShowToAll,
            IsExpiryDateRequired = entity.IsExpiryDateRequired,
            AllowDateOverlap = entity.AllowDateOverlap
        };
    }

    private static CreateOrEditDocumentTypeDto ToCreateOrEditDto(DocumentType entity)
    {
        return new CreateOrEditDocumentTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            NotificationPeriod = entity.NotificationPeriod,
            NotificationConfigId = entity.NotificationConfigId,
            NotificationFrequency = entity.NotificationFrequency,
            NotificationCountLimit = entity.NotificationCountLimit,
            DocumentBelongsTo = entity.DocumentBelongsTo,
            IsLetterType = entity.IsLetterType,
            PrintReportId = entity.PrintReportId,
            IsWithdrawable = entity.IsWithdrawable,
            IncludeInWorkflow = entity.IncludeInWorkflow,
            IsHiddenFromEmployee = entity.IsHiddenFromEmployee,
            ShowToAll = entity.ShowToAll,
            IsExpiryDateRequired = entity.IsExpiryDateRequired,
            AllowDateOverlap = entity.AllowDateOverlap
        };
    }
}
