namespace HCMv3.TrainingManagement;

/// <summary>
/// Delivery method for a <c>TrainingCourse</c> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.DeliveryMethod</c>). Renamed from the legacy bare
/// <c>DeliveryMethod</c> to avoid any future name collision with an unrelated "delivery method" concept in
/// another module.
/// </summary>
public enum TrainingDeliveryMethod
{
    Online = 0,
    Classroom = 1
}
