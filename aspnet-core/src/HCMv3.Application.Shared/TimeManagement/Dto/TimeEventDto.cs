using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TimeEvent"/>.
/// </summary>
public class TimeEventDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public string PIN { get; set; }

    public int? ActualTransTypeId { get; set; }

    public string ActualTransTypeName { get; set; }

    public int TransTypeId { get; set; }

    public string TransTypeName { get; set; }

    public DateTime TransDate { get; set; }

    public DateTime TransTime { get; set; }

    public int? AccessUnitId { get; set; }

    public string AccessUnitName { get; set; }

    public string IPAddress { get; set; }

    public TransSource TransSource { get; set; }

    public long RecordSource { get; set; }

    public string DeviceAgentString { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double RangeDifference { get; set; }

    public bool PunchInRange { get; set; }

    public double Temperature { get; set; }

    public int? WorkLocationId { get; set; }

    public string WorkLocationName { get; set; }

    public string GeoSelfiePath { get; set; }
}
