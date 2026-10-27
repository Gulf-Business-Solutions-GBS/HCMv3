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
using HCMv3.SystemSettings.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSettings;

/// <summary>
/// Application service for <see cref="ManageReportSettings"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageReportSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- ported to <see cref="DeleteReportSettings"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_ReportSettings)]
public class ManageReportSettingsAppService : HCMv3AppServiceBase, IManageReportSettingsAppService
{
    private readonly IRepository<ManageReportSettings> _repository;

    public ManageReportSettingsAppService(IRepository<ManageReportSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<ReportSettingsListDto>> GetReportSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ReportSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<ReportSettingsEditDto> GetReportSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_ReportSettings_Create, AppPermissions.Pages_SystemSettings_ReportSettings_Edit)]
    public async Task CreateOrEditReportSettings(ReportSettingsEditDto input)
    {
        if (input.Id == 0)
        {
            await CreateAsync(input);
        }
        else
        {
            await UpdateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_ReportSettings_Delete)]
    public Task DeleteReportSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    private async Task CreateAsync(ReportSettingsEditDto input)
    {
        var entity = MapToEntity(input, new ManageReportSettings());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    private async Task UpdateAsync(ReportSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ManageReportSettings MapToEntity(ReportSettingsEditDto input, ManageReportSettings entity)
    {
        entity.ShowSubordinatesPayslip = input.ShowSubordinatesPayslip;
        entity.ExportGridReportWithHeader = input.ExportGridReportWithHeader;
        entity.ExportGridReportWithFooter = input.ExportGridReportWithFooter;
        entity.ShowPayslipOnDay = input.ShowPayslipOnDay;
        entity.ShowPayslipFromDate = input.ShowPayslipFromDate;
        entity.DecimalInReports = input.DecimalInReports;
        entity.ShowPeriodInPayrollSheet = input.ShowPeriodInPayrollSheet;
        entity.ShowPeriodInLeaveProvision = input.ShowPeriodInLeaveProvision;
        entity.ShowPeriodInIndemnityProvision = input.ShowPeriodInIndemnityProvision;
        entity.PeriodMonthLimitInPayrollSheet = input.PeriodMonthLimitInPayrollSheet;
        entity.PeriodMonthLimitInLeaveProvision = input.PeriodMonthLimitInLeaveProvision;
        entity.PeriodMonthLimitInIndemnityProvision = input.PeriodMonthLimitInIndemnityProvision;

        return entity;
    }

    private static ReportSettingsListDto ToListDto(ManageReportSettings entity)
    {
        return new ReportSettingsListDto
        {
            Id = entity.Id,
            ShowSubordinatesPayslip = entity.ShowSubordinatesPayslip,
            ExportGridReportWithHeader = entity.ExportGridReportWithHeader,
            ExportGridReportWithFooter = entity.ExportGridReportWithFooter,
            ShowPayslipOnDay = entity.ShowPayslipOnDay,
            ShowPayslipFromDate = entity.ShowPayslipFromDate,
            DecimalInReports = entity.DecimalInReports,
            ShowPeriodInPayrollSheet = entity.ShowPeriodInPayrollSheet,
            ShowPeriodInLeaveProvision = entity.ShowPeriodInLeaveProvision,
            ShowPeriodInIndemnityProvision = entity.ShowPeriodInIndemnityProvision,
            PeriodMonthLimitInPayrollSheet = entity.PeriodMonthLimitInPayrollSheet,
            PeriodMonthLimitInLeaveProvision = entity.PeriodMonthLimitInLeaveProvision,
            PeriodMonthLimitInIndemnityProvision = entity.PeriodMonthLimitInIndemnityProvision
        };
    }

    private static ReportSettingsEditDto ToEditDto(ManageReportSettings entity)
    {
        return new ReportSettingsEditDto
        {
            Id = entity.Id,
            ShowSubordinatesPayslip = entity.ShowSubordinatesPayslip,
            ExportGridReportWithHeader = entity.ExportGridReportWithHeader,
            ExportGridReportWithFooter = entity.ExportGridReportWithFooter,
            ShowPayslipOnDay = entity.ShowPayslipOnDay,
            ShowPayslipFromDate = entity.ShowPayslipFromDate,
            DecimalInReports = entity.DecimalInReports,
            ShowPeriodInPayrollSheet = entity.ShowPeriodInPayrollSheet,
            ShowPeriodInLeaveProvision = entity.ShowPeriodInLeaveProvision,
            ShowPeriodInIndemnityProvision = entity.ShowPeriodInIndemnityProvision,
            PeriodMonthLimitInPayrollSheet = entity.PeriodMonthLimitInPayrollSheet,
            PeriodMonthLimitInLeaveProvision = entity.PeriodMonthLimitInLeaveProvision,
            PeriodMonthLimitInIndemnityProvision = entity.PeriodMonthLimitInIndemnityProvision
        };
    }
}
