using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeVisaInfo"/> (ported from legacy XAF
/// EmployeeVisaInfo business object). Legacy <c>PerformValidations</c> parity: blank checks and
/// EndDate &gt;= StartDate only (no overlap check in the legacy source).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeVisaInfos)]
public class EmployeeVisaInfosAppService : HCMv3AppServiceBase, IEmployeeVisaInfosAppService
{
    private readonly IRepository<EmployeeVisaInfo> _repository;

    public EmployeeVisaInfosAppService(IRepository<EmployeeVisaInfo> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeVisaInfoDto>> GetAll(GetAllEmployeeVisaInfosInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(v => v.Employee)
            .Include(v => v.PassportType)
            .WhereIf(input.EmployeeId.HasValue, v => v.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeVisaInfoDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeVisaInfoForEditOutput> GetEmployeeVisaInfoForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeVisaInfoForEditOutput
        {
            EmployeeVisaInfo = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeVisaInfoDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeVisaInfos_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeVisaInfoDto input)
    {
        var entity = MapToEntity(input, new EmployeeVisaInfo());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDates(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeVisaInfos_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeVisaInfoDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateDates(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeVisaInfos_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateDates(EmployeeVisaInfo entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeVisaInfo MapToEntity(CreateOrEditEmployeeVisaInfoDto input, EmployeeVisaInfo entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AuthorizedSign = input.AuthorizedSign;
        entity.CurrentFileNo = input.CurrentFileNo;
        entity.CurrentContractNo = input.CurrentContractNo;
        entity.CurrentLicenseNo = input.CurrentLicenseNo;
        entity.OldFileNo = input.OldFileNo;
        entity.OldContractNo = input.OldContractNo;
        entity.OldLicenseNo = input.OldLicenseNo;
        entity.MOIFileNumber = input.MOIFileNumber;
        entity.PreviousResidencyPeriod = input.PreviousResidencyPeriod;
        entity.CurrentResidencyPeriod = input.CurrentResidencyPeriod;
        entity.OldContractSalary = input.OldContractSalary;
        entity.CurrentContractSalary = input.CurrentContractSalary;
        entity.NewProfesion = input.NewProfesion;
        entity.NewProfessionAr = input.NewProfessionAr;
        entity.OldProfession = input.OldProfession;
        entity.OldCompany = input.OldCompany;
        entity.OldSponser = input.OldSponser;
        entity.TypeOfPassport = input.TypeOfPassport;
        entity.IssuePlacePassport = input.IssuePlacePassport;
        entity.PassportIssueDate = input.PassportIssueDate;
        entity.PassportExpiryDate = input.PassportExpiryDate;
        entity.EntryDate = input.EntryDate;
        entity.TerminationDate = input.TerminationDate;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.VisaNum = input.VisaNum;
        entity.ResidencyNum = input.ResidencyNum;
        entity.PassportTypeId = input.PassportTypeId;
        return entity;
    }

    private static EmployeeVisaInfoDto ToDto(EmployeeVisaInfo entity)
    {
        return new EmployeeVisaInfoDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AuthorizedSign = entity.AuthorizedSign,
            CurrentFileNo = entity.CurrentFileNo,
            CurrentContractNo = entity.CurrentContractNo,
            CurrentLicenseNo = entity.CurrentLicenseNo,
            OldFileNo = entity.OldFileNo,
            OldContractNo = entity.OldContractNo,
            OldLicenseNo = entity.OldLicenseNo,
            MOIFileNumber = entity.MOIFileNumber,
            PreviousResidencyPeriod = entity.PreviousResidencyPeriod,
            CurrentResidencyPeriod = entity.CurrentResidencyPeriod,
            OldContractSalary = entity.OldContractSalary,
            CurrentContractSalary = entity.CurrentContractSalary,
            NewProfesion = entity.NewProfesion,
            NewProfessionAr = entity.NewProfessionAr,
            OldProfession = entity.OldProfession,
            OldCompany = entity.OldCompany,
            OldSponser = entity.OldSponser,
            TypeOfPassport = entity.TypeOfPassport,
            IssuePlacePassport = entity.IssuePlacePassport,
            PassportIssueDate = entity.PassportIssueDate,
            PassportExpiryDate = entity.PassportExpiryDate,
            EntryDate = entity.EntryDate,
            TerminationDate = entity.TerminationDate,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            VisaNum = entity.VisaNum,
            ResidencyNum = entity.ResidencyNum,
            PassportTypeId = entity.PassportTypeId,
            PassportTypeName = entity.PassportType?.NameEn
        };
    }

    private static CreateOrEditEmployeeVisaInfoDto ToCreateOrEditDto(EmployeeVisaInfo entity)
    {
        return new CreateOrEditEmployeeVisaInfoDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            AuthorizedSign = entity.AuthorizedSign,
            CurrentFileNo = entity.CurrentFileNo,
            CurrentContractNo = entity.CurrentContractNo,
            CurrentLicenseNo = entity.CurrentLicenseNo,
            OldFileNo = entity.OldFileNo,
            OldContractNo = entity.OldContractNo,
            OldLicenseNo = entity.OldLicenseNo,
            MOIFileNumber = entity.MOIFileNumber,
            PreviousResidencyPeriod = entity.PreviousResidencyPeriod,
            CurrentResidencyPeriod = entity.CurrentResidencyPeriod,
            OldContractSalary = entity.OldContractSalary,
            CurrentContractSalary = entity.CurrentContractSalary,
            NewProfesion = entity.NewProfesion,
            NewProfessionAr = entity.NewProfessionAr,
            OldProfession = entity.OldProfession,
            OldCompany = entity.OldCompany,
            OldSponser = entity.OldSponser,
            TypeOfPassport = entity.TypeOfPassport,
            IssuePlacePassport = entity.IssuePlacePassport,
            PassportIssueDate = entity.PassportIssueDate,
            PassportExpiryDate = entity.PassportExpiryDate,
            EntryDate = entity.EntryDate,
            TerminationDate = entity.TerminationDate,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            VisaNum = entity.VisaNum,
            ResidencyNum = entity.ResidencyNum,
            PassportTypeId = entity.PassportTypeId
        };
    }
}
