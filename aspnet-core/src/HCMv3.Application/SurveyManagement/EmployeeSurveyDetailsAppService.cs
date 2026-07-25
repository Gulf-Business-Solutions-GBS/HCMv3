using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.SurveyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SurveyManagement;

/// <summary>
/// Application service for <see cref="EmployeeSurveyDetail"/>, the aggregated child of
/// <see cref="EmployeeSurvey"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SurveyManagement.EmployeeSurveyDetail</c>). Legacy
/// <c>PerformValidation</c> parity ported here: Question (the answered <see cref="SurveyDetail"/> FK) must
/// not be blank -- already satisfied by the non-nullable <see cref="EmployeeSurveyDetail.QuestionId"/> int
/// FK and the <c>[Required]</c> attribute on <see cref="CreateOrEditEmployeeSurveyDetailDto.QuestionId"/>,
/// but is re-asserted here defensively (0 is technically a valid default int) for exact legacy parity.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveyDetails)]
public class EmployeeSurveyDetailsAppService : HCMv3AppServiceBase, IEmployeeSurveyDetailsAppService
{
    private readonly IRepository<EmployeeSurveyDetail> _employeeSurveyDetailRepository;

    public EmployeeSurveyDetailsAppService(IRepository<EmployeeSurveyDetail> employeeSurveyDetailRepository)
    {
        _employeeSurveyDetailRepository = employeeSurveyDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSurveyDetailDto>> GetAll(GetAllEmployeeSurveyDetailsInput input)
    {
        input.Normalize();

        var query = _employeeSurveyDetailRepository.GetAll()
            .Include(d => d.Question)
            .WhereIf(input.EmployeeSurveyId.HasValue, d => d.EmployeeSurveyId == input.EmployeeSurveyId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSurveyDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSurveyDetailForEditOutput> GetEmployeeSurveyDetailForEdit(EntityDto input)
    {
        var entity = await _employeeSurveyDetailRepository.GetAsync(input.Id);

        return new GetEmployeeSurveyDetailForEditOutput
        {
            EmployeeSurveyDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSurveyDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveyDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSurveyDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveyDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSurveyDetailDto input)
    {
        var entity = MapToEntity(input, new EmployeeSurveyDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);

        await _employeeSurveyDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveyDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSurveyDetailDto input)
    {
        var entity = await _employeeSurveyDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        Validate(entity);

        await _employeeSurveyDetailRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Question can't be blank.
    /// </summary>
    private void Validate(EmployeeSurveyDetail entity)
    {
        if (entity.QuestionId <= 0)
        {
            throw new UserFriendlyException(L("QuestionCannotBeBlank"));
        }
    }

    private static EmployeeSurveyDetail MapToEntity(CreateOrEditEmployeeSurveyDetailDto input, EmployeeSurveyDetail entity)
    {
        entity.EmployeeSurveyId = input.EmployeeSurveyId;
        entity.QuestionId = input.QuestionId;
        entity.Answer = input.Answer;

        return entity;
    }

    private static EmployeeSurveyDetailDto ToDto(EmployeeSurveyDetail entity)
    {
        return new EmployeeSurveyDetailDto
        {
            Id = entity.Id,
            EmployeeSurveyId = entity.EmployeeSurveyId,
            QuestionId = entity.QuestionId,
            QuestionText = entity.Question?.Question,
            QuestionType = entity.Question?.QuestionType ?? default,
            Answer = entity.Answer
        };
    }

    private static CreateOrEditEmployeeSurveyDetailDto ToCreateOrEditDto(EmployeeSurveyDetail entity)
    {
        return new CreateOrEditEmployeeSurveyDetailDto
        {
            Id = entity.Id,
            EmployeeSurveyId = entity.EmployeeSurveyId,
            QuestionId = entity.QuestionId,
            Answer = entity.Answer
        };
    }
}
