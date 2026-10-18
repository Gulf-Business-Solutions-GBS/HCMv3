namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Kind of an <c>EmployeeCertification</c> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.CertificationType</c>, same values). Only
/// <see cref="Training"/> certifications can be linked from an <c>EmployeeTraining</c> record, as in legacy
/// (<c>EmployeeTraining.TrainingCertificationList</c>).
/// </summary>
public enum CertificationType
{
    Professional = 0,
    Training = 1
}
