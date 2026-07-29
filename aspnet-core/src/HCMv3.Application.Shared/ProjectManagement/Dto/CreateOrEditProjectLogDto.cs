using System;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ProjectLog"/>. <c>Id == null</c> means create; otherwise update.
/// No business validation (legacy has none beyond plain CRUD).
/// </summary>
public class CreateOrEditProjectLogDto
{
    public int? Id { get; set; }

    public string Message { get; set; }

    public DateTime RunTimeStamp { get; set; }

    /// <summary>Null for run-level rows (whole-run failure / post-JV run summary).</summary>
    public int? EmployeeId { get; set; }

    public int Status { get; set; }

    public DateTime InPeriod { get; set; }

    public bool StopFlag { get; set; }

    public int NoOfEmployee { get; set; }
}
