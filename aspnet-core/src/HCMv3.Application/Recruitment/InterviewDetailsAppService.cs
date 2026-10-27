using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="InterviewDetails"/>, an aggregated child of
/// <see cref="ApplicationStatus"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.InterviewDetails</c>). Legacy <c>PerformValidation</c> parity
/// (required InterviewDate/InterviewTime/Interviewer1) is enforced via data annotations on
/// <see cref="CreateOrEditInterviewDetailsDto"/> plus an explicit default-value check here (data annotations
/// do not catch <c>default(DateTime)</c>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_InterviewDetails)]
public class InterviewDetailsAppService : HCMv3AppServiceBase, IInterviewDetailsAppService
{
    private readonly IRepository<InterviewDetails> _interviewDetailsRepository;

    public InterviewDetailsAppService(IRepository<InterviewDetails> interviewDetailsRepository)
    {
        _interviewDetailsRepository = interviewDetailsRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<InterviewDetailsDto>> GetAll(GetAllInterviewDetailsInput input)
    {
        input.Normalize();

        var query = _interviewDetailsRepository.GetAll()
            .WhereIf(input.ApplicationStatusId.HasValue, i => i.ApplicationStatusId == input.ApplicationStatusId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<InterviewDetailsDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetInterviewDetailsForEditOutput> GetInterviewDetailsForEdit(EntityDto input)
    {
        var entity = await _interviewDetailsRepository.GetAsync(input.Id);

        return new GetInterviewDetailsForEditOutput
        {
            InterviewDetails = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditInterviewDetailsDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_InterviewDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _interviewDetailsRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_InterviewDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditInterviewDetailsDto input)
    {
        ValidateDates(input);

        var entity = MapToEntity(input, new InterviewDetails());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _interviewDetailsRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_InterviewDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditInterviewDetailsDto input)
    {
        ValidateDates(input);

        var entity = await _interviewDetailsRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _interviewDetailsRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: InterviewDate/InterviewTime cannot be
    /// <c>default(DateTime)</c>.
    /// </summary>
    private void ValidateDates(CreateOrEditInterviewDetailsDto input)
    {
        if (input.InterviewDate == default)
        {
            throw new UserFriendlyException(L("Recruitment_InterviewDateCannotBeBlank"));
        }

        if (input.InterviewTime == default)
        {
            throw new UserFriendlyException(L("Recruitment_InterviewTimeCannotBeBlank"));
        }
    }

    private static InterviewDetails MapToEntity(CreateOrEditInterviewDetailsDto input, InterviewDetails entity)
    {
        entity.ApplicationStatusId = input.ApplicationStatusId;
        entity.InterviewDate = input.InterviewDate;
        entity.InterviewTime = input.InterviewTime;
        entity.Place = input.Place;
        entity.Interviewer1Id = input.Interviewer1Id;
        entity.Interviewer2Id = input.Interviewer2Id;
        entity.Interviewer3Id = input.Interviewer3Id;
        entity.Comments1 = input.Comments1;
        entity.Comments2 = input.Comments2;
        entity.Comments3 = input.Comments3;

        return entity;
    }

    private static InterviewDetailsDto ToDto(InterviewDetails entity)
    {
        return new InterviewDetailsDto
        {
            Id = entity.Id,
            ApplicationStatusId = entity.ApplicationStatusId,
            InterviewDate = entity.InterviewDate,
            InterviewTime = entity.InterviewTime,
            Place = entity.Place,
            Interviewer1Id = entity.Interviewer1Id,
            Interviewer2Id = entity.Interviewer2Id,
            Interviewer3Id = entity.Interviewer3Id,
            Comments1 = entity.Comments1,
            Comments2 = entity.Comments2,
            Comments3 = entity.Comments3
        };
    }

    private static CreateOrEditInterviewDetailsDto ToCreateOrEditDto(InterviewDetails entity)
    {
        return new CreateOrEditInterviewDetailsDto
        {
            Id = entity.Id,
            ApplicationStatusId = entity.ApplicationStatusId,
            InterviewDate = entity.InterviewDate,
            InterviewTime = entity.InterviewTime,
            Place = entity.Place,
            Interviewer1Id = entity.Interviewer1Id ?? 0,
            Interviewer2Id = entity.Interviewer2Id,
            Interviewer3Id = entity.Interviewer3Id,
            Comments1 = entity.Comments1,
            Comments2 = entity.Comments2,
            Comments3 = entity.Comments3
        };
    }
}
