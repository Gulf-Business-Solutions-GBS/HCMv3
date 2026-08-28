using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

/// <summary>
/// Report Distribution: CRUD over report schedules, plus manual triggering and run history.
/// </summary>
public interface IReportSchedulesAppService : IApplicationService
{
    Task<PagedResultDto<ReportScheduleListDto>> GetAll(GetAllReportSchedulesInput input);

    Task<GetReportScheduleForEditOutput> GetForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditReportScheduleDto input);

    Task Delete(EntityDto input);

    /// <summary>Duplicates a schedule, including its filters, recipients and destinations.</summary>
    Task<int> Duplicate(EntityDto input);

    /// <summary>Turns a schedule on or off without deleting it or losing its run history.</summary>
    Task SetActive(SetReportScheduleActiveInput input);

    /// <summary>Runs a schedule immediately rather than waiting for its next occurrence.</summary>
    Task<RunReportScheduleOutput> RunNow(EntityDto input);

    Task<PagedResultDto<ReportScheduleRunDto>> GetRuns(GetReportScheduleRunsInput input);

    /// <summary>Per-recipient breakdown of one run -- who got what, and whether their copy was scoped.</summary>
    Task<ListResultDto<ReportScheduleRunSliceDto>> GetRunSlices(EntityDto<long> input);
}

public class SetReportScheduleActiveInput : EntityDto
{
    public bool IsActive { get; set; }
}
