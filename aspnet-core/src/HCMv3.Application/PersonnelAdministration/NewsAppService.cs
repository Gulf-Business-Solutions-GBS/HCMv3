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
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="News"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.News</c> -- see entity remarks for the documented
/// simplifications). Legacy <c>PerformValidation</c> parity: NewsType required, IssueDate/ExpiryDate
/// required and IssueDate &lt;= ExpiryDate. (Legacy's EmailSetup-required check is dropped along with the
/// dropped EmailSetup field.)
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_News)]
public class NewsAppService : HCMv3AppServiceBase, INewsAppService
{
    private readonly IRepository<News> _repository;

    public NewsAppService(IRepository<News> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<NewsDto>> GetAll(GetAllNewsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(n => n.NewsType)
            .Include(n => n.OrganizationUnitType)
            .Include(n => n.OrganizationUnit)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), n =>
                n.Title.Contains(input.Filter) || n.TitleAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<NewsDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetNewsForEditOutput> GetNewsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetNewsForEditOutput
        {
            News = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditNewsDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_News_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_News_Create)]
    protected virtual async Task CreateAsync(CreateOrEditNewsDto input)
    {
        var entity = MapToEntity(input, new News());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDates(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_News_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditNewsDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateDates(entity);

        await _repository.UpdateAsync(entity);
    }

    private void ValidateDates(News entity)
    {
        if (entity.IssueDate > entity.ExpiryDate)
        {
            throw new UserFriendlyException(L("IssueDateShouldBeLessOrEqualToExpiryDate"));
        }
    }

    private static News MapToEntity(CreateOrEditNewsDto input, News entity)
    {
        entity.NewsTypeId = input.NewsTypeId;
        entity.Title = input.Title;
        entity.TitleAr = input.TitleAr;
        entity.IssueDate = input.IssueDate;
        entity.ExpiryDate = input.ExpiryDate;
        entity.OrganizationUnitTypeId = input.OrganizationUnitTypeId;
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.SendNotification = input.SendNotification;
        entity.SendEmail = input.SendEmail;
        entity.Comments = input.Comments;
        entity.CommentAr = input.CommentAr;
        entity.NotificationSent = input.NotificationSent;
        entity.DocumentHtml = input.DocumentHtml;
        return entity;
    }

    private static NewsDto ToDto(News entity)
    {
        return new NewsDto
        {
            Id = entity.Id,
            NewsTypeId = entity.NewsTypeId,
            NewsTypeNameEn = entity.NewsType?.NameEn,
            Title = entity.Title,
            TitleAr = entity.TitleAr,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            OrganizationUnitTypeId = entity.OrganizationUnitTypeId,
            OrganizationUnitTypeNameEn = entity.OrganizationUnitType?.NameEn,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitDisplayName = entity.OrganizationUnit?.DisplayName,
            SendNotification = entity.SendNotification,
            SendEmail = entity.SendEmail,
            Comments = entity.Comments,
            CommentAr = entity.CommentAr,
            NotificationSent = entity.NotificationSent,
            DocumentHtml = entity.DocumentHtml
        };
    }

    private static CreateOrEditNewsDto ToCreateOrEditDto(News entity)
    {
        return new CreateOrEditNewsDto
        {
            Id = entity.Id,
            NewsTypeId = entity.NewsTypeId,
            Title = entity.Title,
            TitleAr = entity.TitleAr,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            OrganizationUnitTypeId = entity.OrganizationUnitTypeId,
            OrganizationUnitId = entity.OrganizationUnitId,
            SendNotification = entity.SendNotification,
            SendEmail = entity.SendEmail,
            Comments = entity.Comments,
            CommentAr = entity.CommentAr,
            NotificationSent = entity.NotificationSent,
            DocumentHtml = entity.DocumentHtml
        };
    }
}
