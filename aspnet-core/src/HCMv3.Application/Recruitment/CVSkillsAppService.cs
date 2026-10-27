using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="CVSkill"/>, an aggregated child of <see cref="ApplicantCV"/> (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.Recruitment.CVSkill</c>). Legacy had no
/// <c>PerformValidation</c> override, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_CVSkills)]
public class CVSkillsAppService : HCMv3AppServiceBase, ICVSkillsAppService
{
    private readonly IRepository<CVSkill> _cvSkillRepository;

    public CVSkillsAppService(IRepository<CVSkill> cvSkillRepository)
    {
        _cvSkillRepository = cvSkillRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CVSkillDto>> GetAll(GetAllCVSkillsInput input)
    {
        input.Normalize();

        var query = _cvSkillRepository.GetAll()
            .WhereIf(input.ApplicantCVId.HasValue, s => s.ApplicantCVId == input.ApplicantCVId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CVSkillDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCVSkillForEditOutput> GetCVSkillForEdit(EntityDto input)
    {
        var entity = await _cvSkillRepository.GetAsync(input.Id);

        return new GetCVSkillForEditOutput
        {
            CVSkill = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCVSkillDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVSkills_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _cvSkillRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVSkills_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCVSkillDto input)
    {
        var entity = MapToEntity(input, new CVSkill());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _cvSkillRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVSkills_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCVSkillDto input)
    {
        var entity = await _cvSkillRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _cvSkillRepository.UpdateAsync(entity);
    }

    private static CVSkill MapToEntity(CreateOrEditCVSkillDto input, CVSkill entity)
    {
        entity.ApplicantCVId = input.ApplicantCVId;
        entity.Skill = input.Skill;
        entity.Proficiency = input.Proficiency;

        return entity;
    }

    private static CVSkillDto ToDto(CVSkill entity)
    {
        return new CVSkillDto
        {
            Id = entity.Id,
            ApplicantCVId = entity.ApplicantCVId,
            Skill = entity.Skill,
            Proficiency = entity.Proficiency
        };
    }

    private static CreateOrEditCVSkillDto ToCreateOrEditDto(CVSkill entity)
    {
        return new CreateOrEditCVSkillDto
        {
            Id = entity.Id,
            ApplicantCVId = entity.ApplicantCVId,
            Skill = entity.Skill,
            Proficiency = entity.Proficiency
        };
    }
}
