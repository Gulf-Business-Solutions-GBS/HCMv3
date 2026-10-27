using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.SurveyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SurveyManagement;

/// <summary>
/// Application service for <see cref="EmployeeSurvey"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SurveyManagement.EmployeeSurvey</c>). Legacy <c>PerformValidation</c>
/// (Employee and Survey both required) is already satisfied by the non-nullable FK ints on the entity and
/// the <c>[Required]</c> attributes on <see cref="CreateOrEditEmployeeSurveyDto"/>, so this is plain CRUD,
/// same pattern as <c>ProjectManagement.ProjectRegistersAppService</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveys)]
public class EmployeeSurveysAppService : HCMv3AppServiceBase, IEmployeeSurveysAppService
{
    private readonly IRepository<EmployeeSurvey> _employeeSurveyRepository;

    public EmployeeSurveysAppService(IRepository<EmployeeSurvey> employeeSurveyRepository)
    {
        _employeeSurveyRepository = employeeSurveyRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSurveyDto>> GetAll(GetAllEmployeeSurveysInput input)
    {
        input.Normalize();

        var query = _employeeSurveyRepository.GetAll()
            .Include(es => es.Employee)
            .Include(es => es.Survey)
            .WhereIf(input.EmployeeId.HasValue, es => es.EmployeeId == input.EmployeeId)
            .WhereIf(input.SurveyId.HasValue, es => es.SurveyId == input.SurveyId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSurveyDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSurveyForEditOutput> GetEmployeeSurveyForEdit(EntityDto input)
    {
        var entity = await _employeeSurveyRepository.GetAsync(input.Id);

        return new GetEmployeeSurveyForEditOutput
        {
            EmployeeSurvey = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSurveyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveys_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSurveyRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveys_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSurveyDto input)
    {
        var entity = MapToEntity(input, new EmployeeSurvey());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _employeeSurveyRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SurveyManagement_EmployeeSurveys_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSurveyDto input)
    {
        var entity = await _employeeSurveyRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _employeeSurveyRepository.UpdateAsync(entity);
    }

    private static EmployeeSurvey MapToEntity(CreateOrEditEmployeeSurveyDto input, EmployeeSurvey entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.SurveyId = input.SurveyId;

        return entity;
    }

    private static EmployeeSurveyDto ToDto(EmployeeSurvey entity)
    {
        return new EmployeeSurveyDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            SurveyId = entity.SurveyId,
            SurveyNameEn = entity.Survey?.NameEn
        };
    }

    private static CreateOrEditEmployeeSurveyDto ToCreateOrEditDto(EmployeeSurvey entity)
    {
        return new CreateOrEditEmployeeSurveyDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            SurveyId = entity.SurveyId
        };
    }
}
