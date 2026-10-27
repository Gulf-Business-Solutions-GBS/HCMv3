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
/// Application service for <see cref="CVWorkExp"/>, an aggregated child of <see cref="ApplicantCV"/> (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.Recruitment.CVWorkExp</c>). Legacy had no
/// <c>PerformValidation</c> override, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_CVWorkExps)]
public class CVWorkExpsAppService : HCMv3AppServiceBase, ICVWorkExpsAppService
{
    private readonly IRepository<CVWorkExp> _cvWorkExpRepository;

    public CVWorkExpsAppService(IRepository<CVWorkExp> cvWorkExpRepository)
    {
        _cvWorkExpRepository = cvWorkExpRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CVWorkExpDto>> GetAll(GetAllCVWorkExpsInput input)
    {
        input.Normalize();

        var query = _cvWorkExpRepository.GetAll()
            .WhereIf(input.ApplicantCVId.HasValue, w => w.ApplicantCVId == input.ApplicantCVId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CVWorkExpDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCVWorkExpForEditOutput> GetCVWorkExpForEdit(EntityDto input)
    {
        var entity = await _cvWorkExpRepository.GetAsync(input.Id);

        return new GetCVWorkExpForEditOutput
        {
            CVWorkExp = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCVWorkExpDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVWorkExps_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _cvWorkExpRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVWorkExps_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCVWorkExpDto input)
    {
        var entity = MapToEntity(input, new CVWorkExp());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _cvWorkExpRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVWorkExps_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCVWorkExpDto input)
    {
        var entity = await _cvWorkExpRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _cvWorkExpRepository.UpdateAsync(entity);
    }

    private static CVWorkExp MapToEntity(CreateOrEditCVWorkExpDto input, CVWorkExp entity)
    {
        entity.ApplicantCVId = input.ApplicantCVId;
        entity.Company = input.Company;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.Position = input.Position;
        entity.Responsibilities = input.Responsibilities;

        return entity;
    }

    private static CVWorkExpDto ToDto(CVWorkExp entity)
    {
        return new CVWorkExpDto
        {
            Id = entity.Id,
            ApplicantCVId = entity.ApplicantCVId,
            Company = entity.Company,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Position = entity.Position,
            Responsibilities = entity.Responsibilities
        };
    }

    private static CreateOrEditCVWorkExpDto ToCreateOrEditDto(CVWorkExp entity)
    {
        return new CreateOrEditCVWorkExpDto
        {
            Id = entity.Id,
            ApplicantCVId = entity.ApplicantCVId,
            Company = entity.Company,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Position = entity.Position,
            Responsibilities = entity.Responsibilities
        };
    }
}
