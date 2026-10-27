namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Input for <c>IEmployeeTrainingsAppService.GetTrainingCertifications</c>.
/// </summary>
public class GetEmployeeTrainingCertificationsInput
{
    /// <summary>The employee on the training record. Only this employee's certifications are offered.</summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// The certification already on the record being edited, if any. It is always returned, even if it is
    /// no longer of the Training type, so an existing link stays visible in the picker.
    /// </summary>
    public int? CurrentCertificationId { get; set; }
}
