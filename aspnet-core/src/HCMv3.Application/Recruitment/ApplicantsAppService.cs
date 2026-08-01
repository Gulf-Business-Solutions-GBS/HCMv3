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
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="Applicant"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.Applicant</c>). Legacy <c>PerformValidation</c> parity (auto
/// Code, duplicate Code/Email checks, name/date rules) is ported here, out of the persistence lifecycle.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_Applicants)]
public class ApplicantsAppService : HCMv3AppServiceBase, IApplicantsAppService
{
    private readonly IRepository<Applicant> _applicantRepository;

    public ApplicantsAppService(IRepository<Applicant> applicantRepository)
    {
        _applicantRepository = applicantRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ApplicantDto>> GetAll(GetAllApplicantsInput input)
    {
        input.Normalize();

        var query = _applicantRepository.GetAll()
            .Include(a => a.Nationality)
            .Include(a => a.JobTitle)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                a.Code.Contains(input.Filter) ||
                a.FirstNameEn.Contains(input.Filter) ||
                a.LastNameEn.Contains(input.Filter) ||
                a.Email.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ApplicantDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetApplicantForEditOutput> GetApplicantForEdit(EntityDto input)
    {
        var entity = await _applicantRepository.GetAsync(input.Id);

        return new GetApplicantForEditOutput
        {
            Applicant = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditApplicantDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_Applicants_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _applicantRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_Applicants_Create)]
    protected virtual async Task CreateAsync(CreateOrEditApplicantDto input)
    {
        var entity = MapToEntity(input, new Applicant());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _applicantRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_Applicants_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditApplicantDto input)
    {
        var entity = await _applicantRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _applicantRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Applicant entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _applicantRepository.GetAll()
            .Where(a => a.Code != null && a.Code != "")
            .Select(a => a.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code, unique Email, and Birth/Joining/Probation date
    /// ordering rules.
    /// </summary>
    private async Task ValidateAsync(Applicant entity)
    {
        var duplicateCode = await _applicantRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id && a.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        if (entity.BirthDate == default || entity.BirthDate.Date >= DateTime.Now.Date)
        {
            throw new UserFriendlyException(L("Recruitment_BirthDateCannotBeFutureOrToday"));
        }

        if (entity.JoiningDate != default && entity.JoiningDate.Date <= entity.BirthDate.Date)
        {
            throw new UserFriendlyException(L("Recruitment_JoiningDateCannotBeBeforeBirthDate"));
        }

        if (entity.ProbationEndDate != default && entity.ProbationEndDate.Date <= entity.JoiningDate.Date)
        {
            throw new UserFriendlyException(L("Recruitment_ProbationEndDateCannotBeBeforeJoiningDate"));
        }

        if (!entity.Email.IsNullOrWhiteSpace())
        {
            var duplicateEmail = await _applicantRepository.GetAll()
                .AnyAsync(a => a.Id != entity.Id && a.Email == entity.Email);
            if (duplicateEmail)
            {
                throw new UserFriendlyException(L("Recruitment_DuplicateEmailFound"));
            }
        }
    }

    private static Applicant MapToEntity(CreateOrEditApplicantDto input, Applicant entity)
    {
        entity.Code = input.Code;
        entity.TitleId = input.TitleId;
        // entity.UserId is deliberately NOT mapped from this DTO: the applicant<->ABP user link is owned by
        // the careers registration flow (CareersAccountAppService), never by the recruiter-facing editor.
        entity.FirstNameEn = input.FirstNameEn;
        entity.FirstNameAr = input.FirstNameAr;
        entity.SecondNameEn = input.SecondNameEn;
        entity.SecondNameAr = input.SecondNameAr;
        entity.ThirdNameEn = input.ThirdNameEn;
        entity.ThirdNameAr = input.ThirdNameAr;
        entity.LastNameEn = input.LastNameEn;
        entity.LastNameAr = input.LastNameAr;
        entity.NationalityId = input.NationalityId;
        entity.JobTitleId = input.JobTitleId;
        entity.Gender = input.Gender;
        entity.MaritalStatusId = input.MaritalStatusId;
        entity.BirthDate = input.BirthDate;
        entity.JoiningDate = input.JoiningDate;
        entity.ProbationEndDate = input.ProbationEndDate;
        entity.Photo = input.Photo;
        entity.Telephone = input.Telephone;
        entity.Mobile = input.Mobile;
        entity.CurrentAddress = input.CurrentAddress;
        entity.PermanentAddress = input.PermanentAddress;
        entity.DrivingLicense = input.DrivingLicense;
        entity.ReligionId = input.ReligionId;
        entity.CountryId = input.CountryId;
        entity.QualificationId = input.QualificationId;
        entity.BloodGroup = input.BloodGroup;
        entity.CivilID = input.CivilID;
        entity.Email = input.Email;
        entity.PassportNum = input.PassportNum;
        entity.PlaceOfBirth = input.PlaceOfBirth;
        entity.Address1 = input.Address1;
        entity.Address2 = input.Address2;
        entity.Comments = input.Comments;
        entity.CompletionPoints = input.CompletionPoints;
        entity.IsOnbdStarted = input.IsOnbdStarted;
        entity.IsAgency = input.IsAgency;
        entity.IsOnbdInit = input.IsOnbdInit;
        entity.OnbCoordinatorId = input.OnbCoordinatorId;
        entity.OnbBuddyId = input.OnbBuddyId;

        return entity;
    }

    private static ApplicantDto ToDto(Applicant entity)
    {
        return new ApplicantDto
        {
            Id = entity.Id,
            Code = entity.Code,
            TitleId = entity.TitleId,
            FirstNameEn = entity.FirstNameEn,
            FirstNameAr = entity.FirstNameAr,
            SecondNameEn = entity.SecondNameEn,
            SecondNameAr = entity.SecondNameAr,
            ThirdNameEn = entity.ThirdNameEn,
            ThirdNameAr = entity.ThirdNameAr,
            LastNameEn = entity.LastNameEn,
            LastNameAr = entity.LastNameAr,
            FullNameEn = $"{entity.FirstNameEn} {entity.SecondNameEn} {entity.ThirdNameEn} {entity.LastNameEn}",
            FullNameAr = $"{entity.FirstNameAr} {entity.SecondNameAr} {entity.ThirdNameAr} {entity.LastNameAr}",
            NationalityId = entity.NationalityId,
            NationalityNameEn = entity.Nationality?.NameEn,
            JobTitleId = entity.JobTitleId,
            JobTitleNameEn = entity.JobTitle?.NameEn,
            Gender = entity.Gender,
            MaritalStatusId = entity.MaritalStatusId,
            BirthDate = entity.BirthDate,
            JoiningDate = entity.JoiningDate,
            ProbationEndDate = entity.ProbationEndDate,
            Telephone = entity.Telephone,
            Mobile = entity.Mobile,
            CurrentAddress = entity.CurrentAddress,
            PermanentAddress = entity.PermanentAddress,
            DrivingLicense = entity.DrivingLicense,
            ReligionId = entity.ReligionId,
            CountryId = entity.CountryId,
            QualificationId = entity.QualificationId,
            BloodGroup = entity.BloodGroup,
            CivilID = entity.CivilID,
            Email = entity.Email,
            PassportNum = entity.PassportNum,
            PlaceOfBirth = entity.PlaceOfBirth,
            Address1 = entity.Address1,
            Address2 = entity.Address2,
            Comments = entity.Comments,
            CompletionPoints = entity.CompletionPoints,
            IsOnbdStarted = entity.IsOnbdStarted,
            IsAgency = entity.IsAgency,
            IsOnbdInit = entity.IsOnbdInit,
            OnbCoordinatorId = entity.OnbCoordinatorId,
            OnbBuddyId = entity.OnbBuddyId
        };
    }

    private static CreateOrEditApplicantDto ToCreateOrEditDto(Applicant entity)
    {
        return new CreateOrEditApplicantDto
        {
            Id = entity.Id,
            Code = entity.Code,
            TitleId = entity.TitleId,
            FirstNameEn = entity.FirstNameEn,
            FirstNameAr = entity.FirstNameAr,
            SecondNameEn = entity.SecondNameEn,
            SecondNameAr = entity.SecondNameAr,
            ThirdNameEn = entity.ThirdNameEn,
            ThirdNameAr = entity.ThirdNameAr,
            LastNameEn = entity.LastNameEn,
            LastNameAr = entity.LastNameAr,
            NationalityId = entity.NationalityId ?? 0,
            JobTitleId = entity.JobTitleId,
            Gender = entity.Gender,
            MaritalStatusId = entity.MaritalStatusId,
            BirthDate = entity.BirthDate,
            JoiningDate = entity.JoiningDate,
            ProbationEndDate = entity.ProbationEndDate,
            Photo = entity.Photo,
            Telephone = entity.Telephone,
            Mobile = entity.Mobile,
            CurrentAddress = entity.CurrentAddress,
            PermanentAddress = entity.PermanentAddress,
            DrivingLicense = entity.DrivingLicense,
            ReligionId = entity.ReligionId,
            CountryId = entity.CountryId,
            QualificationId = entity.QualificationId,
            BloodGroup = entity.BloodGroup,
            CivilID = entity.CivilID,
            Email = entity.Email,
            PassportNum = entity.PassportNum,
            PlaceOfBirth = entity.PlaceOfBirth,
            Address1 = entity.Address1,
            Address2 = entity.Address2,
            Comments = entity.Comments,
            CompletionPoints = entity.CompletionPoints,
            IsOnbdStarted = entity.IsOnbdStarted,
            IsAgency = entity.IsAgency,
            IsOnbdInit = entity.IsOnbdInit,
            OnbCoordinatorId = entity.OnbCoordinatorId,
            OnbBuddyId = entity.OnbBuddyId
        };
    }
}
