using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AttendanceType"/>.
/// </summary>
public class AttendanceTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public bool IsPaid { get; set; }

    public bool HaveQuotaAccrual { get; set; }

    public bool IncludeWeekend { get; set; }

    public bool IncludeRestDay { get; set; }

    public bool IncludePublicHoliday { get; set; }

    public bool CalcOnCalendarDays { get; set; }

    public bool IsFullDay { get; set; }

    public int? WageTypeId { get; set; }

    public AccrualPeriod AccrualPeriod { get; set; }

    public int? LinkLeaveQuotaId { get; set; }

    public string LinkLeaveQuotaName { get; set; }
}
