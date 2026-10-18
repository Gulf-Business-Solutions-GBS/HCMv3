using Abp.Application.Services.Dto;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// One row of the Employee Training "Certification" picker: an <c>EmployeeCertification</c> of the
/// training's employee whose type is Training (legacy <c>EmployeeTraining.TrainingCertificationList</c>).
/// </summary>
public class EmployeeTrainingCertificationLookupDto : EntityDto<int>
{
    /// <summary>The certificate name, or the course when no certificate name was entered.</summary>
    public string DisplayName { get; set; }

    public string Course { get; set; }

    public string Certificate { get; set; }
}
