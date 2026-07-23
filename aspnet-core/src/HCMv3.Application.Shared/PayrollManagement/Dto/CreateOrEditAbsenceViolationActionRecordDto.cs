namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AbsenceViolationActionRecord"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditAbsenceViolationActionRecordDto
{
    public int? Id { get; set; }

    public ActionLogType ActionLogType { get; set; }

    public LogStatus LogStatus { get; set; }

    public int EntityObjectId { get; set; }
}
