using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TimeEvent"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditTimeEventDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [StringLength(50)]
    public string PIN { get; set; }

    public int? ActualTransTypeId { get; set; }

    [Required]
    public int TransTypeId { get; set; }

    [Required]
    public DateTime TransDate { get; set; }

    public DateTime TransTime { get; set; }

    public int? AccessUnitId { get; set; }

    [StringLength(50)]
    public string IPAddress { get; set; }

    public TransSource TransSource { get; set; }

    public long RecordSource { get; set; }

    [StringLength(1000)]
    public string DeviceAgentString { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double RangeDifference { get; set; }

    public bool PunchInRange { get; set; }

    public double Temperature { get; set; }

    public int? WorkLocationId { get; set; }

    [StringLength(1000)]
    public string GeoSelfiePath { get; set; }
}
