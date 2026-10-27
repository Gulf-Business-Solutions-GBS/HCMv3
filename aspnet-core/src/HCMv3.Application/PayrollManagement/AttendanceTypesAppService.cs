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
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the Attendance Type master (ported from legacy XAF AttendanceType business
/// object). See <see cref="AttendanceType"/> class remarks for the WageTypeId FK simplification.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AttendanceTypes)]
public class AttendanceTypesAppService : HCMv3AppServiceBase, IAttendanceTypesAppService
{
    private readonly IRepository<AttendanceType> _attendanceTypeRepository;

    public AttendanceTypesAppService(IRepository<AttendanceType> attendanceTypeRepository)
    {
        _attendanceTypeRepository = attendanceTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AttendanceTypeDto>> GetAll(GetAllAttendanceTypesInput input)
    {
        input.Normalize();

        var query = _attendanceTypeRepository.GetAll()
            .Include(a => a.LinkLeaveQuota)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                a.Code.Contains(input.Filter) ||
                a.NameEn.Contains(input.Filter) ||
                a.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AttendanceTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAttendanceTypeForEditOutput> GetAttendanceTypeForEdit(EntityDto input)
    {
        var entity = await _attendanceTypeRepository.GetAsync(input.Id);

        return new GetAttendanceTypeForEditOutput
        {
            AttendanceType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAttendanceTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AttendanceTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _attendanceTypeRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AttendanceTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAttendanceTypeDto input)
    {
        var entity = MapToEntity(input, new AttendanceType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _attendanceTypeRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AttendanceTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAttendanceTypeDto input)
    {
        var entity = await _attendanceTypeRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _attendanceTypeRepository.UpdateAsync(entity);
    }

    private static AttendanceType MapToEntity(CreateOrEditAttendanceTypeDto input, AttendanceType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.IsPaid = input.IsPaid;
        entity.HaveQuotaAccrual = input.HaveQuotaAccrual;
        entity.IncludeWeekend = input.IncludeWeekend;
        entity.IncludeRestDay = input.IncludeRestDay;
        entity.IncludePublicHoliday = input.IncludePublicHoliday;
        entity.CalcOnCalendarDays = input.CalcOnCalendarDays;
        entity.IsFullDay = input.IsFullDay;
        entity.WageTypeId = input.WageTypeId;
        entity.AccrualPeriod = input.AccrualPeriod;
        entity.LinkLeaveQuotaId = input.LinkLeaveQuotaId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(AttendanceType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _attendanceTypeRepository.GetAll()
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
    /// Legacy <c>PerformValidation</c> parity: unique Code, unique NameEn/NameAr, and LinkLeaveQuotaId
    /// required when HaveQuotaAccrual is true.
    /// </summary>
    private async Task ValidateAsync(AttendanceType entity)
    {
        var duplicateCode = await _attendanceTypeRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id && a.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _attendanceTypeRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id &&
                           (a.NameEn == entity.NameEn || a.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.HaveQuotaAccrual && entity.LinkLeaveQuotaId == null)
        {
            throw new UserFriendlyException(L("LinkLeaveQuotaRequiredWhenQuotaAccrualEnabled"));
        }
    }

    private static AttendanceTypeDto ToDto(AttendanceType entity)
    {
        return new AttendanceTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsPaid = entity.IsPaid,
            HaveQuotaAccrual = entity.HaveQuotaAccrual,
            IncludeWeekend = entity.IncludeWeekend,
            IncludeRestDay = entity.IncludeRestDay,
            IncludePublicHoliday = entity.IncludePublicHoliday,
            CalcOnCalendarDays = entity.CalcOnCalendarDays,
            IsFullDay = entity.IsFullDay,
            WageTypeId = entity.WageTypeId,
            AccrualPeriod = entity.AccrualPeriod,
            LinkLeaveQuotaId = entity.LinkLeaveQuotaId,
            LinkLeaveQuotaName = entity.LinkLeaveQuota?.NameEn
        };
    }

    private static CreateOrEditAttendanceTypeDto ToCreateOrEditDto(AttendanceType entity)
    {
        return new CreateOrEditAttendanceTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsPaid = entity.IsPaid,
            HaveQuotaAccrual = entity.HaveQuotaAccrual,
            IncludeWeekend = entity.IncludeWeekend,
            IncludeRestDay = entity.IncludeRestDay,
            IncludePublicHoliday = entity.IncludePublicHoliday,
            CalcOnCalendarDays = entity.CalcOnCalendarDays,
            IsFullDay = entity.IsFullDay,
            WageTypeId = entity.WageTypeId,
            AccrualPeriod = entity.AccrualPeriod,
            LinkLeaveQuotaId = entity.LinkLeaveQuotaId
        };
    }
}
