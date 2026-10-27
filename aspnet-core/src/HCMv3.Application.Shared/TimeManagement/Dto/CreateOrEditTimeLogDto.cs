using System;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TimeLog"/>. <c>Id == null</c> means create; otherwise update.
/// No validation attributes -- the legacy source has no validation at all for this entity (see
/// <see cref="TimeLog"/> remarks), faithfully preserved here.
/// </summary>
public class CreateOrEditTimeLogDto
{
    public int? Id { get; set; }

    public string Message { get; set; }

    public DateTime RunTimeStamp { get; set; }

    public int? EmployeeId { get; set; }

    public int Status { get; set; }

    public DateTime DayDate { get; set; }

    public bool StopFlag { get; set; }

    public int NoOfEmployee { get; set; }
}
