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
using HCMv3.SurveyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SurveyManagement;

/// <summary>
/// Application service for <see cref="SurveyDetail"/>, the aggregated child of <see cref="Survey"/> (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.SurveyManagement.SurveyDetail</c>). Legacy
/// <c>PerformValidation</c> parity ported here: auto-assign OrderNo (max sibling OrderNo + 1, or 1 if none
/// have a non-zero OrderNo yet), Question required, and Options required when QuestionType is RadioList or
/// CheckboxList.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SurveyManagement_SurveyDetails)]
public class SurveyDetailsAppService : HCMv3AppServiceBase, ISurveyDetailsAppService
{
    private readonly IRepository<SurveyDetail> _surveyDetailRepository;

    public SurveyDetailsAppService(IRepository<SurveyDetail> surveyDetailRepository)
    {
        _surveyDetailRepository = surveyDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SurveyDetailDto>> GetAll(GetAllSurveyDetailsInput input)
    {
        input.Normalize();

        var query = _surveyDetailRepository.GetAll()
            .WhereIf(input.SurveyId.HasValue, d => d.SurveyId == input.SurveyId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SurveyDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSurveyDetailForEditOutput> GetSurveyDetailForEdit(EntityDto input)
    {
        var entity = await _surveyDetailRepository.GetAsync(input.Id);

        return new GetSurveyDetailForEditOutput
        {
            SurveyDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSurveyDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_SurveyDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _surveyDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_SurveyDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSurveyDetailDto input)
    {
        var entity = MapToEntity(input, new SurveyDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignOrderNoIfEmptyAsync(entity);
        Validate(entity);

        await _surveyDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_SurveyDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSurveyDetailDto input)
    {
        var entity = await _surveyDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignOrderNoIfEmptyAsync(entity);
        Validate(entity);

        await _surveyDetailRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: when OrderNo is 0, assign (max sibling OrderNo) + 1 if any
    /// sibling already has a non-zero OrderNo, otherwise 1.
    /// </summary>
    private async Task AssignOrderNoIfEmptyAsync(SurveyDetail entity)
    {
        if (entity.OrderNo != 0)
        {
            return;
        }

        var hasOrderedSiblings = await _surveyDetailRepository.GetAll()
            .AnyAsync(d => d.SurveyId == entity.SurveyId && d.Id != entity.Id && d.OrderNo != 0);

        if (!hasOrderedSiblings)
        {
            entity.OrderNo = 1;
            return;
        }

        var maxOrderNo = await _surveyDetailRepository.GetAll()
            .Where(d => d.SurveyId == entity.SurveyId && d.Id != entity.Id)
            .Select(d => (int?)d.OrderNo)
            .MaxAsync() ?? 0;

        entity.OrderNo = maxOrderNo + 1;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Question required, Options required when QuestionType is
    /// RadioList/CheckboxList.
    /// </summary>
    private void Validate(SurveyDetail entity)
    {
        if (entity.Question.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("QuestionCannotBeBlank"));
        }

        if ((entity.QuestionType == QuestionType.RadioList || entity.QuestionType == QuestionType.CheckboxList) &&
            entity.Options.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("OptionsRequiredForChoiceQuestion"));
        }
    }

    private static SurveyDetail MapToEntity(CreateOrEditSurveyDetailDto input, SurveyDetail entity)
    {
        entity.SurveyId = input.SurveyId;
        entity.OrderNo = input.OrderNo;
        entity.Question = input.Question;
        entity.QuestionType = input.QuestionType;
        entity.Options = input.Options;

        return entity;
    }

    private static SurveyDetailDto ToDto(SurveyDetail entity)
    {
        return new SurveyDetailDto
        {
            Id = entity.Id,
            SurveyId = entity.SurveyId,
            OrderNo = entity.OrderNo,
            Question = entity.Question,
            QuestionType = entity.QuestionType,
            Options = entity.Options
        };
    }

    private static CreateOrEditSurveyDetailDto ToCreateOrEditDto(SurveyDetail entity)
    {
        return new CreateOrEditSurveyDetailDto
        {
            Id = entity.Id,
            SurveyId = entity.SurveyId,
            OrderNo = entity.OrderNo,
            Question = entity.Question,
            QuestionType = entity.QuestionType,
            Options = entity.Options
        };
    }
}
