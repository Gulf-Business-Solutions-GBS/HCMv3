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
/// Application service for <see cref="Survey"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SurveyManagement.Survey</c>). Legacy <c>PerformValidation</c> parity
/// (auto Code, duplicate Code/Name checks, StartDate/EndDate required, EndDate &gt;= StartDate) is ported
/// here, same pattern as <c>ProjectManagement.ProjectsAppService</c> -- not reused directly because
/// <see cref="Survey"/> lacks a type FK and adds the EndDate&gt;=StartDate rule that Project does not have.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SurveyManagement_Surveys)]
public class SurveysAppService : HCMv3AppServiceBase, ISurveysAppService
{
    private readonly IRepository<Survey> _surveyRepository;

    public SurveysAppService(IRepository<Survey> surveyRepository)
    {
        _surveyRepository = surveyRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SurveyDto>> GetAll(GetAllSurveysInput input)
    {
        input.Normalize();

        var query = _surveyRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), s =>
                s.Code.Contains(input.Filter) ||
                s.NameEn.Contains(input.Filter) ||
                s.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SurveyDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSurveyForEditOutput> GetSurveyForEdit(EntityDto input)
    {
        var entity = await _surveyRepository.GetAsync(input.Id);

        return new GetSurveyForEditOutput
        {
            Survey = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSurveyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_Surveys_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _surveyRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_Surveys_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSurveyDto input)
    {
        var entity = MapToEntity(input, new Survey());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _surveyRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_Surveys_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSurveyDto input)
    {
        var entity = await _surveyRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _surveyRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Survey entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _surveyRepository.GetAll()
            .Where(s => s.Code != null && s.Code != "")
            .Select(s => s.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code, unique NameEn/NameAr, and EndDate must not be
    /// earlier than StartDate.
    /// </summary>
    private async Task ValidateAsync(Survey entity)
    {
        var duplicateCode = await _surveyRepository.GetAll()
            .AnyAsync(s => s.Id != entity.Id && s.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _surveyRepository.GetAll()
            .AnyAsync(s => s.Id != entity.Id &&
                           (s.NameEn == entity.NameEn || s.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static Survey MapToEntity(CreateOrEditSurveyDto input, Survey entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;

        return entity;
    }

    private static SurveyDto ToDto(Survey entity)
    {
        return new SurveyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditSurveyDto ToCreateOrEditDto(Survey entity)
    {
        return new CreateOrEditSurveyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
